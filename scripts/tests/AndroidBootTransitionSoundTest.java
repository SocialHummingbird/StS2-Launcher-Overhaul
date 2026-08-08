package com.game.sts2launcher;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public final class AndroidBootTransitionSoundTest {
	public static void main(String[] args) {
		assertCueMapping();
		assertCueOrder();
		assertDuplicateAndBackwardSuppression();
		assertDisabledSession("skipped transition");
		assertDisabledSession("reduced-motion transition");
		assertSilentImplementation();
		assertPauseBeforeCue();
		assertPauseResumeDropsPausedCues();
		assertDuplicatePauseResumeSuppression();
		assertCloseWhilePaused();
		assertIdempotentCleanup();
		assertSoundFailureCannotEscape();
		assertLifecycleFailureCannotEscape();
		System.out.println("Android boot transition sound tests passed.");
	}

	private static void assertCueMapping() {
		AndroidBootSequence.Phase[] phases = AndroidBootSequence.Phase.values();
		AndroidBootTransitionCue[] cues = AndroidBootTransitionCue.values();
		assertEquals("one cue per phase", phases.length, cues.length);
		for (int index = 0; index < phases.length; index++) {
			AndroidBootSequence.Phase phase = phases[index];
			AndroidBootTransitionCue cue = cues[index];
			assertSame("cue phase " + phase, phase, cue.phase());
			assertSame("phase mapping " + phase, cue, AndroidBootTransitionCue.fromPhase(phase));
			assertLong("cue timeline " + cue, phase.startMs(), cue.timelineMs());
		}
	}

	private static void assertCueOrder() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		for (long elapsed = 0L; elapsed <= AndroidBootSequence.FULL_DURATION_MS; elapsed++) {
			session.enterPhase(AndroidBootSequence.frameAt(elapsed).phase());
		}
		assertListEquals(
			"cue order",
			Arrays.asList(AndroidBootTransitionCue.values()),
			sound.cues
		);
	}

	private static void assertDuplicateAndBackwardSuppression() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.enterPhase(AndroidBootSequence.Phase.SEPARATION);
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.enterPhase(AndroidBootSequence.Phase.SEPARATION);
		session.enterPhase(AndroidBootSequence.Phase.IDENTITY_REVEAL);
		assertListEquals(
			"duplicates and backwards phases suppressed",
			Arrays.asList(
				AndroidBootTransitionCue.REGISTRATION,
				AndroidBootTransitionCue.SEPARATION,
				AndroidBootTransitionCue.IDENTITY_REVEAL
			),
			sound.cues
		);
	}

	private static void assertDisabledSession(String label) {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, false);
		for (AndroidBootSequence.Phase phase : AndroidBootSequence.Phase.values()) {
			session.enterPhase(phase);
		}
		session.pause();
		session.resume();
		session.close();
		session.close();
		assertEquals(label + " emits no staged cues", 0, sound.cues.size());
		assertEquals(label + " never pauses sound", 0, sound.pauseCount);
		assertEquals(label + " never resumes sound", 0, sound.resumeCount);
		assertEquals(label + " closes once", 1, sound.closeCount);
	}

	private static void assertSilentImplementation() {
		SilentBootTransitionSound sound = new SilentBootTransitionSound();
		for (AndroidBootTransitionCue cue : AndroidBootTransitionCue.values()) {
			sound.play(cue);
		}
		sound.pause();
		sound.resume();
		sound.close();
		sound.close();
	}

	private static void assertPauseBeforeCue() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.pause();
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.resume();
		session.enterPhase(AndroidBootSequence.Phase.SEPARATION);
		assertListEquals(
			"pause before cue drops stale phase",
			Arrays.asList(AndroidBootTransitionCue.SEPARATION),
			sound.cues
		);
		assertEquals("pause before cue pauses once", 1, sound.pauseCount);
		assertEquals("pause before cue resumes once", 1, sound.resumeCount);
	}

	private static void assertPauseResumeDropsPausedCues() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.pause();
		session.enterPhase(AndroidBootSequence.Phase.SEPARATION);
		session.enterPhase(AndroidBootSequence.Phase.IDENTITY_REVEAL);
		session.resume();
		session.enterPhase(AndroidBootSequence.Phase.FINAL_SETTLE);
		assertListEquals(
			"paused cues are not replayed after resume",
			Arrays.asList(
				AndroidBootTransitionCue.REGISTRATION,
				AndroidBootTransitionCue.FINAL_SETTLE
			),
			sound.cues
		);
	}

	private static void assertDuplicatePauseResumeSuppression() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.pause();
		session.pause();
		session.resume();
		session.resume();
		assertEquals("duplicate pause suppressed", 1, sound.pauseCount);
		assertEquals("duplicate resume suppressed", 1, sound.resumeCount);
	}

	private static void assertCloseWhilePaused() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.pause();
		session.close();
		session.close();
		session.resume();
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		assertEquals("close while paused pauses once", 1, sound.pauseCount);
		assertEquals("close while paused never resumes", 0, sound.resumeCount);
		assertEquals("close while paused closes once", 1, sound.closeCount);
		assertEquals("close while paused emits no cues", 0, sound.cues.size());
	}

	private static void assertIdempotentCleanup() {
		RecordingSound sound = new RecordingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.close();
		session.close();
		session.enterPhase(AndroidBootSequence.Phase.SEPARATION);
		session.pause();
		session.resume();
		assertListEquals(
			"closed session ignores later phases",
			Arrays.asList(AndroidBootTransitionCue.REGISTRATION),
			sound.cues
		);
		assertEquals("closed session ignores pause", 0, sound.pauseCount);
		assertEquals("closed session ignores resume", 0, sound.resumeCount);
		assertEquals("cleanup closes sound once", 1, sound.closeCount);
	}

	private static void assertSoundFailureCannotEscape() {
		ThrowingSound sound = new ThrowingSound();
		AndroidBootTransitionSoundSession session =
			new AndroidBootTransitionSoundSession(sound, true);
		session.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		session.enterPhase(AndroidBootSequence.Phase.SEPARATION);
		session.close();
		assertEquals("failing sound attempted one cue", 1, sound.playCount);
		assertEquals("failing sound cleanup attempted once", 1, sound.closeCount);
	}

	private static void assertLifecycleFailureCannotEscape() {
		LifecycleThrowingSound pauseFailure = new LifecycleThrowingSound(true, false);
		AndroidBootTransitionSoundSession pauseSession =
			new AndroidBootTransitionSoundSession(pauseFailure, true);
		pauseSession.pause();
		pauseSession.resume();
		pauseSession.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		assertEquals("failing pause attempted once", 1, pauseFailure.pauseCount);
		assertEquals("failing pause closes once", 1, pauseFailure.closeCount);
		assertEquals("failing pause emits no cues", 0, pauseFailure.cues.size());

		LifecycleThrowingSound resumeFailure = new LifecycleThrowingSound(false, true);
		AndroidBootTransitionSoundSession resumeSession =
			new AndroidBootTransitionSoundSession(resumeFailure, true);
		resumeSession.pause();
		resumeSession.resume();
		resumeSession.enterPhase(AndroidBootSequence.Phase.REGISTRATION);
		assertEquals("failing resume pauses once", 1, resumeFailure.pauseCount);
		assertEquals("failing resume attempted once", 1, resumeFailure.resumeCount);
		assertEquals("failing resume closes once", 1, resumeFailure.closeCount);
		assertEquals("failing resume emits no cues", 0, resumeFailure.cues.size());
	}

	private static void assertListEquals(
		String label,
		List<AndroidBootTransitionCue> expected,
		List<AndroidBootTransitionCue> actual
	) {
		if (!expected.equals(actual)) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertSame(String label, Object expected, Object actual) {
		if (expected != actual) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertEquals(String label, int expected, int actual) {
		if (expected != actual) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertLong(String label, long expected, long actual) {
		if (expected != actual) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static final class RecordingSound implements AndroidBootTransitionSound {
		private final List<AndroidBootTransitionCue> cues = new ArrayList<>();
		private int pauseCount;
		private int resumeCount;
		private int closeCount;

		@Override
		public void play(AndroidBootTransitionCue cue) {
			cues.add(cue);
		}

		@Override
		public void pause() {
			pauseCount++;
		}

		@Override
		public void resume() {
			resumeCount++;
		}

		@Override
		public void close() {
			closeCount++;
		}
	}

	private static final class ThrowingSound implements AndroidBootTransitionSound {
		private int playCount;
		private int closeCount;

		@Override
		public void play(AndroidBootTransitionCue cue) {
			playCount++;
			throw new IllegalStateException("playback failure");
		}

		@Override
		public void pause() {
		}

		@Override
		public void resume() {
		}

		@Override
		public void close() {
			closeCount++;
			throw new IllegalStateException("cleanup failure");
		}
	}

	private static final class LifecycleThrowingSound implements AndroidBootTransitionSound {
		private final boolean throwOnPause;
		private final boolean throwOnResume;
		private final List<AndroidBootTransitionCue> cues = new ArrayList<>();
		private int pauseCount;
		private int resumeCount;
		private int closeCount;

		private LifecycleThrowingSound(boolean throwOnPause, boolean throwOnResume) {
			this.throwOnPause = throwOnPause;
			this.throwOnResume = throwOnResume;
		}

		@Override
		public void play(AndroidBootTransitionCue cue) {
			cues.add(cue);
		}

		@Override
		public void pause() {
			pauseCount++;
			if (throwOnPause) {
				throw new IllegalStateException("pause failure");
			}
		}

		@Override
		public void resume() {
			resumeCount++;
			if (throwOnResume) {
				throw new IllegalStateException("resume failure");
			}
		}

		@Override
		public void close() {
			closeCount++;
		}
	}
}
