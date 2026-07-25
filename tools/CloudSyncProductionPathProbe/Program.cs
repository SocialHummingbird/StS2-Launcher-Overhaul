using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class Program
{
    private const string SavePath = "profile1/saves/progress.save";
    private const string SaveContent = "{\"stage8\":\"cloud-save\"}";
    private static int _passed;

    private static async Task Main()
    {
        var tracePath = Path.Combine(
            Path.GetTempPath(),
            $"sts2-cloud-production-probe-{Guid.NewGuid():N}.log"
        );
        Environment.SetEnvironmentVariable(
            "STS2_BOOTSTRAP_TRACE_FILE",
            tracePath
        );
        try
        {
            CloudSyncCoordinator.SetLocalBackupEnabled(false);
            LauncherCloudSaveState.ClearCredentials();

            await RunAsync("slow Pull exposes progress", SlowPullReportsProgressAsync);
            await RunAsync("missing credentials fail before cloud access", MissingCredentialsFailClosedAsync);
            await RunAsync("unavailable cloud file is skipped safely", UnavailableFileIsSkippedAsync);
            await RunAsync("branch mismatch blocks Push without I/O", BranchMismatchBlocksPushAsync);
            await RunAsync("selected mods block Push without I/O", SelectedModsBlockPushAsync);
            await RunAsync("cancellation drains transfer without late writes", CancellationDrainsWithoutMutationAsync);
            await RunAsync("failed Pull recovers on retry", FailedPullRecoversOnRetryAsync);
            await RunAsync("in-memory Push uses production transfer path", InMemoryPushUsesProductionPathAsync);

            Expect(
                LauncherCloudSaveState.StatusSummary.Contains(
                    "HasToken=False",
                    StringComparison.Ordinal
                ),
                "The probe must not leave Steam credentials in process state."
            );
            Console.WriteLine(
                $"Cloud sync production-path probe passed {_passed}/8 scenarios."
            );
            Console.WriteLine(
                "Mutation audit: no credentials, network store, hardware, or real Steam Cloud Push was used."
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                "STS2_BOOTSTRAP_TRACE_FILE",
                null
            );
            if (File.Exists(tracePath))
                File.Delete(tracePath);
        }
    }

    private static async Task SlowPullReportsProgressAsync()
    {
        var local = new InMemoryCloudSaveStore("slow-pull-local");
        var cloud = CloudWithSave("slow-pull-cloud");
        var entered = NewSignal();
        var release = NewSignal();
        cloud.BeforeReadAsync = async (path, token) =>
        {
            if (!path.Equals(SavePath, StringComparison.OrdinalIgnoreCase))
                return;
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
        };
        var published = new List<CloudOperationState>();
        var progress = new CloudOperationProgressTracker(
            CloudOperationKind.Pull,
            state => published.Add(state)
        );

        var pull = CloudSyncCoordinator.ManualPullAllAsync(
            local,
            cloud,
            progress,
            CancellationToken.None
        );
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var active = progress.State;
        Expect(
            active.Phase == CloudOperationPhase.Transferring,
            $"Expected transferring progress, got {active.Phase}."
        );
        Expect(
            active.CurrentItem.Equals(
                SavePath,
                StringComparison.OrdinalIgnoreCase
            ),
            $"Expected current Pull path {SavePath}, got {active.CurrentItem}."
        );
        Expect(
            !local.TryReadSeeded(SavePath, out _),
            "A delayed Pull wrote local content before its read completed."
        );

        release.TrySetResult();
        var result = await pull.WaitAsync(TimeSpan.FromSeconds(15));
        Expect(
            result.CompletedPathCount >= 1,
            "The slow Pull did not complete the seeded cloud file."
        );
        Expect(
            local.TryReadSeeded(SavePath, out var content)
                && content == SaveContent,
            "The slow Pull did not preserve downloaded content."
        );
        Expect(
            progress.State.IsTerminal
                && published.Any(state =>
                    state.Phase == CloudOperationPhase.Transferring),
            "The slow Pull did not publish active and terminal progress."
        );
        Expect(
            cloud.WriteCount == 0,
            "Pull must never write to its cloud source."
        );
    }

    private static async Task MissingCredentialsFailClosedAsync()
    {
        LauncherCloudSaveState.ClearCredentials();
        var progress = new CloudOperationProgressTracker(
            CloudOperationKind.Pull
        );
        InvalidOperationException? failure = null;
        try
        {
            await LauncherCloudSaveState.ManualPullAllAsync(
                progress,
                CancellationToken.None
            );
        }
        catch (InvalidOperationException ex)
        {
            failure = ex;
        }

        Expect(
            failure is not null
                && failure.Message.Contains(
                    "No saved Steam credentials",
                    StringComparison.Ordinal
                ),
            "Missing credentials did not fail before cloud-store creation."
        );
        Expect(
            progress.State.Phase == CloudOperationPhase.Idle,
            "Missing credentials unexpectedly entered cloud operation progress."
        );
    }

    private static async Task UnavailableFileIsSkippedAsync()
    {
        var local = new InMemoryCloudSaveStore("unavailable-local");
        var cloud = CloudWithSave("unavailable-cloud");
        cloud.ReadFailure = path =>
            path.Equals(SavePath, StringComparison.OrdinalIgnoreCase)
                ? new FileNotFoundException("Cloud object disappeared.", path)
                : null;
        var progress = new CloudOperationProgressTracker(
            CloudOperationKind.Pull
        );

        var result = await CloudSyncCoordinator.ManualPullAllAsync(
            local,
            cloud,
            progress,
            CancellationToken.None
        );

        Expect(
            result.CompletedPathCount == 0
                && result.SkippedPathCount >= 1
                && result.FailedPathCount == 0,
            "A disappearing cloud file was not classified as skipped."
        );
        Expect(
            result.Completion == ManualCloudSyncCompletion.Failure,
            "A Pull with no completed files must not report success."
        );
        Expect(
            !local.TryReadSeeded(SavePath, out _),
            "An unavailable cloud file changed local save content."
        );
        Expect(
            cloud.WriteCount == 0,
            "Unavailable-file handling wrote to the cloud source."
        );
    }

    private static Task BranchMismatchBlocksPushAsync()
    {
        var local = new InMemoryCloudSaveStore("branch-local");
        var cloud = new InMemoryCloudSaveStore("branch-cloud");
        local.Seed(SavePath, SaveContent);
        var eligibility = EvaluateEligibility(
            selectedMods: 0,
            pullMatchesVersion: false
        );

        Expect(!eligibility.IsEligible, "Branch mismatch unexpectedly allowed Push.");
        Expect(
            eligibility.BlockingReasons.Any(block =>
                block.Code
                    == CloudPushEligibilityBlockCode.ManualPullVersionMismatch),
            "Branch mismatch did not expose its precise Push blocker."
        );
        ExpectNoTransferIo(local, cloud, "branch mismatch");
        return Task.CompletedTask;
    }

    private static Task SelectedModsBlockPushAsync()
    {
        var local = new InMemoryCloudSaveStore("mods-local");
        var cloud = new InMemoryCloudSaveStore("mods-cloud");
        local.Seed(SavePath, SaveContent);
        var eligibility = EvaluateEligibility(
            selectedMods: 2,
            pullMatchesVersion: true
        );

        Expect(!eligibility.IsEligible, "Selected mods unexpectedly allowed Push.");
        Expect(
            eligibility.BlockingReasons.Any(block =>
                block.Code == CloudPushEligibilityBlockCode.ModsSelected),
            "Selected mods did not expose their precise Push blocker."
        );
        ExpectNoTransferIo(local, cloud, "selected mods");
        return Task.CompletedTask;
    }

    private static async Task CancellationDrainsWithoutMutationAsync()
    {
        var local = new InMemoryCloudSaveStore("cancel-local");
        var cloud = CloudWithSave("cancel-cloud");
        var entered = NewSignal();
        cloud.BeforeReadAsync = async (path, token) =>
        {
            if (!path.Equals(SavePath, StringComparison.OrdinalIgnoreCase))
                return;
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        };
        var progress = new CloudOperationProgressTracker(
            CloudOperationKind.Pull
        );
        using var cancellation = new CancellationTokenSource();
        var pull = CloudSyncCoordinator.ManualPullAllAsync(
            local,
            cloud,
            progress,
            cancellation.Token
        );

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await ExpectCanceledAsync(pull);
        await WaitUntilAsync(
            () => cloud.ActiveOperations == 0,
            TimeSpan.FromSeconds(5)
        );
        var writesAfterDrain = local.WriteCount;
        await Task.Delay(150);

        Expect(
            local.WriteCount == writesAfterDrain
                && !local.TryReadSeeded(SavePath, out _),
            "Canceled Pull performed a late local save write."
        );
        Expect(
            cloud.ActiveOperations == 0,
            "Canceled Pull left fake cloud work running."
        );
        Expect(
            cloud.WriteCount == 0,
            "Canceled Pull wrote to the cloud source."
        );
        Expect(
            progress.State.Phase == CloudOperationPhase.Failed,
            "Canceled Pull did not enter a terminal failed progress state."
        );
    }

    private static async Task FailedPullRecoversOnRetryAsync()
    {
        var local = new InMemoryCloudSaveStore("recovery-local");
        var cloud = CloudWithSave("recovery-cloud");
        cloud.ReadFailure = path =>
            path.Equals(SavePath, StringComparison.OrdinalIgnoreCase)
                ? new IOException("Injected transient read failure.")
                : null;

        var failed = await CloudSyncCoordinator.ManualPullAllAsync(
            local,
            cloud,
            new CloudOperationProgressTracker(CloudOperationKind.Pull),
            CancellationToken.None
        );
        Expect(
            failed.Completion == ManualCloudSyncCompletion.Failure
                && failed.FailedPathCount >= 1,
            "Injected transient failure did not fail the first Pull."
        );
        Expect(
            !local.TryReadSeeded(SavePath, out _),
            "Failed Pull partially wrote the target save."
        );

        cloud.ReadFailure = null;
        var recovered = await CloudSyncCoordinator.ManualPullAllAsync(
            local,
            cloud,
            new CloudOperationProgressTracker(CloudOperationKind.Pull),
            CancellationToken.None
        );
        Expect(
            recovered.CompletedPathCount >= 1,
            "A retry did not recover after the transient cloud failure."
        );
        Expect(
            local.TryReadSeeded(SavePath, out var content)
                && content == SaveContent,
            "Recovered Pull did not write the expected save."
        );
        Expect(
            cloud.WriteCount == 0,
            "Pull recovery wrote to the cloud source."
        );
    }

    private static async Task InMemoryPushUsesProductionPathAsync()
    {
        var local = new InMemoryCloudSaveStore("push-local");
        var cloud = new InMemoryCloudSaveStore("push-cloud");
        local.Seed(SavePath, SaveContent);
        var localWritesBefore = local.WriteCount;
        var result = await CloudSyncCoordinator.ManualPushAllAsync(
            local,
            cloud,
            new CloudOperationProgressTracker(CloudOperationKind.Push),
            CancellationToken.None
        );

        Expect(
            result.CompletedPathCount >= 1,
            "The production Push plan did not transfer the fake local save."
        );
        Expect(
            cloud.TryReadSeeded(SavePath, out var content)
                && content == SaveContent,
            "The production Push plan did not preserve fake save content."
        );
        Expect(
            local.WriteCount == localWritesBefore,
            "Push unexpectedly mutated its local source."
        );
        Expect(
            cloud.Operations.All(operation =>
                !operation.Contains("Steam", StringComparison.OrdinalIgnoreCase)),
            "The in-memory Push operation log contains a network-store marker."
        );
    }

    private static CloudPushEligibilityResult EvaluateEligibility(
        int selectedMods,
        bool pullMatchesVersion
    )
        => CloudPushEligibilityPolicy.Evaluate(
            new CloudPushEligibilityState(
                SelectedVersion: "public-beta",
                SelectedModCount: selectedMods,
                ManualPullCompleted: true,
                ManualPullMatchesSelectedVersion: pullMatchesVersion,
                HasImportantLocalSaveEvidence: true,
                LocalSaveOriginMatchesSelectedRuntime: true,
                HasBranchSwitchMarker: false,
                BranchSwitchEvidenceValid: true,
                HasManualPullAfterBranchSwitch: true,
                IsLocalBackupEnabled: true,
                HasBackupStoragePermission: true
            )
        );

    private static InMemoryCloudSaveStore CloudWithSave(string name)
    {
        var cloud = new InMemoryCloudSaveStore(name);
        cloud.Seed(SavePath, SaveContent);
        return cloud;
    }

    private static void ExpectNoTransferIo(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        string scenario
    )
    {
        Expect(
            local.ReadCount == 0
                && cloud.ReadCount == 0
                && cloud.WriteCount == 0,
            $"Push blocker {scenario} allowed transfer I/O."
        );
    }

    private static async Task ExpectCanceledAsync(Task operation)
    {
        try
        {
            await operation.WaitAsync(TimeSpan.FromSeconds(10));
            throw new InvalidOperationException(
                "Expected the cloud operation to be canceled."
            );
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout
    )
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Timed out waiting for fake I/O to drain.");
            await Task.Delay(10);
        }
    }

    private static TaskCompletionSource NewSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task RunAsync(
        string name,
        Func<Task> test
    )
    {
        await test();
        _passed++;
        Console.WriteLine($"PASS: {name}");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
