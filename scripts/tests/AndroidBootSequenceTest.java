package com.game.sts2launcher;

public final class AndroidBootSequenceTest {
	public static void main(String[] args) {
		assertPhaseTiming();
		assertPhaseFrames();
		assertReducedMotionDirectFade();
		assertEveryFrameIsValid();
		System.out.println("Android boot sequence tests passed.");
	}

	private static void assertPhaseTiming() {
		assertLong("full duration", 4_000L, AndroidBootSequence.FULL_DURATION_MS);
		assertLong("reduced-motion duration", 200L, AndroidBootSequence.REDUCED_MOTION_DURATION_MS);

		AndroidBootSequence.Phase[] phases = AndroidBootSequence.Phase.values();
		long expectedStart = 0L;
		for (AndroidBootSequence.Phase phase : phases) {
			assertLong(phase + " contiguous start", expectedStart, phase.startMs());
			if (phase.durationMs() <= 0L) {
				throw new AssertionError(phase + " must have a positive duration");
			}
			expectedStart = phase.endMs();
		}
		assertLong("phases end at full duration", AndroidBootSequence.FULL_DURATION_MS, expectedStart);

		assertPhase("registration start", AndroidBootSequence.Phase.REGISTRATION, 0L);
		assertPhase("registration end exclusive", AndroidBootSequence.Phase.REGISTRATION, 449L);
		assertPhase("separation start", AndroidBootSequence.Phase.SEPARATION, 450L);
		assertPhase("identity reveal start", AndroidBootSequence.Phase.IDENTITY_REVEAL, 700L);
		assertPhase("final settle start", AndroidBootSequence.Phase.FINAL_SETTLE, 1_500L);
		assertPhase("confirmation hold start", AndroidBootSequence.Phase.CONFIRMATION_HOLD, 1_850L);
		assertPhase("pulse start", AndroidBootSequence.Phase.IDENTITY_PULSE, 3_050L);
		assertPhase("handoff start", AndroidBootSequence.Phase.LAUNCHER_HANDOFF, 3_300L);
		assertPhase("handoff completion", AndroidBootSequence.Phase.LAUNCHER_HANDOFF, 4_000L);
	}

