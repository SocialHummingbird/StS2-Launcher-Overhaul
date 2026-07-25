package com.game.sts2launcher;

import android.app.Activity;
import android.content.Intent;
import android.graphics.Color;
import android.os.Bundle;
import android.os.SystemClock;
import android.provider.Settings;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.FrameLayout;
import android.widget.ImageView;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.security.MessageDigest;

public class LauncherActivity extends Activity {
	private static final String TAG = "STS2Mobile";
	private static final String PCK_FILE = "SlayTheSpire2.pck";
	private static final String GAME_BRANCH_FILE = "game_branch";
	private static final String GAME_VERSIONS_DIR = "game_versions";
	private static final String BRANCH_MARKER_FILE = "steam_branch.txt";
	private static final String PREFS_NAME = "sts2mobile";
	private static final String KEY_LAUNCH_GAME_ON_NEXT_START = "launch_game_on_next_start";
	private static final String EXTRA_LAUNCH_GAME_ON_START = "sts2_launch_game";
	private static final String LAST_STARTUP_CONTEXT_FILE = "last_startup_context.txt";
	private static final String LAST_STARTUP_TIMELINE_FILE = "last_startup_timeline.txt";
	private final AndroidStartupRouteGate routeGate =
		new AndroidStartupRouteGate();
	private final Runnable startupRouting = this::routeStartup;
	private View routingPlaceholder;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		routingPlaceholder = createRoutingPlaceholder();
		setContentView(routingPlaceholder);
		routingPlaceholder.postOnAnimation(startupRouting);
	}

	@Override
	protected void onDestroy() {
		if (routingPlaceholder != null) {
			routingPlaceholder.removeCallbacks(startupRouting);
			routingPlaceholder = null;
		}
		super.onDestroy();
	}

	private void routeStartup() {
		boolean pendingGameLaunch = hasPendingGameLaunchRequest();
		recordStartupPhase("native launcher activity onCreate", "pendingGameLaunch=" + pendingGameLaunch);
		logSelectedBranchBeforeRouting(false);

		if (shouldUseNativeX86Fallback()) {
			routeOnce(NativeFallbackActivity.class, null);
			return;
		}

		String selectedBranch = readSelectedBranch();
		File gameDirectory = resolveGameDir();
		AndroidAssemblyBootstrapper assemblyBootstrapper =
			new AndroidAssemblyBootstrapper(
				this,
				gameDirectory,
				selectedBranch,
				pendingGameLaunch,
				this::recordStartupPhase
			);
		assemblyBootstrapper.logStartupFreshnessProbe();
		AndroidAssemblyBootstrapper.Result assemblyResult =
			assemblyBootstrapper.prepare();
		if (!assemblyResult.isSuccess()) {
			routeOnce(NativeFallbackActivity.class, assemblyResult);
			return;
		}

		routeOnce(GodotApp.class, null);
	}

	private View createRoutingPlaceholder() {
		FrameLayout root = new FrameLayout(this);
		root.setBackgroundColor(Color.rgb(14, 20, 29));
		root.setLayoutParams(new ViewGroup.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.MATCH_PARENT
		));

		ImageView mark = new ImageView(this);
		mark.setImageResource(R.drawable.godot_boot_mark);
		mark.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
		int size = Math.round(
			144 * getResources().getDisplayMetrics().density
		);
		FrameLayout.LayoutParams markParameters =
			new FrameLayout.LayoutParams(size, size, Gravity.CENTER);
		root.addView(mark, markParameters);
		return root;
	}

	private void routeOnce(
		Class<?> target,
		AndroidAssemblyBootstrapper.Result assemblyFailure
	) {
		if (!routeGate.tryClaim()) {
			Log.e(TAG, "Ignoring duplicate native startup route to " + target.getSimpleName());
			recordStartupPhase("native duplicate route blocked", target.getSimpleName());
			return;
		}

		recordStartupPhase("native route selected", target.getSimpleName());
		Intent intent = new Intent(this, target);
		Intent sourceIntent = getIntent();
		if (sourceIntent != null && sourceIntent.getExtras() != null) {
			intent.putExtras(sourceIntent);
		}
		attachAssemblyFailure(intent, assemblyFailure);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		startActivity(intent);
		recordStartupPhase("native route started", target.getSimpleName());
		finish();
	}

	private void attachAssemblyFailure(
		Intent intent,
		AndroidAssemblyBootstrapper.Result assemblyFailure
	) {
		if (
			intent == null
				|| assemblyFailure == null
				|| assemblyFailure.isSuccess()
		) {
			return;
		}
		intent.putExtra(
			NativeFallbackActivity.EXTRA_REASON_TITLE,
			assemblyFailure.title()
		);
		intent.putExtra(
			NativeFallbackActivity.EXTRA_REASON_MESSAGE,
			assemblyFailure.message()
		);
		intent.putExtra(
			NativeFallbackActivity.EXTRA_REASON_DIAGNOSTICS,
			assemblyFailure.diagnostics()
		);
	}

	private void recordStartupPhase(String phase, String detail) {
		String safePhase = sanitizeStartupMarkerValue(phase);
		String safeDetail = sanitizeStartupMarkerValue(detail);
		long elapsedMs = SystemClock.elapsedRealtime();
		long utcMillis = System.currentTimeMillis();
		String context =
			"StS2 Launcher native launcher routing context\n" +
			"UTC millis: " + utcMillis + "\n" +
			"Elapsed realtime ms: " + elapsedMs + "\n" +
			"Phase: " + safePhase + "\n" +
			"Detail: " + safeDetail + "\n" +
			"Package: " + getPackageName() + "\n" +
			"Version: " + BuildConfig.VERSION_NAME + " (" + BuildConfig.VERSION_CODE + ")\n" +
			"Selected branch: " + readSelectedBranch() + "\n";
		writeInternalTextFile(LAST_STARTUP_CONTEXT_FILE, context);
		appendInternalTextFile(
			LAST_STARTUP_TIMELINE_FILE,
			utcMillis + "\telapsedRealtimeMs=" + elapsedMs + "\tphase=" + safePhase + "\tdetail=" + safeDetail + "\n"
		);
		Log.i(TAG, "Native startup phase: elapsedRealtimeMs=" + elapsedMs + " phase=" + safePhase + " detail=" + safeDetail);
	}

	private String sanitizeStartupMarkerValue(String value) {
		if (value == null || value.trim().isEmpty()) {
			return "<none>";
		}
		return value.replace('\r', ' ').replace('\n', ' ').trim();
	}

	private void writeInternalTextFile(String name, String text) {
		try (FileOutputStream out = new FileOutputStream(new File(getFilesDir(), name))) {
			out.write(text.getBytes("UTF-8"));
		} catch (Exception e) {
			Log.w(TAG, "Failed to write " + name, e);
		}
	}

	private void appendInternalTextFile(String name, String text) {
		try (FileOutputStream out = new FileOutputStream(new File(getFilesDir(), name), true)) {
			out.write(text.getBytes("UTF-8"));
		} catch (Exception e) {
			Log.w(TAG, "Failed to append " + name, e);
		}
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

	private boolean hasPendingGameLaunchRequest() {
		Intent intent = getIntent();
		if (intent != null && intent.getBooleanExtra(EXTRA_LAUNCH_GAME_ON_START, false)) {
			return true;
		}

		try {
			return getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
				.getBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, false);
		} catch (Exception e) {
			Log.w(TAG, "Could not inspect pending game launch request", e);
			return false;
		}
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

	private void logSelectedBranchBeforeRouting(boolean includeExpensiveHashes) {
		File gameDir = resolveGameDir();
		String branch = readSelectedBranch();
		File branchMarker = new File(gameDir, BRANCH_MARKER_FILE);
		Log.i(TAG, "Selected Steam branch before routing: " + branch);
		Log.i(TAG, "Selected Steam branch note before routing: " + SteamBranchInfo.selectorHelpText(branch));
		Log.i(TAG, "Selected game version slot kind before routing: " + SteamBranchInfo.installSlotKind(branch));
		Log.i(TAG, "Selected game version slot directory before routing: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), branch).getAbsolutePath());
		Log.i(TAG, "Resolved game directory before routing: " + gameDir.getAbsolutePath());
		Log.i(TAG, "Selected game PCK before routing: " + describeGamePck(new File(gameDir, PCK_FILE), includeExpensiveHashes));
		Log.i(TAG, "Steam branch marker install slot kind before routing: " + readMarkerValue(branchMarker, "Install slot kind:"));
		Log.i(TAG, "Steam branch marker expected install slot kind before routing: " + SteamBranchInfo.installSlotKind(branch));
		Log.i(TAG, "Steam branch marker install slot directory before routing: " + readMarkerValue(branchMarker, "Install slot directory:"));
		Log.i(TAG, "Steam branch marker expected install slot directory before routing: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), branch).getAbsolutePath());
		Log.i(TAG, "Steam branch marker has matching install slot provenance before routing: " + hasInstallSlotProvenance(branchMarker, branch));
		Log.i(TAG, "Steam branch marker has depot manifests before routing: " + hasDepotManifestProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker has branch integrity provenance before routing: " + hasBranchIntegrityProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker depot manifest entries before routing: " + depotManifestCount(branchMarker));
		Log.i(TAG, "Steam branch marker ready before routing: " + isBranchMarkerReady(gameDir, branch));
	}

	private boolean isBranchMarkerReady(File gameDir, String branch) {
		File marker = new File(gameDir, BRANCH_MARKER_FILE);
		if (!marker.exists() || !marker.isFile()) {
			return "public".equalsIgnoreCase(branch);
		}

		try (java.io.BufferedReader reader = new java.io.BufferedReader(new java.io.FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (!line.regionMatches(true, 0, "Branch:", 0, "Branch:".length())) {
					continue;
				}
				String markerBranch = line.substring("Branch:".length()).trim();
				boolean ready = markerBranch.equalsIgnoreCase(branch);
				if (!ready) {
					Log.w(TAG, "Steam branch marker mismatch before routing: selected=" + branch + " marker=" + markerBranch);
				}
				return ready && ("public".equalsIgnoreCase(branch) || (hasInstallSlotProvenance(marker, branch) && hasDepotManifestProvenance(marker) && hasBranchIntegrityProvenance(marker)));
			}
			Log.w(TAG, "Steam branch marker has no Branch line before routing: " + marker.getAbsolutePath());
		} catch (Exception e) {
			Log.w(TAG, "Failed to read Steam branch marker before routing: " + marker.getAbsolutePath(), e);
		}

		return false;
	}

	private boolean hasDepotManifestProvenance(File marker) {
		return depotManifestCount(marker) > 0;
	}

	private boolean hasBranchIntegrityProvenance(File marker) {
		return markerHasValue(marker, "Depot manifests matching public count:")
			&& markerHasValue(marker, "Depot manifests differing from public count:")
			&& markerHasValue(marker, "Depot manifests without public comparison count:")
			&& markerHasValue(marker, "Depot manifests inherited from public count:")
			&& markerHasValue(marker, "Depot manifests missing selected branch manifest count:");
	}

	private boolean markerHasValue(File marker, String prefix) {
		return !readMarkerValue(marker, prefix).isEmpty();
	}

	private boolean hasInstallSlotProvenance(File marker, String branch) {
		return SteamBranchInfo.installSlotKind(branch).equalsIgnoreCase(readMarkerValue(marker, "Install slot kind:"))
			&& normalizeMarkerPath(SteamBranchInfo.installSlotDirectory(getFilesDir(), branch).getAbsolutePath()).equalsIgnoreCase(
				normalizeMarkerPath(readMarkerValue(marker, "Install slot directory:"))
			);
	}

	private String readMarkerValue(File marker, String prefix) {
		if (marker == null || !marker.exists() || !marker.isFile()) {
			return "";
		}

		try (java.io.BufferedReader reader = new java.io.BufferedReader(new java.io.FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (line.regionMatches(true, 0, prefix, 0, prefix.length())) {
					return line.substring(prefix.length()).trim();
				}
			}
		} catch (Exception e) {
			Log.w(TAG, "Failed to inspect Steam branch marker install slot provenance before routing: " + marker.getAbsolutePath(), e);
		}

		return "";
	}

	private String normalizeMarkerPath(String path) {
		if (path == null || path.trim().isEmpty() || path.startsWith("<")) {
			return "";
		}

		String normalized = path.trim().replace('\\', '/');
		while (normalized.endsWith("/") && normalized.length() > 1) {
			normalized = normalized.substring(0, normalized.length() - 1);
		}

		String packageName = getPackageName();
		String dataDataRoot = "/data/data/" + packageName;
		String dataUserRoot = "/data/user/0/" + packageName;
		if (normalized.equals(dataDataRoot) || normalized.startsWith(dataDataRoot + "/")) {
			normalized = dataUserRoot + normalized.substring(dataDataRoot.length());
		}
		return normalized;
	}

	private int depotManifestCount(File marker) {
		if (marker == null || !marker.exists() || !marker.isFile()) {
			return 0;
		}

		try (java.io.BufferedReader reader = new java.io.BufferedReader(new java.io.FileReader(marker))) {
			int count = 0;
			String line;
			while ((line = reader.readLine()) != null) {
				if (line.regionMatches(true, 0, "Depot manifest:", 0, "Depot manifest:".length())) {
					count++;
				}
			}
			return count;
		} catch (Exception e) {
			Log.w(TAG, "Failed to inspect Steam branch marker depot provenance before routing: " + marker.getAbsolutePath(), e);
		}
		return 0;
	}

	private String describeGamePck(File pckFile, boolean includeSha256) {
		if (pckFile == null) {
			return "<null>";
		}
		StringBuilder state = new StringBuilder();
		state.append(pckFile.getAbsolutePath());
		state.append(" exists=");
		state.append(pckFile.exists() && pckFile.isFile());
		if (pckFile.exists() && pckFile.isFile()) {
			state.append(" bytes=");
			state.append(pckFile.length());
			if (includeSha256) {
				state.append(" sha256=");
				state.append(sha256Hex(pckFile));
			} else {
				state.append(" sha256=<skipped>");
			}
		}
		return state.toString();
	}

	private String sha256Hex(File file) {
		try (InputStream in = new FileInputStream(file)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = in.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return bytesToHex(digest.digest());
		} catch (Exception e) {
			Log.w(TAG, "Failed to compute game PCK SHA-256 before routing: " + file.getAbsolutePath(), e);
			return "<unavailable:" + e.getClass().getSimpleName() + ">";
		}
	}

	private String bytesToHex(byte[] bytes) {
		char[] hex = new char[bytes.length * 2];
		final char[] alphabet = "0123456789abcdef".toCharArray();
		for (int i = 0; i < bytes.length; i++) {
			int value = bytes[i] & 0xff;
			hex[i * 2] = alphabet[value >>> 4];
			hex[i * 2 + 1] = alphabet[value & 0x0f];
		}
		return new String(hex);
	}

	private File resolveGameDir() {
		String branch = readSelectedBranch();
		return SteamBranchInfo.gameDirectory(getFilesDir(), branch);
	}

	private String readSelectedBranch() {
		File branchFile = new File(getFilesDir(), GAME_BRANCH_FILE);
		if (!branchFile.exists() || !branchFile.isFile()) {
			return "public";
		}
		try (java.io.FileInputStream in = new java.io.FileInputStream(branchFile);
				java.io.ByteArrayOutputStream out = new java.io.ByteArrayOutputStream()) {
			byte[] buffer = new byte[128];
			int read;
			while ((read = in.read(buffer)) > 0) {
				out.write(buffer, 0, read);
			}
			String branch = out.toString("UTF-8").trim();
			return branch.isEmpty() ? "public" : branch;
		} catch (Exception e) {
			Log.w(TAG, "Could not read selected game branch", e);
			return "public";
		}
	}

}
