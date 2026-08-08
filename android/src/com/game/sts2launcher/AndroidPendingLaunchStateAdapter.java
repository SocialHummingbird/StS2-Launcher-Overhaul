package com.game.sts2launcher;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.util.Log;

final class AndroidPendingLaunchStateAdapter {
	private static final String TAG = "STS2Mobile";

	static AndroidPendingLaunchState.ClearResult clearForRecovery(
		Context context,
		Intent intent
	) {
		try {
			return AndroidPendingLaunchState.clearForRecovery(
				preferences(context),
				payload(intent)
			);
		} catch (RuntimeException error) {
			Log.e(
				TAG,
				"Failed to clear pending native recovery launch state",
				error
			);
			return AndroidPendingLaunchState.failed(
				error.getClass().getSimpleName() + ": "
					+ String.valueOf(error.getMessage())
			);
		}
	}

	static boolean hasPendingGameLaunch(Context context, Intent intent) {
		try {
			return AndroidPendingLaunchState.hasPendingGameLaunch(
				preferences(context),
				payload(intent)
			);
		} catch (RuntimeException error) {
			Log.w(TAG, "Could not inspect pending game launch request", error);
			return false;
		}
	}

	private static AndroidPendingLaunchState.Preferences preferences(
		Context context
	) {
		SharedPreferences preferences = context.getSharedPreferences(
			AndroidPendingLaunchState.PREFERENCES_NAME,
			Context.MODE_PRIVATE
		);
		return new AndroidPendingLaunchState.Preferences() {
			@Override
			public boolean gameLaunchPreferencePending() {
				return preferences.getBoolean(
					AndroidPendingLaunchState.GAME_PREFERENCE,
					false
				);
			}

			@Override
			public boolean safeLaunchPreferencePending() {
				return preferences.getBoolean(
					AndroidPendingLaunchState.SAFE_PREFERENCE,
					false
				);
			}

			@Override
			public boolean clearPendingPreferences() {
				return preferences.edit()
					.remove(AndroidPendingLaunchState.GAME_PREFERENCE)
					.remove(AndroidPendingLaunchState.SAFE_PREFERENCE)
					.commit();
			}
		};
	}

	private static AndroidPendingLaunchState.Payload payload(Intent intent) {
		return new AndroidPendingLaunchState.Payload() {
			@Override
			public boolean gameLaunchPayloadPending() {
				return intent != null
					&& intent.getBooleanExtra(
						AndroidPendingLaunchState.GAME_INTENT_EXTRA,
						false
					);
			}

			@Override
			public boolean safeLaunchPayloadPending() {
				return intent != null
					&& intent.getBooleanExtra(
						AndroidPendingLaunchState.SAFE_INTENT_EXTRA,
						false
					);
			}

			@Override
			public void clearPendingPayload() {
				if (intent == null) {
					return;
				}
				intent.removeExtra(
					AndroidPendingLaunchState.GAME_INTENT_EXTRA
				);
				intent.removeExtra(
					AndroidPendingLaunchState.SAFE_INTENT_EXTRA
				);
			}
		};
	}

	private AndroidPendingLaunchStateAdapter() {
	}
}
