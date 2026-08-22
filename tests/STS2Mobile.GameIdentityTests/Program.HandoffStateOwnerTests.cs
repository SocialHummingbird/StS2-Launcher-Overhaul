using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void HandoffActiveMainMenuProducerIsIdempotent()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();

        True(owner.AttachOverlay(overlay), "The launcher overlay must attach.");
        True(!owner.MarkActiveMainMenuReady(), "No readiness may be produced without an active attempt.");
        True(owner.Begin(HandoffAttemptA), "The launch attempt must begin.");
        True(owner.MarkActiveMainMenuReady(), "The real transition must ready the active attempt.");
        True(!owner.MarkActiveMainMenuReady(), "A repeated main-menu callback must be harmless.");

        var pending = owner.Capture();
        Equal(HandoffAttemptA, pending.AttemptId, "The producer must use the active attempt ID.");
        True(pending.MainMenuReady, "The active attempt must retain authoritative readiness.");
    }

    private const string HandoffAttemptA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HandoffAttemptB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string HandoffAttemptC = "cccccccccccccccccccccccccccccccc";

    private static void HandoffStateOwnerNormalEvents()
    {
        var owner = new LauncherHandoffStateOwner();
        Equal(
            LauncherHandoffState.LauncherVisible,
            owner.Capture().State,
            "The launcher must own initial visibility."
        );

        True(owner.Begin(HandoffAttemptA), "A new launch must begin a pending handoff.");
        True(owner.MarkMainMenuReady(HandoffAttemptA), "Main-menu readiness must be accepted.");
        Equal(
            LauncherHandoffState.HandoffPending,
            owner.Capture().State,
            "Main-menu readiness alone must not claim that the game is visible."
        );

        True(owner.ObserveVisibility(HandoffAttemptA, true, false), "Foreground state must be accepted.");
        Equal(
            LauncherHandoffState.HandoffPending,
            owner.Capture().State,
            "Foreground state without window focus must remain pending."
        );

        True(owner.ObserveVisibility(HandoffAttemptA, true, true), "Window focus must complete the handoff.");
        var completed = owner.Capture();
        Equal(LauncherHandoffState.GameVisible, completed.State, "All visibility evidence must reveal the game.");
        Equal(HandoffAttemptA, completed.AttemptId, "The completed handoff must retain its attempt ID.");
    }

    private static void HandoffStateOwnerRepeatedEvents()
    {
        var owner = new LauncherHandoffStateOwner();
        True(owner.Begin(HandoffAttemptA), "The first begin event must change state.");
        True(!owner.Begin(HandoffAttemptA), "A repeated begin event must be idempotent.");
        True(owner.MarkMainMenuReady(HandoffAttemptA), "The first readiness event must change state.");
        True(!owner.MarkMainMenuReady(HandoffAttemptA), "Repeated readiness must be idempotent.");
        True(!owner.Begin(HandoffAttemptA), "A repeated begin must not restart the active attempt.");
        True(owner.Capture().MainMenuReady, "A repeated begin must preserve accepted readiness.");
        True(owner.ObserveVisibility(HandoffAttemptA, true, false), "The first foreground event must change state.");
        True(!owner.ObserveVisibility(HandoffAttemptA, true, false), "Repeated foreground must be idempotent.");
        True(owner.ObserveVisibility(HandoffAttemptA, true, true), "The first focused event must complete the handoff.");
        True(!owner.ObserveVisibility(HandoffAttemptA, true, true), "Repeated terminal events must be ignored.");
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Repeated events must preserve completion.");
    }

    private static void HandoffStateOwnerReorderedEvents()
    {
        var owner = new LauncherHandoffStateOwner();
        owner.Begin(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, true, true);
        Equal(
            LauncherHandoffState.HandoffPending,
            owner.Capture().State,
            "Focus and foreground arriving first must wait for readiness."
        );

        owner.MarkMainMenuReady(HandoffAttemptA);
        Equal(
            LauncherHandoffState.GameVisible,
            owner.Capture().State,
            "Reordered visibility evidence must complete once all conditions are true."
        );
    }

    private static void HandoffStateOwnerStaleEvents()
    {
        var owner = new LauncherHandoffStateOwner();
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);
        True(!owner.Begin(HandoffAttemptB), "A concurrent launch must not supersede the active attempt.");
        True(owner.Fail(HandoffAttemptA), "The active attempt must end before another launch begins.");
        True(owner.Begin(HandoffAttemptB), "A later launch may begin after the earlier attempt ends.");

        True(!owner.ObserveVisibility(HandoffAttemptA, true, false), "Stale foreground callbacks must be ignored.");
        True(!owner.ObserveVisibility(HandoffAttemptA, true, true), "Stale focus callbacks must be ignored.");
        True(!owner.Fail(HandoffAttemptA), "A stale failure must not cancel the current attempt.");
        var pending = owner.Capture();
        Equal(LauncherHandoffState.HandoffPending, pending.State, "Stale events must leave the new handoff pending.");
        Equal(HandoffAttemptB, pending.AttemptId, "Stale events must not replace the current attempt ID.");
        True(!pending.MainMenuReady, "Readiness from the superseded attempt must be cleared.");

        owner.MarkMainMenuReady(HandoffAttemptB);
        owner.ObserveVisibility(HandoffAttemptB, true, true);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "The current attempt must still complete normally.");
    }

    private static void HandoffStateOwnerCancelledEvents()
    {
        var owner = new LauncherHandoffStateOwner();
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);
        True(owner.Cancel(HandoffAttemptA), "Cancellation must return ownership to the launcher.");
        AssertLauncherVisible(owner, "Cancellation");
        True(!owner.ObserveVisibility(HandoffAttemptA, true, true), "Callbacks after cancellation must be ignored.");
        True(!owner.Cancel(HandoffAttemptA), "Repeated cancellation must be idempotent.");
        AssertLauncherVisible(owner, "Repeated cancellation");
    }

    private static void HandoffStateOwnerFailedEvents()
    {
        var owner = new LauncherHandoffStateOwner();
        owner.Begin(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, true, false);
        True(owner.Fail(HandoffAttemptA), "Failure must return ownership to the launcher.");
        AssertLauncherVisible(owner, "Failure");
        True(!owner.MarkMainMenuReady(HandoffAttemptA), "Callbacks after failure must be ignored.");
        True(!owner.Fail(HandoffAttemptA), "Repeated failure must be idempotent.");
        AssertLauncherVisible(owner, "Repeated failure");
    }

    private static void HandoffOwnerDismissesOverlayExactlyOnce()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        True(owner.AttachOverlay(overlay), "The launcher overlay must attach to its sole owner.");
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, true, true);

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Confirmed visibility must complete the handoff.");
        Equal(1, overlay.DismissCalls, "The active overlay must be dismissed exactly once.");
        True(!overlay.IsVisible, "A completed handoff must leave the overlay hidden.");

        True(!owner.ObserveVisibility(HandoffAttemptA, true, true), "Repeated callbacks after completion must be ignored.");
        True(!owner.Fail(HandoffAttemptA), "A late failure must not reverse a completed handoff.");
        True(!owner.AttachOverlay(new FakeHandoffOverlay()), "No callback may attach another overlay after GameVisible.");
        Equal(1, overlay.DismissCalls, "Late callbacks must not repeat overlay dismissal.");
    }

    private static void HandoffRecoveryLaunchUsesCurrentAttempt()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);
        owner.Fail(HandoffAttemptA);
        True(owner.Begin(HandoffAttemptB), "A recovery launch must enter through the same owner.");

        True(!owner.MarkMainMenuReady(HandoffAttemptA), "The failed attempt cannot ready the recovery launch.");
        True(!owner.ObserveVisibility(HandoffAttemptA, true, true), "Stale Android visibility cannot complete recovery.");
        Equal(0, overlay.DismissCalls, "Stale recovery callbacks must not dismiss the overlay.");

        owner.MarkMainMenuReady(HandoffAttemptB);
        owner.ObserveVisibility(HandoffAttemptB, true, true);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "The recovery attempt must complete through the owner.");
        Equal(1, overlay.DismissCalls, "Recovery must use the same single dismissal path.");
    }

    private static void AndroidHandoffVisibilityRequiresResumedFocus()
    {
        var resumed = LauncherHandoffVisibility.ParseAndroid("resumed\ntrue");
        True(resumed.ActivityForeground, "Android resumed state must confirm foreground activity.");
        True(resumed.WindowFocused, "Android focus must be parsed independently.");

        var paused = LauncherHandoffVisibility.ParseAndroid("paused\ntrue");
        True(!paused.ActivityForeground, "Window focus cannot substitute for a resumed activity.");
        True(paused.WindowFocused, "The current window-focus signal must remain observable.");
        True(paused.SuspendsHandoffTimeout, "Background lifecycle time must not consume the handoff bound.");

        var unfocused = LauncherHandoffVisibility.ParseAndroid("resumed\nfalse");
        True(unfocused.ActivityForeground, "A resumed activity remains foreground evidence.");
        True(!unfocused.WindowFocused, "A resumed but unfocused activity must not complete handoff.");
        True(!unfocused.SuspendsHandoffTimeout, "Foreground focus loss must remain bounded.");

        var destroyed = LauncherHandoffVisibility.ParseAndroid("destroyed\nfalse");
        True(!destroyed.SuspendsHandoffTimeout, "A destroyed activity without recreation must eventually time out.");
    }

    private static void HandoffProcessRestorationRetainsAttemptId()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);

        True(owner.RestorePending(HandoffAttemptA), "Process restoration must restore the persisted attempt.");
        True(owner.RestorePending(HandoffAttemptA), "Repeated activity restoration must be harmless.");
        var restored = owner.Capture();
        Equal(LauncherHandoffState.HandoffPending, restored.State, "A restored launch must remain pending.");
        Equal(HandoffAttemptA, restored.AttemptId, "Restoration must retain the native launch-attempt ID.");

        owner.MarkMainMenuReady(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, true, true);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "The restored attempt must complete normally.");
        Equal(1, overlay.DismissCalls, "Restoration must still use the single overlay dismissal path.");
    }

    private static void HandoffAndroidLifecyclePermutations()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);

        var pendingEvents = new[]
        {
            (Foreground: false, Focused: false), // cold activity creation
            (Foreground: true, Focused: false),  // resumed before focus
            (Foreground: false, Focused: false), // backgrounded
            (Foreground: false, Focused: true),  // stale focus while paused
            (Foreground: true, Focused: false),  // resumed again
        };
        foreach (var lifecycleEvent in pendingEvents)
        {
            owner.ObserveVisibility(
                HandoffAttemptA,
                lifecycleEvent.Foreground,
                lifecycleEvent.Focused
            );
            Equal(
                LauncherHandoffState.HandoffPending,
                owner.Capture().State,
                "No paused or unfocused lifecycle permutation may expose the game."
            );
        }

        owner.ObserveVisibility(HandoffAttemptA, true, true);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Resumed focus must complete once.");
        Equal(1, overlay.DismissCalls, "Lifecycle permutations must dismiss the overlay exactly once.");

        foreach (var terminalEvent in new[]
        {
            (Foreground: true, Focused: false),
            (Foreground: false, Focused: false),
            (Foreground: true, Focused: true),
        })
        {
            True(
                !owner.ObserveVisibility(
                    HandoffAttemptA,
                    terminalEvent.Foreground,
                    terminalEvent.Focused
                ),
                "Warm-launch lifecycle callbacks after GameVisible must be ignored."
            );
        }
        Equal(1, overlay.DismissCalls, "Focus restoration cannot resurrect or re-dismiss the overlay.");
        Equal(0, overlay.ReturnToLauncherCalls, "Terminal lifecycle callbacks cannot restore the launcher.");
    }

    private static void HandoffFailureAndCancellationRestoreUsableLauncher()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);

        owner.Begin(HandoffAttemptA);
        True(owner.Fail(HandoffAttemptA), "A timed-out handoff must fail through the owner.");
        True(overlay.IsVisible, "Failure must reveal a usable launcher.");
        Equal(1, overlay.ReturnToLauncherCalls, "Failure must restore the launcher once.");

        True(owner.Begin(HandoffAttemptB), "A consecutive launch must be accepted after failure.");
        True(owner.Cancel(HandoffAttemptB), "Cancellation must return through the same owner.");
        True(overlay.IsVisible, "Cancellation must retain a usable launcher.");
        Equal(2, overlay.ReturnToLauncherCalls, "Cancellation must restore the same launcher once.");

        True(owner.Begin(HandoffAttemptC), "A launch after cancellation must remain available.");
        True(!owner.Fail(HandoffAttemptA), "A stale timeout cannot fail the current launch.");
        Equal(2, overlay.ReturnToLauncherCalls, "A stale failure cannot mutate the launcher.");
    }

    private static void HandoffActivityRecreationPreservesEvidence()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, false, false);

        overlay.IsAvailable = false;
        var recreatedOverlay = new FakeHandoffOverlay();
        var beforeReattach = owner.CaptureObservation();
        True(
            owner.AttachOverlay(recreatedOverlay),
            "Activity recreation must replace an unavailable overlay through the same owner."
        );
        True(
            beforeReattach.Changed.IsCompleted,
            "Reattaching the handoff consumer must wake state reconciliation."
        );

        True(owner.RestorePending(HandoffAttemptA), "Activity recreation must reaccept the active persisted attempt.");
        var recreated = owner.Capture();
        True(recreated.MainMenuReady, "Activity recreation must retain readiness evidence.");
        Equal(HandoffAttemptA, recreated.AttemptId, "Activity recreation must retain attempt identity.");

        owner.ObserveVisibility(HandoffAttemptA, true, false);
        owner.ObserveVisibility(HandoffAttemptA, true, true);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "The recreated activity must complete normally.");
        Equal(0, overlay.DismissCalls, "The detached activity overlay must never receive a stale dismissal.");
        Equal(1, recreatedOverlay.DismissCalls, "Only the recreated activity overlay may be dismissed.");
    }

    private static void HandoffTimeoutExcludesPausedTime()
    {
        long clock = 0;
        var deadline = LauncherMonotonicDeadline.Start(
            System.TimeSpan.FromMilliseconds(100),
            () => clock
        );
        var child = deadline.CreateChild(System.TimeSpan.FromMilliseconds(70));

        clock = 40;
        True(deadline.Pause(), "Backgrounding must pause the active handoff budget.");
        clock = 1_000;
        Equal(40L, deadline.ElapsedMilliseconds, "Background time must not consume the handoff timeout.");
        True(!child.IsExpired, "Child rendering deadlines must pause with the handoff lifecycle.");

        True(deadline.Resume(), "Foreground restoration must resume the existing budget.");
        clock = 1_029;
        True(!child.IsExpired, "The child budget must retain its remaining active time.");
        clock = 1_030;
        True(child.IsExpired, "The child budget must still remain bounded while foregrounded.");
        True(!deadline.IsExpired, "The parent handoff budget must retain its independent bound.");
        clock = 1_060;
        True(deadline.IsExpired, "The handoff must time out after its active foreground budget.");
    }

    private static void HandoffPausedWaitResumesDeterministically()
    {
        long clock = 0;
        var deadline = LauncherMonotonicDeadline.Start(
            System.TimeSpan.FromMilliseconds(100),
            () => clock
        );
        var lifecycle = new LauncherOperationLifecycle();
        var signals = new ControlledHandoffSignalSource();
        var waiter = new LauncherAsyncSignalWaiter(signals, lifecycle);

        deadline.Pause();
        lifecycle.Pause();
        var wait = waiter.WaitForProcessFrameAsync(deadline);
        clock = 10_000;
        True(!wait.IsCompleted, "A paused handoff wait must not finish from wall-clock time.");

        deadline.Resume();
        lifecycle.Resume();
        signals.SignalFrame();
        Equal(
            LauncherAsyncWaitOutcome.Signaled,
            wait.GetAwaiter().GetResult(),
            "The same bounded wait must continue after onResume."
        );
    }

    private static void HandoffOfflineColdAndWarmLaunches()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, true, false);
        owner.ObserveVisibility(HandoffAttemptA, true, true);

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "A cold launch must complete normally.");
        Equal(1, overlay.DismissCalls, "A cold launch must dismiss its overlay exactly once.");

        foreach (var warmEvent in new[]
        {
            (Foreground: false, Focused: false),
            (Foreground: true, Focused: false),
            (Foreground: true, Focused: true),
        })
        {
            True(
                !owner.ObserveVisibility(
                    HandoffAttemptA,
                    warmEvent.Foreground,
                    warmEvent.Focused
                ),
                "Warm-launch lifecycle callbacks must be terminal no-ops."
            );
        }

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Warm resume must keep the game visible.");
        Equal(1, overlay.DismissCalls, "Warm resume must not resurrect or re-dismiss the overlay.");
        True(!owner.AttachOverlay(new FakeHandoffOverlay()), "Warm resume cannot attach a new overlay.");
    }

    private static void HandoffTwoConsecutiveProcessLaunches()
    {
        CompleteOfflineProcessLaunch(HandoffAttemptA, readinessFirst: true);
        CompleteOfflineProcessLaunch(HandoffAttemptB, readinessFirst: false);
    }

    private static void CompleteOfflineProcessLaunch(
        string attemptId,
        bool readinessFirst
    )
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(attemptId);
        if (readinessFirst)
        {
            owner.MarkMainMenuReady(attemptId);
            owner.ObserveVisibility(attemptId, true, true);
        }
        else
        {
            owner.ObserveVisibility(attemptId, true, true);
            owner.MarkMainMenuReady(attemptId);
        }

        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "Each process launch must complete independently.");
        Equal(1, overlay.DismissCalls, "Each process launch must dismiss exactly one overlay.");
    }

    private static void HandoffTimeoutRecoveryRejectsLateCallbacks()
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);
        owner.MarkMainMenuReady(HandoffAttemptA);
        owner.ObserveVisibility(HandoffAttemptA, true, false);

        True(owner.Fail(HandoffAttemptA), "The bounded visibility timeout must return to the launcher.");
        AssertLauncherVisible(owner, "Visibility timeout");
        Equal(1, overlay.ReturnToLauncherCalls, "Timeout recovery must restore the launcher once.");
        True(overlay.IsVisible, "Timeout recovery must leave the launcher usable.");

        True(!owner.ObserveVisibility(HandoffAttemptA, true, true), "Late focus cannot complete a timed-out attempt.");
        True(!owner.MarkMainMenuReady(HandoffAttemptA), "Late readiness cannot complete a timed-out attempt.");
        Equal(0, overlay.DismissCalls, "Late timeout callbacks cannot dismiss the restored launcher.");

        True(owner.Begin(HandoffAttemptB), "A new attempt must be available after timeout recovery.");
        owner.MarkMainMenuReady(HandoffAttemptB);
        owner.ObserveVisibility(HandoffAttemptB, true, true);
        Equal(LauncherHandoffState.GameVisible, owner.Capture().State, "The launch after timeout must complete.");
        Equal(1, overlay.DismissCalls, "Only the new active attempt may dismiss the overlay.");
    }

    private static void HandoffSimulatesReorderedEventSequences()
    {
        const int sequenceCount = 256;
        var template = new[]
        {
            SimulatedHandoffEvent.MainMenuReady,
            SimulatedHandoffEvent.ForegroundWithoutFocus,
            SimulatedHandoffEvent.ForegroundFocused,
            SimulatedHandoffEvent.Backgrounded,
            SimulatedHandoffEvent.FocusRestored,
            SimulatedHandoffEvent.RepeatedReadiness,
            SimulatedHandoffEvent.RestoreSameAttempt,
            SimulatedHandoffEvent.StaleReadiness,
            SimulatedHandoffEvent.StaleVisibility,
            SimulatedHandoffEvent.StaleFailure,
        };

        for (var sequenceIndex = 0; sequenceIndex < sequenceCount; sequenceIndex++)
        {
            var events = (SimulatedHandoffEvent[])template.Clone();
            uint random = unchecked((uint)(sequenceIndex + 1) * 0x9E3779B9u);
            for (var index = events.Length - 1; index > 0; index--)
            {
                random = NextDeterministic(random);
                var other = (int)(random % (uint)(index + 1));
                (events[index], events[other]) = (events[other], events[index]);
            }

            SimulateReorderedSequence(sequenceIndex, events);
        }
    }

    private static void SimulateReorderedSequence(
        int sequenceIndex,
        SimulatedHandoffEvent[] events
    )
    {
        var owner = new LauncherHandoffStateOwner();
        var overlay = new FakeHandoffOverlay();
        owner.AttachOverlay(overlay);
        owner.Begin(HandoffAttemptA);

        var expectedReady = false;
        var expectedForeground = false;
        var expectedFocus = false;
        var expectedState = LauncherHandoffState.HandoffPending;
        foreach (var handoffEvent in events)
        {
            ApplySimulatedEvent(owner, handoffEvent);
            if (expectedState == LauncherHandoffState.HandoffPending)
            {
                switch (handoffEvent)
                {
                    case SimulatedHandoffEvent.MainMenuReady:
                    case SimulatedHandoffEvent.RepeatedReadiness:
                        expectedReady = true;
                        break;
                    case SimulatedHandoffEvent.ForegroundWithoutFocus:
                        expectedForeground = true;
                        expectedFocus = false;
                        break;
                    case SimulatedHandoffEvent.ForegroundFocused:
                    case SimulatedHandoffEvent.FocusRestored:
                        expectedForeground = true;
                        expectedFocus = true;
                        break;
                    case SimulatedHandoffEvent.Backgrounded:
                        expectedForeground = false;
                        expectedFocus = false;
                        break;
                }

                if (expectedReady && expectedForeground && expectedFocus)
                    expectedState = LauncherHandoffState.GameVisible;
            }

            var actual = owner.Capture();
            Equal(
                expectedState,
                actual.State,
                $"Reordered sequence {sequenceIndex} diverged at {handoffEvent}."
            );
            Equal(
                expectedState == LauncherHandoffState.GameVisible ? 1 : 0,
                overlay.DismissCalls,
                $"Reordered sequence {sequenceIndex} changed overlay ownership at {handoffEvent}."
            );
            Equal(0, overlay.ReturnToLauncherCalls, "Stale sequence events cannot restore the launcher.");
        }
    }

    private static void ApplySimulatedEvent(
        LauncherHandoffStateOwner owner,
        SimulatedHandoffEvent handoffEvent
    )
    {
        switch (handoffEvent)
        {
            case SimulatedHandoffEvent.MainMenuReady:
            case SimulatedHandoffEvent.RepeatedReadiness:
                owner.MarkMainMenuReady(HandoffAttemptA);
                break;
            case SimulatedHandoffEvent.ForegroundWithoutFocus:
                owner.ObserveVisibility(HandoffAttemptA, true, false);
                break;
            case SimulatedHandoffEvent.ForegroundFocused:
            case SimulatedHandoffEvent.FocusRestored:
                owner.ObserveVisibility(HandoffAttemptA, true, true);
                break;
            case SimulatedHandoffEvent.Backgrounded:
                owner.ObserveVisibility(HandoffAttemptA, false, false);
                break;
            case SimulatedHandoffEvent.RestoreSameAttempt:
                owner.RestorePending(HandoffAttemptA);
                break;
            case SimulatedHandoffEvent.StaleReadiness:
                owner.MarkMainMenuReady(HandoffAttemptB);
                break;
            case SimulatedHandoffEvent.StaleVisibility:
                owner.ObserveVisibility(HandoffAttemptB, true, true);
                break;
            case SimulatedHandoffEvent.StaleFailure:
                owner.Fail(HandoffAttemptB);
                break;
        }
    }

    private static uint NextDeterministic(uint value)
    {
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        return value;
    }

    private enum SimulatedHandoffEvent
    {
        MainMenuReady,
        ForegroundWithoutFocus,
        ForegroundFocused,
        Backgrounded,
        FocusRestored,
        RepeatedReadiness,
        RestoreSameAttempt,
        StaleReadiness,
        StaleVisibility,
        StaleFailure,
    }

    private static void AssertLauncherVisible(LauncherHandoffStateOwner owner, string eventName)
    {
        var snapshot = owner.Capture();
        Equal(
            LauncherHandoffState.LauncherVisible,
            snapshot.State,
            $"{eventName} must make the launcher authoritative."
        );
        True(snapshot.AttemptId == null, $"{eventName} must clear the active attempt ID.");
        True(!snapshot.MainMenuReady, $"{eventName} must clear readiness.");
        True(!snapshot.ActivityForeground, $"{eventName} must clear foreground state.");
        True(!snapshot.WindowFocused, $"{eventName} must clear focus state.");
    }

    private sealed class FakeHandoffOverlay : ILauncherHandoffOverlay
    {
        public bool IsAvailable { get; set; } = true;
        public bool IsVisible { get; private set; } = true;
        internal int DismissCalls { get; private set; }
        internal int ReturnToLauncherCalls { get; private set; }

        public bool Dismiss()
        {
            DismissCalls++;
            IsVisible = false;
            return true;
        }

        public bool ReturnToLauncher(string attemptId)
        {
            ReturnToLauncherCalls++;
            IsVisible = true;
            return true;
        }
    }

    private sealed class ControlledHandoffSignalSource : ILauncherAsyncSignalSource
    {
        private readonly TaskCompletionSource<bool> _frame = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        internal void SignalFrame() => _frame.TrySetResult(true);

        public Task WaitForProcessFrameAsync() => _frame.Task;

        public Task WaitForFramePostDrawAsync() => _frame.Task;

        public Task WaitForDelayAsync(
            int milliseconds,
            CancellationToken cancellationToken
        )
        {
            var delay = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            cancellationToken.Register(
                () => delay.TrySetCanceled(cancellationToken)
            );
            return delay.Task;
        }
    }
}
