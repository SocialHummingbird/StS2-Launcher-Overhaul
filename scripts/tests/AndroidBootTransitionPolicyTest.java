package com.game.sts2launcher;

public final class AndroidBootTransitionPolicyTest {
	public static void main(String[] args) {
		assertDecision("cold start", true, "cold-launch", false, false, false, false);
		assertDecision("second activity", false, "already-consumed-in-process", false, false, false, true);
		assertDecision("normal game restart", false, "pending-game-launch", true, false, true, false);
		assertDecision("safe start", false, "safe-start", false, true, true, false);
		assertDecision("return to launcher", false, "explicit-restart-handoff", false, false, true, false);
		assertDecision("safe start has highest priority", false, "safe-start", true, true, true, true);
		assertDecision("pending game beats restart and consumption", false, "pending-game-launch", true, false, true, true);
		assertDecision("restart handoff beats consumption", false, "explicit-restart-handoff", false, false, true, true);

		assertTrue(
			"disabled platform animators use reduced motion",
			AndroidBootTransitionPolicy.isReducedMotion(1.0f, false)
		);
		assertTrue(
			"zero animator scale uses reduced motion",
			AndroidBootTransitionPolicy.isReducedMotion(0.0f, true)
		);
		assertFalse(
			"normal animator settings use full sequence",
			AndroidBootTransitionPolicy.isReducedMotion(1.0f, true)
		);

		AndroidBootTransitionPolicy.ReadinessGate early =
			new AndroidBootTransitionPolicy.ReadinessGate();
		assertFalse("new readiness gate is not terminal", early.isTerminal());
		assertEvent("early readiness cached", AndroidBootTransitionPolicy.Event.NONE, early.onLauncherReady());
		assertEvent("duplicate early readiness ignored", AndroidBootTransitionPolicy.Event.NONE, early.onLauncherReady());
		assertEvent("early readiness starts on attach", AndroidBootTransitionPolicy.Event.START, early.onOverlayAttached());
		assertEvent("duplicate overlay attach ignored", AndroidBootTransitionPolicy.Event.NONE, early.onOverlayAttached());
		assertEvent("duplicate readiness ignored", AndroidBootTransitionPolicy.Event.NONE, early.onLauncherReady());
		assertEvent("completion accepted", AndroidBootTransitionPolicy.Event.COMPLETED, early.onCompleted());
		assertTrue("completion marks readiness gate terminal", early.isTerminal());
		assertEvent("timeout ignored after completion", AndroidBootTransitionPolicy.Event.NONE, early.onTimeout());

		AndroidBootTransitionPolicy.ReadinessGate late =
			new AndroidBootTransitionPolicy.ReadinessGate();
		assertEvent("overlay can attach first", AndroidBootTransitionPolicy.Event.NONE, late.onOverlayAttached());
		assertEvent("late readiness starts", AndroidBootTransitionPolicy.Event.START, late.onLauncherReady());

		AndroidBootTransitionPolicy.ReadinessGate timeout =
			new AndroidBootTransitionPolicy.ReadinessGate();
		assertEvent("timeout terminates", AndroidBootTransitionPolicy.Event.TIMED_OUT, timeout.onTimeout());
		assertTrue("timeout marks readiness gate terminal", timeout.isTerminal());
		assertEvent("duplicate timeout ignored", AndroidBootTransitionPolicy.Event.NONE, timeout.onTimeout());
		assertEvent("readiness ignored after timeout", AndroidBootTransitionPolicy.Event.NONE, timeout.onLauncherReady());
		assertEvent("attach ignored after timeout", AndroidBootTransitionPolicy.Event.NONE, timeout.onOverlayAttached());

		AndroidBootTransitionPolicy.ReadinessGate completionBeforeStart =
			new AndroidBootTransitionPolicy.ReadinessGate();
		assertEvent(
			"completion before start terminates",
			AndroidBootTransitionPolicy.Event.COMPLETED,
			completionBeforeStart.onCompleted()
		);
		assertEvent(
			"completion prevents later start",
			AndroidBootTransitionPolicy.Event.NONE,
			completionBeforeStart.onOverlayAttached()
		);
		assertEvent(
			"completion ignores later readiness",
			AndroidBootTransitionPolicy.Event.NONE,
			completionBeforeStart.onLauncherReady()
		);

		System.out.println("Android boot transition policy tests passed.");
	}

	private static void assertDecision(
		String label,
		boolean shouldPlay,
		String reason,
		boolean pendingNormal,
		boolean pendingSafe,
		boolean explicitSkip,
		boolean alreadyConsumed
	) {
		AndroidBootTransitionPolicy.Decision decision = AndroidBootTransitionPolicy.resolve(
			pendingNormal,
			pendingSafe,
			explicitSkip,
			alreadyConsumed
		);
		if (decision.shouldPlay() != shouldPlay) {
			throw new AssertionError(label + ": expected shouldPlay=" + shouldPlay);
		}
		if (!reason.equals(decision.reason())) {
			throw new AssertionError(
				label + ": expected reason=" + reason + " actual=" + decision.reason()
			);
		}
	}

	private static void assertEvent(
		String label,
		AndroidBootTransitionPolicy.Event expected,
		AndroidBootTransitionPolicy.Event actual
	) {
		if (expected != actual) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertTrue(String label, boolean value) {
		if (!value) {
			throw new AssertionError(label + ": expected true");
		}
	}

	private static void assertFalse(String label, boolean value) {
		if (value) {
			throw new AssertionError(label + ": expected false");
		}
	}
}
