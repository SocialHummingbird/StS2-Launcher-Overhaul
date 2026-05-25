package com.game.sts2launcher;

import android.app.Activity;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Typeface;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.Gravity;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;

public class NativeFallbackActivity extends Activity {
	private static final String TAG = "STS2Mobile";
	private static final String PCK_FILE = "SlayTheSpire2.pck";

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		Log.w(TAG, "Showing native x86 fallback instead of starting Godot.");
		setContentView(createContentView());
	}

	private ScrollView createContentView() {
		String diagnostics = describeRuntimeState();

		ScrollView scroll = new ScrollView(this);
		scroll.setFillViewport(true);
		scroll.setBackgroundColor(Color.rgb(20, 24, 28));
		scroll.setLayoutParams(new ScrollView.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.MATCH_PARENT
		));

		LinearLayout root = new LinearLayout(this);
		root.setOrientation(LinearLayout.VERTICAL);
		root.setGravity(Gravity.CENTER);
		root.setPadding(48, 48, 48, 48);
		root.setLayoutParams(new ScrollView.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.MATCH_PARENT
		));

		TextView title = new TextView(this);
		title.setText("StS2 Launcher");
		title.setTextColor(Color.rgb(245, 230, 190));
		title.setTextSize(28);
		title.setTypeface(Typeface.DEFAULT_BOLD);
		title.setGravity(Gravity.CENTER);
		root.addView(title, new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.WRAP_CONTENT
		));

		TextView message = new TextView(this);
		message.setText(
			"Game files are downloaded.\n\n" +
			"The Android x86 emulator cannot safely run the Godot/.NET game runtime. It crashes inside the Mono/GodotSharp native layer before the launcher can take over.\n\n" +
			"Use an ARM64 Android device/build to test launching the game. The emulator remains useful for auth and download testing before this point.\n\n" +
			diagnostics
		);
		message.setTextColor(Color.rgb(220, 220, 210));
		message.setTextSize(16);
		message.setGravity(Gravity.CENTER);
		message.setPadding(0, 28, 0, 28);
		root.addView(message, new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.WRAP_CONTENT
		));

		Button copyButton = new Button(this);
		copyButton.setText("Copy diagnostics");
		copyButton.setOnClickListener(v -> copyDiagnostics(diagnostics));
		root.addView(copyButton, new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.WRAP_CONTENT
		));

		Button restartButton = new Button(this);
		restartButton.setText("Restart launcher");
		restartButton.setOnClickListener(v -> restartApp());
		LinearLayout.LayoutParams restartParams = new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.WRAP_CONTENT
		);
		restartParams.topMargin = 18;
		root.addView(restartButton, restartParams);

		Button clearButton = new Button(this);
		clearButton.setText("Clear downloaded files");
		clearButton.setOnClickListener(v -> {
			deleteRecursive(new File(getFilesDir(), "game"));
			deleteRecursive(new File(getFilesDir(), ".godot"));
			restartApp();
		});
		LinearLayout.LayoutParams clearParams = new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.WRAP_CONTENT
		);
		clearParams.topMargin = 18;
		root.addView(clearButton, clearParams);

		scroll.addView(root);
		return scroll;
	}

	private void copyDiagnostics(String diagnostics) {
		ClipboardManager clipboard = (ClipboardManager)getSystemService(Context.CLIPBOARD_SERVICE);
		if (clipboard == null) {
			Toast.makeText(this, "Clipboard unavailable", Toast.LENGTH_SHORT).show();
			return;
		}

		clipboard.setPrimaryClip(ClipData.newPlainText("StS2 Launcher diagnostics", diagnostics));
		Toast.makeText(this, "Diagnostics copied", Toast.LENGTH_SHORT).show();
	}

	private String describeRuntimeState() {
		File pck = new File(new File(getFilesDir(), "game"), PCK_FILE);
		StringBuilder state = new StringBuilder();
		state.append("App version: ");
		state.append(describeAppVersion());
		state.append("\nAndroid SDK: ");
		state.append(Build.VERSION.SDK_INT);
		state.append("\nDevice: ");
		appendDeviceName(state);
		state.append("\nRuntime ABI: ");
		appendSupportedAbis(state);
		state.append("\nGame PCK: ");
		state.append(pck.getAbsolutePath());
		state.append("\nPCK exists: ");
		state.append(pck.exists() && pck.isFile() ? "yes" : "no");
		if (pck.exists() && pck.isFile()) {
			state.append("\nPCK bytes: ");
			state.append(pck.length());
			state.append("\nPCK magic valid: ");
			state.append(describePckMagicStatus(pck));
		}
		return state.toString();
	}

	private String describePckMagicStatus(File pck) {
		if (pck.length() < 4) {
			return "no (file too small)";
		}

		try (InputStream in = new FileInputStream(pck)) {
			byte[] magic = new byte[4];
			if (in.read(magic) != magic.length) {
				return "unknown (could not read header)";
			}
			boolean valid = magic[0] == 0x47 && magic[1] == 0x44 && magic[2] == 0x50 && magic[3] == 0x43;
			return valid ? "yes" : "no";
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect game PCK magic", e);
			return "unknown (" + e.getClass().getSimpleName() + ")";
		}
	}

	private String describeAppVersion() {
		try {
			PackageInfo packageInfo = getPackageManager().getPackageInfo(getPackageName(), 0);
			long versionCode;
			if (Build.VERSION.SDK_INT >= 28) {
				versionCode = packageInfo.getLongVersionCode();
			} else {
				versionCode = packageInfo.versionCode;
			}
			return packageInfo.versionName + " (" + versionCode + ")";
		} catch (PackageManager.NameNotFoundException e) {
			Log.w(TAG, "Could not read package version", e);
			return "unknown";
		}
	}

	private void appendDeviceName(StringBuilder state) {
		if (Build.MANUFACTURER != null && !Build.MANUFACTURER.isEmpty()) {
			state.append(Build.MANUFACTURER);
			state.append(" ");
		}
		if (Build.MODEL != null && !Build.MODEL.isEmpty()) {
			state.append(Build.MODEL);
			return;
		}
		state.append("unknown");
	}

	private void appendSupportedAbis(StringBuilder state) {
		String[] abis = Build.SUPPORTED_ABIS;
		if (abis == null || abis.length == 0) {
			state.append("unknown");
			return;
		}

		for (int i = 0; i < abis.length; i++) {
			if (i > 0) {
				state.append(", ");
			}
			state.append(abis[i]);
		}
	}

	private void restartApp() {
		Intent intent = new Intent(this, LauncherActivity.class);
		intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
		startActivity(intent);
		finish();
	}

	private void deleteRecursive(File target) {
		if (target == null || !target.exists()) {
			return;
		}
		File[] children = target.listFiles();
		if (children != null) {
			for (File child : children) {
				deleteRecursive(child);
			}
		}
		if (!target.delete()) {
			Log.w(TAG, "Could not delete file: " + target.getAbsolutePath());
		}
	}
}
