using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private const string Rc3PassingAttempt = "1004837cbd894299903aac5f3f5c9ece";
    private const string Rc3FailedAttempt = "4c8e75ccc9cb48dd8f96c233b0b07313";

    private static void Rc3CapturedPassingLaunchSequence()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();

        True(owner.AttachOverlay(overlay), "RC3 launch 1 must attach its overlay.");
        True(owner.Begin(Rc3PassingAttempt), "RC3 launch 1 must begin with its captured attempt ID.");

        owner.ObserveVisibility(Rc3PassingAttempt, false, true);
        Equal(
            LauncherHandoffState.HandoffPending,
            owner.Capture().State,
            "The captured paused-window-focus callback cannot expose the game."
        );
        owner.ObserveVisibility(Rc3PassingAttempt, false, false);
        owner.ObserveVisibility(Rc3PassingAttempt, true, true);
        Equal(
            LauncherHandoffState.HandoffPending,
            owner.Capture().State,
            "RC3 launch 1 focus before main-menu readiness must wait."
        );

        True(
            owner.MarkMainMenuReady(Rc3PassingAttempt),
            "The captured RC3 launch 1 readiness callback must complete from retained focus."
        );
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "RC3 launch 1 must reach GameVisible.");
        Equal(1, overlay.DismissCalls, "RC3 launch 1 must dismiss its overlay exactly once.");
        True(
            !owner.ObserveVisibility(Rc3PassingAttempt, true, true),
            "The captured repeated focus callback must be harmless after completion."
        );
        Equal(1, overlay.DismissCalls, "Late RC3 launch 1 callbacks cannot dismiss twice.");
    }

    private static void Rc3CapturedFailingLaunchSequenceNowCompletes()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        var producer = new CharacterizedMainMenuProducer(owner);

        True(owner.AttachOverlay(overlay), "The RC3 launcher overlay must attach to the handoff owner.");
        True(owner.Begin(Rc3FailedAttempt), "Launch 2 must enter HandoffPending with its captured attempt ID.");

        True(producer.SetMainMenuReady(), "NMainMenu._Ready must ready the active attempt.");

        var ready = owner.Capture();
        Equal(LauncherHandoffState.HandoffPending, ready.State, "Readiness alone must leave the handoff pending.");
        Equal(Rc3FailedAttempt, ready.AttemptId, "Readiness must bind to the active attempt ID.");
        True(ready.MainMenuReady, "The authoritative owner must retain pre-consumer readiness.");
        True(overlay.IsVisible, "The overlay must remain visible until foreground and focus are confirmed.");

        var consumer = new CharacterizedHandoffConsumer(
            owner,
            Rc3FailedAttempt
        );
        True(
            consumer.Attach(activityForeground: true, windowFocused: true),
            "A late consumer must complete from retained readiness and current window state."
        );
        var completed = owner.Capture();
        Equal(LauncherHandoffState.GameVisible, completed.State, "Retained readiness must complete the handoff.");
        True(!overlay.IsVisible, "The launcher overlay must be dismissed after visibility confirmation.");
        Equal(1, overlay.DismissCalls, "The late consumer must dismiss the overlay exactly once.");
    }

    private static void StaleMainMenuTrueCannotCompleteActiveHandoff()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(Rc3FailedAttempt);
        owner.Fail(Rc3FailedAttempt);
        owner.Begin(HandoffAttemptB);
        owner.ObserveVisibility(HandoffAttemptB, true, true);

        True(
            !owner.MarkMainMenuReady(Rc3FailedAttempt),
            "mainMenu=True from RC3 launch 2 must be rejected after a new attempt begins."
        );
        var pending = owner.Capture();
        Equal(HandoffAttemptB, pending.AttemptId, "The stale main menu callback cannot replace the active attempt.");
        Equal(LauncherHandoffState.HandoffPending, pending.State, "Stale mainMenu=True cannot complete the active handoff.");
        True(!pending.MainMenuReady, "The active attempt cannot inherit stale readiness.");
        Equal(0, overlay.DismissCalls, "Stale readiness cannot hide the active launcher overlay.");

        owner.MarkMainMenuReady(HandoffAttemptB);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Only active-attempt readiness may complete.");
        Equal(1, overlay.DismissCalls, "The active attempt must dismiss exactly once.");
    }

    private static void HandoffIntegrationSubscriptionBeforeReadiness()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        var producer = new CharacterizedMainMenuProducer(owner);
        var consumer = new CharacterizedHandoffConsumer(owner, HandoffAttemptA);

        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);
        True(
            consumer.Attach(activityForeground: true, windowFocused: true),
            "The visibility consumer must attach before readiness."
        );
        Equal(LauncherHandoffState.HandoffPending, owner.Capture().State, "Subscription without readiness must wait.");

        True(producer.SetMainMenuReady(), "Later readiness must wake the attached consumer.");
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Subscription before readiness must complete.");
        Equal(1, overlay.DismissCalls, "The overlay must be dismissed exactly once.");
    }

    private sealed class CharacterizedMainMenuProducer
    {
        private readonly LauncherHandoffStateOwner _owner;

        internal CharacterizedMainMenuProducer(LauncherHandoffStateOwner owner)
        {
            _owner = owner;
        }

        internal bool SetMainMenuReady()
            => _owner.MarkActiveMainMenuReady();
    }

    private sealed class CharacterizedHandoffConsumer
    {
        private readonly LauncherHandoffStateOwner _owner;
        private readonly string _attemptId;

        internal CharacterizedHandoffConsumer(
            LauncherHandoffStateOwner owner,
            string attemptId
        )
        {
            _owner = owner;
            _attemptId = attemptId;
        }

        internal bool Attach(bool activityForeground, bool windowFocused)
            => _owner.ObserveVisibility(
                _attemptId,
                activityForeground,
                windowFocused
            );
    }
}
