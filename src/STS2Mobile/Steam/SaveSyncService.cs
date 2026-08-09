using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// The sole save synchronization policy and transfer boundary. Every future
// automatic or manual caller invokes the same manifest-based SyncAsync method.
internal sealed class SaveSyncService
{
    private const int MaxSaveFiles = 1000;
    private const int MaxDirectoryDepth = 8;
    internal const string StateFileName = "sync-state.json";

    internal enum SyncOutcome
    {
        NoChange,
        Pull,
        Push,
        Conflict,
    }

    internal enum SyncRequest
    {
        Reconcile,
        Pull,
        Push,
    }

    internal enum SyncPrompt
    {
        None,
        ChooseSource,
        ConfirmOverwrite,
    }

    internal sealed record ManifestEntry
    {
        [JsonPropertyName("path")]
        public string Path { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("contentHash")]
        public string ContentHash { get; init; } = string.Empty;
    }

    internal sealed record SaveManifest
    {
        [JsonPropertyName("files")]
        public List<ManifestEntry> Files { get; init; } = [];

        internal bool IsEmpty => Files.Count == 0;
    }

    internal sealed record SyncState
    {
        [JsonPropertyName("lastLocalManifest")]
        public SaveManifest LastLocalManifest { get; init; }

        [JsonPropertyName("lastRemoteManifest")]
        public SaveManifest LastRemoteManifest { get; init; }

        [JsonPropertyName("localChangesWaitingToUpload")]
        public bool LocalChangesWaitingToUpload { get; init; }

        [JsonPropertyName("interruptedTransferMustBeRetried")]
        public bool InterruptedTransferMustBeRetried { get; init; }

    }

    internal readonly record struct SyncDecision(
        SyncOutcome Outcome,
        SyncPrompt Prompt
    );

    internal readonly record struct SyncResult(
        SyncOutcome Outcome,
        bool Success,
        bool Canceled,
        SyncPrompt Prompt,
        int FilesTransferred,
        string Message
    );

    internal readonly record struct StatusSnapshot(
        bool HasCredentials,
        bool HasSuccessfulSync,
        DateTimeOffset? LastSuccessfulSyncUtc,
        bool ChangesQueued,
        bool RetryRequired
    );

    private sealed class CapturedFile
    {
        internal CapturedFile(
            ManifestEntry entry,
            byte[] bytes,
            SteamCloudTransport.RemoteFile remote
        )
        {
            Entry = entry;
            Bytes = bytes;
            Remote = remote;
        }

        internal ManifestEntry Entry { get; }
        internal byte[] Bytes { get; set; }
        internal SteamCloudTransport.RemoteFile Remote { get; }
    }

    private sealed class CapturedSaveSet
    {
        internal CapturedSaveSet(
            SaveManifest manifest,
            Dictionary<string, CapturedFile> files
        )
        {
            Manifest = manifest;
            Files = files;
        }

        internal SaveManifest Manifest { get; }
        internal Dictionary<string, CapturedFile> Files { get; }
    }

    private sealed class LocalSnapshotChangedException(string message)
        : Exception(message);

    private static readonly object ActiveLock = new();
    private static SaveSyncService _active;

    private readonly AndroidLocalSaveStore _local;
    private readonly ISaveStore _localStore;
    private string _statePath;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly SemaphoreSlim _stateFileLock = new(1, 1);
    private readonly object _stateLock = new();
    private readonly object _credentialLock = new();
    private readonly Func<ISaveRemote> _transportFactory;
    private ISaveRemote _transport;
    private string _accountName;
    private string _refreshToken;
    private string _pendingAccountName;
    private string _pendingRefreshToken;
    private bool _credentialUpdatePending;
    private SyncState _state;
    private long _localChangeVersion;
    private int _automaticPushAllowed;
    private readonly object _automaticPushLock = new();
    private Task _automaticPushWorker = Task.CompletedTask;
    private bool _automaticPushRunning;
    private bool _automaticPushPending;
    private DateTimeOffset? _lastSuccessfulSyncUtc;

    private SaveSyncService(
        AndroidLocalSaveStore local,
        string accountName,
        string refreshToken,
        Func<ISaveRemote> transportFactory = null
    )
    {
        _local = local;
        _localStore = local;
        _accountName = accountName ?? string.Empty;
        _refreshToken = refreshToken ?? string.Empty;
        _transportFactory = transportFactory;
        _statePath = StatePath(local.RootPath, _accountName);
        _state = LoadState();
    }

    internal SaveSyncService(
        AndroidLocalSaveStore local,
        ISaveRemote transport
    )
        : this(
            local,
            "contract-account",
            "contract-token",
            () => transport
        )
    {
    }

    private bool HasCredentials
        => !string.IsNullOrWhiteSpace(_accountName)
            && !string.IsNullOrWhiteSpace(_refreshToken);

    internal bool AutomaticPushAllowed
        => Volatile.Read(ref _automaticPushAllowed) == 1;

    internal void PauseAutomaticPush()
        => Volatile.Write(ref _automaticPushAllowed, 0);

    internal static void Configure(SteamCredentialStore credentialStore)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(credentialStore);
            var accountName = string.Empty;
            var refreshToken = string.Empty;
            credentialStore.TryUseCredentials((account, token) =>
            {
                accountName = account;
                refreshToken = token;
            });

            SaveSyncService service;
            lock (ActiveLock)
            {
                _active ??= new SaveSyncService(
                    new AndroidLocalSaveStore(),
                    accountName,
                    refreshToken
                );
                service = _active;
            }

