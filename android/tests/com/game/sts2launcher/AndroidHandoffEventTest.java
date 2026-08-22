package com.game.sts2launcher;

import org.junit.Test;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;

public final class AndroidHandoffEventTest {
	@Test
	public void attemptIdIsParsedAndIncludedInEveryFieldRecord() {
		String attemptId = "0123456789abcdef0123456789abcdef";
		String marker = "StS2 Launcher launch attempt\n"
			+ "UTC: 2026-08-22T00:00:00Z\n"
			+ "Attempt ID: " + attemptId + "\n"
			+ "Phase: restart requested\n";

		assertEquals(attemptId, AndroidHandoffEvent.parseAttemptId(marker));
		String line = AndroidHandoffEvent.format(
			attemptId,
			AndroidHandoffEvent.GODOT_SURFACE_CREATED,
			123456789L,
			"main#1",
			"created",
			"false",
			"unknown",
			"surface_created"
		);
		assertTrue(line.contains("attemptId=" + attemptId));
		assertTrue(line.contains("event=godot_surface_created"));
		assertTrue(line.contains("timestampUtcMs=123456789"));
		assertTrue(line.contains("thread=main#1"));
		assertTrue(line.contains("activityLifecycle=created"));
		assertTrue(line.contains("windowFocus=false"));
		assertTrue(line.contains("overlayVisible=unknown"));
		assertTrue(line.contains("godotReady=surface_created"));
	}
}
