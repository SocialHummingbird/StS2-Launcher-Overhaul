using System;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudPullProgressTest
{
    private static int _passed;

    private static int Main()
    {
        Run("idle state stays hidden", IdleStateStaysHidden);
        Run("all live Pull phases are observable", AllLivePullPhasesAreObservable);
        Run("backup counts are cumulative", BackupCountsAreCumulative);
        Run("verified transfer count and current path are visible", TransferProgressIsVisible);
        Run("every pending phase reads as active", PendingPhasesReadAsActive);
        Run("current path is visible before an await completes", CurrentPathIsVisibleBeforeCompletion);
        Run("completion remains visible with final counts", CompletionRemainsVisible);
        Run("failure retains verified progress", FailureRetainsProgress);
        Run("terminal state ignores late operation updates", TerminalStateIgnoresLateUpdates);

        AssertEqual("Pull progress tests passed", 9, _passed);
        Console.WriteLine("Launcher cloud Pull progress tests passed 9/9.");
        return 0;
    }

    private static void IdleStateStaysHidden()
    {
        var state = CloudOperationState.Idle(CloudOperationKind.Pull);
        var presentation = CloudOperationPresentation.Create(state);

        AssertFalse("idle active", state.IsActive);
        AssertFalse("idle terminal", state.IsTerminal);
        AssertFalse("idle visible", presentation.Visible);
        AssertEqual("idle progress", 0d, presentation.ProgressValue);
    }

    private static void AllLivePullPhasesAreObservable()
    {
        var phases = RunCompletePull()
            .Select(state => state.Phase)
            .Distinct()
            .ToArray();

        AssertSequence(
            "observable phase order",
            new[]
            {
                CloudOperationPhase.Preparing,
                CloudOperationPhase.Enumerating,
                CloudOperationPhase.BackingUp,
                CloudOperationPhase.Transferring,
                CloudOperationPhase.Finalizing,
                CloudOperationPhase.Completed,
            },
            phases
        );
    }

    private static void BackupCountsAreCumulative()
    {
        var backup = RunCompletePull().Last(
            state => state.Phase == CloudOperationPhase.BackingUp
        );
        var presentation = CloudOperationPresentation.Create(backup);

        AssertEqual("enumerated paths", 3, backup.EnumeratedPathCount);
        AssertEqual("backup processed", 3, backup.BackupProcessedCount);
        AssertEqual("backup total", 3, backup.BackupTotalCount);
        AssertEqual("backups created", 2, backup.BackupCreatedCount);
        AssertContains("backup phase", presentation.PhaseText, "Backing up");
        AssertContains("backup checked count", presentation.DetailText, "3/3 checked");
        AssertContains("backup created count", presentation.DetailText, "2 backup(s) created");
    }

    private static void TransferProgressIsVisible()
    {
        var transfer = RunCompletePull().Last(
            state => state.Phase == CloudOperationPhase.Transferring
        );
        var presentation = CloudOperationPresentation.Create(transfer);

        AssertEqual("transfer processed", 3, transfer.TransferProcessedCount);
        AssertEqual("transfer total", 3, transfer.TransferTotalCount);
        AssertEqual("transfer completed", 3, transfer.TransferCompletedCount);
        AssertContains("transfer phase", presentation.PhaseText, "Downloading");
        AssertContains("transfer checked count", presentation.DetailText, "3/3 checked");
        AssertContains("transfer verified count", presentation.DetailText, "3 verified");
        AssertContains("current transfer path", presentation.DetailText, "profile3/saves/prefs.save");
    }

    private static void PendingPhasesReadAsActive()
    {
        foreach (var state in RunCompletePull().Where(state => !state.IsTerminal))
        {
            var presentation = CloudOperationPresentation.Create(state);
            AssertTrue($"{state.Phase} active", state.IsActive);
            AssertTrue($"{state.Phase} visible", presentation.Visible);
            AssertFalse($"{state.Phase} phase text", string.IsNullOrWhiteSpace(presentation.PhaseText));
            AssertFalse($"{state.Phase} detail text", string.IsNullOrWhiteSpace(presentation.DetailText));
            AssertTrue($"{state.Phase} progress above zero", presentation.ProgressValue > 0);
        }
    }

    private static void CurrentPathIsVisibleBeforeCompletion()
    {
        var tracker = new CloudOperationProgressTracker(CloudOperationKind.Pull);
        tracker.TransferStarted(5);
        tracker.TransferPathStarted("profile2/saves/history/2026-07-25.run");

        var waiting = tracker.State;
        var presentation = CloudOperationPresentation.Create(waiting);
        AssertEqual("waiting transfer processed", 0, waiting.TransferProcessedCount);
        AssertContains("waiting current path", presentation.DetailText, "profile2/saves/history/2026-07-25.run");
        AssertTrue("waiting state active", waiting.IsActive);
    }

    private static void CompletionRemainsVisible()
    {
        var complete = RunCompletePull().Last();
        var presentation = CloudOperationPresentation.Create(complete);

        AssertEqual("complete phase", CloudOperationPhase.Completed, complete.Phase);
        AssertTrue("complete terminal", complete.IsTerminal);
        AssertFalse("complete active", complete.IsActive);
        AssertTrue("complete visible", presentation.Visible);
        AssertTrue("complete presentation", presentation.IsComplete);
        AssertEqual("complete progress", 100d, presentation.ProgressValue);
        AssertContains("complete checked", presentation.DetailText, "3/3 checked");
        AssertContains("complete verified", presentation.DetailText, "3 verified");
        AssertContains("complete finished", presentation.DetailText, "Pull is finished");
    }

    private static void FailureRetainsProgress()
    {
        var tracker = new CloudOperationProgressTracker(CloudOperationKind.Pull);
        tracker.TransferStarted(4);
        tracker.TransferProcessed("profile1/saves/progress.save");
        tracker.TransferPathStarted("profile2/saves/progress.save");
        tracker.Failed("network unavailable");

        var failed = tracker.State;
        var presentation = CloudOperationPresentation.Create(failed);
        AssertEqual("failed phase", CloudOperationPhase.Failed, failed.Phase);
        AssertTrue("failed terminal", failed.IsTerminal);
        AssertTrue("failed presentation", presentation.IsFailed);
        AssertEqual("failed retained download", 1, failed.TransferCompletedCount);
        AssertContains("failed reason", presentation.DetailText, "network unavailable");
        AssertTrue("failed retained progress", presentation.ProgressValue > 30);
    }

    private static void TerminalStateIgnoresLateUpdates()
    {
        var snapshots = new List<CloudOperationState>();
        var tracker = new CloudOperationProgressTracker(CloudOperationKind.Pull, snapshots.Add);
        tracker.Preparing("Connecting");
        tracker.TransferStarted(3);
        tracker.Failed("request timed out");
        var terminalSnapshotCount = snapshots.Count;

        tracker.TransferPathStarted("late.save");
        tracker.TransferProcessed("late.save");
        tracker.Finalizing("Late finalization");
        tracker.Completed("Late completion");

        AssertEqual("late update snapshot count", terminalSnapshotCount, snapshots.Count);
        AssertEqual("terminal phase preserved", CloudOperationPhase.Failed, tracker.State.Phase);
        AssertEqual("late download excluded", 0, tracker.State.TransferCompletedCount);
        AssertEqual("terminal reason preserved", "request timed out", tracker.State.ErrorMessage);
    }

    private static IReadOnlyList<CloudOperationState> RunCompletePull()
    {
        var snapshots = new List<CloudOperationState>();
        var tracker = new CloudOperationProgressTracker(CloudOperationKind.Pull, snapshots.Add);
        tracker.Preparing("Connecting to Steam Cloud");
        tracker.EnumerationStarted("Checking save locations");
        tracker.EnumerationCompleted(3);
        tracker.BackupStarted(3);
        tracker.BackupProcessed("profile1/saves/progress.save", created: true);
        tracker.BackupProcessed("profile2/saves/progress.save", created: false);
        tracker.BackupProcessed("profile3/saves/progress.save", created: true);
        tracker.TransferStarted(3);
        tracker.TransferProcessed("profile1/saves/progress.save");
        tracker.TransferProcessed("profile2/saves/progress.save");
        tracker.TransferProcessed("profile3/saves/prefs.save");
        tracker.Finalizing("Verifying the local save-context marker");
        tracker.Completed("All save files were transferred and verified");
        return snapshots;
    }

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine($"[PASS] {name}");
    }

    private static void AssertTrue(string name, bool actual)
    {
        if (!actual)
            throw new InvalidOperationException($"{name}: expected true.");
    }

    private static void AssertFalse(string name, bool actual)
    {
        if (actual)
            throw new InvalidOperationException($"{name}: expected false.");
    }

    private static void AssertEqual<T>(string name, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}.");
    }

    private static void AssertContains(string name, string actual, string expected)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"{name}: expected '{expected}' in '{actual}'.");
    }

    private static void AssertSequence<T>(
        string name,
        IReadOnlyCollection<T> expected,
        IReadOnlyCollection<T> actual
    )
    {
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException(
                $"{name}: expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}]."
            );
    }
}
