package com.game.sts2launcher;

final class AndroidBootSequence {
	static final long FULL_DURATION_MS = 4_000L;
	static final long REDUCED_MOTION_DURATION_MS = 200L;
	static final float REGISTRATION_OFFSET_DP = 18.0f;

	enum Phase {
		REGISTRATION("registration", 0L, 450L),
		SEPARATION("separation", 450L, 700L),
		IDENTITY_REVEAL("identity-reveal", 700L, 1_500L),
		FINAL_SETTLE("final-settle", 1_500L, 1_850L),
		CONFIRMATION_HOLD("confirmation-hold", 1_850L, 3_050L),
		IDENTITY_PULSE("identity-pulse", 3_050L, 3_300L),
		LAUNCHER_HANDOFF("launcher-handoff", 3_300L, FULL_DURATION_MS);

		private final String timelineName;
		private final long startMs;
		private final long endMs;

		Phase(String timelineName, long startMs, long endMs) {
			this.timelineName = timelineName;
			this.startMs = startMs;
			this.endMs = endMs;
		}

		String timelineName() {
			return timelineName;
		}

		long startMs() {
			return startMs;
		}

		long endMs() {
			return endMs;
		}

		long durationMs() {
			return endMs - startMs;
		}
	}

	static final class Frame {
		private final Phase phase;
		private final float godotOpacity;
		private final float godotScale;
		private final float identityOpacity;
		private final float identityScale;
		private final float cyanOpacity;
		private final float orangeOpacity;
		private final float resolvedOpacity;
		private final float wordmarkOpacity;
		private final float cyanOffsetX;
		private final float cyanOffsetY;
		private final float orangeOffsetX;
		private final float orangeOffsetY;
		private final float revealProgress;
		private final float pulseStrength;
		private final float overlayOpacity;

		private Frame(
			Phase phase,
			float godotOpacity,
			float godotScale,
			float identityOpacity,
			float identityScale,
			float cyanOpacity,
			float orangeOpacity,
			float resolvedOpacity,
			float wordmarkOpacity,
			float cyanOffsetX,
			float cyanOffsetY,
			float orangeOffsetX,
			float orangeOffsetY,
			float revealProgress,
			float pulseStrength,
			float overlayOpacity
		) {
			this.phase = phase;
			this.godotOpacity = godotOpacity;
			this.godotScale = godotScale;
			this.identityOpacity = identityOpacity;
			this.identityScale = identityScale;
			this.cyanOpacity = cyanOpacity;
			this.orangeOpacity = orangeOpacity;
			this.resolvedOpacity = resolvedOpacity;
			this.wordmarkOpacity = wordmarkOpacity;
			this.cyanOffsetX = cyanOffsetX;
			this.cyanOffsetY = cyanOffsetY;
			this.orangeOffsetX = orangeOffsetX;
			this.orangeOffsetY = orangeOffsetY;
			this.revealProgress = revealProgress;
			this.pulseStrength = pulseStrength;
			this.overlayOpacity = overlayOpacity;
		}

		Phase phase() {
			return phase;
		}

		float godotOpacity() {
			return godotOpacity;
		}

		float godotScale() {
			return godotScale;
		}

		float identityOpacity() {
			return identityOpacity;
		}

		float identityScale() {
			return identityScale;
		}

		float cyanOpacity() {
			return cyanOpacity;
		}

		float orangeOpacity() {
			return orangeOpacity;
		}

		float resolvedOpacity() {
			return resolvedOpacity;
		}

		float wordmarkOpacity() {
			return wordmarkOpacity;
		}

		float cyanOffsetX() {
			return cyanOffsetX;
		}

		float cyanOffsetY() {
			return cyanOffsetY;
		}

		float orangeOffsetX() {
			return orangeOffsetX;
		}

		float orangeOffsetY() {
			return orangeOffsetY;
		}

		float revealProgress() {
			return revealProgress;
		}

		float pulseStrength() {
			return pulseStrength;
		}

		float overlayOpacity() {
			return overlayOpacity;
		}
	}

	private AndroidBootSequence() {
	}

	static Phase phaseAt(long elapsedMs) {
		long clamped = clampTime(elapsedMs, FULL_DURATION_MS);
		for (Phase phase : Phase.values()) {
			if (clamped < phase.endMs() || phase == Phase.LAUNCHER_HANDOFF) {
				return phase;
			}
		}
		return Phase.LAUNCHER_HANDOFF;
	}

	static Frame frameAt(long elapsedMs) {
		long clamped = clampTime(elapsedMs, FULL_DURATION_MS);
		Phase phase = phaseAt(clamped);
		float progress = progress(clamped, phase);
		switch (phase) {
			case REGISTRATION:
				return registrationFrame(phase, progress);
			case SEPARATION:
				return separationFrame(phase);
			case IDENTITY_REVEAL:
				return identityRevealFrame(phase, progress);
			case FINAL_SETTLE:
				return finalSettleFrame(phase, progress);
			case CONFIRMATION_HOLD:
				return resolvedFrame(phase, 0.0f, 1.0f, 1.0f);
			case IDENTITY_PULSE:
				return identityPulseFrame(phase, progress);
			case LAUNCHER_HANDOFF:
				return launcherHandoffFrame(phase, progress);
			default:
				throw new IllegalStateException("Unhandled boot phase: " + phase);
		}
	}

