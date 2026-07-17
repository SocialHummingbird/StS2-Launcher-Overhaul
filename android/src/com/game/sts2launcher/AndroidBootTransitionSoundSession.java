package com.game.sts2launcher;

import java.util.Objects;

final class AndroidBootTransitionSoundSession implements AutoCloseable {
	private final AndroidBootTransitionSound sound;
	private final boolean stagedCuesEnabled;
	private int lastCueOrdinal = -1;
	private boolean paused;
	private boolean closed;

	AndroidBootTransitionSoundSession(
		AndroidBootTransitionSound sound,
		boolean stagedCuesEnabled
	) {
		this.sound = Objects.requireNonNull(sound, "sound");
		this.stagedCuesEnabled = stagedCuesEnabled;
	}

	void enterPhase(AndroidBootSequence.Phase phase) {
		if (closed || !stagedCuesEnabled) {
			return;
		}
		AndroidBootTransitionCue cue = AndroidBootTransitionCue.fromPhase(phase);
		if (cue.ordinal() <= lastCueOrdinal) {
			return;
		}
		lastCueOrdinal = cue.ordinal();
		if (paused) {
			return;
		}
		try {
			sound.play(cue);
		} catch (RuntimeException ignored) {
			close();
		}
	}

	void pause() {
		if (closed || paused || !stagedCuesEnabled) {
			return;
		}
		paused = true;
		try {
			sound.pause();
		} catch (RuntimeException ignored) {
			close();
		}
	}

	void resume() {
		if (closed || !paused || !stagedCuesEnabled) {
			return;
		}
		paused = false;
		try {
			sound.resume();
		} catch (RuntimeException ignored) {
			close();
		}
	}

	@Override
	public void close() {
		if (closed) {
			return;
		}
		closed = true;
		try {
			sound.close();
		} catch (RuntimeException ignored) {
			// Sound is optional and cannot delay launcher handoff or lifecycle cleanup.
		}
	}
}
