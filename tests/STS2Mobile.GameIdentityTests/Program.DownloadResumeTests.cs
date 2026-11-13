using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void PendingDownloadUpdatingDoesNotRequireRedownload()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V2,
            ManagedReadinessEntries()
        );
        var state = BranchInstallStateStore.Current.Read(fixture.DataDir, fixture.Branch);
        BranchInstallStateStore.Current.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            Guid.NewGuid(),
            "cancelled",
            state.Depots
        );

        True(
            !LauncherGameFiles.HasBranchMetadataProblem(fixture.DataDir, fixture.Branch),
            "Interrupted game downloads with valid target depots must resume without requiring destructive redownload."
        );
        True(
            !LauncherGameFiles.DownloadedForValidation(fixture.DataDir, fixture.Branch, out _),
            "Resumable download metadata must not authorize launch readiness."
        );
    }

    private static void PendingDownloadInspectionRecognizesInterruptedTransfers()
    {
        foreach (var phase in new[] { "downloading", "cancelled", "failed", "legacy-transfer-phase" })
        {
            using var fixture = BranchStateFixture.Create();
            BranchInstallStateStore.Current.BeginUpdating(
                fixture.DataDir, fixture.Branch, Guid.NewGuid(), phase, StateDepots()
            );
            True(
                LauncherPendingDownload.Inspect(fixture.DataDir, fixture.Branch).CanResume,
                $"A validated interrupted game transfer in phase '{phase}' must offer resume."
            );
        }
    }

    private static void PendingDownloadInspectionFindsValidatedStaging()
    {
        using var fixture = BranchStateFixture.Create();
        var stateRoot = SteamGameInstallPaths.DownloadStateDirectoryPath(fixture.DataDir, fixture.Branch);
        var generation = DepotDownloadStaging.GenerationRoot(stateRoot, 123, 1001);
        Directory.CreateDirectory(generation);
        File.WriteAllText(Path.Combine(generation, "manifest.bin"), "invalid pending manifest");
        True(
            !LauncherPendingDownload.Inspect(fixture.DataDir, fixture.Branch).CanResume,
            "An arbitrary staging directory or corrupt manifest must not offer resume."
        );
        WritePendingResumeManifest(fixture.DataDir, fixture.Branch);
        True(
            LauncherPendingDownload.Inspect(fixture.DataDir, fixture.Branch).CanResume,
            "A validated pending manifest must remain resumable after restart without installation state."
        );
        True(
            !LauncherGameFiles.DownloadedForValidation(fixture.DataDir, fixture.Branch, out _),
            "Staging metadata alone must never authorize launch."
        );
    }

    private static void PendingDownloadInspectionIgnoresReadyStaging()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V2,
            ManagedReadinessEntries()
        );
        WritePendingResumeManifest(fixture.DataDir, fixture.Branch);
        True(
            !LauncherPendingDownload.Inspect(fixture.DataDir, fixture.Branch).CanResume,
            "Stale pending manifests must not replace a Ready installation with Resume."
        );
        Equal(
            InstalledGameVersionReadiness.Ready,
            LocalPckRepairOperation.ClassifyForLauncherRouting(fixture.DataDir, fixture.Branch),
            "Inspecting stale staging must preserve authoritative Ready classification."
        );
    }

    private static void PendingDownloadInspectionExcludesRepairAndRecovery()
    {
        foreach (var phase in new[] { LocalPckRepairOperation.StartingPhase, "selected-branch-recovery", "selected-branch-recovery-failed" })
        {
            using var fixture = BranchStateFixture.Create();
            BranchInstallStateStore.Current.BeginUpdating(
                fixture.DataDir, fixture.Branch, Guid.NewGuid(), phase, StateDepots()
            );
            WritePendingResumeManifest(fixture.DataDir, fixture.Branch);
            True(
                !LauncherPendingDownload.Inspect(fixture.DataDir, fixture.Branch).CanResume,
                $"Phase '{phase}' must retain its repair/recovery route even with leftover staging and depots."
            );
        }
    }

    private static void PendingDownloadInspectionRejectsInvalidState()
    {
        using var fixture = BranchStateFixture.Create();
        var statePath = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch);
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
        File.WriteAllText(statePath, "{broken install state");
        WritePendingResumeManifest(fixture.DataDir, fixture.Branch);
        var inspection = LauncherPendingDownload.Inspect(fixture.DataDir, fixture.Branch);
        True(!inspection.CanResume, "Unreadable install state must fail closed even if pending staging exists.");
        True(!string.IsNullOrWhiteSpace(inspection.Problem), "The inspection must retain its state-read failure.");
        True(
            !LauncherGameFiles.DownloadedForValidation(fixture.DataDir, fixture.Branch, out _),
            "Invalid installation state must never authorize launch."
        );
    }

    private static void DownloadLifetimePauseWaitsForDrain()
        => DownloadLifetimePauseWaitsForDrainAsync().GetAwaiter().GetResult();

    private static async Task DownloadLifetimePauseWaitsForDrainAsync()
    {
        var lifetime = new LauncherDownloadLifetime();
        var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = false;
        var worker = lifetime.TryRunAsync(async token =>
        {
            using var registration = token.Register(() => cancelled.TrySetResult(true));
            await drain.Task;
            released = true;
            token.ThrowIfCancellationRequested();
        }, pausable: true)!;
        var pause = lifetime.PauseAsync();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        True(!pause.IsCompleted && lifetime.IsRunning, "Pause must stay busy until in-flight work has drained.");
        True(!released, "Cancellation must not release worker-owned resources early.");
        True(lifetime.TryRunAsync(_ => Task.CompletedTask, pausable: true) == null, "Resume must not overlap the old operation.");
        drain.SetResult(true);
        await pause.WaitAsync(TimeSpan.FromSeconds(10));
        try { await worker; }
        catch (OperationCanceledException) { }
        True(released && !lifetime.IsRunning, "Pause completion must follow worker cleanup.");
        await lifetime.TryRunAsync(_ => Task.CompletedTask, pausable: true)!;
    }

    private static void DownloadLifetimeResetWaitsForDrain()
        => DownloadLifetimeResetWaitsForDrainAsync().GetAwaiter().GetResult();

    private static async Task DownloadLifetimeResetWaitsForDrainAsync()
    {
        var lifetime = new LauncherDownloadLifetime();
        var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken token = default;
        var worker = lifetime.TryRunAsync(async value => { token = value; await drain.Task; }, pausable: true)!;
        var deleted = false;
        var reset = lifetime.DrainThenAsync(() => { deleted = true; return Task.CompletedTask; });
        True(token.IsCancellationRequested, "Reset must stop additional download scheduling immediately.");
        True(!deleted && !reset.IsCompleted, "Reset must not delete files while a worker can still access them.");
        True(lifetime.TryRunAsync(_ => Task.CompletedTask, pausable: true) == null, "Reset must exclude replacement operations until cleanup finishes.");
        drain.SetResult(true);
        await Task.WhenAll(worker, reset).WaitAsync(TimeSpan.FromSeconds(10));
        True(deleted, "Reset must run its file cleanup after drain.");
        await lifetime.TryRunAsync(_ => Task.CompletedTask, pausable: true)!;
    }

    private static void DownloadLifetimeCloseWaitsForRepair()
        => DownloadLifetimeCloseWaitsForRepairAsync().GetAwaiter().GetResult();

    private static async Task DownloadLifetimeCloseWaitsForRepairAsync()
    {
        var lifetime = new LauncherDownloadLifetime();
        var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken token = default;
        var worker = lifetime.TryRunAsync(async value => { token = value; await drain.Task; }, pausable: false)!;
        await lifetime.PauseAsync();
        True(!token.IsCancellationRequested, "Pause must not cancel automatic local PCK repair.");
        var disposed = false;
        var close = lifetime.DrainThenAsync(() => { disposed = true; return Task.CompletedTask; }, close: true);
        True(!disposed && !close.IsCompleted, "Disposal must retain shared session resources until local repair settles.");
        True(!token.IsCancellationRequested, "Closing must await atomic local repair without interrupting it.");
        drain.SetResult(true);
        await Task.WhenAll(worker, close).WaitAsync(TimeSpan.FromSeconds(10));
        True(disposed, "Disposal must complete after drain.");
        True(lifetime.TryRunAsync(_ => Task.CompletedTask, pausable: true) == null, "Disposed model lifetime must not accept new operations.");
    }

    private static void WritePendingResumeManifest(string dataDir, string branch)
    {
        using var chunks = new ChunkFixture();
        var manifest = chunks.Manifest(1001, ("archive", new[] { "aaaa" }));
        manifest.DepotID = 123;
        var root = DepotDownloadStaging.GenerationRoot(
            SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch), 123, 1001
        );
        DepotDownloadStaging.SaveManifestAsync(root, manifest, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static void DownloadModelRejectsOverlapWithoutReleasingUi()
    {
        using var fixture = CreateReadinessFixture(AndroidPckPreparationVersions.V2, ManagedReadinessEntries());
        using var model = new LauncherModel(fixture.DataDir);
        var lifetime = (LauncherDownloadLifetime)typeof(LauncherModel)
            .GetField("_downloadLifetime", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;
        var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = lifetime.TryRunAsync(_ => drain.Task, pausable: true)!;
        var failed = 0;
        model.DownloadFailed += _ => failed++;
        try
        {
            model.StartDownloadAsync(fixture.Branch).GetAwaiter().GetResult();
            Equal(0, failed, "Rejecting a duplicate start must not publish failure that re-enables the active download UI.");
            True(model.SelectedVersionOperationIsRunning, "The original operation must retain ownership after a duplicate start.");
        }
        finally
        {
            drain.TrySetResult(true);
            active.GetAwaiter().GetResult();
        }
    }

    private static void DownloadModelQueuedCallbacksAreInvalidated()
    {
        var bind = typeof(LauncherModel).GetMethod("BindCurrentDownloadCallback", BindingFlags.Instance | BindingFlags.NonPublic);
        True(bind != null, "Queued download UI callbacks must retain the generation that produced them.");
        foreach (var action in new[] { "reset", "retry", "dispose" })
        {
            using var fixture = GameInstallFixture.Create();
            using var model = new LauncherModel(fixture.DataDir);
            var applied = 0;
            var queued = (Action)bind!.Invoke(model, new object[] { new Action(() => applied++) })!;
            switch (action)
            {
                case "reset": model.ResetGameFilesForRedownloadAsync(fixture.Branch).GetAwaiter().GetResult(); break;
                case "retry":
                    var lifetime = (LauncherDownloadLifetime)typeof(LauncherModel)
                        .GetField("_downloadLifetime", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;
                    var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var worker = lifetime.TryRunAsync(_ => drain.Task, pausable: true)!;
                    var retry = model.RetryAsync();
                    queued();
                    Equal(0, applied, "Retry must invalidate an old queued callback before its worker has drained.");
                    var disposal = model.DisposeAsync();
                    drain.SetResult(true);
                    Task.WhenAll(worker, retry, disposal).WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                    break;
                case "dispose": model.DisposeAsync().GetAwaiter().GetResult(); break;
            }
            queued();
            Equal(0, applied, $"A queued old download callback must be ignored after {action} supersedes it.");
        }
    }

    private static void DownloadModelResetWaitsForOwnedWorker()
        => DownloadModelResetWaitsForOwnedWorkerAsync().GetAwaiter().GetResult();

    private static async Task DownloadModelResetWaitsForOwnedWorkerAsync()
    {
        using var fixture = GameInstallFixture.Create();
        using var model = new LauncherModel(fixture.DataDir);
        var lifetime = (LauncherDownloadLifetime)typeof(LauncherModel)
            .GetField("_downloadLifetime", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;
        var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken token = default;
        var worker = lifetime.TryRunAsync(async value => { token = value; await drain.Task; }, pausable: true)!;
        var reset = model.ResetGameFilesForRedownloadAsync(fixture.Branch);
        try
        {
            True(token.IsCancellationRequested, "Model reset must request download cancellation immediately.");
            True(!reset.IsCompleted && model.SelectedVersionOperationIsRunning, "The model must retain busy ownership while reset waits for workers.");
            True(File.Exists(fixture.PckPath), "Reset must retain the installed PCK until workers release it.");
        }
        finally
        {
            drain.TrySetResult(true);
            await Task.WhenAll(worker, reset).WaitAsync(TimeSpan.FromSeconds(10));
        }
        True(!File.Exists(fixture.PckPath), "Model reset must remove selected files after the owned worker drains.");
        True(!model.SelectedVersionOperationIsRunning, "Reset must release busy ownership after cleanup.");
    }

    private static void DownloadModelDisposeSupersedesPendingRetry()
        => DownloadModelDisposeSupersedesPendingRetryAsync().GetAwaiter().GetResult();

    private static async Task DownloadModelDisposeSupersedesPendingRetryAsync()
    {
        using var fixture = GameInstallFixture.Create();
        using var model = new LauncherModel(fixture.DataDir);
        var lifetime = (LauncherDownloadLifetime)typeof(LauncherModel)
            .GetField("_downloadLifetime", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;
        var drain = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = lifetime.TryRunAsync(_ => drain.Task, pausable: true)!;
        var retry = model.RetryAsync();
        var disposal = model.DisposeAsync();
        drain.SetResult(true);
        await Task.WhenAll(worker, retry, disposal).WaitAsync(TimeSpan.FromSeconds(10));
        object? result = await retry;
        True(result == null, "A pending retry superseded by disposal must not restart session flow or update launcher UI.");
    }

    private static void DownloadModelTimeoutReportsFailure()
    {
        using var fixture = BranchStateFixture.Create();
        using var model = new LauncherModel(fixture.DataDir);
        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        cancellation.Dispose();
        var failed = new System.Collections.Generic.List<LauncherBranchOperationFailure>();
        var paused = 0;
        model.DownloadFailed += failed.Add;
        model.DownloadCancelled += _ => paused++;

        CompleteDownloadTerminalForTest(
            model, fixture.Branch,
            Task.FromException(new TaskCanceledException("CDN manifest request timed out")),
            token
        );

        Equal(1, failed.Count, "A request timeout must report download failure when the owning operation token was not cancelled.");
        Equal("CDN manifest request timed out", failed[0].Message, "Timeout failure must retain the actual request error.");
        Equal(0, paused, "An unrequested request timeout must never be reported as a paused download.");
    }

    private static void DownloadModelRequestedCancellationReportsPaused()
    {
        using var fixture = BranchStateFixture.Create();
        using var model = new LauncherModel(fixture.DataDir);
        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        cancellation.Cancel();
        cancellation.Dispose();
        var failed = 0;
        var paused = 0;
        model.DownloadFailed += _ => failed++;
        model.DownloadCancelled += branch =>
        {
            Equal(fixture.Branch, branch, "A requested pause must identify the operation's branch.");
            paused++;
        };

        CompleteDownloadTerminalForTest(model, fixture.Branch, Task.FromCanceled(token), token);

        Equal(1, paused, "A cancelled owning token must still report pause after its CTS has been disposed by worker cleanup.");
        Equal(0, failed, "A requested pause must not report a transfer failure.");
    }

    private static void CompleteDownloadTerminalForTest(LauncherModel model, string branch, Task operation, CancellationToken token)
    {
        var method = typeof(LauncherModel).GetMethod("CompleteSelectedVersionOperationAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var completion = new Func<BranchInstallCompletion>(() => throw new InvalidOperationException("A failed operation cannot complete successfully."));
        ((Task)method.Invoke(model, new object[] { operation, branch, 0, token, completion })!).GetAwaiter().GetResult();
    }
}