	static float reducedMotionOverlayOpacity(long elapsedMs) {
		float progress = clampTime(elapsedMs, REDUCED_MOTION_DURATION_MS)
			/ (float) REDUCED_MOTION_DURATION_MS;
		return 1.0f - progress;
	}

	private static Frame registrationFrame(Phase phase, float progress) {
		float eased = smoothStep(progress);
		float registrationVisibility = (float) Math.sin(Math.PI * progress) * 0.76f;
		float offset = lerp(1.16f, 0.42f, easeOutCubic(progress));
		return new Frame(
			phase,
			1.0f - eased,
			lerp(1.0f, 0.84f, easeOutCubic(progress)),
			registrationVisibility,
			lerp(0.94f, 0.99f, eased),
			1.0f,
			1.0f,
			0.0f,
			0.0f,
			-offset,
			0.16f * offset,
			offset,
			-0.16f * offset,
			1.0f,
			0.0f,
			1.0f
		);
	}

	private static Frame separationFrame(Phase phase) {
		return new Frame(
			phase,
			0.0f,
			0.84f,
			0.0f,
			0.82f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			1.0f
		);
	}

	private static Frame identityRevealFrame(Phase phase, float progress) {
		float registration = 1.0f - smoothStep(unit(progress / 0.78f));
		float colourOpacity = 1.0f - smoothStep(unit((progress - 0.58f) / 0.42f));
		float resolvedOpacity = smoothStep(unit((progress - 0.36f) / 0.64f));
		return new Frame(
			phase,
			0.0f,
			0.84f,
			smoothStep(progress),
			lerp(0.82f, 1.035f, easeOutCubic(progress)),
			colourOpacity,
			colourOpacity,
			resolvedOpacity,
			0.0f,
			-registration,
			0.14f * registration,
			registration,
			-0.14f * registration,
			smoothStep(progress),
			0.0f,
			1.0f
		);
	}

	private static Frame finalSettleFrame(Phase phase, float progress) {
		float identityScale = progress < 0.62f
			? lerp(1.035f, 0.992f, easeOutCubic(progress / 0.62f))
			: lerp(0.992f, 1.0f, smoothStep((progress - 0.62f) / 0.38f));
		return resolvedFrame(
			phase,
			0.0f,
			identityScale,
			smoothStep(unit((progress - 0.24f) / 0.76f))
		);
	}

	private static Frame identityPulseFrame(Phase phase, float progress) {
		float pulse = (float) Math.sin(Math.PI * progress);
		return resolvedFrame(phase, pulse, 1.0f + (pulse * 0.014f), 1.0f);
	}

	private static Frame launcherHandoffFrame(Phase phase, float progress) {
		float eased = smoothStep(progress);
		Frame resolved = resolvedFrame(
			phase,
			0.0f,
			lerp(1.0f, 0.992f, eased),
			1.0f
		);
		return new Frame(
			phase,
			resolved.godotOpacity(),
			resolved.godotScale(),
			lerp(1.0f, 0.82f, eased),
			resolved.identityScale(),
			resolved.cyanOpacity(),
			resolved.orangeOpacity(),
			resolved.resolvedOpacity(),
			resolved.wordmarkOpacity(),
			resolved.cyanOffsetX(),
			resolved.cyanOffsetY(),
			resolved.orangeOffsetX(),
			resolved.orangeOffsetY(),
			resolved.revealProgress(),
			resolved.pulseStrength(),
			1.0f - eased
		);
	}

	private static Frame resolvedFrame(
		Phase phase,
		float pulseStrength,
		float identityScale,
		float wordmarkOpacity
	) {
		return new Frame(
			phase,
			0.0f,
			0.84f,
			1.0f,
			identityScale,
			0.0f,
			0.0f,
			1.0f,
			wordmarkOpacity,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			1.0f,
			pulseStrength,
			1.0f
		);
	}

	private static float progress(long elapsedMs, Phase phase) {
		return (elapsedMs - phase.startMs()) / (float) phase.durationMs();
	}

	private static long clampTime(long value, long maximum) {
		return Math.max(0L, Math.min(maximum, value));
	}

	private static float unit(float value) {
		return Math.max(0.0f, Math.min(1.0f, value));
	}

	private static float smoothStep(float value) {
		float normalized = unit(value);
		return normalized * normalized * (3.0f - (2.0f * normalized));
	}

	private static float easeOutCubic(float value) {
		float inverse = 1.0f - unit(value);
		return 1.0f - (inverse * inverse * inverse);
	}

	private static float lerp(float from, float to, float progress) {
		return from + ((to - from) * unit(progress));
	}
}
