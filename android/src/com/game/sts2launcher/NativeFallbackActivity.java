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
import java.io.ByteArrayOutputStream;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.security.MessageDigest;

import org.json.JSONObject;

public class NativeFallbackActivity extends Activity {
	private static final String TAG = "STS2Mobile";
	private static final String PCK_FILE = "SlayTheSpire2.pck";
	private static final String GAME_BRANCH_FILE = "game_branch";
	private static final String GAME_VERSIONS_DIR = "game_versions";
	private static final String BRANCH_MARKER_FILE = "steam_branch.txt";
	private static final String CURRENT_RUNTIME_SLOT_MARKER = "current_runtime_slot.json";
	private static final String GAME_CODE_ASSEMBLY = "sts2.dll";
	public static final String EXTRA_REASON_TITLE = "com.game.sts2launcher.REASON_TITLE";
	public static final String EXTRA_REASON_MESSAGE = "com.game.sts2launcher.REASON_MESSAGE";
	public static final String EXTRA_REASON_DIAGNOSTICS = "com.game.sts2launcher.REASON_DIAGNOSTICS";

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		Log.w(TAG, "Showing native x86 fallback instead of starting Godot.");
		setContentView(createContentView());
	}

	private ScrollView createContentView() {
		String diagnostics = describeRuntimeState();
		String extraDiagnostics = getIntent().getStringExtra(EXTRA_REASON_DIAGNOSTICS);
		if (extraDiagnostics != null && !extraDiagnostics.isEmpty()) {
			diagnostics += "\n\nFailure diagnostics:\n" + extraDiagnostics;
		}
		String reasonTitle = getIntent().getStringExtra(EXTRA_REASON_TITLE);
		String reasonMessage = getIntent().getStringExtra(EXTRA_REASON_MESSAGE);
		if (reasonTitle == null || reasonTitle.isEmpty()) {
			reasonTitle = "StS2 Mobile";
		}
		if (reasonMessage == null || reasonMessage.isEmpty()) {
			reasonMessage =
				"This Android x86 emulator cannot safely run the Godot/.NET runtime. It crashes inside the Mono/GodotSharp native layer before the launcher can take over.\n\n" +
				"Use an ARM64 Android device/build to test the launcher and game runtime. This screen is expected on x86 emulator builds.";
		}
		final String diagnosticsText = diagnostics;

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
		title.setText(reasonTitle);
		title.setTextColor(Color.rgb(245, 230, 190));
		title.setTextSize(28);
		title.setTypeface(Typeface.DEFAULT_BOLD);
		title.setGravity(Gravity.CENTER);
		root.addView(title, new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MATCH_PARENT,
			ViewGroup.LayoutParams.WRAP_CONTENT
		));

		TextView message = new TextView(this);
		message.setText(reasonMessage + "\n\n" + diagnostics);
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
		copyButton.setOnClickListener(v -> copyDiagnostics(diagnosticsText));
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
			deleteRecursive(new File(getFilesDir(), GAME_VERSIONS_DIR));
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

		clipboard.setPrimaryClip(ClipData.newPlainText("StS2 Mobile diagnostics", diagnostics));
		Toast.makeText(this, "Diagnostics copied", Toast.LENGTH_SHORT).show();
	}

	private String describeRuntimeState() {
		File pck = new File(resolveGameDir(), PCK_FILE);
		StringBuilder state = new StringBuilder();
		state.append("App version: ");
		state.append(describeAppVersion());
		state.append("\nAndroid SDK: ");
		state.append(Build.VERSION.SDK_INT);
		state.append("\nDevice: ");
		appendDeviceName(state);
		state.append("\nRuntime ABI: ");
		appendSupportedAbis(state);
		state.append("\nSelected Steam branch: ");
		state.append(readSelectedBranch());
		state.append("\nSelected Steam branch note: ");
		state.append(SteamBranchInfo.selectorHelpText(readSelectedBranch()));
		state.append("\nResolved game directory: ");
		state.append(resolveGameDir().getAbsolutePath());
		appendBranchMarkerState(state);
		state.append("\nGame PCK: ");
		state.append(pck.getAbsolutePath());
		state.append("\nPCK exists: ");
		state.append(pck.exists() && pck.isFile() ? "yes" : "no");
		if (pck.exists() && pck.isFile()) {
			state.append("\nPCK bytes: ");
			state.append(pck.length());
			state.append("\nPCK SHA-256: ");
			state.append(sha256Hex(pck));
			state.append("\nPCK magic valid: ");
			state.append(describePckMagicStatus(pck));
		}
		appendRuntimeSlotState(state);
		appendAssemblyCacheState(state);
		return state.toString();
	}

	private void appendRuntimeSlotState(StringBuilder state) {
		File marker = new File(getFilesDir(), CURRENT_RUNTIME_SLOT_MARKER);
		state.append("\nRuntime slot evidence: ");
		state.append(marker.getAbsolutePath());
		state.append("\nRuntime slot evidence exists: ");
		state.append(marker.exists() && marker.isFile() ? "yes" : "no");
		if (!marker.exists() || !marker.isFile()) {
			return;
		}

		try {
			JSONObject json = new JSONObject(readSmallTextFile(marker, 64 * 1024));
			state.append("\nRuntime slot branch: ");
			state.append(json.optString("branch", "<missing>"));
			state.append("\nRuntime slot ID: ");
			state.append(json.optString("runtimeSlotId", "<missing>"));
			state.append("\nRuntime slot files ready: ");
			state.append(json.optBoolean("filesReady", false) ? "yes" : "no");
			state.append("\nRuntime slot playable: ");
			state.append(json.optBoolean("playable", false) ? "yes" : "no");
			state.append("\nRuntime slot runtime compatible: ");
			state.append(json.optBoolean("runtimeCompatible", false) ? "yes" : "no");
			state.append("\nRuntime slot patch compatible: ");
			state.append(json.optBoolean("patchCompatible", false) ? "yes" : "no");
			String markerPckSha256 = json.optString("pckSha256", "");
			String markerSourceAssemblySha256 = json.optString("sourceAssemblySha256", "");
			File currentPck = new File(resolveGameDir(), PCK_FILE);
			String currentPckSha256 = currentPck.exists() && currentPck.isFile() ? sha256Hex(currentPck) : "";
			File currentSourceAssembly = findSelectedSourceAssembly();
			String currentSourceAssemblySha256 = currentSourceAssembly != null && currentSourceAssembly.exists() && currentSourceAssembly.isFile()
				? sha256Hex(currentSourceAssembly)
				: "";
			state.append("\nRuntime slot marker PCK SHA-256: ");
			state.append(markerPckSha256);
			state.append("\nRuntime slot current PCK SHA-256: ");
			state.append(currentPckSha256);
			state.append("\nRuntime slot PCK hash matches selected file: ");
			state.append(!markerPckSha256.isEmpty() && markerPckSha256.equalsIgnoreCase(currentPckSha256) ? "yes" : "no");
			state.append("\nRuntime slot marker source sts2.dll SHA-256: ");
			state.append(markerSourceAssemblySha256);
			state.append("\nRuntime slot current source sts2.dll: ");
			state.append(currentSourceAssembly == null ? "<missing>" : currentSourceAssembly.getAbsolutePath());
			state.append("\nRuntime slot current source sts2.dll SHA-256: ");
			state.append(currentSourceAssemblySha256);
			state.append("\nRuntime slot source sts2.dll hash matches selected file: ");
			state.append(!markerSourceAssemblySha256.isEmpty() && markerSourceAssemblySha256.equalsIgnoreCase(currentSourceAssemblySha256) ? "yes" : "no");
			state.append("\nRuntime slot readiness problem: ");
			state.append(json.optString("readinessProblem", ""));
			state.append("\nRuntime slot runtime pack usability: ");
			state.append(json.optString("runtimePackUsabilityStatus", ""));
			state.append("\nRuntime slot patch compatibility status: ");
			state.append(json.optString("patchCompatibilityStatus", ""));
		} catch (Exception e) {
			state.append("\nRuntime slot evidence readable: no");
			state.append("\nRuntime slot evidence read error: ");
			state.append(e.getMessage());
		}
	}

	private File findSelectedSourceAssembly() {
		File gameDir = resolveGameDir();
		if (!gameDir.exists() || !gameDir.isDirectory()) {
			return null;
		}

		File[] children = gameDir.listFiles();
		if (children == null) {
			return null;
		}

		File fallback = null;
		for (File child : children) {
			if (!child.isDirectory() || !child.getName().startsWith("data_")) {
				continue;
			}
			if (child.getName().contains("android") && containsAssemblies(child)) {
				return new File(child, GAME_CODE_ASSEMBLY);
			}
			if (fallback == null && containsAssemblies(child)) {
				fallback = child;
			}
		}

		return fallback == null ? null : new File(fallback, GAME_CODE_ASSEMBLY);
	}

	private boolean containsAssemblies(File dir) {
		if (dir == null || !dir.exists() || !dir.isDirectory()) {
			return false;
		}
		File[] files = dir.listFiles((file, name) -> name.endsWith(".dll"));
		return files != null && files.length > 0;
	}

	private void appendBranchMarkerState(StringBuilder state) {
		File marker = new File(resolveGameDir(), BRANCH_MARKER_FILE);
		String selectedBranch = readSelectedBranch();
		state.append("\nSteam branch marker: ");
		state.append(marker.getAbsolutePath());
		state.append("\nSteam branch marker exists: ");
		state.append(marker.exists() && marker.isFile() ? "yes" : "no");
		if (!marker.exists() || !marker.isFile()) {
			state.append("\nSteam branch marker ready: ");
			state.append("public".equalsIgnoreCase(selectedBranch) ? "yes" : "no");
			return;
		}

		try (java.io.BufferedReader reader = new java.io.BufferedReader(new java.io.FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (line.regionMatches(true, 0, "Branch:", 0, "Branch:".length())) {
					String markerBranch = line.substring("Branch:".length()).trim();
					state.append("\nSteam branch marker branch: ");
					state.append(markerBranch);
					state.append("\nSteam branch marker install slot kind: ");
					state.append(readMarkerValue(marker, "Install slot kind:"));
					state.append("\nSteam branch marker expected install slot kind: ");
					state.append(SteamBranchInfo.installSlotKind(selectedBranch));
					state.append("\nSteam branch marker install slot directory: ");
					state.append(readMarkerValue(marker, "Install slot directory:"));
					state.append("\nSteam branch marker expected install slot directory: ");
					state.append(SteamBranchInfo.installSlotDirectory(getFilesDir(), selectedBranch).getAbsolutePath());
					state.append("\nSteam branch marker has matching install slot provenance: ");
					state.append(hasInstallSlotProvenance(marker, selectedBranch) ? "yes" : "no");
					state.append("\nSteam branch marker has depot manifests: ");
					state.append(hasDepotManifestProvenance(marker) ? "yes" : "no");
					state.append("\nSteam branch marker has branch integrity provenance: ");
					state.append(hasBranchIntegrityProvenance(marker) ? "yes" : "no");
					state.append("\nSteam branch marker depot manifest entries: ");
					state.append(depotManifestCount(marker));
					state.append("\nSteam branch marker ready: ");
					state.append(
						markerBranch.equalsIgnoreCase(selectedBranch)
							&& ("public".equalsIgnoreCase(selectedBranch) || (hasInstallSlotProvenance(marker, selectedBranch) && hasDepotManifestProvenance(marker) && hasBranchIntegrityProvenance(marker)))
							? "yes"
							: "no"
					);
					return;
				}
			}
			state.append("\nSteam branch marker branch: <missing>");
			state.append("\nSteam branch marker has matching install slot provenance: no");
			state.append("\nSteam branch marker has depot manifests: no");
			state.append("\nSteam branch marker has branch integrity provenance: no");
			state.append("\nSteam branch marker depot manifest entries: 0");
			state.append("\nSteam branch marker ready: no");
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect Steam branch marker", e);
			state.append("\nSteam branch marker branch: <read failed>");
			state.append("\nSteam branch marker has matching install slot provenance: no");
			state.append("\nSteam branch marker has depot manifests: no");
			state.append("\nSteam branch marker has branch integrity provenance: no");
			state.append("\nSteam branch marker depot manifest entries: 0");
			state.append("\nSteam branch marker ready: no");
		}
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
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect Steam branch marker install slot provenance", e);
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
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect Steam branch marker depot provenance", e);
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

	private void appendAssemblyCacheState(StringBuilder state) {
		File assemblyDir = new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir());
		state.append("\nAssembly cache: ");
		state.append(assemblyDir.getAbsolutePath());
		state.append("\nAssembly cache exists: ");
		state.append(assemblyDir.exists() && assemblyDir.isDirectory() ? "yes" : "no");
		appendFileState(state, assemblyDir, "STS2Mobile.dll");
		appendFileState(state, assemblyDir, "GodotSharp.dll");
		appendFileState(state, assemblyDir, "System.Private.CoreLib.dll");
		appendFileState(state, assemblyDir, "sts2.dll");
	}

	private void appendFileState(StringBuilder state, File dir, String name) {
		File file = new File(dir, name);
		state.append("\nAssembly ");
		state.append(name);
		state.append(": ");
		if (!file.exists() || !file.isFile()) {
			state.append("missing");
			return;
		}
		state.append(file.length());
		state.append(" bytes");
	}

	private String readSmallTextFile(File file, int maxBytes) throws IOException {
		try (InputStream input = new FileInputStream(file); ByteArrayOutputStream output = new ByteArrayOutputStream()) {
			byte[] buffer = new byte[4096];
			int total = 0;
			int read;
			while ((read = input.read(buffer)) != -1 && total < maxBytes) {
				int toWrite = Math.min(read, maxBytes - total);
				output.write(buffer, 0, toWrite);
				total += toWrite;
			}
			return output.toString("UTF-8");
		}
	}

	private String getRuntimeGodotArchDir() {
		String nativeLibraryDir = getApplicationInfo().nativeLibraryDir;
		if (nativeLibraryDir != null) {
			if (nativeLibraryDir.contains("x86_64")) {
				return "x86_64";
			}
			if (nativeLibraryDir.contains("arm64")) {
				return "arm64";
			}
		}

		for (String abi : Build.SUPPORTED_ABIS) {
			if ("x86_64".equals(abi)) {
				return "x86_64";
			}
			if ("arm64-v8a".equals(abi)) {
				return "arm64";
			}
		}

		return "arm64";
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
			Log.w(TAG, "Failed to compute game PCK SHA-256 for native fallback diagnostics", e);
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