	private static void assertPhaseFrames() {
		AndroidBootSequence.Frame start = AndroidBootSequence.frameAt(0L);
		assertFloat("start Godot opacity", 1.0f, start.godotOpacity());
		assertFloat("start Godot scale", 1.0f, start.godotScale());
		assertFloat("start identity hidden", 0.0f, start.identityOpacity());
		assertFloat("start overlay opaque", 1.0f, start.overlayOpacity());

		AndroidBootSequence.Frame registration = AndroidBootSequence.frameAt(225L);
		assertBetween("registration Godot fade", registration.godotOpacity(), 0.0f, 1.0f);
		assertBetween("registration identity visibility", registration.identityOpacity(), 0.0f, 1.0f);
		assertPositive("registration cyan", registration.cyanOpacity());
		assertPositive("registration orange", registration.orangeOpacity());
		assertNegative("registration cyan offset", registration.cyanOffsetX());
		assertPositive("registration orange offset", registration.orangeOffsetX());
		assertFloat("registration unresolved", 0.0f, registration.resolvedOpacity());
		assertFloat("registration wordmark hidden", 0.0f, registration.wordmarkOpacity());

		AndroidBootSequence.Frame separation = AndroidBootSequence.frameAt(575L);
		assertFloat("separation Godot hidden", 0.0f, separation.godotOpacity());
		assertFloat("separation identity hidden", 0.0f, separation.identityOpacity());
		assertFloat("separation overlay remains opaque", 1.0f, separation.overlayOpacity());

		AndroidBootSequence.Frame revealStart = AndroidBootSequence.frameAt(700L);
		AndroidBootSequence.Frame revealMiddle = AndroidBootSequence.frameAt(1_100L);
		AndroidBootSequence.Frame revealEnd = AndroidBootSequence.frameAt(1_499L);
		assertFloat("reveal begins clipped", 0.0f, revealStart.revealProgress());
		assertBetween("reveal progresses", revealMiddle.revealProgress(), 0.0f, 1.0f);
		assertGreater("reveal identity grows", revealMiddle.identityOpacity(), revealStart.identityOpacity());
		assertLess("cyan offset converges", Math.abs(revealEnd.cyanOffsetX()), Math.abs(revealMiddle.cyanOffsetX()));
		assertLess("orange offset converges", Math.abs(revealEnd.orangeOffsetX()), Math.abs(revealMiddle.orangeOffsetX()));
		assertGreater("resolved mark forms", revealEnd.resolvedOpacity(), revealMiddle.resolvedOpacity());
		assertFloat("wordmark waits for settle", 0.0f, revealEnd.wordmarkOpacity());

		AndroidBootSequence.Frame settleStart = AndroidBootSequence.frameAt(1_500L);
		AndroidBootSequence.Frame settleMiddle = AndroidBootSequence.frameAt(1_675L);
		AndroidBootSequence.Frame compression = AndroidBootSequence.frameAt(1_717L);
		AndroidBootSequence.Frame settled = AndroidBootSequence.frameAt(1_850L);
		assertFloat("settle starts overscale", 1.035f, settleStart.identityScale());
		assertBetween("wordmark reveals during settle", settleMiddle.wordmarkOpacity(), 0.0f, 1.0f);
		assertLess("settle compresses below rest", compression.identityScale(), 1.0f);
		assertFloat("settle resolves scale", 1.0f, settled.identityScale());
		assertFloat("settle resolves wordmark", 1.0f, settled.wordmarkOpacity());
		assertFloat("settle resolves mark", 1.0f, settled.resolvedOpacity());

		AndroidBootSequence.Frame holdMiddle = AndroidBootSequence.frameAt(2_450L);
		assertResolved("confirmation hold", holdMiddle);

		AndroidBootSequence.Frame pulseStart = AndroidBootSequence.frameAt(3_050L);
		AndroidBootSequence.Frame pulsePeak = AndroidBootSequence.frameAt(3_175L);
		AndroidBootSequence.Frame pulseEnd = AndroidBootSequence.frameAt(3_300L);
		assertFloat("pulse begins at rest", 0.0f, pulseStart.pulseStrength());
		assertFloat("pulse reaches one restrained peak", 1.0f, pulsePeak.pulseStrength());
		assertFloat("pulse returns to rest", 0.0f, pulseEnd.pulseStrength());
		assertGreater("pulse scales identity", pulsePeak.identityScale(), 1.0f);
		assertGreater("pulse has tactile weight", pulsePeak.identityScale(), 1.01f);

		AndroidBootSequence.Frame handoffMiddle = AndroidBootSequence.frameAt(3_650L);
		AndroidBootSequence.Frame handoffEnd = AndroidBootSequence.frameAt(4_000L);
		assertBetween("handoff reveals launcher", handoffMiddle.overlayOpacity(), 0.0f, 1.0f);
		assertLess("handoff eases identity back", handoffMiddle.identityScale(), 1.0f);
		assertFloat("handoff ends transparent", 0.0f, handoffEnd.overlayOpacity());
	}

	private static void assertReducedMotionDirectFade() {
		assertFloat("reduced fade starts opaque", 1.0f, AndroidBootSequence.reducedMotionOverlayOpacity(0L));
		assertFloat("reduced fade midpoint", 0.5f, AndroidBootSequence.reducedMotionOverlayOpacity(100L));
		assertFloat("reduced fade ends transparent", 0.0f, AndroidBootSequence.reducedMotionOverlayOpacity(200L));
		assertFloat("reduced fade clamps before start", 1.0f, AndroidBootSequence.reducedMotionOverlayOpacity(-1L));
		assertFloat("reduced fade clamps after end", 0.0f, AndroidBootSequence.reducedMotionOverlayOpacity(201L));
		float previous = 1.0f;
		for (long elapsed = 1L; elapsed <= AndroidBootSequence.REDUCED_MOTION_DURATION_MS; elapsed++) {
			float opacity = AndroidBootSequence.reducedMotionOverlayOpacity(elapsed);
			if (opacity >= previous) {
				throw new AssertionError("reduced-motion fade must be direct and strictly decreasing");
			}
			previous = opacity;
		}
	}

