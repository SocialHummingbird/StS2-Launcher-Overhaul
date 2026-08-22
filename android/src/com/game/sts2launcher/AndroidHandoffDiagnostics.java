package com.game.sts2launcher;

import android.util.Log;

import java.io.File;

final class AndroidHandoffDiagnostics {
	private static final String TAG = "STS2Mobile";
	static void record(
		File filesDirectory,
		String eventName,
		String suppliedAttemptId,
		String activityLifecycle,
		String windowFocus,
		String overlayVisible,
		String godotReadiness
	) {
		String attemptId = resolveAttemptId(filesDirectory, suppliedAttemptId);
		Thread thread = Thread.currentThread();
		String threadDescription = thread.getName() + "#" + thread.getId();
		Log.i(
			TAG,
			AndroidHandoffEvent.format(
				attemptId,
				eventName,
				System.currentTimeMillis(),
				threadDescription,
				activityLifecycle,
				windowFocus,
				overlayVisible,
				godotReadiness
			)
		);
	}

	private static String resolveAttemptId(
		File filesDirectory,
		String suppliedAttemptId
	) {
		if (suppliedAttemptId != null
			&& !suppliedAttemptId.isBlank()
			&& !"unknown".equals(suppliedAttemptId)) {
			return suppliedAttemptId;
		}

		return AndroidHandoffEvent.readAttemptId(filesDirectory);
	}

	private AndroidHandoffDiagnostics() {
	}
}
