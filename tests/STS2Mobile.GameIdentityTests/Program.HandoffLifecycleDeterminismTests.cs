using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void HandoffLifecycleBackgroundResumeBeforeReadiness()
    {
        var owner = NewPendingHandoff(out var overlay);

        True(!owner.ObserveVisibility(HandoffAttemptA, false, false), "Initial background state is idempotent.");
        True(owner.ObserveVisibility(HandoffAttemptA, true, true), "Resume and focus must reconcile current state.");
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Focus without readiness must wait.");
        True(owner.MarkMainMenuReady(HandoffAttemptA), "Readiness must consume retained resumed focus.");

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Ready plus focused must complete.");
        Equal(1, overlay.DismissCalls, "The overlay must be dismissed exactly once.");
    }

    private static void HandoffLifecycleBackgroundResumeAfterReadiness()
    {
        var owner = NewPendingHandoff(out var overlay);

        True(owner.MarkMainMenuReady(HandoffAttemptA), "Readiness must be retained while backgrounded.");
        True(!owner.ObserveVisibility(HandoffAttemptA, false, false), "Repeated background state is harmless.");
        True(owner.ObserveVisibility(HandoffAttemptA, true, false), "Resume without focus must reconcile.");
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Ready without focus must wait.");
        True(owner.ObserveVisibility(HandoffAttemptA, true, true), "Focus restoration must complete from retained readiness.");

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "The resumed focused game must be visible.");
        Equal(1, overlay.DismissCalls, "The overlay must be dismissed exactly once.");
    }

    private static void HandoffLifecycleLockUnlockFocus()
    {
        var owner = NewPendingHandoff(out var overlay);

        owner.ObserveVisibility(HandoffAttemptA, true, true);
        owner.ObserveVisibility(HandoffAttemptA, false, false);
        True(owner.MarkMainMenuReady(HandoffAttemptA), "Readiness must survive lock-equivalent backgrounding.");
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "A locked window cannot complete handoff.");
        owner.ObserveVisibility(HandoffAttemptA, true, false);
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Unlock without focus must still wait.");
        owner.ObserveVisibility(HandoffAttemptA, true, true);

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Unlock focus must complete handoff.");
        Equal(1, overlay.DismissCalls, "Lock/unlock callbacks must dismiss exactly once.");
    }

    private static void HandoffLifecycleFocusLossRestoration()
    {
        var owner = NewPendingHandoff(out var overlay);

        owner.ObserveVisibility(HandoffAttemptA, true, true);
        owner.ObserveVisibility(HandoffAttemptA, true, false);
        owner.MarkMainMenuReady(HandoffAttemptA);
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Lost focus must override earlier focus evidence.");
        owner.ObserveVisibility(HandoffAttemptA, true, true);

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Restored current focus must complete handoff.");
        Equal(1, overlay.DismissCalls, "Focus restoration must dismiss exactly once.");
        True(!owner.ObserveVisibility(HandoffAttemptA, false, false), "Late focus loss cannot reverse completion.");
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Completed handoff must remain terminal.");
    }

    private static void HandoffCoordinatorWakesOnlyForStateChanges()
    {
        var owner = NewPendingHandoff(out _);
        var initial = owner.CaptureObservation();

        True(!owner.ObserveVisibility(HandoffAttemptA, false, false), "Repeated state must be ignored.");
        True(!owner.ObserveVisibility(HandoffAttemptB, true, true), "Stale state must be ignored.");
        True(!initial.Changed.IsCompleted, "No-op callbacks must not wake the coordinator.");

        True(owner.ObserveVisibility(HandoffAttemptA, true, false), "A real lifecycle change must be accepted.");
        True(initial.Changed.IsCompleted, "A real lifecycle change must wake the coordinator.");

        var afterResume = owner.CaptureObservation();
        True(!owner.ObserveVisibility(HandoffAttemptA, true, false), "Repeated resume must remain idempotent.");
        True(!afterResume.Changed.IsCompleted, "Repeated callbacks must not cause polling-style wakes.");
        True(owner.MarkMainMenuReady(HandoffAttemptA), "Readiness is an authoritative state change.");
        True(afterResume.Changed.IsCompleted, "Readiness must wake the coordinator once.");
    }
}
