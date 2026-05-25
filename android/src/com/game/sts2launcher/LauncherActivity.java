package com.game.sts2launcher;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import java.io.File;

public class LauncherActivity extends Activity {
	private static final String TAG = "STS2Mobile";
	private static final String PCK_FILE = "SlayTheSpire2.pck";

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		Intent intent = new Intent(this, shouldUseNativeX86Fallback() ? NativeFallbackActivity.class : GodotApp.class);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		startActivity(intent);
		finish();
	}

	private boolean shouldUseNativeX86Fallback() {
		boolean fallback = isX86Runtime();
		if (fallback) {
			Log.w(TAG, "Routing to native x86 fallback; Godot/.NET runtime crashes Android x86 emulator.");
		}
		return fallback;
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
