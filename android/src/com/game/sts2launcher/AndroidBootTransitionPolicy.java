package com.game.sts2launcher;

final class AndroidBootTransitionPolicy {
	static final String SKIP_INTENT_EXTRA = "sts2_skip_boot_transition";

	private AndroidBootTransitionPolicy() {
	}

	static Decision resolve(
		boolean pendingNormalLaunch,
		boolean pendingSafeLaunch,
		boolean explicitSkip,
		boolean alreadyConsumed
	) {
		if (pendingSafeLaunch) {
			return Decision.skip("safe-start");
		}
		if (pendingNormalLaunch) {
			return Decision.skip("pending-game-launch");
		}
		if (explicitSkip) {
			return Decision.skip("explicit-restart-handoff");
		}
		if (alreadyConsumed) {
			return Decision.skip("already-consumed-in-process");
		}
		return Decision.play();
	}

	static boolean isReducedMotion(float animatorDurationScale, boolean animatorsEnabled) {
		return !animatorsEnabled || animatorDurationScale <= 0.0f;
	}

	static final class Decision {
		private final boolean shouldPlay;
		private final String reason;

		private Decision(boolean shouldPlay, String reason) {
			this.shouldPlay = shouldPlay;
			this.reason = reason;
		}

		static Decision play() {
			return new Decision(true, "cold-launch");
		}

		static Decision skip(String reason) {
			return new Decision(false, reason);
		}

		boolean shouldPlay() {
			return shouldPlay;
		}

		String reason() {
			return reason;
		}
	}

	static final class ReadinessGate {
		private boolean overlayAttached;
		private boolean launcherReady;
		private boolean started;
		private boolean terminal;

		Event onOverlayAttached() {
			if (terminal) {
				return Event.NONE;
		}
			overlayAttached = true;
			return startIfReady();
		}

		Event onLauncherReady() {
			if (terminal) {
				return Event.NONE;
		}
			launcherReady = true;
			return startIfReady();
		}

		Event onCompleted() {
			if (terminal) {
				return Event.NONE;
			}
			terminal = true;
			return Event.COMPLETED;
		}

		Event onTimeout() {
			if (terminal) {
				return Event.NONE;
			}
			terminal = true;
			return Event.TIMED_OUT;
		}

		boolean isTerminal() {
			return terminal;
		}

		private Event startIfReady() {
			if (!started && overlayAttached && launcherReady) {
				started = true;
				return Event.START;
			}
			return Event.NONE;
		}
	}

	enum Event {
		NONE,
		START,
		COMPLETED,
		TIMED_OUT
	}
}