	private static void assertEveryFrameIsValid() {
		for (long elapsed = 0L; elapsed <= AndroidBootSequence.FULL_DURATION_MS; elapsed++) {
			AndroidBootSequence.Frame frame = AndroidBootSequence.frameAt(elapsed);
			assertUnit("Godot opacity at " + elapsed, frame.godotOpacity());
			assertScale("Godot scale at " + elapsed, frame.godotScale());
			assertUnit("identity opacity at " + elapsed, frame.identityOpacity());
			assertScale("identity scale at " + elapsed, frame.identityScale());
			assertUnit("cyan opacity at " + elapsed, frame.cyanOpacity());
			assertUnit("orange opacity at " + elapsed, frame.orangeOpacity());
			assertUnit("resolved opacity at " + elapsed, frame.resolvedOpacity());
			assertUnit("wordmark opacity at " + elapsed, frame.wordmarkOpacity());
			assertFinite("cyan x at " + elapsed, frame.cyanOffsetX());
			assertFinite("cyan y at " + elapsed, frame.cyanOffsetY());
			assertFinite("orange x at " + elapsed, frame.orangeOffsetX());
			assertFinite("orange y at " + elapsed, frame.orangeOffsetY());
			assertUnit("reveal at " + elapsed, frame.revealProgress());
			assertUnit("pulse at " + elapsed, frame.pulseStrength());
			assertUnit("overlay opacity at " + elapsed, frame.overlayOpacity());
		}
	}

	private static void assertResolved(String label, AndroidBootSequence.Frame frame) {
		assertFloat(label + " identity opacity", 1.0f, frame.identityOpacity());
		assertFloat(label + " identity scale", 1.0f, frame.identityScale());
		assertFloat(label + " resolved opacity", 1.0f, frame.resolvedOpacity());
		assertFloat(label + " wordmark opacity", 1.0f, frame.wordmarkOpacity());
		assertFloat(label + " pulse", 0.0f, frame.pulseStrength());
		assertFloat(label + " overlay opacity", 1.0f, frame.overlayOpacity());
	}

	private static void assertPhase(String label, AndroidBootSequence.Phase expected, long elapsedMs) {
		AndroidBootSequence.Phase actual = AndroidBootSequence.phaseAt(elapsedMs);
		if (actual != expected) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertUnit(String label, float value) {
		assertBetweenInclusive(label, value, 0.0f, 1.0f);
	}

	private static void assertScale(String label, float value) {
		assertBetweenInclusive(label, value, 0.0f, 2.0f);
	}

	private static void assertBetween(String label, float value, float minimum, float maximum) {
		if (!(value > minimum && value < maximum)) {
			throw new AssertionError(label + ": expected between " + minimum + " and " + maximum + ", actual=" + value);
		}
	}

	private static void assertBetweenInclusive(String label, float value, float minimum, float maximum) {
		assertFinite(label, value);
		if (value < minimum || value > maximum) {
			throw new AssertionError(label + ": expected in [" + minimum + ", " + maximum + "], actual=" + value);
		}
	}

	private static void assertPositive(String label, float value) {
		if (!(value > 0.0f)) {
			throw new AssertionError(label + ": expected positive, actual=" + value);
		}
	}

	private static void assertNegative(String label, float value) {
		if (!(value < 0.0f)) {
			throw new AssertionError(label + ": expected negative, actual=" + value);
		}
	}

	private static void assertGreater(String label, float first, float second) {
		if (!(first > second)) {
			throw new AssertionError(label + ": expected " + first + " > " + second);
		}
	}

	private static void assertLess(String label, float first, float second) {
		if (!(first < second)) {
			throw new AssertionError(label + ": expected " + first + " < " + second);
		}
	}

	private static void assertFinite(String label, float value) {
		if (!Float.isFinite(value)) {
			throw new AssertionError(label + ": expected finite value, actual=" + value);
		}
	}

	private static void assertFloat(String label, float expected, float actual) {
		if (Float.compare(expected, actual) != 0) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertLong(String label, long expected, long actual) {
		if (expected != actual) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}
}
