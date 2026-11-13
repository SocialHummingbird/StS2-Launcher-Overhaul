using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Orchestrates launcher session state and delegates concrete workflows to focused helpers.
// Events fire from background threads; the controller marshals them to the main thread.
internal partial class LauncherModel : IDisposable
{
    internal enum FastPathOutcome
    {
        ShowLogin,
        AutoConnect,
        ReadyToLaunch,
    }

    internal readonly struct FastPathResult
    {
        private FastPathResult(
            FastPathOutcome outcome,
            LauncherLaunchReadiness readiness
        )
        {
            Outcome = outcome;
            Readiness = readiness;
        }

        internal FastPathOutcome Outcome { get; }
        internal LauncherLaunchReadiness Readiness { get; }
        internal bool ReadyToLaunch => Outcome == FastPathOutcome.ReadyToLaunch;

        internal static FastPathResult Ready(LauncherLaunchReadiness readiness)
            => new(FastPathOutcome.ReadyToLaunch, readiness);

        internal static FastPathResult AutoConnect()
            => new(FastPathOutcome.AutoConnect, readiness: null);

        internal static FastPathResult ShowLogin()
            => new(FastPathOutcome.ShowLogin, readiness: null);
    }

    private volatile bool _connectionResolved;
    private readonly string _dataDir;
    private readonly SteamCredentialStore _credentialStore;
    private readonly LauncherSteamSession _steamSession;
    private readonly string _processGameBranch;
    private int _sessionAttemptId;
    private string _failReason;
    private SessionState _sessionState = SessionState.Disconnected;

    internal string DataDir => _dataDir;

    internal Task<LauncherModsPresentation> ReadModsPresentationAsync()
        => Task.Run(() =>
        {
            try { return LauncherModsPresentationState.ReadCurrent(); }
            catch (Exception error)
            {
                PatchHelper.Log($"[Mods] Failed to acquire launcher mod presentation: {error.Message}");
                return LauncherModsPresentationState.Build(
                    LauncherModSelectionState.Load(AppPaths.AppPrivateModSelectionPath, persistMigration: false),
                    Array.Empty<LauncherKnownMod>(), marker: null);
            }
        });

    internal LauncherModel(string dataDir)
    {
        _dataDir = dataDir;
        PatchHelper.Log($"[Launcher] Model data dir: '{_dataDir}' length={(_dataDir == null ? -1 : _dataDir.Length)} rooted={(!string.IsNullOrWhiteSpace(_dataDir) && System.IO.Path.IsPathRooted(_dataDir))}");
        _processGameBranch = LauncherPreferences.ReadGameBranch();
        _credentialStore = new SteamCredentialStore(dataDir);
        _steamSession = new LauncherSteamSession(dataDir, _credentialStore);
    }

    internal async Task<FastPathResult?> RetryAsync()
    {
        Task drain;
        int generation;
        lock (_downloadSync)
        {
            generation = Interlocked.Increment(ref _downloadGeneration);
            drain = _downloadLifetime.DrainThenAsync(
                () => Task.Run(() => _steamSession.Retry(downloadActive: false))
            );
        }
        await drain.ConfigureAwait(false);
        lock (_downloadSync)
        {
            if (generation != _downloadGeneration || _disposeTask != null)
                return null;
            return StartSession();
        }
    }

    internal Task ResetGameFilesForRedownloadAsync(string branch)
    {
        lock (_downloadSync)
        {
            branch = SteamGameBranch.Normalize(branch);
            Interlocked.Increment(ref _downloadGeneration);
            return _downloadLifetime.DrainThenAsync(
                () => Task.Run(() => LauncherGameFiles.DeleteDownloadedState(_dataDir, branch))
            );
        }
    }

    void IDisposable.Dispose()
        => Dispose();

    private Task _disposeTask;
    private readonly object _downloadSync = new();

    internal Task DisposeAsync()
    {
        lock (_downloadSync)
        {
            if (_disposeTask != null)
                return _disposeTask;
            Interlocked.Increment(ref _downloadGeneration);
            _disposeTask = _downloadLifetime.DrainThenAsync(
                () => Task.Run(_steamSession.Dispose),
                close: true
            );
            return _disposeTask;
        }
    }

    internal void Dispose() => _ = ObserveDisposeAsync();

    private async Task ObserveDisposeAsync()
    {
        try { await DisposeAsync().ConfigureAwait(false); }
        catch (Exception error) { PatchHelper.Log($"[Launcher] Model disposal failed: {error}"); }
    }

    private static void Raise(Action callback, string name)
        => RunCallback(name, () => callback?.Invoke());

    private static void Raise<T>(Action<T> callback, T value, string name)
        => RunCallback(name, () => callback?.Invoke(value));

    private static void RunCallback(string name, Action invoke)
    {
        try
        {
            invoke();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] {name} callback failed: {ex.Message}");
        }
    }
}
