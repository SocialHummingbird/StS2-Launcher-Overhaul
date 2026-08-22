package com.game.sts2launcher;

import java.io.File;
import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;

final class AndroidHandoffEvent {
	static final String LAUNCH_REQUESTED = "launch_requested";
	static final String NATIVE_BOOTSTRAP_ACCEPTED = "native_bootstrap_accepted";
	static final String GODOT_SURFACE_CREATED = "godot_surface_created";
	static final String MAIN_MENU_READY = "main_menu_ready";
	static final String WINDOW_FOCUS_GAINED = "window_focus_gained";
	static final String WINDOW_FOCUS_LOST = "window_focus_lost";
	static final String OVERLAY_SHOWN = "overlay_shown";
	static final String OVERLAY_HIDDEN = "overlay_hidden";
	static final String HANDOFF_COMPLETED = "handoff_completed";
	static final String HANDOFF_FAILED = "handoff_failed";
	private static final String LAUNCH_ATTEMPT_FILE = "last_launch_attempt.txt";
	private static final String ATTEMPT_ID_PREFIX = "Attempt ID:";

	static String format(
		String attemptId,
		String eventName,
		long timestampUtcMs,
		String thread,
		String activityLifecycle,
		String windowFocus,
		String overlayVisible,
		String godotReadiness
	) {
		return "[Handoff]"
			+ " attemptId=" + token(attemptId)
			+ " event=" + token(eventName)
			+ " timestampUtcMs=" + timestampUtcMs
			+ " thread=" + token(thread)
			+ " activityLifecycle=" + token(activityLifecycle)
			+ " windowFocus=" + token(windowFocus)
			+ " overlayVisible=" + token(overlayVisible)
			+ " godotReady=" + token(godotReadiness);
	}

	static String readAttemptId(File filesDirectory) {
		if (filesDirectory == null) {
			return "unknown";
		}

		File marker = new File(filesDirectory, LAUNCH_ATTEMPT_FILE);
		if (!marker.isFile()) {
			return "unknown";
		}

		try {
			try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
				String line;
				while ((line = reader.readLine()) != null) {
					if (line.startsWith(ATTEMPT_ID_PREFIX)) {
						return parseAttemptId(line);
					}
				}
			}
		} catch (IOException ignored) {
		}
		return "unknown";
	}

	static String parseAttemptId(String markerText) {
		if (markerText == null || markerText.isBlank()) {
			return "unknown";
		}

		for (String line : markerText.split("\\R")) {
			if (!line.startsWith(ATTEMPT_ID_PREFIX)) {
				continue;
			}

			String attemptId = line.substring(ATTEMPT_ID_PREFIX.length()).trim();
			return attemptId.isEmpty() ? "unknown" : token(attemptId);
		}
		return "unknown";
	}

	private static String token(String value) {
		if (value == null || value.isBlank()) {
			return "unknown";
		}

		return value.trim()
			.replace(' ', '_')
			.replace('\t', '_')
			.replace('\r', '_')
			.replace('\n', '_')
			.replace('=', '_');
	}

	private AndroidHandoffEvent() {
	}
}
