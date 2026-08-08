package com.game.sts2launcher;

final class AndroidPendingLaunchState {
	static final String PREFERENCES_NAME = "sts2mobile";
	static final String GAME_PREFERENCE = "launch_game_on_next_start";
	static final String SAFE_PREFERENCE = "safe_launch_on_next_start";
	static final String GAME_INTENT_EXTRA = "sts2_launch_game";
	static final String SAFE_INTENT_EXTRA = "sts2_safe_launch";
	static final String NATIVE_RECOVERY_INTENT_EXTRA =
		"sts2_native_recovery";

	interface Preferences {
		boolean gameLaunchPreferencePending();

		boolean safeLaunchPreferencePending();

		boolean clearPendingPreferences();
	}

	interface Payload {
		boolean gameLaunchPayloadPending();

		boolean safeLaunchPayloadPending();

		void clearPendingPayload();
	}

	static ClearResult clearForRecovery(
		Preferences preferences,
		Payload payload
	) {
		boolean preferenceGameBefore =
			preferences.gameLaunchPreferencePending();
		boolean preferenceSafeBefore =
			preferences.safeLaunchPreferencePending();
		boolean payloadGameBefore = payload.gameLaunchPayloadPending();
		boolean payloadSafeBefore = payload.safeLaunchPayloadPending();

		payload.clearPendingPayload();
		boolean preferenceRemovalCommitted =
			preferences.clearPendingPreferences();

		return new ClearResult(
			preferenceGameBefore,
			preferenceSafeBefore,
			payloadGameBefore,
			payloadSafeBefore,
			preferenceRemovalCommitted,
			preferences.gameLaunchPreferencePending(),
			preferences.safeLaunchPreferencePending(),
			payload.gameLaunchPayloadPending(),
			payload.safeLaunchPayloadPending(),
			""
		);
	}

	static boolean hasPendingGameLaunch(
		Preferences preferences,
		Payload payload
	) {
		return preferences.gameLaunchPreferencePending()
			|| payload.gameLaunchPayloadPending();
	}

	static ClearResult failed(String failure) {
		return new ClearResult(
			false,
			false,
			false,
			false,
			false,
			true,
			true,
			true,
			true,
			failure == null ? "unknown" : failure
		);
	}

	static final class ClearResult {
		private final boolean preferenceGameBefore;
		private final boolean preferenceSafeBefore;
		private final boolean payloadGameBefore;
		private final boolean payloadSafeBefore;
		private final boolean preferenceRemovalCommitted;
		private final boolean preferenceGameAfter;
		private final boolean preferenceSafeAfter;
		private final boolean payloadGameAfter;
		private final boolean payloadSafeAfter;
		private final String failure;

		ClearResult(
			boolean preferenceGameBefore,
			boolean preferenceSafeBefore,
			boolean payloadGameBefore,
			boolean payloadSafeBefore,
			boolean preferenceRemovalCommitted,
			boolean preferenceGameAfter,
			boolean preferenceSafeAfter,
			boolean payloadGameAfter,
			boolean payloadSafeAfter,
			String failure
		) {
			this.preferenceGameBefore = preferenceGameBefore;
			this.preferenceSafeBefore = preferenceSafeBefore;
			this.payloadGameBefore = payloadGameBefore;
			this.payloadSafeBefore = payloadSafeBefore;
			this.preferenceRemovalCommitted = preferenceRemovalCommitted;
			this.preferenceGameAfter = preferenceGameAfter;
			this.preferenceSafeAfter = preferenceSafeAfter;
			this.payloadGameAfter = payloadGameAfter;
			this.payloadSafeAfter = payloadSafeAfter;
			this.failure = failure;
		}

		boolean cleared() {
			return preferenceRemovalCommitted
				&& !preferenceGameAfter
				&& !preferenceSafeAfter
				&& !payloadGameAfter
				&& !payloadSafeAfter;
		}

		String summary() {
			return "preferenceGameBefore=" + preferenceGameBefore
				+ "; preferenceSafeBefore=" + preferenceSafeBefore
				+ "; payloadGameBefore=" + payloadGameBefore
				+ "; payloadSafeBefore=" + payloadSafeBefore
				+ "; preferenceRemovalCommitted="
					+ preferenceRemovalCommitted
				+ "; preferenceGameAfter=" + preferenceGameAfter
				+ "; preferenceSafeAfter=" + preferenceSafeAfter
				+ "; payloadGameAfter=" + payloadGameAfter
				+ "; payloadSafeAfter=" + payloadSafeAfter
				+ (
					failure.isEmpty()
						? ""
						: "; failure=" + failure
				);
		}
	}

	private AndroidPendingLaunchState() {
	}
}
