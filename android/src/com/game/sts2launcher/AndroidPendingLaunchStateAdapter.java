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
				return durableRequestPending(preferences) || preferences.getBoolean(
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
				String request = preferences.getString(AndroidLaunchRestartStore.KEY, "");
                android.content.SharedPreferences.Editor editor = preferences.edit();
                if (!request.isEmpty()) {
                    try {
                        editor.putString(AndroidLaunchRestartStore.KEY,
                            new org.json.JSONObject(request).put("state", "cancelled").toString());
                    } catch (Exception error) {
                        // Keep malformed payload as failure evidence, outside the live request key.
                        editor.putString(AndroidLaunchRestartStore.KEY + "_invalid", request)
                            .remove(AndroidLaunchRestartStore.KEY);
                    }
                }
                return editor
                    .remove(AndroidPendingLaunchState.GAME_PREFERENCE)
					.remove(AndroidPendingLaunchState.SAFE_PREFERENCE)
					.commit();
			}
		};
	}

    private static boolean durableRequestPending(SharedPreferences preferences) {
        try {
            org.json.JSONObject request = AndroidLaunchRestartStore.validate(
                preferences.getString(AndroidLaunchRestartStore.KEY, ""), System.currentTimeMillis());
            String state = request.getString("state");
            return "pending".equals(state) || "claimed".equals(state);
        } catch (Exception error) { return false; }
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
