package com.game.sts2launcher;

import java.util.Objects;

enum AndroidBootTransitionCue {
	REGISTRATION(AndroidBootSequence.Phase.REGISTRATION),
	SEPARATION(AndroidBootSequence.Phase.SEPARATION),
	IDENTITY_REVEAL(AndroidBootSequence.Phase.IDENTITY_REVEAL),
	FINAL_SETTLE(AndroidBootSequence.Phase.FINAL_SETTLE),
	CONFIRMATION_HOLD(AndroidBootSequence.Phase.CONFIRMATION_HOLD),
	IDENTITY_PULSE(AndroidBootSequence.Phase.IDENTITY_PULSE),
	LAUNCHER_HANDOFF(AndroidBootSequence.Phase.LAUNCHER_HANDOFF);

	private final AndroidBootSequence.Phase phase;

	AndroidBootTransitionCue(AndroidBootSequence.Phase phase) {
		this.phase = phase;
	}

	AndroidBootSequence.Phase phase() {
		return phase;
	}

	long timelineMs() {
		return phase.startMs();
	}

	static AndroidBootTransitionCue fromPhase(AndroidBootSequence.Phase phase) {
		Objects.requireNonNull(phase, "phase");
		switch (phase) {
			case REGISTRATION:
				return REGISTRATION;
			case SEPARATION:
				return SEPARATION;
			case IDENTITY_REVEAL:
				return IDENTITY_REVEAL;
			case FINAL_SETTLE:
				return FINAL_SETTLE;
			case CONFIRMATION_HOLD:
				return CONFIRMATION_HOLD;
			case IDENTITY_PULSE:
				return IDENTITY_PULSE;
			case LAUNCHER_HANDOFF:
				return LAUNCHER_HANDOFF;
			default:
				throw new IllegalArgumentException("Unsupported boot transition phase: " + phase);
		}
	}
}