            service.TryUpdateCredentials(accountName, refreshToken);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Optional sync configuration failed: {ex.GetType().Name}"
            );
        }
    }

    internal static bool TryGetActive(out SaveSyncService service)
    {
        lock (ActiveLock)
        {
            service = _active;
            return service?.HasCredentials == true;
        }
    }

    internal static StatusSnapshot GetStatusSnapshot()
    {
        SaveSyncService service;
        lock (ActiveLock)
            service = _active;

        return service?.ReadStatusSnapshot() ?? default;
    }

    internal static void NotifyGameplayMutationCommitted(string path)
    {
        try
        {
            SaveSyncService service;
            lock (ActiveLock)
                service = _active;

            service?.NotifyLocalMutationCommitted(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Local save notification failed: {ex.GetType().Name}"
            );
        }
    }

    internal bool HasPendingLocalChanges()
    {
        lock (_stateLock)
            return _state.LocalChangesWaitingToUpload;
    }

    private StatusSnapshot ReadStatusSnapshot()
    {
        lock (_stateLock)
        {
            return new StatusSnapshot(
                HasCredentials,
                _state.LastLocalManifest != null,
                _lastSuccessfulSyncUtc,
                _state.LocalChangesWaitingToUpload,
                _state.InterruptedTransferMustBeRetried
            );
        }
    }

    internal bool MarkLocalChangesPending()
    {
        Interlocked.Increment(ref _localChangeVersion);
        try
        {
            UpdateStateAsync(
                state => state.InterruptedTransferMustBeRetried
                        && !state.LocalChangesWaitingToUpload
                    ? state with
                    {
                        // A gameplay write invalidates an interrupted Pull. Drop
                        // that retry direction so the next pass reports a conflict
                        // instead of pushing a partially pulled tree to Steam.
                        LocalChangesWaitingToUpload = true,
                        InterruptedTransferMustBeRetried = false,
                    }
                    : state with { LocalChangesWaitingToUpload = true },
                CancellationToken.None
            ).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Could not persist pending local changes: {ex.GetType().Name}"
            );
            return false;
        }
    }

    internal void NotifyLocalMutationCommitted(string path)
    {
        if (!MarkLocalChangesPending())
            return;

        if (!QueueAutomaticPush(path))
        {
            PatchHelper.Log(
                $"[Cloud] Local commit marked dirty; automatic Push paused: {path}"
            );
        }
    }

    private bool QueueAutomaticPush(string committedPath = null)
    {
        lock (_automaticPushLock)
        {
            if (_automaticPushRunning)
            {
                _automaticPushPending = true;
                LogQueuedCommit(committedPath);
                return true;
            }

            if (!AutomaticPushAllowed || !HasCredentials)
                return false;

            _automaticPushRunning = true;
            _automaticPushPending = false;
            LogQueuedCommit(committedPath);
            _automaticPushWorker = Task.Run(RunAutomaticPushWorkerAsync);
            return true;
        }
    }

    private static void LogQueuedCommit(string committedPath)
    {
        if (!string.IsNullOrWhiteSpace(committedPath))
        {
            PatchHelper.Log(
                $"[Cloud] Local commit -> Push queued: {committedPath}"
            );
        }
    }

    private async Task RunAutomaticPushWorkerAsync()
    {
        var ownershipReleased = false;
        try
        {
            while (true)
            {
                lock (_automaticPushLock)
                    _automaticPushPending = false;

                if (!AutomaticPushAllowed)
                    break;

                PatchHelper.Log("[Cloud] Automatic Push started");
                var result = await SyncAsync(
                    SyncRequest.Reconcile,
                    overwriteConfirmed: false,
                    localWritesAreStopped: false,
                    cancellationToken: CancellationToken.None
                ).ConfigureAwait(false);

                if (result.Success && result.Outcome == SyncOutcome.Push)
                {
                    PatchHelper.Log(
                        $"[Cloud] Automatic Push verified: {result.FilesTransferred} file changes"
                    );
                }
                else if (result.Success)
                {
                    PatchHelper.Log(
                        $"[Cloud] Automatic Push reconciled: {result.Outcome}"
                    );
                }
                else if (AutomaticPushAllowed)
                {
                    PatchHelper.Log(
                        "[Cloud] Automatic Push snapshot changed; coalesced retry pending"
                    );
                }
                else
                {
                    PatchHelper.Log(
                        $"[Cloud] Automatic Push stopped; dirty state retained: {result.Message}"
                    );
                }

                bool retry;
                lock (_automaticPushLock)
                {
                    retry = AutomaticPushAllowed
                        && (
                            _automaticPushPending
                            || HasPendingLocalChanges()
                        );
                    if (retry)
                        _automaticPushPending = false;
                    else
                    {
                        ownershipReleased = true;
                        _automaticPushRunning = false;
                    }
                }

                if (!retry)
                    return;

                PatchHelper.Log(
                    "[Cloud] Automatic Push coalesced retry started"
                );
            }
        }
        catch (Exception ex)
        {
            PauseAutomaticPush();
            PatchHelper.Log(
                $"[Cloud] Automatic Push worker failed; dirty state retained: {ex.GetType().Name}"
            );
        }
        finally
        {
            if (!ownershipReleased)
            {
                lock (_automaticPushLock)
                {
                    _automaticPushPending = false;
                    _automaticPushRunning = false;
                }
            }
        }
    }

    internal bool FlushAutomaticPush(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            Task worker;
            lock (_automaticPushLock)
            {
                if (!_automaticPushRunning)
                    return true;
                worker = _automaticPushWorker;
            }

            var remaining = timeout - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
                return false;

            AndroidBridgeDispatcher.Pump();
            var slice = remaining < TimeSpan.FromMilliseconds(10)
                ? remaining
                : TimeSpan.FromMilliseconds(10);
            try
            {
                worker.Wait(slice);
            }
            catch (AggregateException ex)
            {
                PatchHelper.Log(
                    $"[Cloud] Automatic Push flush failed: {ex.GetBaseException().GetType().Name}"
                );
                return false;
            }
        }
    }

    internal static bool RequestFinalPushAndFlush(TimeSpan timeout)
    {
        if (!TryGetActive(out var service))
            return true;
        if (!service.MarkLocalChangesPending())
            return false;

        if (!service.QueueAutomaticPush())
            return false;
        return service.FlushAutomaticPush(timeout)
            && !service.HasPendingLocalChanges();
    }

    internal async Task<SyncResult> SyncAsync(
        SyncRequest request,
        bool overwriteConfirmed,
        bool localWritesAreStopped,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Canceled();
        }

        try
        {
            ApplyPendingCredentials();
            var automaticPushWasAllowed = AutomaticPushAllowed;
            Volatile.Write(ref _automaticPushAllowed, 0);
            if (!HasCredentials)
                return Failed("Steam credentials are unavailable");

            var transport = GetTransport();
            var remoteFiles = await transport.EnumerateAsync(cancellationToken)
                .ConfigureAwait(false);
            var local = CaptureStableLocalSet(out var capturedLocalVersion);
            var remote = await CaptureRemoteSetAsync(
                transport,
                remoteFiles,
                cancellationToken
            ).ConfigureAwait(false);
            var state = CurrentState();
            var decision = Decide(
                state,
                local.Manifest,
                remote.Manifest,
                request,
                overwriteConfirmed
            );

            if (decision.Outcome == SyncOutcome.Conflict)
            {
                var localChanged = state.LastLocalManifest == null
                    ? !local.Manifest.IsEmpty
                    : !ManifestsEqual(
                        local.Manifest,
                        state.LastLocalManifest
                    );
                if (!state.InterruptedTransferMustBeRetried)
                {
                    await UpdateStateAsync(
                        current => current with
                        {
                            LocalChangesWaitingToUpload =
                                current.LocalChangesWaitingToUpload || localChanged,
                        },
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                return new SyncResult(
                    SyncOutcome.Conflict,
                    false,
                    false,
                    decision.Prompt,
                    0,
                    "Android and Steam saves changed differently; neither side was overwritten"
                );
            }

            if (decision.Prompt != SyncPrompt.None)
            {
                Volatile.Write(
                    ref _automaticPushAllowed,
                    decision.Prompt == SyncPrompt.ChooseSource
                        ? 0
                        : automaticPushWasAllowed ? 1 : 0
                );
                return new SyncResult(
                    decision.Outcome,
                    false,
                    false,
                    decision.Prompt,
                    0,
                    decision.Outcome == SyncOutcome.Pull
                        ? "Pull will overwrite differing saves on this device"
                        : "Push will overwrite differing saves on Steam"
                );
            }

            if (decision.Outcome == SyncOutcome.NoChange)
            {
                if (
                    state.LastLocalManifest != null
                    && !state.LocalChangesWaitingToUpload
                    && !state.InterruptedTransferMustBeRetried
                    && ManifestsEqual(local.Manifest, state.LastLocalManifest)
                    && ManifestsEqual(remote.Manifest, state.LastRemoteManifest)
                    && Interlocked.Read(ref _localChangeVersion)
                        == capturedLocalVersion
                )
                {
                    await WriteCompletedStateAsync(
                        local.Manifest,
                        capturedLocalVersion,
                        cancellationToken
                    ).ConfigureAwait(false);
                    return Completed(
                        SyncOutcome.NoChange,
                        0,
                        "Save manifests are unchanged"
                    );
                }

                await WriteCompletedStateAsync(
                    local.Manifest,
                    capturedLocalVersion,
                    cancellationToken
                ).ConfigureAwait(false);
                return Completed(SyncOutcome.NoChange, 0, "Save manifests already match");
            }

            // Pull is a pre-game/launcher operation. It is never allowed while
            // gameplay can write the same application-local files.
            if (decision.Outcome == SyncOutcome.Pull && !localWritesAreStopped)
                return new SyncResult(
                    SyncOutcome.Pull,
                    false,
                    false,
                    SyncPrompt.None,
                    0,
                    "Pull requires gameplay save writes to be stopped"
                );

            if (
                decision.Outcome == SyncOutcome.Push
                && !localWritesAreStopped
                && !automaticPushWasAllowed
            )
                return new SyncResult(
                    SyncOutcome.Push,
                    false,
                    false,
                    SyncPrompt.None,
                    0,
                    "Automatic Push is paused until Steam state is reconciled"
                );

            if (Interlocked.Read(ref _localChangeVersion) != capturedLocalVersion)
                throw new LocalSnapshotChangedException(
                    "Android saves changed before synchronization could start"
                );

            var interruptedState = state with
            {
                LocalChangesWaitingToUpload =
                    decision.Outcome == SyncOutcome.Push,
                InterruptedTransferMustBeRetried = true,
            };

            if (decision.Outcome == SyncOutcome.Pull)
            {
                var pulled = await PullSteamToLocalTransactionAsync(
                    transport,
                    remote,
                    local,
                    state,
                    interruptedState,
                    capturedLocalVersion,
                    cancellationToken
                ).ConfigureAwait(false);
                return Completed(
                    SyncOutcome.Pull,
                    pulled,
                    $"Pull completed: {pulled} file changes"
                );
            }

            await WriteStateAsync(
                interruptedState,
                cancellationToken
            ).ConfigureAwait(false);

            var transferred = await MirrorLocalToSteamAsync(
                transport,
                local,
                remote,
                cancellationToken
            ).ConfigureAwait(false);

            await WriteCompletedStateAsync(
                local.Manifest,
                capturedLocalVersion,
                cancellationToken
            ).ConfigureAwait(false);
            return Completed(
                decision.Outcome,
                transferred,
                $"{decision.Outcome} completed: {transferred} file changes"
            );
        }
        catch (LocalSnapshotChangedException ex)
        {
            if (!localWritesAreStopped)
                return RetryAfterLocalSnapshotChange(ex.Message);
            return Failed(ex.Message);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            ResetTransport();
            return Canceled();
        }
        catch (Exception ex)
        {
            ResetTransport();
            PatchHelper.Log(
                $"[Cloud] Save synchronization failed: {ex.GetType().Name}"
            );
            return Failed(ex.GetType().Name);
        }
        finally
        {
            ApplyPendingCredentials();
            _syncLock.Release();
        }
    }

    internal static SyncDecision Decide(
        SyncState state,
        SaveManifest local,
        SaveManifest remote,
        SyncRequest request,
        bool overwriteConfirmed
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(remote);
        ValidateBaselinePair(state);

        if (ManifestsEqual(local, remote))
            return new SyncDecision(SyncOutcome.NoChange, SyncPrompt.None);

        var reconciled = DecideReconcile(state, local, remote);
        if (request == SyncRequest.Reconcile)
            return reconciled;

        var requestedOutcome = request == SyncRequest.Pull
            ? SyncOutcome.Pull
            : SyncOutcome.Push;
        var source = requestedOutcome == SyncOutcome.Pull ? remote : local;
        var destination = requestedOutcome == SyncOutcome.Pull ? local : remote;
        var prompt = overwriteConfirmed
            ? SyncPrompt.None
            : reconciled.Outcome == SyncOutcome.Conflict
                ? SyncPrompt.ChooseSource
                : WouldOverwriteDestination(source, destination)
                    ? SyncPrompt.ConfirmOverwrite
                    : SyncPrompt.None;
        return new SyncDecision(
            requestedOutcome,
            prompt
        );
    }

    private static SyncDecision DecideReconcile(
        SyncState state,
        SaveManifest local,
        SaveManifest remote
    )
    {
        var conflict = new SyncDecision(
            SyncOutcome.Conflict,
            SyncPrompt.ChooseSource
        );

        var hasBaseline = state.LastLocalManifest != null;
        if (state.InterruptedTransferMustBeRetried)
        {
            if (!hasBaseline)
            {
                var retryOutcome = state.LocalChangesWaitingToUpload
                    ? SyncOutcome.Push
                    : SyncOutcome.Pull;
                var source = retryOutcome == SyncOutcome.Push ? local : remote;
                var destination = retryOutcome == SyncOutcome.Push ? remote : local;
                if (IsManifestSubset(destination, source))
                    return new SyncDecision(retryOutcome, SyncPrompt.None);

                return conflict;
            }

            if (state.LocalChangesWaitingToUpload)
                return IsManifestBlend(
                    remote,
                    state.LastRemoteManifest,
                    local
                )
                    ? new SyncDecision(SyncOutcome.Push, SyncPrompt.None)
                    : conflict;

            return IsManifestBlend(
                local,
                state.LastLocalManifest,
                remote
            )
                ? new SyncDecision(SyncOutcome.Pull, SyncPrompt.None)
                : conflict;
        }

        if (!hasBaseline)
            return DecideFirstSync(local, remote);

        var localChanged = !ManifestsEqual(local, state.LastLocalManifest);
        var remoteChanged = !ManifestsEqual(remote, state.LastRemoteManifest);
        return (localChanged, remoteChanged) switch
        {
            (false, false) => new SyncDecision(SyncOutcome.NoChange, SyncPrompt.None),
            (true, false) => new SyncDecision(SyncOutcome.Push, SyncPrompt.None),
            (false, true) => new SyncDecision(SyncOutcome.Pull, SyncPrompt.None),
            _ => conflict,
        };
    }

    private static SyncDecision DecideFirstSync(
        SaveManifest local,
        SaveManifest remote
    )
    {
        if (local.IsEmpty && !remote.IsEmpty)
            return new SyncDecision(SyncOutcome.Pull, SyncPrompt.None);
        if (!local.IsEmpty && remote.IsEmpty)
            return new SyncDecision(SyncOutcome.Push, SyncPrompt.None);
        if (local.IsEmpty && remote.IsEmpty)
            return new SyncDecision(SyncOutcome.NoChange, SyncPrompt.None);

        return new SyncDecision(
            SyncOutcome.Conflict,
            SyncPrompt.ChooseSource
        );
    }

    internal static bool WouldOverwriteDestination(
        SaveManifest source,
        SaveManifest destination
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var sourceFiles = ToEntryMap(source);
        foreach (var destinationFile in destination.Files)
        {
            if (
                !sourceFiles.TryGetValue(destinationFile.Path, out var sourceFile)
                || !EntriesEqual(sourceFile, destinationFile)
            )
                return true;
        }
        return false;
    }

    private async Task<int> PullSteamToLocalTransactionAsync(
        ISaveRemote transport,
        CapturedSaveSet remote,
        CapturedSaveSet local,
        SyncState previousState,
        SyncState interruptedState,
        long capturedLocalVersion,
        CancellationToken cancellationToken
    )
    {
        var stagingRoot = PullStagingRoot();
        var incomingRoot = Path.Combine(stagingRoot, "incoming");
        var localMutationStarted = false;
        var interruptedStateWritten = false;
        var stateFileExisted = File.Exists(_statePath);
        try
        {
            RecreatePullStaging(stagingRoot);
            PatchHelper.Log("[Cloud] Pull staging started");
            var stagedEntries = new List<ManifestEntry>();
            foreach (
                var file in remote.Files.Values.OrderBy(
                    value => value.Entry.Path,
                    StringComparer.Ordinal
                )
            )
            {
                if (
                    local.Files.TryGetValue(file.Entry.Path, out var existing)
                    && EntriesEqual(existing.Entry, file.Entry)
                )
                {
                    stagedEntries.Add(existing.Entry);
                    continue;
                }

                await EnsureRemoteBytesAsync(transport, file, cancellationToken)
                    .ConfigureAwait(false);
                var stagedPath = ContainedStagingPath(
                    incomingRoot,
                    file.Entry.Path
                );
                await CancellableAtomicFile.WriteAllBytesAsync(
                    stagedPath,
                    file.Bytes,
                    overwrite: true,
                    cancellationToken
                ).ConfigureAwait(false);
                var stagedBytes = await File.ReadAllBytesAsync(
                    stagedPath,
                    cancellationToken
                ).ConfigureAwait(false);
                var stagedEntry = CreateEntry(file.Entry.Path, stagedBytes);
                if (!EntriesEqual(stagedEntry, file.Entry))
                    throw new InvalidDataException(
                        $"Staged Steam save hash mismatch: {file.Entry.Path}"
                    );
                file.Bytes = stagedBytes;
                stagedEntries.Add(stagedEntry);
            }

            if (!ManifestsEqual(CreateManifest(stagedEntries), remote.Manifest))
                throw new InvalidDataException(
                    "Staged Steam saves do not match the remote manifest"
                );
            PatchHelper.Log(
                $"[Cloud] Pull staging validated: {stagedEntries.Count} files"
            );

            var currentLocal = CaptureStableLocalSet(out var currentLocalVersion);
            if (
                currentLocalVersion != capturedLocalVersion
                || !ManifestsEqual(currentLocal.Manifest, local.Manifest)
            )
                throw new InvalidOperationException(
                    "Android saves changed while Pull was staging"
                );

            cancellationToken.ThrowIfCancellationRequested();
            await WriteStateAsync(interruptedState, cancellationToken)
                .ConfigureAwait(false);
            interruptedStateWritten = true;
            PatchHelper.Log("[Cloud] Pull apply started");

            var changed = ApplyStagedPull(
                remote,
                local,
                cancellationToken,
                ref localMutationStarted
            );
            cancellationToken.ThrowIfCancellationRequested();
            var verifiedLocal = CaptureLocalSet();
            if (!ManifestsEqual(verifiedLocal.Manifest, remote.Manifest))
                throw new InvalidDataException(
                    "Pulled Android saves do not match the staged manifest"
                );

            await WriteCompletedStateAsync(
                remote.Manifest,
                capturedLocalVersion,
                CancellationToken.None
            ).ConfigureAwait(false);
            localMutationStarted = false;
            PatchHelper.Log("[Cloud] Pull apply completed");
            return changed;
        }
        catch (Exception failure)
        {
            try
            {
                if (localMutationStarted)
                    RestoreLocalSnapshot(local, remote);
                if (interruptedStateWritten)
                {
                    await RestoreStateAfterFailedPullAsync(
                        previousState,
                        stateFileExisted
                    ).ConfigureAwait(false);
                }
                if (localMutationStarted || interruptedStateWritten)
                    PatchHelper.Log(
                        "[Cloud] Failed Pull restored Android saves and sync state"
                    );
            }
            catch (Exception rollbackFailure)
            {
                throw new IOException(
                    "Pull failed and its prior local state could not be restored",
                    new AggregateException(failure, rollbackFailure)
                );
            }
            throw;
        }
        finally
        {
            DeletePullStagingBestEffort(stagingRoot);
        }
    }

    private int ApplyStagedPull(
        CapturedSaveSet remote,
        CapturedSaveSet local,
        CancellationToken cancellationToken,
        ref bool localMutationStarted
    )
    {
        var changed = 0;
        foreach (
            var file in remote.Files.Values.OrderBy(
                value => value.Entry.Path,
                StringComparer.Ordinal
            )
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                local.Files.TryGetValue(file.Entry.Path, out var existing)
                && EntriesEqual(existing.Entry, file.Entry)
            )
                continue;

            localMutationStarted = true;
            _localStore.WriteFile(file.Entry.Path, file.Bytes);
            changed++;
        }

        foreach (
            var localPath in local.Files.Keys.OrderBy(
                path => path,
                StringComparer.Ordinal
            )
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (remote.Files.ContainsKey(localPath))
                continue;
            localMutationStarted = true;
            _localStore.DeleteFile(localPath);
            changed++;
        }
        return changed;
    }

    private async Task RestoreStateAfterFailedPullAsync(
        SyncState previousState,
        bool stateFileExisted
    )
    {
        if (stateFileExisted)
        {
            await WriteStateAsync(previousState, CancellationToken.None)
                .ConfigureAwait(false);
            return;
        }

        await _stateFileLock.WaitAsync(CancellationToken.None)
            .ConfigureAwait(false);
        try
        {
            if (File.Exists(_statePath))
                File.Delete(_statePath);
            lock (_stateLock)
                _state = previousState;
        }
        finally
        {
            _stateFileLock.Release();
        }
    }

    private void RestoreLocalSnapshot(
        CapturedSaveSet local,
        CapturedSaveSet remote
    )
    {
        try
        {
            foreach (
                var file in local.Files.Values.OrderBy(
                    value => value.Entry.Path,
                    StringComparer.Ordinal
                )
            )
                _localStore.WriteFile(file.Entry.Path, file.Bytes);

            foreach (
                var remoteOnlyPath in remote.Files.Keys
                    .Where(path => !local.Files.ContainsKey(path))
                    .OrderBy(path => path, StringComparer.Ordinal)
            )
                _localStore.DeleteFile(remoteOnlyPath);
        }
        catch (Exception rollbackFailure)
        {
            throw new IOException(
                "Pull failed and original Android saves could not be restored",
                rollbackFailure
            );
        }
    }

    private string PullStagingRoot()
        => Path.Combine(
            Path.GetDirectoryName(_statePath)
                ?? throw new InvalidOperationException("Sync state has no directory"),
            "pull-staging"
        );

    private static string ContainedStagingPath(
        string stagingRoot,
        string relativePath
    )
    {
        var root = Path.GetFullPath(stagingRoot);
        var rootWithSeparator = root.EndsWith(
            Path.DirectorySeparatorChar.ToString(),
            StringComparison.Ordinal
        )
            ? root
            : root + Path.DirectorySeparatorChar;
        var destination = Path.GetFullPath(
            Path.Combine(
                root,
                SteamCloudTransport.CanonicalizePath(relativePath)
                    .Replace('/', Path.DirectorySeparatorChar)
            )
        );
        if (!destination.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            throw new IOException($"Pull staging path escaped: {relativePath}");
        return destination;
    }

    private static void RecreatePullStaging(string stagingRoot)
    {
        if (Directory.Exists(stagingRoot))
            Directory.Delete(stagingRoot, recursive: true);
        Directory.CreateDirectory(stagingRoot);
    }

    private static void DeletePullStagingBestEffort(string stagingRoot)
    {
        try
        {
            if (Directory.Exists(stagingRoot))
                Directory.Delete(stagingRoot, recursive: true);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Pull staging cleanup failed: {ex.GetType().Name}"
            );
        }
    }

    private static async Task<int> MirrorLocalToSteamAsync(
        ISaveRemote transport,
        CapturedSaveSet local,
        CapturedSaveSet remote,
        CancellationToken cancellationToken
    )
    {
        var changed = 0;
        foreach (var file in local.Files.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                remote.Files.TryGetValue(file.Entry.Path, out var existing)
                && EntriesEqual(existing.Entry, file.Entry)
            )
                continue;

            if (
                await transport.UploadAsync(
                    new SteamCloudTransport.UploadFile(
                        file.Entry.Path,
                        file.Bytes,
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                ).ConfigureAwait(false)
            )
                changed++;
        }

        foreach (var remotePath in remote.Files.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (local.Files.ContainsKey(remotePath))
                continue;
            await transport.DeleteAsync(remotePath, cancellationToken)
                .ConfigureAwait(false);
            changed++;
        }

        return changed;
    }

    private CapturedSaveSet CaptureStableLocalSet(out long capturedVersion)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var before = Interlocked.Read(ref _localChangeVersion);
            var captured = CaptureLocalSet();
            var after = Interlocked.Read(ref _localChangeVersion);
            if (before == after)
            {
                capturedVersion = after;
                return captured;
            }
        }

        throw new LocalSnapshotChangedException(
            "Android saves kept changing during synchronization capture"
        );
    }

    private CapturedSaveSet CaptureLocalSet()
    {
        var files = new Dictionary<string, CapturedFile>(StringComparer.Ordinal);
        foreach (var path in DiscoverLocalSavePaths())
        {
            var bytes = _local.ReadBytes(path);
            var entry = CreateEntry(path, bytes);
            files.Add(
                entry.Path,
                new CapturedFile(entry, bytes, default)
            );
        }

        return new CapturedSaveSet(
            CreateManifest(files.Values.Select(file => file.Entry)),
            files
        );
    }

    private async Task<CapturedSaveSet> CaptureRemoteSetAsync(
        ISaveRemote transport,
        IReadOnlyList<SteamCloudTransport.RemoteFile> remoteFiles,
        CancellationToken cancellationToken
    )
    {
        var files = new Dictionary<string, CapturedFile>(StringComparer.Ordinal);
        foreach (var remote in remoteFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetManagedSavePath(remote.Path, out var path))
                continue;
            if (!files.ContainsKey(path) && files.Count == MaxSaveFiles)
                throw new InvalidOperationException(
                    $"Save file count exceeds {MaxSaveFiles}"
                );
            var hash = NormalizeRemoteHash(remote.ContentHash);
            byte[] bytes = null;
            var size = remote.Size;
            if (hash == null || size < 0)
            {
                bytes = await transport.DownloadBytesAsync(remote, cancellationToken)
                    .ConfigureAwait(false);
                hash = Hash(bytes);
                size = bytes.LongLength;
            }

            var entry = new ManifestEntry
            {
                Path = path,
                Size = size,
                ContentHash = hash,
            };
            files.Add(path, new CapturedFile(entry, bytes, remote));
        }

        return new CapturedSaveSet(
            CreateManifest(files.Values.Select(file => file.Entry)),
            files
        );
    }

    private static async Task EnsureRemoteBytesAsync(
        ISaveRemote transport,
        CapturedFile file,
        CancellationToken cancellationToken
    )
    {
        if (file.Bytes == null)
            file.Bytes = await transport.DownloadBytesAsync(
                file.Remote,
                cancellationToken
            ).ConfigureAwait(false);

        var actual = CreateEntry(file.Entry.Path, file.Bytes);
        if (!EntriesEqual(actual, file.Entry))
            throw new InvalidDataException(
                $"Steam file changed after enumeration: {file.Entry.Path}"
            );
    }

    private static ManifestEntry CreateEntry(string path, byte[] bytes)
        => new()
        {
            Path = SteamCloudTransport.CanonicalizePath(path),
            Size = bytes.LongLength,
            ContentHash = Hash(bytes),
        };

    internal static SaveManifest CreateManifest(
        IEnumerable<ManifestEntry> entries
    )
    {
        ArgumentNullException.ThrowIfNull(entries);
        var files = new Dictionary<string, ManifestEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var path = SteamCloudTransport.CanonicalizePath(entry.Path);
            if (entry.Size < 0)
                throw new InvalidDataException($"Negative save size: {path}");
            var hash = NormalizeStoredHash(entry.ContentHash)
                ?? throw new InvalidDataException($"Invalid save hash: {path}");
            files.Add(
                path,
                new ManifestEntry
                {
                    Path = path,
                    Size = entry.Size,
                    ContentHash = hash,
                }
            );
        }

        return new SaveManifest
        {
            Files = files.Values
                .OrderBy(file => file.Path, StringComparer.Ordinal)
                .ToList(),
        };
    }

    internal static bool ManifestsEqual(
        SaveManifest left,
        SaveManifest right
    )
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null || left.Files.Count != right.Files.Count)
            return false;

        for (var index = 0; index < left.Files.Count; index++)
        {
            if (!EntriesEqual(left.Files[index], right.Files[index]))
                return false;
        }
        return true;
    }

    private static bool IsManifestBlend(
        SaveManifest destination,
        SaveManifest before,
        SaveManifest target
    )
    {
        var destinationFiles = ToEntryMap(destination);
        var beforeFiles = ToEntryMap(before);
        var targetFiles = ToEntryMap(target);
        foreach (
            var path in destinationFiles.Keys
                .Concat(beforeFiles.Keys)
                .Concat(targetFiles.Keys)
                .Distinct(StringComparer.Ordinal)
        )
        {
            destinationFiles.TryGetValue(path, out var actual);
            beforeFiles.TryGetValue(path, out var oldValue);
            targetFiles.TryGetValue(path, out var targetValue);
            if (
                !OptionalEntriesEqual(actual, oldValue)
                && !OptionalEntriesEqual(actual, targetValue)
            )
                return false;
        }
        return true;
    }

    private static bool IsManifestSubset(
        SaveManifest candidate,
        SaveManifest source
    )
    {
        var sourceFiles = ToEntryMap(source);
        foreach (var entry in candidate.Files)
        {
            if (
                !sourceFiles.TryGetValue(entry.Path, out var sourceEntry)
                || !EntriesEqual(entry, sourceEntry)
            )
                return false;
        }
        return true;
    }

    private static Dictionary<string, ManifestEntry> ToEntryMap(
        SaveManifest manifest
    )
        => manifest.Files.ToDictionary(
            file => file.Path,
            StringComparer.Ordinal
        );

    private static bool OptionalEntriesEqual(
        ManifestEntry left,
        ManifestEntry right
    )
        => left == null ? right == null : right != null && EntriesEqual(left, right);

    private static bool EntriesEqual(ManifestEntry left, ManifestEntry right)
        => string.Equals(left.Path, right.Path, StringComparison.Ordinal)
            && left.Size == right.Size
            && string.Equals(
                left.ContentHash,
                right.ContentHash,
                StringComparison.Ordinal
            );

    private static string Hash(byte[] bytes)
        => Convert.ToHexString(ManagedSha1.Hash(bytes));

    private static string NormalizeRemoteHash(string hash)
    {
        var normalized = NormalizeStoredHash(hash);
        if (normalized != null)
            return normalized;
        if (string.IsNullOrWhiteSpace(hash))
            return null;
        try
        {
            var bytes = Convert.FromBase64String(hash);
            return bytes.Length == 20 ? Convert.ToHexString(bytes) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string NormalizeStoredHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return null;
        var normalized = hash.Trim().ToUpperInvariant();
        return normalized.Length == 40
            && normalized.All(Uri.IsHexDigit)
                ? normalized
                : null;
    }

    private IReadOnlyList<string> DiscoverLocalSavePaths()
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        DiscoverDirectory(string.Empty, 0, paths);
        return paths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    private void DiscoverDirectory(
        string directory,
        int depth,
        HashSet<string> paths
    )
    {
        foreach (var file in _localStore.GetFilesInDirectory(directory))
        {
            var path = CombinePath(directory, file);
            if (!TryGetManagedSavePath(path, out var canonical))
                continue;

            if (!paths.Contains(canonical) && paths.Count == MaxSaveFiles)
                throw new InvalidOperationException(
                    $"Save file count exceeds {MaxSaveFiles}"
                );
            paths.Add(canonical);
        }

        foreach (var child in _localStore.GetDirectoriesInDirectory(directory))
        {
            var childPath = CombinePath(directory, child);
            if (ShouldIgnoreDirectory(childPath))
                continue;
            if (depth == MaxDirectoryDepth)
                continue;
            DiscoverDirectory(childPath, depth + 1, paths);
        }
    }

    private static string CombinePath(string directory, string name)
        => string.IsNullOrEmpty(directory)
            ? name
            : $"{directory.TrimEnd('/')}/{name}";

    private static bool TryGetManagedSavePath(string path, out string canonical)
    {
        canonical = SteamCloudTransport.CanonicalizePath(path);
        if (
            ShouldIgnoreDirectory(canonical)
            || canonical.Count(character => character == '/') > MaxDirectoryDepth
        )
            return false;

        var lower = canonical.ToLowerInvariant();
        return lower.EndsWith(".save", StringComparison.Ordinal)
            || lower.EndsWith(".save.backup", StringComparison.Ordinal)
            || lower.EndsWith(".run", StringComparison.Ordinal)
            || lower.EndsWith(".bak", StringComparison.Ordinal)
            || lower.EndsWith("/prefs", StringComparison.Ordinal)
            || lower.Equals("prefs", StringComparison.Ordinal)
            || lower.EndsWith("/prefs.backup", StringComparison.Ordinal)
            || lower.Equals("prefs.backup", StringComparison.Ordinal);
    }

    private static bool ShouldIgnoreDirectory(string path)
    {
        var topLevel = SteamCloudTransport.CanonicalizePath(path)
            .Split('/', 2)[0]
            .ToLowerInvariant();
        return topLevel is ".godot"
            or ".sts2-launcher"
            or "cache"
            or "game"
            or "mods"
            or "tmp"
            or "workshop_mods";
    }

    private static string StatePath(string rootPath, string accountName)
    {
        var accountKey = Convert.ToHexString(
            ManagedSha1.Hash(Encoding.UTF8.GetBytes(accountName ?? string.Empty))
        );
        return Path.Combine(
            rootPath,
            ".sts2-launcher",
            accountKey,
            StateFileName
        );
    }

    private SyncState LoadState()
    {
        try
        {
            if (!File.Exists(_statePath))
                return new SyncState();
            var state = JsonSerializer.Deserialize<SyncState>(
                    File.ReadAllText(_statePath)
                ) ?? new SyncState();
            return NormalizeState(state);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Ignoring unreadable sync state: {ex.GetType().Name}"
            );
            return new SyncState();
        }
    }

    private static SyncState NormalizeState(SyncState state)
    {
        ValidateBaselinePair(state);
        if (state.LastLocalManifest == null)
            return state with
            {
                LastLocalManifest = null,
                LastRemoteManifest = null,
            };

        var local = CreateManifest(state.LastLocalManifest.Files);
        var remote = CreateManifest(state.LastRemoteManifest.Files);
        if (!ManifestsEqual(local, remote))
            throw new InvalidDataException(
                "Last successful manifests do not match"
            );
        return state with
        {
            LastLocalManifest = local,
            LastRemoteManifest = remote,
        };
    }

    private static void ValidateBaselinePair(SyncState state)
    {
        if (
            (state.LastLocalManifest == null)
            != (state.LastRemoteManifest == null)
        )
            throw new InvalidDataException(
                "Sync state must contain both last successful manifests or neither"
            );
    }

    private SyncState CurrentState()
    {
        lock (_stateLock)
            return _state;
    }

    private async Task WriteStateAsync(
        SyncState state,
        CancellationToken cancellationToken
    )
    {
        await _stateFileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteStateCoreAsync(state, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _stateFileLock.Release();
        }
    }

    private async Task WriteCompletedStateAsync(
        SaveManifest baseline,
        long capturedLocalVersion,
        CancellationToken cancellationToken
    )
    {
        await _stateFileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var pendingLocalChanges =
                Interlocked.Read(ref _localChangeVersion) != capturedLocalVersion;
            await WriteStateCoreAsync(
                CompletedState(baseline, pendingLocalChanges),
                cancellationToken
            ).ConfigureAwait(false);
            lock (_stateLock)
                _lastSuccessfulSyncUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            _stateFileLock.Release();
        }
    }

    private async Task UpdateStateAsync(
        Func<SyncState, SyncState> update,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(update);
        await _stateFileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SyncState next;
            lock (_stateLock)
                next = update(_state);
            await WriteStateCoreAsync(next, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _stateFileLock.Release();
        }
    }

    private async Task WriteStateCoreAsync(
        SyncState state,
        CancellationToken cancellationToken
    )
    {
        var json = JsonSerializer.Serialize(state);
        await CancellableAtomicFile.WriteAllTextAsync(
            _statePath,
            json,
            overwrite: true,
            cancellationToken
        ).ConfigureAwait(false);
        lock (_stateLock)
            _state = state;
    }

    private static SyncState CompletedState(
        SaveManifest baseline,
        bool localChangesWaiting
    )
        => new()
        {
            LastLocalManifest = baseline,
            LastRemoteManifest = baseline,
            LocalChangesWaitingToUpload = localChangesWaiting,
            InterruptedTransferMustBeRetried = false,
        };

    private ISaveRemote GetTransport()
        => _transport ??= _transportFactory?.Invoke()
            ?? new SteamCloudTransport(_accountName, _refreshToken);

    private void ResetTransport()
    {
        var transport = _transport;
        _transport = null;
        try
        {
            transport?.Dispose();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Failed to reset Steam transport: {ex.GetType().Name}"
            );
        }
    }

    private void TryUpdateCredentials(string accountName, string refreshToken)
    {
        lock (_credentialLock)
        {
            _pendingAccountName = accountName ?? string.Empty;
            _pendingRefreshToken = refreshToken ?? string.Empty;
            _credentialUpdatePending = true;
        }

        if (!_syncLock.Wait(0))
        {
            PatchHelper.Log(
                "[Cloud] Credential refresh deferred while synchronization is active"
            );
            return;
        }

        try
        {
            ApplyPendingCredentials();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private void ApplyPendingCredentials()
    {
        string accountName;
        string refreshToken;
        lock (_credentialLock)
        {
            if (!_credentialUpdatePending)
                return;
            accountName = _pendingAccountName;
            refreshToken = _pendingRefreshToken;
            _credentialUpdatePending = false;
        }

        if (
            string.Equals(_accountName, accountName, StringComparison.Ordinal)
            && string.Equals(_refreshToken, refreshToken, StringComparison.Ordinal)
        )
            return;

        var previousTransport = _transport;
        _transport = null;
        var accountChanged = !string.Equals(
            _accountName,
            accountName,
            StringComparison.Ordinal
        );
        _accountName = accountName;
        _refreshToken = refreshToken;
        Volatile.Write(ref _automaticPushAllowed, 0);
        try
        {
            previousTransport?.Dispose();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Old Steam transport disposal failed: {ex.GetType().Name}"
            );
        }

        if (!accountChanged)
            return;

        _stateFileLock.Wait();
        try
        {
            _statePath = StatePath(_local.RootPath, accountName);
            var state = LoadState();
            lock (_stateLock)
            {
                _state = state;
                _lastSuccessfulSyncUtc = null;
            }
        }
        finally
        {
            _stateFileLock.Release();
        }
    }

    private SyncResult Completed(
        SyncOutcome outcome,
        int filesTransferred,
        string message
    )
    {
        Volatile.Write(ref _automaticPushAllowed, 1);
        return new(outcome, true, false, SyncPrompt.None, filesTransferred, message);
    }

    private SyncResult Failed(string message)
    {
        Volatile.Write(ref _automaticPushAllowed, 0);
        return new(SyncOutcome.Conflict, false, false, SyncPrompt.None, 0, message);
    }

    private SyncResult RetryAfterLocalSnapshotChange(string message)
    {
        Volatile.Write(ref _automaticPushAllowed, 1);
        return new(SyncOutcome.Push, false, false, SyncPrompt.None, 0, message);
    }

    private SyncResult Canceled()
    {
        Volatile.Write(ref _automaticPushAllowed, 0);
        return new(
            SyncOutcome.Conflict,
            false,
            true,
            SyncPrompt.None,
            0,
            "Save synchronization canceled"
        );
    }
}
