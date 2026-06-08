package com.game.sts2launcher;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.provider.Settings;
import android.util.Log;

import java.io.File;

public class LauncherActivity extends Activity {
	private static final String TAG = "STS2Mobile";
	private static final String PCK_FILE = "SlayTheSpire2.pck";

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		Intent intent = new Intent(this, shouldUseNativeX86Fallback() ? NativeFallbackActivity.class : GodotApp.class);
		Intent sourceIntent = getIntent();
		if (sourceIntent != null && sourceIntent.getExtras() != null) {
			intent.putExtras(sourceIntent);
		}
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		startActivity(intent);
		finish();
	}

	private boolean shouldUseNativeX86Fallback() {
		if (isForcedX86GodotTest()) {
			Log.w(TAG, "Bypassing native x86 fallback because sts2_force_godot_x86=1.");
			return false;
		}

		boolean fallback = isX86Runtime();
		if (fallback) {
			Log.w(TAG, "Routing to native x86 fallback; Godot/.NET runtime crashes Android x86 emulator.");
		}
		return fallback;
	}

	private boolean isForcedX86GodotTest() {
		try {
			return Settings.Global.getInt(getContentResolver(), "sts2_force_godot_x86", 0) == 1;
		} catch (Exception e) {
			Log.w(TAG, "Could not read sts2_force_godot_x86 setting", e);
			return false;
		}
	}

	private boolean isX86Runtime() {
		for (String abi : android.os.Build.SUPPORTED_ABIS) {
			if (abi != null && abi.contains("x86")) {
				return true;
			}
		}
		return false;
	}

	private boolean hasDownloadedGamePck() {
		File pck = new File(new File(getFilesDir(), "game"), PCK_FILE);
		return pck.exists() && pck.isFile() && pck.length() > 0;
	}
}
