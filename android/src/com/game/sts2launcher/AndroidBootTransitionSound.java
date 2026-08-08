package com.game.sts2launcher;

interface AndroidBootTransitionSound extends AutoCloseable {
	void play(AndroidBootTransitionCue cue);

	void pause();

	void resume();

	@Override
	void close();
}
