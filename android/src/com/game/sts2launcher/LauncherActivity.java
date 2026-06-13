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
	private static final String GAME_BRANCH_FILE = "game_branch";
	private static final String GAME_VERSIONS_DIR = "game_versions";
	private static final String BRANCH_MARKER_FILE = "steam_branch.txt";

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		logSelectedBranchBeforeRouting();

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

	private void logSelectedBranchBeforeRouting() {
		File gameDir = resolveGameDir();
		String branch = readSelectedBranch();
		File branchMarker = new File(gameDir, BRANCH_MARKER_FILE);
		Log.i(TAG, "Selected Steam branch before routing: " + branch);
		Log.i(TAG, "Selected Steam branch note before routing: " + SteamBranchInfo.selectorHelpText(branch));
		Log.i(TAG, "Selected game version slot kind before routing: " + SteamBranchInfo.installSlotKind(branch));
		Log.i(TAG, "Selected game version slot directory before routing: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), branch).getAbsolutePath());
		Log.i(TAG, "Resolved game directory before routing: " + gameDir.getAbsolutePath());
		Log.i(TAG, "Steam branch marker install slot kind before routing: " + readMarkerValue(branchMarker, "Install slot kind:"));
		Log.i(TAG, "Steam branch marker expected install slot kind before routing: " + SteamBranchInfo.installSlotKind(branch));
		Log.i(TAG, "Steam branch marker install slot directory before routing: " + readMarkerValue(branchMarker, "Install slot directory:"));
		Log.i(TAG, "Steam branch marker expected install slot directory before routing: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), branch).getAbsolutePath());
		Log.i(TAG, "Steam branch marker has matching install slot provenance before routing: " + hasInstallSlotProvenance(branchMarker, branch));
		Log.i(TAG, "Steam branch marker has depot manifests before routing: " + hasDepotManifestProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker depot manifest entries before routing: " + depotManifestCount(branchMarker));
		Log.i(TAG, "Steam branch marker ready before routing: " + isBranchMarkerReady(gameDir, branch));
	}

	private boolean hasDownloadedGamePck() {
		File gameDir = resolveGameDir();
		String branch = readSelectedBranch();
		File pck = new File(gameDir, PCK_FILE);
		return pck.exists() && pck.isFile() && pck.length() > 0 && isBranchMarkerReady(gameDir, branch);
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
				return ready && ("public".equalsIgnoreCase(branch) || (hasInstallSlotProvenance(marker, branch) && hasDepotManifestProvenance(marker)));
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
