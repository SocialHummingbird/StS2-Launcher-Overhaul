using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void HandoffIntegrationReadinessBeforeFocus()
    {
        var owner = NewPendingHandoff(out var overlay);

        True(owner.MarkMainMenuReady(HandoffAttemptA), "Readiness must be retained before focus.");
        True(
            owner.ObserveVisibility(HandoffAttemptA, true, false),
            "The resumed activity state must be accepted."
        );
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Focus is still required.");
        True(
            owner.ObserveVisibility(HandoffAttemptA, true, true),
            "Returning focus must re-check authoritative readiness."
        );

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Focus must complete the ready handoff.");
        Equal(1, overlay.DismissCalls, "The overlay must be dismissed exactly once.");
    }

    private static void HandoffIntegrationFocusBeforeReadiness()
    {
        var owner = NewPendingHandoff(out var overlay);

        True(
            owner.ObserveVisibility(HandoffAttemptA, true, true),
            "Foreground focus must be retained before readiness."
        );
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Readiness is still required.");
        True(owner.MarkMainMenuReady(HandoffAttemptA), "Readiness must re-check retained window state.");

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Readiness must complete the focused handoff.");
        Equal(1, overlay.DismissCalls, "The overlay must be dismissed exactly once.");
    }

    private static void HandoffIntegrationIgnoresStaleEvents()
    {
        var owner = NewPendingHandoff(out var overlay);
        True(owner.Fail(HandoffAttemptA), "The first attempt must fail closed.");
        True(owner.Begin(HandoffAttemptB), "A replacement attempt must begin.");

        True(!owner.MarkMainMenuReady(HandoffAttemptA), "Stale readiness must be ignored.");
        True(
            !owner.ObserveVisibility(HandoffAttemptA, true, true),
            "Stale resume and focus callbacks must be ignored."
        );
        var pending = owner.Capture();
        Equal(HandoffAttemptB, pending.AttemptId, "Stale callbacks must not replace the active attempt.");
        Equal(LauncherHandoffState.HandoffPending, pending.State, "The active attempt must remain pending.");
        True(!pending.MainMenuReady, "Stale readiness must not leak into the active attempt.");
        Equal(0, overlay.DismissCalls, "Stale callbacks must not hide the launcher.");

        True(owner.MarkMainMenuReady(HandoffAttemptB), "The active attempt must accept readiness.");
        True(owner.ObserveVisibility(HandoffAttemptB, true, true), "The active window must complete handoff.");
        Equal(1, overlay.DismissCalls, "Only the active attempt may dismiss the overlay.");
    }

    private static LauncherHandoffStateOwner NewPendingHandoff(
        out FakeHandoffOverlay overlay
    )
    {
        var owner = new LauncherHandoffStateOwner();
        overlay = new FakeHandoffOverlay();
        True(owner.AttachOverlay(overlay), "The launcher overlay must attach.");
        True(owner.Begin(HandoffAttemptA), "The launch attempt must begin.");
        return owner;
    }
}
