package com.game.sts2launcher;

import org.godotengine.godot.GodotActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.activity.EdgeToEdge;
import androidx.core.content.FileProvider;
import androidx.core.splashscreen.SplashScreen;

import android.content.SharedPreferences;
import android.os.SystemClock;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.RandomAccessFile;
import java.io.ByteArrayOutputStream;
import java.io.BufferedReader;
import java.io.FileReader;
import java.security.KeyStore;
import java.security.KeyFactory;
import java.security.MessageDigest;
import java.security.PublicKey;
import java.security.SecureRandom;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.nio.charset.StandardCharsets;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.Mac;
import javax.crypto.SecretKey;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import javax.crypto.spec.GCMParameterSpec;
import java.security.spec.X509EncodedKeySpec;
import java.security.spec.RSAPublicKeySpec;

import android.content.Context;
import android.content.res.Configuration;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.net.wifi.WifiManager;
import android.text.InputType;
import android.util.Base64;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewStructure;
import android.view.Window;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputMethodManager;
import android.view.autofill.AutofillManager;
import android.widget.Button;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.net.HttpURLConnection;
import java.net.URL;

// Main activity for the mobile launcher. Handles .NET assembly setup, PCK loading,
// LAN multicast, and Android Keystore encryption for credentials.
public class GodotApp extends GodotActivity {
	private static final String TAG = "STS2Mobile";
	private static GodotApp instance;
	private static final SecureRandom SECURE_RANDOM = new SecureRandom();
	private WifiManager.MulticastLock multicastLock;
	private String gameDir;
	private File cachedRuntimePackDir;
	private String cachedRuntimePackValidationKey = "";
	private static final String KEYSTORE_ALIAS = "sts2mobile_credentials";
	private static final String PCK_FILE = "SlayTheSpire2.pck";
	private static final String PREFS_NAME = "sts2mobile";
	private static final String KEY_INSTALLED_VERSION_CODE = "installed_version_code";
	private static final String KEY_INSTALLED_PACKAGE_NAME = "installed_package_name";
	private static final String KEY_ASSEMBLY_CACHE_SCHEMA = "assembly_cache_schema";
	private static final String KEY_ASSEMBLY_CACHE_BRANCH = "assembly_cache_branch";
	private static final String KEY_ASSEMBLY_CACHE_RUNTIME_ID = "assembly_cache_runtime_id";
	private static final String KEY_LAUNCH_GAME_ON_NEXT_START = "launch_game_on_next_start";
	private static final String KEY_SAFE_LAUNCH_ON_NEXT_START = "safe_launch_on_next_start";
	private static final String GAME_BRANCH_FILE = "game_branch";
	private static final String GAME_VERSIONS_DIR = "game_versions";
	private static final String BRANCH_MARKER_FILE = "steam_branch.txt";
	private static final String ENV_LAUNCHER_BOOTSTRAP = "STS2_LAUNCHER_BOOTSTRAP";
	private static final String ENV_AUTO_LAUNCH_GAME = "STS2_AUTO_LAUNCH_GAME";
	private static final String ENV_AUTO_SAFE_LAUNCH = "STS2_AUTO_SAFE_LAUNCH";
	private static final String ENV_ANDROID_FILES_DIR = "STS2_ANDROID_FILES_DIR";
	private static final String ENV_BOOTSTRAP_UI_MODE = "STS2_BOOTSTRAP_UI_MODE";
	private static final String ENV_MINIMAL_BOOTSTRAP_UI = "STS2_MINIMAL_BOOTSTRAP_UI";
	private static final String ENV_FORCE_CRITICAL_PATCH_FAILURE = "STS2_FORCE_CRITICAL_PATCH_FAILURE";
	private static final String ENV_STEAMKIT_DEBUG_LOGS = "STS2_STEAMKIT_DEBUG_LOGS";
	private static final String EXTRA_LAUNCH_GAME_ON_START = "sts2_launch_game";
	private static final String EXTRA_SAFE_LAUNCH_ON_START = "sts2_safe_launch";
    private static final int ASSEMBLY_CACHE_SCHEMA = 24;
	private static final String PCK_ANDROID_PATCH_MARKER = ".android_pck_patch_v29";
	private static final String LAST_ANDROID_EXCEPTION_FILE = "last_android_uncaught_exception.txt";
	private static final String LAST_STARTUP_CONTEXT_FILE = "last_startup_context.txt";
	private static final String LAST_STARTUP_TIMELINE_FILE = "last_startup_timeline.txt";
	private static final long STREAM_HTTP_RESPONSE_THRESHOLD_BYTES = 256L * 1024L;
	private static final int MAX_BUFFERED_HTTP_RESPONSE_BYTES = 1024 * 1024;
	private static boolean exceptionHandlerInstalled;
	private long lastHttpResponseCleanupAt;
	private final Object steamLoginCredentialLock = new Object();
	private String pendingSteamLoginCredentialUsername = "";
	private String pendingSteamLoginCredentialPassword = "";
	private long pendingSteamLoginCredentialExpiresAtMs = 0L;
	private FrameLayout steamLoginCredentialOverlay;
	private ScrollView steamLoginCredentialScrollView;
	private EditText steamLoginCredentialUsernameField;
	private EditText steamLoginCredentialPasswordField;
	private TextView steamLoginCredentialStatusText;
	private Button steamLoginCredentialSubmitButton;
	private Button steamLoginCredentialCancelButton;
	private Button steamLoginCredentialNextPasswordButton;
	private Button steamLoginCredentialPasswordVisibilityButton;
	private boolean steamLoginCredentialPasswordVisible;
	private boolean steamLoginCredentialWideLayout;
	private boolean steamLoginCredentialShortHeightLayout;
	private static final String STEAM_CREDENTIAL_WEB_DOMAIN_STORE = "store.steampowered.com";
	private static final long STEAM_LOGIN_CREDENTIAL_RESULT_TTL_MS = 60L * 1000L;
	private static final String[] BOOTSTRAP_REQUIRED_ASSEMBLIES = {
		"STS2Mobile.dll",
		"SteamKit2.dll",
		"0Harmony.dll",
		"protobuf-net.dll",
		"protobuf-net.Core.dll",
		"System.IO.Hashing.dll",
		"System.Private.CoreLib.dll",
		"ZstdSharp.dll",
		"GodotSharp.dll"
	};
	private static final String[] GAME_REQUIRED_ASSEMBLIES = {
		"sts2.dll",
		"Steamworks.NET.dll",
		"Sentry.dll"
	};
	private static final String[] BRANCH_GAME_CODE_ASSEMBLIES = {
		"sts2.dll"
	};
	private static final String RUNTIME_PACKS_DIRECTORY = "runtime_packs";
	private static final String RUNTIME_PACK_ANDROID_ASSEMBLY = "sts2.dll";
	private static final String RUNTIME_PACK_COMPATIBILITY_MANIFEST = "compatibility.json";
	private static final String RUNTIME_PACK_PATCH_VALIDATION_REPORT = "patch_validation.json";
	private static final String CURRENT_RUNTIME_SLOT_MARKER = "current_runtime_slot.json";
	private static final String CURRENT_RUNTIME_CACHE_MARKER = "current_runtime_cache.txt";

	@Override
	public void onCreate(Bundle savedInstanceState) {
		instance = this;
		installAndroidExceptionHandler();
		recordStartupPhase("native godot activity onCreate", "GodotApp.onCreate entered");
		gameDir = resolveGameDir().getAbsolutePath();
		String selectedBranch = readSelectedBranch();
		boolean pendingGameLaunch = hasPendingGameLaunchRequest();
		recordStartupPhase("native game directory resolved", "branch=" + selectedBranch + "; pendingGameLaunch=" + pendingGameLaunch);
		File branchMarker = new File(gameDir, BRANCH_MARKER_FILE);
		Log.i(TAG, "Selected Steam branch: " + selectedBranch);
		Log.i(TAG, "Selected Steam branch note: " + SteamBranchInfo.selectorHelpText(selectedBranch));
		Log.i(TAG, "Selected game version slot kind: " + SteamBranchInfo.installSlotKind(selectedBranch));
		Log.i(TAG, "Selected game version slot directory: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), selectedBranch).getAbsolutePath());
		Log.i(TAG, "Resolved game directory: " + gameDir);
		Log.i(TAG, "Selected game PCK before Godot init: " + describeGamePck(new File(gameDir, PCK_FILE), false));
		Log.i(TAG, "Steam branch marker install slot kind: " + readMarkerValue(branchMarker, "Install slot kind:"));
		Log.i(TAG, "Steam branch marker expected install slot kind: " + SteamBranchInfo.installSlotKind(selectedBranch));
		Log.i(TAG, "Steam branch marker install slot directory: " + readMarkerValue(branchMarker, "Install slot directory:"));
		Log.i(TAG, "Steam branch marker expected install slot directory: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), selectedBranch).getAbsolutePath());
		Log.i(TAG, "Steam branch marker has matching install slot provenance: " + hasInstallSlotProvenance(branchMarker, selectedBranch));
		Log.i(TAG, "Steam branch marker has depot manifests: " + hasDepotManifestProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker has branch integrity provenance: " + hasBranchIntegrityProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker depot manifest entries: " + depotManifestCount(branchMarker));
		Log.i(TAG, "Steam branch marker ready: " + isBranchMarkerReady(selectedBranch));
		configureTempDirectory();
		configureMonoForEmulator();
		cleanupStaleHttpResponseFiles();
		logStartupFreshnessProbe(pendingGameLaunch);

		recordStartupPhase("native splash setup", "Installing splash screen and edge-to-edge");
		SplashScreen.installSplashScreen(this);
		EdgeToEdge.enable(this);

		try {
			recordStartupPhase("native assembly setup", "Preparing Mono/.NET assembly cache");
			setupAssemblies();
			recordStartupPhase("native assembly setup complete", "Assembly cache prepared");
		} catch (RuntimeException ex) {
			recordStartupPhase("native assembly setup failed", ex.getMessage());
			Log.e(TAG, "Assembly setup failed, attempting one-time cache reset", ex);
			resetAssemblyCacheState();
			try {
				recordStartupPhase("native assembly setup retry", "Cache reset complete");
				setupAssemblies();
				recordStartupPhase("native assembly setup retry complete", "Assembly cache prepared after reset");
			} catch (RuntimeException ex2) {
				recordStartupPhase("native assembly setup fatal", ex2.getMessage());
				Log.e(TAG, "Assembly setup failed after recovery. Routing to native diagnostics instead of starting Godot.", ex2);
				showNativeFailure(
					"StS2 Mobile diagnostics",
					"The launcher could not prepare the Android .NET assemblies required by native Godot.\n\nNative Godot was not started, because continuing would only trigger the generic '.NET assemblies not found' failure.",
					Log.getStackTraceString(ex2)
				);
				return;
			}
		}
		recordStartupPhase("native godot super onCreate", "Starting Godot runtime");
		super.onCreate(savedInstanceState);
		recordStartupPhase("native godot super onCreate complete", "Godot runtime returned from onCreate");

		// Android WiFi power saving drops broadcast packets without a MulticastLock.
		try {
			WifiManager wifiMgr = (WifiManager) getApplicationContext().getSystemService(Context.WIFI_SERVICE);
			multicastLock = wifiMgr.createMulticastLock("sts2_lan_discovery");
			multicastLock.setReferenceCounted(false);
			multicastLock.acquire();
			recordStartupPhase("native multicast lock acquired");
			Log.i(TAG, "WiFi MulticastLock acquired for LAN discovery");
		} catch (Exception e) {
			recordStartupPhase("native multicast lock failed", e.getMessage());
			Log.w(TAG, "Failed to acquire MulticastLock", e);
		}
	}

	private void installAndroidExceptionHandler() {
		if (exceptionHandlerInstalled) {
			return;
		}
		exceptionHandlerInstalled = true;

		final Thread.UncaughtExceptionHandler previous = Thread.getDefaultUncaughtExceptionHandler();
		Thread.setDefaultUncaughtExceptionHandler((thread, throwable) -> {
			String text =
				"UTC millis: " + System.currentTimeMillis() + "\n" +
				"Startup context: " + readInternalTextFile(LAST_STARTUP_CONTEXT_FILE) + "\n" +
				"Thread: " + (thread != null ? thread.getName() : "<unknown>") + "\n" +
				"Package: " + getPackageName() + "\n" +
				"Version: " + BuildConfig.VERSION_NAME + " (" + BuildConfig.VERSION_CODE + ")\n\n" +
				Log.getStackTraceString(throwable);
			writeInternalTextFile(LAST_ANDROID_EXCEPTION_FILE, text);
			Log.e(TAG, "Uncaught Android exception persisted", throwable);

			if (previous != null) {
				previous.uncaughtException(thread, throwable);
			} else {
				Runtime.getRuntime().exit(2);
			}
		});
		Log.i(TAG, "Android uncaught exception handler installed");
	}

	private void recordStartupPhase(String phase) {
		recordStartupPhase(phase, "");
	}

	private void recordStartupPhase(String phase, String detail) {
		String safePhase = sanitizeStartupMarkerValue(phase);
		String safeDetail = sanitizeStartupMarkerValue(detail);
		long elapsedMs = SystemClock.elapsedRealtime();
		long utcMillis = System.currentTimeMillis();
		String context =
			"StS2 Mobile native startup context\n" +
			"UTC millis: " + utcMillis + "\n" +
			"Elapsed realtime ms: " + elapsedMs + "\n" +
			"Phase: " + safePhase + "\n" +
			"Detail: " + safeDetail + "\n" +
			"Package: " + getPackageName() + "\n" +
			"Version: " + BuildConfig.VERSION_NAME + " (" + BuildConfig.VERSION_CODE + ")\n" +
			"Selected branch: " + readSelectedBranchSafely() + "\n";
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

	private String readSelectedBranchSafely() {
		try {
			return readSelectedBranch();
		} catch (Exception e) {
			return "<unavailable:" + e.getClass().getSimpleName() + ">";
		}
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

	private String readInternalTextFile(String name) {
		File file = new File(getFilesDir(), name);
		if (!file.exists() || !file.isFile()) {
			return "<missing>";
		}
		try (FileInputStream in = new FileInputStream(file)) {
			ByteArrayOutputStream out = new ByteArrayOutputStream();
			byte[] buffer = new byte[4096];
			int read;
			while ((read = in.read(buffer)) != -1 && out.size() < 64 * 1024) {
				out.write(buffer, 0, read);
			}
			return out.toString("UTF-8");
		} catch (Exception e) {
			return "<failed:" + e.getClass().getSimpleName() + ">";
		}
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
		try (FileInputStream in = new FileInputStream(branchFile);
				ByteArrayOutputStream out = new ByteArrayOutputStream()) {
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

	private void configureMonoForEmulator() {
		// Native x86_64 emulator builds should use the normal Mono execution mode.
		// Forcing interpreter options here can destabilize Godot's Mono startup.
	}

	private void showNativeFailure(String title, String message, String diagnostics) {
		Intent intent = new Intent(this, NativeFallbackActivity.class);
		intent.putExtra(NativeFallbackActivity.EXTRA_REASON_TITLE, title);
		intent.putExtra(NativeFallbackActivity.EXTRA_REASON_MESSAGE, message);
		intent.putExtra(NativeFallbackActivity.EXTRA_REASON_DIAGNOSTICS, diagnostics);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		startActivity(intent);
		finish();
	}

	private boolean shouldRefreshAssemblyCache(String currentRuntimeId) {
		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		int lastSchema = prefs.getInt(KEY_ASSEMBLY_CACHE_SCHEMA, -1);
		int lastVersion = prefs.getInt(KEY_INSTALLED_VERSION_CODE, -1);
		int currentVersion = BuildConfig.VERSION_CODE;

		if (
			lastSchema == ASSEMBLY_CACHE_SCHEMA &&
			lastVersion == currentVersion &&
			getPackageName().equals(prefs.getString(KEY_INSTALLED_PACKAGE_NAME, ""))
		) {
			File destDir = new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir());
			String cachedRuntimeId = prefs.getString(KEY_ASSEMBLY_CACHE_RUNTIME_ID, "");
			if (!currentRuntimeId.equals(cachedRuntimeId)) {
				Log.i(TAG, "Assembly cache runtime changed from " + cachedRuntimeId + " to " + currentRuntimeId);
				return true;
			}
			return !hasRequiredCacheFiles(destDir);
		}

		return true;
	}

	private String bootstrapRuntimeCacheId() {
		return "bootstrap"
			+ "|package=" + getPackageName()
			+ "|version=" + BuildConfig.VERSION_CODE
			+ "|schema=" + ASSEMBLY_CACHE_SCHEMA
			+ "|arch=" + getRuntimeGodotArchDir();
	}

	private void logStartupFreshnessProbe(boolean includeSelectedRuntime) {
		try {
			SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
			String arch = getRuntimeGodotArchDir();
			File destDir = new File(getFilesDir(), ".godot/mono/publish/" + arch);
			File patcher = new File(destDir, "STS2Mobile.dll");
			Log.i(
				TAG,
				"Android startup freshness:"
					+ " package=" + getPackageName()
					+ " versionName=" + BuildConfig.VERSION_NAME
					+ " versionCode=" + BuildConfig.VERSION_CODE
					+ " schema=" + ASSEMBLY_CACHE_SCHEMA
					+ " storedSchema=" + prefs.getInt(KEY_ASSEMBLY_CACHE_SCHEMA, -1)
					+ " storedVersionCode=" + prefs.getInt(KEY_INSTALLED_VERSION_CODE, -1)
					+ " storedPackage=" + prefs.getString(KEY_INSTALLED_PACKAGE_NAME, "")
					+ " storedRuntimeId=" + prefs.getString(KEY_ASSEMBLY_CACHE_RUNTIME_ID, "")
					+ " currentRuntimeId=" + (includeSelectedRuntime ? currentRuntimeCacheId() : bootstrapRuntimeCacheId())
					+ " arch=" + arch
					+ " cacheDirExists=" + destDir.isDirectory()
					+ " sts2MobileBytes=" + (patcher.exists() ? patcher.length() : -1)
			);
		} catch (Exception e) {
			Log.w(TAG, "Android startup freshness probe failed", e);
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

		for (String abi : android.os.Build.SUPPORTED_ABIS) {
			if ("x86_64".equals(abi)) {
				return "x86_64";
			}
			if ("arm64-v8a".equals(abi)) {
				return "arm64";
			}
		}

		return "arm64";
	}

	private boolean isX86Runtime() {
		String nativeLibraryDir = getApplicationInfo().nativeLibraryDir;
		if (nativeLibraryDir != null) {
			return nativeLibraryDir.contains("x86_64");
		}

		for (String abi : android.os.Build.SUPPORTED_ABIS) {
			if (abi != null && abi.contains("x86")) {
				return true;
			}
		}

		return false;
	}

	private boolean hasRequiredCacheFiles(File destDir, boolean requireGameAssemblies) {
		if (destDir == null || !destDir.exists() || !destDir.isDirectory()) {
			return false;
		}

		ArrayList<String> required = new ArrayList<>(java.util.Arrays.asList(BOOTSTRAP_REQUIRED_ASSEMBLIES));
		if (requireGameAssemblies)
			required.addAll(java.util.Arrays.asList(GAME_REQUIRED_ASSEMBLIES));

		for (String fileName : required.toArray(new String[0])) {
			File file = new File(destDir, fileName);
			if (!file.exists()) {
				Log.w(TAG, "Missing required cache file: " + file.getAbsolutePath());
				return false;
			}
		}

		return true;
	}

	private boolean hasRequiredCacheFiles(File destDir) {
		return hasRequiredCacheFiles(destDir, false);
	}

	private void logAssemblyCacheState(String phase, File destDir, File srcDir, boolean requireGameAssemblies, Set<String> packagedBclNames, String runtimeCacheId) {
		StringBuilder sb = new StringBuilder();
		sb.append("Assembly cache diagnostics [").append(phase).append("]");
		sb.append(" schema=").append(ASSEMBLY_CACHE_SCHEMA);
		sb.append(" arch=").append(getRuntimeGodotArchDir());
		sb.append(" nativeLibraryDir=").append(getApplicationInfo().nativeLibraryDir);
		sb.append(" filesDir=").append(getFilesDir().getAbsolutePath());
		sb.append(" dest=").append(destDir == null ? "<none>" : destDir.getAbsolutePath());
		sb.append(" destExists=").append(destDir != null && destDir.exists());
		sb.append(" destIsDir=").append(destDir != null && destDir.isDirectory());
		sb.append(" src=").append(srcDir == null ? "<none>" : srcDir.getAbsolutePath());
		sb.append(" srcExists=").append(srcDir != null && srcDir.exists());
		sb.append(" runtimeId=").append(runtimeCacheId);
		sb.append(" packagedBclCount=").append(packagedBclNames == null ? 0 : packagedBclNames.size());
		sb.append(" requireGameAssemblies=").append(requireGameAssemblies);
		Log.i(TAG, sb.toString());

		ArrayList<String> required = new ArrayList<>(java.util.Arrays.asList(BOOTSTRAP_REQUIRED_ASSEMBLIES));
		if (requireGameAssemblies)
			required.addAll(java.util.Arrays.asList(GAME_REQUIRED_ASSEMBLIES));
		for (String name : required.toArray(new String[0])) {
			File file = destDir == null ? null : new File(destDir, name);
			File sourceFile = srcDir == null ? null : new File(srcDir, name);
			boolean branchGameCodeBlocked = selectedBranchRequiresRuntimePack() && isBranchGameCodeAssembly(name);
			boolean branchGameCode = isBranchGameCodeAssembly(name) && sourceFile != null && sourceFile.exists() && !branchGameCodeBlocked;
			boolean packagedBcl = packagedBclNames != null && packagedBclNames.contains(name) && !branchGameCode;
			long sourceBytes = branchGameCodeBlocked
				? 0
				: branchGameCode
				? sourceFile.length()
				: packagedBcl
				? packagedAssetLength("dotnet_bcl/" + name)
				: sourceFile != null && sourceFile.exists()
					? sourceFile.length()
					: packagedAssetLength("dotnet_bcl/" + name);
			String expectedSource = branchGameCodeBlocked
				? "no-usable-runtime"
				: branchGameCode
				? "selected-game"
				: packagedBcl
				? "packaged-bcl"
				: sourceFile != null && sourceFile.exists()
					? "game"
					: "packaged-bcl-fallback";
			Log.i(TAG, "Assembly cache required file [" + phase + "]: " + name
				+ " exists=" + (file != null && file.exists())
				+ " bytes=" + (file != null && file.exists() ? file.length() : 0)
				+ " expectedSource=" + expectedSource
				+ " expectedBytes=" + sourceBytes);
		}
	}

	private long packagedAssetLength(String assetPath) {
		try (InputStream asset = getAssets().open(assetPath)) {
			long total = 0L;
			byte[] buffer = new byte[8192];
			int read;
			while ((read = asset.read(buffer)) > 0) {
				total += read;
			}
			return total;
		} catch (IOException e) {
			return -1L;
		}
	}

	private void markAssemblyCacheStateAsCurrent(int currentVersion) {
		markAssemblyCacheStateAsCurrent(currentVersion, currentRuntimeCacheId(), true);
	}

	private void markAssemblyCacheStateAsCurrent(int currentVersion, String currentRuntimeId, boolean writeRuntimeMarker) {
		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		prefs.edit()
			.putInt(KEY_ASSEMBLY_CACHE_SCHEMA, ASSEMBLY_CACHE_SCHEMA)
			.putString(KEY_ASSEMBLY_CACHE_BRANCH, readSelectedBranch())
			.putString(KEY_ASSEMBLY_CACHE_RUNTIME_ID, currentRuntimeId)
			.putInt(KEY_INSTALLED_VERSION_CODE, currentVersion)
			.putString(KEY_INSTALLED_PACKAGE_NAME, getPackageName())
			.apply();
		if (writeRuntimeMarker) {
			writeRuntimeCacheMarker(currentVersion, currentRuntimeId);
		}
	}

	private void writeRuntimeCacheMarker(int currentVersion, String currentRuntimeId) {
		try {
			String selectedBranch = readSelectedBranch();
			File pck = new File(gameDir, PCK_FILE);
			File srcDir = findAssembliesDir();
			File runtimePackDir = findRuntimePackDir();
			File runtimePackGameAssembly = runtimePackGameAssembly(runtimePackDir);
			File selectedSourceAssembly = srcDir == null ? null : new File(srcDir, "sts2.dll");
			boolean selectedBranchRequiresRuntimePack = selectedBranchRequiresRuntimePack();
			File activeSourceAssembly = runtimePackGameAssembly != null && runtimePackGameAssembly.exists()
				? runtimePackGameAssembly
				: selectedBranchRequiresRuntimePack
					? null
					: selectedSourceAssembly;
			File publishDir = new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir());
			String runtimeSource = runtimePackGameAssembly != null && runtimePackGameAssembly.exists()
				? "runtime-pack"
				: selectedBranchRequiresRuntimePack
					? "no-usable-runtime"
					: "selected-game";
			String text =
				"UTC millis: " + System.currentTimeMillis() + "\n"
				+ "Package: " + getPackageName() + "\n"
				+ "Version name: " + BuildConfig.VERSION_NAME + "\n"
				+ "Version code: " + currentVersion + "\n"
				+ "Assembly cache schema: " + ASSEMBLY_CACHE_SCHEMA + "\n"
				+ "Selected branch: " + selectedBranch + "\n"
				+ "Runtime ID: " + currentRuntimeId + "\n"
				+ "Runtime source: " + runtimeSource + "\n"
				+ "Runtime pack directory: " + (runtimePackDir == null ? "<none>" : runtimePackDir.getAbsolutePath()) + "\n"
				+ "Runtime pack game assembly: " + (runtimePackGameAssembly == null ? "<none>" : runtimePackGameAssembly.getAbsolutePath()) + "\n"
				+ "Selected branch requires runtime pack: " + selectedBranchRequiresRuntimePack + "\n"
				+ "Game directory: " + gameDir + "\n"
				+ "Selected PCK path: " + pck.getAbsolutePath() + "\n"
				+ "Selected PCK identity: " + fileIdentity(pck) + "\n"
				+ "Selected PCK SHA256: " + (pck.exists() && pck.isFile() ? sha256Hex(pck) : "<missing>") + "\n"
				+ "Selected source sts2.dll: " + (selectedSourceAssembly == null ? "<none>" : selectedSourceAssembly.getAbsolutePath()) + "\n"
				+ "Selected source sts2.dll SHA256: " + (selectedSourceAssembly != null && selectedSourceAssembly.exists() ? sha256Hex(selectedSourceAssembly) : "<missing>") + "\n"
				+ "Active source sts2.dll: " + (activeSourceAssembly == null ? "<none>" : activeSourceAssembly.getAbsolutePath()) + "\n"
				+ "Active source sts2.dll SHA256: " + (activeSourceAssembly != null && activeSourceAssembly.exists() ? sha256Hex(activeSourceAssembly) : "<missing>") + "\n"
				+ "Publish cache directory: " + publishDir.getAbsolutePath() + "\n"
				+ "Publish cache active sts2.dll SHA256: " + sha256Hex(new File(publishDir, "sts2.dll")) + "\n";
			writeInternalTextFile(CURRENT_RUNTIME_CACHE_MARKER, text);
		} catch (Exception e) {
			Log.w(TAG, "Failed to write runtime cache marker", e);
		}
	}

	private void resetAssemblyCacheState() {
		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		prefs.edit()
			.remove(KEY_ASSEMBLY_CACHE_SCHEMA)
			.remove(KEY_ASSEMBLY_CACHE_BRANCH)
			.remove(KEY_ASSEMBLY_CACHE_RUNTIME_ID)
			.remove(KEY_INSTALLED_VERSION_CODE)
			.remove(KEY_INSTALLED_PACKAGE_NAME)
			.apply();
		File destDir = new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir());
		clearAssemblyCache(destDir);
	}

	private void clearAssemblyCache(File dir) {
		if (dir == null || !dir.exists()) {
			return;
		}
		File[] files = dir.listFiles();
		if (files == null) {
			return;
		}
		for (File file : files) {
			deleteRecursive(file);
		}
		Log.i(TAG, "Cleared assembly cache: " + dir.getAbsolutePath());
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
			Log.w(TAG, "Could not delete cached file: " + target.getAbsolutePath());
		}
	}

	private void configureTempDirectory() {
		File tempDir = new File(getFilesDir(), "tmp");
		if (!tempDir.exists() && !tempDir.mkdirs()) {
			Log.w(TAG, "Failed to create temp directory: " + tempDir.getAbsolutePath());
			return;
		}

		String tempPath = tempDir.getAbsolutePath();
		System.setProperty("java.io.tmpdir", tempPath);
		try {
			android.system.Os.setenv("TMPDIR", tempPath, true);
			android.system.Os.setenv("TMP", tempPath, true);
			android.system.Os.setenv("TEMP", tempPath, true);
			Log.i(TAG, "Configured native temp directory: " + tempPath);
		} catch (Exception e) {
			Log.w(TAG, "Failed to configure native temp directory", e);
		}
	}

	// Copies .NET BCL from APK assets and game assemblies from the download
	// directory
	// into the location Godot expects. Skips if already done unless the APK version
	// changed.
	private void setupAssemblies() {
		boolean pendingGameLaunch = hasPendingGameLaunchRequest();
		File srcDir = pendingGameLaunch ? findAssembliesDir() : null;
		File runtimePackDir = pendingGameLaunch ? findRuntimePackDir() : null;
		File runtimePackGameAssembly = runtimePackGameAssembly(runtimePackDir);
		File destDir = new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir());
		int currentVersion = BuildConfig.VERSION_CODE;
		boolean gameReady = isGamePckReady();
		boolean hasRuntimePackGameAssembly = runtimePackGameAssembly != null
			&& runtimePackGameAssembly.exists()
			&& runtimePackGameAssembly.isFile();
		boolean selectedBranchRequiresRuntimePack = gameReady
			&& !"public".equalsIgnoreCase(readSelectedBranch());
		boolean requiresGameAssemblies = gameReady
			&& pendingGameLaunch
			&& (hasGameAssemblies(srcDir) || hasRuntimePackGameAssembly)
			&& (!selectedBranchRequiresRuntimePack || hasRuntimePackGameAssembly);
		Set<String> packagedBclNames = getPackagedBclNames();
		String assemblyCacheRuntimeId = pendingGameLaunch ? currentRuntimeCacheId() : bootstrapRuntimeCacheId();

		boolean launcherBootstrapHasGameCodeAssemblies = !requiresGameAssemblies && hasStaleCachedBranchGameCodeAssemblies(destDir);
		boolean refreshCache = shouldRefreshAssemblyCache(assemblyCacheRuntimeId)
			|| shouldRefreshAssemblyCacheForSelectedBranch()
			|| launcherBootstrapHasGameCodeAssemblies;
		logAssemblyCacheState("before-copy", destDir, srcDir, requiresGameAssemblies, packagedBclNames, assemblyCacheRuntimeId);
		Log.i(TAG, "Runtime pack directory: " + (runtimePackDir == null ? "<none>" : runtimePackDir.getAbsolutePath()));
		Log.i(TAG, "Runtime pack game assembly: " + (hasRuntimePackGameAssembly ? runtimePackGameAssembly.getAbsolutePath() : "<none>"));
		if (selectedBranchRequiresRuntimePack && !hasRuntimePackGameAssembly && pendingGameLaunch) {
			Log.w(TAG, "Selected non-public branch requires a usable runtime pack for game launch; selected-game sts2.dll fallback will not be copied.");
			clearAssemblyCache(destDir);
			logAssemblyCacheState("blocked-no-runtime-pack", destDir, srcDir, requiresGameAssemblies, packagedBclNames, assemblyCacheRuntimeId);
			writeRuntimeCacheMarker(currentVersion, assemblyCacheRuntimeId);
			throw new RuntimeException(
				"Selected Steam branch '" + readSelectedBranch()
					+ "' has no usable Android runtime pack. Native Godot startup was blocked to avoid loading stale or public sts2.dll code against the selected branch PCK."
			);
		}
		if (selectedBranchRequiresRuntimePack && !hasRuntimePackGameAssembly && pendingGameLaunch) {
			Log.w(TAG, "Selected non-public branch has no usable runtime pack yet; starting launcher-only bootstrap without selected-game sts2.dll so managed validation can regenerate runtime-pack evidence.");
		}

		File patcherMarker = new File(destDir, "STS2Mobile.dll");
		File sts2Marker = new File(destDir, "sts2.dll");
		if (!gameReady && sts2Marker.exists()) {
			Log.w(TAG, "Game PCK is not ready; clearing stale game assembly cache");
			refreshCache = true;
		}
		if (
			!refreshCache
				&& hasRequiredCacheFiles(destDir, requiresGameAssemblies)
				&& hasCurrentPackagedRequiredAssemblies(destDir)
				&& (!requiresGameAssemblies || hasCachedGameAssemblies(destDir, srcDir, packagedBclNames, runtimePackDir, runtimePackGameAssembly))
		) {
			Log.i(TAG, "Assemblies already set up at: " + destDir.getAbsolutePath());
			logAssemblyCacheState("cache-hit", destDir, srcDir, requiresGameAssemblies, packagedBclNames, assemblyCacheRuntimeId);
			markAssemblyCacheStateAsCurrent(
				currentVersion,
				assemblyCacheRuntimeId,
				pendingGameLaunch
			);
			return;
		}

		if (refreshCache) {
			Log.i(TAG, "Assembly cache refresh required, re-copying all assemblies");
			clearAssemblyCache(destDir);
		}

		destDir.mkdirs();
		if (!destDir.exists() || !destDir.isDirectory()) {
			throw new RuntimeException("Failed to create Mono/cache assembly directory: " + destDir.getAbsolutePath());
		}

		try {
			if (!packagedBclNames.isEmpty()) {
				int count = 0;
				for (String name : packagedBclNames) {
					try (InputStream in = getAssets().open("dotnet_bcl/" + name);
							OutputStream out = new FileOutputStream(new File(destDir, name))) {
						byte[] buf = new byte[8192];
						int len;
						while ((len = in.read(buf)) > 0) {
							out.write(buf, 0, len);
						}
						count++;
					}
				}
				Log.i(TAG, "Copied " + count + " BCL assemblies from assets");
			}
		} catch (IOException e) {
			Log.e(TAG, "Failed to copy BCL assemblies", e);
		}

		// Only copy game assemblies that don't already exist in BCL. The depot has
		// desktop
		// CoreCLR versions that are incompatible with Android's Mono runtime.
		if (srcDir == null || !srcDir.exists() || !srcDir.isDirectory()) {
			Log.w(TAG, "Game assemblies source dir not found: " + (srcDir == null ? "<none>" : srcDir.getAbsolutePath()));
		} else {
			File[] files = srcDir.listFiles();
			if (files != null) {
				Log.i(TAG, "Copying game assemblies from " + srcDir + " to " + destDir);
				int count = 0;
				for (File src : files) {
					if (src.isFile()) {
						String name = src.getName();
						if (hasRuntimePackGameAssembly && isBranchGameCodeAssembly(name)) {
							continue;
						}
						if (selectedBranchRequiresRuntimePack && !hasRuntimePackGameAssembly && isBranchGameCodeAssembly(name)) {
							Log.w(TAG, "Skipping selected-game branch code assembly without usable runtime pack: " + name);
							continue;
						}
						if (!shouldCopyGameAssemblyFile(name, packagedBclNames)) {
							continue;
						}
						File dest = new File(destDir, name);
						try {
							copyFile(src, dest);
							count++;
						} catch (IOException e) {
							Log.e(TAG, "Failed to copy: " + name, e);
						}
					}
				}
				Log.i(TAG, "Copied " + count + " game assembly files");
			}
		}

		if (hasRuntimePackGameAssembly) {
			Set<String> runtimePackDeclaredAssemblies = runtimePackDeclaredAssemblyNames(runtimePackDir);
			File[] files = runtimePackDir.listFiles();
			if (files != null) {
				int count = 0;
				for (File src : files) {
					if (!src.isFile()) {
						continue;
					}
					if (!runtimePackDeclaredAssemblies.contains(src.getName().toLowerCase(java.util.Locale.ROOT))) {
						continue;
					}
					if (!shouldCopyGameAssemblyFile(src.getName(), packagedBclNames)) {
						continue;
					}
					File dest = new File(destDir, src.getName());
					try {
						copyFile(src, dest);
						count++;
					} catch (IOException e) {
						Log.e(TAG, "Failed to copy runtime-pack assembly: " + src.getName(), e);
					}
				}
				Log.i(TAG, "Copied " + count + " runtime-pack assembly files");
			}
		}

		if (!hasRequiredCacheFiles(destDir, requiresGameAssemblies)) {
			logAssemblyCacheState("missing-after-copy", destDir, srcDir, requiresGameAssemblies, packagedBclNames, assemblyCacheRuntimeId);
			String mode = requiresGameAssemblies ? "game" : "launcher-only";
			throw new RuntimeException("Missing required Mono/cache assemblies after copy for " + mode + " mode.");
		}

		logAssemblyCacheState("after-copy", destDir, srcDir, requiresGameAssemblies, packagedBclNames, assemblyCacheRuntimeId);
		markAssemblyCacheStateAsCurrent(
			currentVersion,
			assemblyCacheRuntimeId,
			pendingGameLaunch
		);
	}

	private boolean shouldRefreshAssemblyCacheForSelectedBranch() {
		String selectedBranch = readSelectedBranch();
		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		String cachedBranch = prefs.getString(KEY_ASSEMBLY_CACHE_BRANCH, "");
		if (cachedBranch == null || cachedBranch.trim().isEmpty()) {
			if (isGamePckReady()) {
				Log.i(TAG, "Assembly cache has no selected-branch marker; refreshing for branch: " + selectedBranch);
				return true;
			}
			return false;
		}

		if (!cachedBranch.trim().equalsIgnoreCase(selectedBranch)) {
			Log.i(TAG, "Assembly cache branch changed from " + cachedBranch + " to " + selectedBranch + "; refreshing game assemblies");
			return true;
		}

		return false;
	}

	private boolean hasStaleCachedBranchGameCodeAssemblies(File destDir) {
		if (destDir == null || !destDir.exists() || !destDir.isDirectory()) {
			return false;
		}

		for (String gameCodeAssembly : BRANCH_GAME_CODE_ASSEMBLIES) {
			File cached = new File(destDir, gameCodeAssembly);
			if (cached.exists() && cached.isFile() && !matchesPackagedAsset(cached, "dotnet_bcl/" + gameCodeAssembly)) {
				Log.i(TAG, "Launcher bootstrap assembly cache contains stale branch game-code assembly; refreshing: " + cached.getAbsolutePath());
				return true;
			}
		}

		return false;
	}

	private boolean matchesPackagedAsset(File file, String assetPath) {
		if (file == null || assetPath == null || !file.exists() || !file.isFile()) {
			return false;
		}
		long assetLength = packagedAssetLength(assetPath);
		if (assetLength < 0 || file.length() != assetLength) {
			return false;
		}
		try {
			return sha256Hex(file).equalsIgnoreCase(sha256HexAsset(assetPath));
		} catch (Exception e) {
			Log.w(TAG, "Failed to compare cached assembly with packaged asset: " + assetPath, e);
			return false;
		}
	}

	private String sha256HexAsset(String assetPath) throws IOException {
		try (InputStream in = getAssets().open(assetPath)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = in.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return bytesToHex(digest.digest());
		} catch (java.security.NoSuchAlgorithmException e) {
			throw new IOException("SHA-256 unavailable", e);
		}
	}

	private Set<String> getPackagedBclNames() {
		Set<String> names = new HashSet<>();
		try {
			String[] bclFiles = getAssets().list("dotnet_bcl");
			if (bclFiles != null) {
				for (String name : bclFiles) {
					names.add(name);
				}
			}
		} catch (IOException e) {
			Log.e(TAG, "Failed to list packaged BCL assemblies", e);
		}
		return names;
	}

	private boolean shouldCopyGameAssemblyFile(String name, Set<String> packagedBclNames) {
		if (name == null) {
			return false;
		}

		String lower = name.toLowerCase(java.util.Locale.ROOT);
		if (packagedBclNames.contains(name) && !isBranchGameCodeAssembly(lower)) {
			return false;
		}

		if (!lower.endsWith(".dll") && !lower.endsWith(".json")) {
			return false;
		}

		if (lower.equals("coreclr.dll")
			|| lower.equals("clrjit.dll")
			|| lower.equals("clrgc.dll")
			|| lower.equals("clrgcexp.dll")
			|| lower.equals("clretwrc.dll")
			|| lower.equals("createdump.exe")
			|| lower.equals("hostfxr.dll")
			|| lower.equals("hostpolicy.dll")
			|| lower.equals("mscordaccore.dll")
			|| lower.equals("mscordbi.dll")
			|| lower.equals("mscorrc.dll")
			|| lower.equals("msquic.dll")
			|| lower.equals("steam_api64.dll")
			|| lower.equals("sharpgen.runtime.dll")
			|| lower.equals("sharpgen.runtime.com.dll")
			|| lower.equals("system.io.compression.native.dll")
			|| lower.equals("vortice.directx.dll")
			|| lower.equals("vortice.dxgi.dll")
			|| lower.equals("vortice.mathematics.dll")
			|| lower.startsWith("mscordaccore_")
			|| lower.startsWith("microsoft.diasymreader.native.")) {
			return false;
		}

		return true;
	}

	private boolean isBranchGameCodeAssembly(String name) {
		if (name == null) {
			return false;
		}
		String lower = name.toLowerCase(java.util.Locale.ROOT);
		for (String gameCodeAssembly : BRANCH_GAME_CODE_ASSEMBLIES) {
			if (lower.equals(gameCodeAssembly.toLowerCase(java.util.Locale.ROOT))) {
				return true;
			}
		}
		return false;
	}

	private String currentRuntimeCacheId() {
		String selectedBranch = readSelectedBranch();
		File pck = new File(gameDir, PCK_FILE);
		File srcDir = findAssembliesDir();
		File runtimePackDir = findRuntimePackDir();
		File runtimePackGameAssembly = runtimePackGameAssembly(runtimePackDir);
		boolean selectedBranchRequiresRuntimePack = selectedBranchRequiresRuntimePack();
		File sourceGameAssembly = runtimePackGameAssembly != null && runtimePackGameAssembly.exists()
			? runtimePackGameAssembly
			: selectedBranchRequiresRuntimePack
				? null
			: (srcDir == null ? null : new File(srcDir, "sts2.dll"));
		String pckIdentity = fileIdentity(pck);
		String sourceHash = sourceGameAssembly != null && sourceGameAssembly.exists()
			? sha256Hex(sourceGameAssembly)
			: "<no-source-sts2>";
		String runtimeSource = runtimePackGameAssembly != null && runtimePackGameAssembly.exists()
			? "runtime-pack"
			: selectedBranchRequiresRuntimePack
				? "no-usable-runtime"
				: "selected-game";
		return selectedBranch
			+ "|pck=" + pckIdentity
			+ "|runtimeSource=" + runtimeSource
			+ "|runtimePack=" + runtimePackIdentity(runtimePackDir)
			+ "|sts2=" + sourceHash;
	}

	private boolean selectedBranchRequiresRuntimePack() {
		return !"public".equalsIgnoreCase(readSelectedBranch());
	}

	private String runtimePackIdentity(File runtimePackDir) {
		if (runtimePackDir == null || !runtimePackDir.exists() || !runtimePackDir.isDirectory()) {
			return "<none>";
		}

		StringBuilder sb = new StringBuilder();
		sb.append("dir=").append(runtimePackDir.getName());
		appendRuntimePackFileIdentity(sb, new File(runtimePackDir, "compatibility.json"));
		appendRuntimePackFileIdentity(sb, new File(runtimePackDir, "patch_validation.json"));
		File[] files = runtimePackDir.listFiles((dir, name) -> name.toLowerCase(java.util.Locale.ROOT).endsWith(".dll"));
		if (files != null) {
			java.util.Arrays.sort(files, (a, b) -> a.getName().compareToIgnoreCase(b.getName()));
			for (File file : files) {
				appendRuntimePackFileIdentity(sb, file);
			}
		}
		return sb.toString();
	}

	private void appendRuntimePackFileIdentity(StringBuilder sb, File file) {
		sb.append("|").append(file.getName()).append("=").append(fileIdentity(file));
	}

	private String fileIdentity(File file) {
		if (file == null || !file.exists() || !file.isFile()) {
			return "<missing>";
		}
		return "bytes=" + file.length() + ",mtime=" + file.lastModified();
	}

	private File findAssembliesDir() {
		if (!isGamePckReady()) {
			return null;
		}

		File gameDirFile = new File(gameDir);
		if (gameDirFile.exists() && gameDirFile.isDirectory()) {
			File[] children = gameDirFile.listFiles();
			if (children != null) {
				File fallback = null;
				for (File child : children) {
					if (child.isDirectory() && child.getName().startsWith("data_")) {
						String dirName = child.getName();
						Log.i(TAG, "Found assemblies dir candidate: " + dirName);
						if (dirName.contains("android")) {
							if (containsAssemblies(child)) {
								return child;
							}
						}
						if (fallback == null) {
							fallback = child;
						}
					}
				}
				if (fallback != null && containsAssemblies(fallback)) {
					return fallback;
				}
			}
		}
		return null;
	}

	private File findRuntimePackDir() {
		if (!isGamePckReady()) {
			return null;
		}

		File runtimePackDir = new File(
			new File(getFilesDir(), RUNTIME_PACKS_DIRECTORY),
			SteamBranchInfo.stateDirectoryName(readSelectedBranch())
		);
		if (runtimePackDir.exists() && runtimePackDir.isDirectory()) {
			String validationKey = runtimePackValidationCacheKey(runtimePackDir);
			if (
				cachedRuntimePackDir != null
					&& cachedRuntimePackDir.equals(runtimePackDir)
					&& validationKey.equals(cachedRuntimePackValidationKey)
			) {
				return cachedRuntimePackDir;
			}
			if (!isRuntimePackManifestUsable(runtimePackDir)) {
				Log.w(TAG, "Ignoring runtime pack with incomplete or mismatched manifest: " + runtimePackDir.getAbsolutePath());
				cachedRuntimePackDir = null;
				cachedRuntimePackValidationKey = "";
				return null;
			}
			cachedRuntimePackDir = runtimePackDir;
			cachedRuntimePackValidationKey = validationKey;
			return runtimePackDir;
		}

		return null;
	}

	private String runtimePackValidationCacheKey(File runtimePackDir) {
		File selectedPck = new File(gameDir, PCK_FILE);
		File srcDir = findAssembliesDir();
		File selectedSourceAssembly = srcDir == null ? null : new File(srcDir, RUNTIME_PACK_ANDROID_ASSEMBLY);
		return "branch=" + readSelectedBranch()
			+ "|pck=" + fileIdentity(selectedPck)
			+ "|source=" + fileIdentity(selectedSourceAssembly)
			+ "|runtimePack=" + runtimePackIdentity(runtimePackDir);
	}

	private boolean isRuntimePackManifestUsable(File runtimePackDir) {
		if (runtimePackDir == null || !runtimePackDir.exists() || !runtimePackDir.isDirectory()) {
			return false;
		}

		File manifest = new File(runtimePackDir, RUNTIME_PACK_COMPATIBILITY_MANIFEST);
		File report = new File(runtimePackDir, RUNTIME_PACK_PATCH_VALIDATION_REPORT);
		File gameAssembly = new File(runtimePackDir, RUNTIME_PACK_ANDROID_ASSEMBLY);
		if (!manifest.exists() || !manifest.isFile() || !report.exists() || !report.isFile() || !gameAssembly.exists() || !gameAssembly.isFile()) {
			Log.w(TAG, "Runtime pack missing required manifest/report/assembly files: " + runtimePackDir.getAbsolutePath());
			return false;
		}

		try {
			org.json.JSONObject json = new org.json.JSONObject(readSmallTextFile(manifest, 64 * 1024));
			String packId = json.optString("packId", "");
			String sourceRuntimeSlotId = json.optString("sourceRuntimeSlotId", "");
			String sourceBranch = json.optString("sourceBranch", "");
			String sourcePckSha256 = json.optString("sourcePckSha256", "");
			String sourceAssemblySha256 = json.optString("sourceAssemblySha256", "");
			String androidAssemblySha256 = json.optString("androidAssemblySha256", "");
			String patchValidationStatus = json.optString("patchValidationStatus", "");
			boolean generatedFromCleanDirectory = json.optBoolean("generatedFromCleanDirectory", false);
			org.json.JSONArray supportAssemblies = json.optJSONArray("supportAssemblies");
			org.json.JSONObject supportAssemblySha256 = json.optJSONObject("supportAssemblySha256");
			if (packId.trim().isEmpty()
				|| sourceRuntimeSlotId.trim().isEmpty()
				|| sourceBranch.trim().isEmpty()
				|| sourcePckSha256.trim().isEmpty()
				|| sourceAssemblySha256.trim().isEmpty()
				|| androidAssemblySha256.trim().isEmpty()) {
				Log.w(TAG, "Runtime pack manifest is missing required runtime identity/hash fields: " + manifest.getAbsolutePath());
				return false;
			}
			if (!generatedFromCleanDirectory) {
				Log.w(TAG, "Runtime pack was not generated from a clean directory: " + manifest.getAbsolutePath());
				return false;
			}
			if (!runtimePackSupportAssembliesUsable(runtimePackDir, supportAssemblies, supportAssemblySha256)) {
				return false;
			}
			String selectedBranch = readSelectedBranch();
			if (!sourceBranch.equalsIgnoreCase(selectedBranch)) {
				Log.w(TAG, "Runtime pack branch mismatch: declared=" + sourceBranch + " selected=" + selectedBranch);
				return false;
			}
			File selectedPck = new File(gameDir, PCK_FILE);
			if (!selectedPck.exists() || !selectedPck.isFile()) {
				Log.w(TAG, "Runtime pack cannot be matched because selected PCK is missing: " + selectedPck.getAbsolutePath());
				return false;
			}
			String selectedPckSha256 = selectedPckSha256ForRuntimePackValidation(selectedPck, sourcePckSha256);
			if (!pckMatchesRuntimeSource(selectedPck, sourcePckSha256, selectedPckSha256)) {
				Log.w(TAG, "Runtime pack selected PCK hash mismatch: declared=" + sourcePckSha256 + " selected=" + selectedPckSha256);
				return false;
			}
			File srcDir = findAssembliesDir();
			File selectedSourceAssembly = srcDir == null ? null : new File(srcDir, RUNTIME_PACK_ANDROID_ASSEMBLY);
			if (selectedSourceAssembly == null || !selectedSourceAssembly.exists() || !selectedSourceAssembly.isFile()) {
				Log.w(TAG, "Runtime pack cannot be matched because selected source sts2.dll is missing: " + (selectedSourceAssembly == null ? "<none>" : selectedSourceAssembly.getAbsolutePath()));
				return false;
			}
			String selectedSourceAssemblySha256 = sha256Hex(selectedSourceAssembly);
			if (!sourceAssemblySha256.equalsIgnoreCase(selectedSourceAssemblySha256)) {
				Log.w(TAG, "Runtime pack selected source assembly hash mismatch: declared=" + sourceAssemblySha256 + " selected=" + selectedSourceAssemblySha256);
				return false;
			}
			if (!"passed".equalsIgnoreCase(patchValidationStatus)) {
				Log.w(TAG, "Runtime pack manifest patch validation did not pass: " + patchValidationStatus);
				return false;
			}
			org.json.JSONObject reportJson = new org.json.JSONObject(readSmallTextFile(report, 64 * 1024));
			String reportStatus = reportJson.optString("status", "");
			if (!"passed".equalsIgnoreCase(reportStatus)) {
				Log.w(TAG, "Runtime pack patch validation report did not pass: " + reportStatus);
				return false;
			}
			if (!packId.equalsIgnoreCase(reportJson.optString("runtimePackId", ""))
				|| !sourceRuntimeSlotId.equalsIgnoreCase(reportJson.optString("sourceRuntimeSlotId", ""))
				|| !sourceBranch.equalsIgnoreCase(reportJson.optString("branch", ""))
				|| !sourcePckSha256.equalsIgnoreCase(reportJson.optString("pckSha256", ""))
				|| !sourceAssemblySha256.equalsIgnoreCase(reportJson.optString("sourceAssemblySha256", ""))
				|| !androidAssemblySha256.equalsIgnoreCase(reportJson.optString("androidAssemblySha256", ""))
				|| !json.optString("patchSetVersion", "").equalsIgnoreCase(reportJson.optString("patchSetVersion", ""))
				|| !json.optString("validationSurfaceVersion", "").equalsIgnoreCase(reportJson.optString("validationSurfaceVersion", ""))
				|| !jsonArrayStringsEqual(supportAssemblies, reportJson.optJSONArray("supportAssemblies"))
				|| !jsonStringObjectEqual(supportAssemblySha256, reportJson.optJSONObject("supportAssemblySha256"))
				|| reportJson.optBoolean("generatedFromCleanDirectory", false) != generatedFromCleanDirectory) {
				Log.w(TAG, "Runtime pack patch validation report does not match compatibility manifest.");
				return false;
			}
			String actualAssemblySha256 = sha256Hex(gameAssembly);
			if (!androidAssemblySha256.equalsIgnoreCase(actualAssemblySha256)) {
				Log.w(TAG, "Runtime pack Android assembly hash mismatch: declared=" + androidAssemblySha256 + " actual=" + actualAssemblySha256);
				return false;
			}
			return true;
		} catch (Exception e) {
			Log.w(TAG, "Failed to parse runtime pack compatibility manifest: " + manifest.getAbsolutePath(), e);
			return false;
		}
	}

	private boolean runtimePackSupportAssembliesUsable(File runtimePackDir, org.json.JSONArray supportAssemblies, org.json.JSONObject supportAssemblySha256) {
		if (supportAssemblies == null || supportAssemblySha256 == null) {
			Log.w(TAG, "Runtime pack manifest is missing support assembly declarations.");
			return false;
		}

		HashSet<String> declaredDlls = new HashSet<>();
		HashSet<String> declaredSupportAssemblies = new HashSet<>();
		declaredDlls.add(RUNTIME_PACK_ANDROID_ASSEMBLY.toLowerCase(java.util.Locale.ROOT));
		for (int i = 0; i < supportAssemblies.length(); i++) {
			String name = supportAssemblies.optString(i, "");
			String lowerName = name.toLowerCase(java.util.Locale.ROOT);
			if (name.trim().isEmpty() || name.contains("/") || name.contains("\\") || !name.endsWith(".dll")) {
				Log.w(TAG, "Runtime pack support assembly has unsafe name: " + name);
				return false;
			}
			if (lowerName.equals(RUNTIME_PACK_ANDROID_ASSEMBLY.toLowerCase(java.util.Locale.ROOT))) {
				Log.w(TAG, "Runtime pack support assemblies must not redeclare sts2.dll.");
				return false;
			}
			if (!declaredDlls.add(lowerName)) {
				Log.w(TAG, "Runtime pack support assembly is duplicated: " + name);
				return false;
			}
			declaredSupportAssemblies.add(lowerName);

			File supportAssembly = new File(runtimePackDir, name);
			if (!supportAssembly.exists() || !supportAssembly.isFile()) {
				Log.w(TAG, "Runtime pack support assembly missing: " + supportAssembly.getAbsolutePath());
				return false;
			}
			String declaredSha256 = supportAssemblySha256.optString(name, "");
			if (declaredSha256.trim().isEmpty()) {
				Log.w(TAG, "Runtime pack support assembly hash missing: " + name);
				return false;
			}
			String actualSha256 = sha256Hex(supportAssembly);
			if (!declaredSha256.equalsIgnoreCase(actualSha256)) {
				Log.w(TAG, "Runtime pack support assembly hash mismatch: " + name + " declared=" + declaredSha256 + " actual=" + actualSha256);
				return false;
			}
		}
		if (supportAssemblySha256.length() != declaredSupportAssemblies.size()) {
			Log.w(TAG, "Runtime pack support assembly hash set does not match declared support assemblies.");
			return false;
		}
		Iterator<String> supportHashKeys = supportAssemblySha256.keys();
		while (supportHashKeys.hasNext()) {
			String key = supportHashKeys.next();
			if (!declaredSupportAssemblies.contains(key.toLowerCase(java.util.Locale.ROOT))) {
				Log.w(TAG, "Runtime pack support assembly hash is undeclared: " + key);
				return false;
			}
		}

		File[] runtimePackDlls = runtimePackDir.listFiles((dir, name) -> name.toLowerCase(java.util.Locale.ROOT).endsWith(".dll"));
		if (runtimePackDlls != null) {
			for (File dll : runtimePackDlls) {
				String lowerName = dll.getName().toLowerCase(java.util.Locale.ROOT);
				if (!declaredDlls.contains(lowerName)) {
					Log.w(TAG, "Runtime pack contains undeclared DLL: " + dll.getAbsolutePath());
					return false;
				}
			}
		}

		return true;
	}

	private String selectedPckSha256ForRuntimePackValidation(File selectedPck, String expectedSourcePckSha256) {
		if (selectedPck == null || !selectedPck.exists() || !selectedPck.isFile()) {
			return "";
		}

		try {
			File marker = new File(getFilesDir(), CURRENT_RUNTIME_SLOT_MARKER);
			if (marker.exists() && marker.isFile() && markerIsFreshForFile(marker, selectedPck)) {
				JSONObject json = new JSONObject(readSmallTextFile(marker, 64 * 1024));
				String selectedBranch = readSelectedBranch();
				String markerBranch = json.optString("branch", "");
				String markerPckSha256 = json.optString("pckSha256", "");
				if (
					markerBranch.equalsIgnoreCase(selectedBranch)
						&& !markerPckSha256.trim().isEmpty()
						&& pckMatchesRuntimeSource(selectedPck, expectedSourcePckSha256, markerPckSha256)
				) {
					Log.i(TAG, "Using current runtime slot marker PCK hash for native runtime-pack validation.");
					return markerPckSha256;
				}
			}
		} catch (Exception e) {
			Log.w(TAG, "Current runtime slot marker could not satisfy native runtime-pack validation; hashing selected PCK.", e);
		}

		return sha256Hex(selectedPck);
	}

	private boolean markerIsFreshForFile(File marker, File file) {
		if (marker == null || file == null || !marker.exists() || !file.exists()) {
			return false;
		}

		return marker.lastModified() + 2000L >= file.lastModified();
	}

	private Set<String> runtimePackDeclaredAssemblyNames(File runtimePackDir) {
		Set<String> names = new HashSet<>();
		names.add(RUNTIME_PACK_ANDROID_ASSEMBLY.toLowerCase(java.util.Locale.ROOT));

		if (runtimePackDir == null) {
			return names;
		}

		File manifest = new File(runtimePackDir, RUNTIME_PACK_COMPATIBILITY_MANIFEST);
		try {
			org.json.JSONObject json = new org.json.JSONObject(readSmallTextFile(manifest, 64 * 1024));
			org.json.JSONArray supportAssemblies = json.optJSONArray("supportAssemblies");
			if (supportAssemblies != null) {
				for (int i = 0; i < supportAssemblies.length(); i++) {
					String name = supportAssemblies.optString(i, "").trim();
					if (!name.isEmpty()) {
						names.add(name.toLowerCase(java.util.Locale.ROOT));
					}
				}
			}
		} catch (Exception e) {
			Log.w(TAG, "Failed to read manifest-declared runtime-pack assemblies; copying runtime-pack sts2.dll only.", e);
		}

		return names;
	}

	private boolean jsonArrayStringsEqual(org.json.JSONArray left, org.json.JSONArray right) {
		if (left == null || right == null || left.length() != right.length()) {
			return false;
		}
		for (int i = 0; i < left.length(); i++) {
			if (!left.optString(i, "").equalsIgnoreCase(right.optString(i, ""))) {
				return false;
			}
		}
		return true;
	}

	private boolean jsonStringObjectEqual(org.json.JSONObject left, org.json.JSONObject right) {
		if (left == null || right == null || left.length() != right.length()) {
			return false;
		}
		Iterator<String> keys = left.keys();
		while (keys.hasNext()) {
			String key = keys.next();
			if (!left.optString(key, "").equalsIgnoreCase(right.optString(key, ""))) {
				return false;
			}
		}
		return true;
	}

	private File runtimePackGameAssembly(File runtimePackDir) {
		if (runtimePackDir == null) {
			return null;
		}

		File candidate = new File(runtimePackDir, RUNTIME_PACK_ANDROID_ASSEMBLY);
		if (candidate.exists() && candidate.isFile()) {
			return candidate;
		}

		return null;
	}

	private boolean hasGameAssemblies(File srcDir) {
		if (srcDir == null || !srcDir.exists() || !srcDir.isDirectory()) {
			return false;
		}
		return new File(srcDir, GAME_REQUIRED_ASSEMBLIES[0]).exists();
	}

	private boolean hasCurrentPackagedRequiredAssemblies(File destDir) {
		if (destDir == null || !destDir.exists() || !destDir.isDirectory()) {
			return false;
		}

		for (String name : BOOTSTRAP_REQUIRED_ASSEMBLIES) {
			File cached = new File(destDir, name);
			if (!cached.exists() || !packagedAssetMatchesFile("dotnet_bcl/" + name, cached)) {
				Log.i(TAG, "Packaged assembly cache is stale: " + name);
				return false;
			}
		}

		return true;
	}

	private boolean packagedAssetMatchesFile(String assetPath, File cached) {
		try (InputStream asset = getAssets().open(assetPath);
				InputStream current = new FileInputStream(cached)) {
			byte[] assetBuffer = new byte[8192];
			byte[] currentBuffer = new byte[8192];
			while (true) {
				int assetRead = asset.read(assetBuffer);
				int currentRead = current.read(currentBuffer);
				if (assetRead != currentRead) {
					return false;
				}
				if (assetRead < 0) {
					return true;
				}
				for (int i = 0; i < assetRead; i++) {
					if (assetBuffer[i] != currentBuffer[i]) {
						return false;
					}
				}
			}
		} catch (IOException e) {
			Log.w(TAG, "Could not compare packaged assembly asset: " + assetPath, e);
			return false;
		}
	}

	private boolean containsAssemblies(File dir) {
		if (dir == null || !dir.exists() || !dir.isDirectory()) {
			return false;
		}
		File[] files = dir.listFiles((file, name) -> name.endsWith(".dll"));
		return files != null && files.length > 0;
	}

	private boolean hasCachedGameAssemblies(File destDir, File srcDir, Set<String> packagedBclNames, File runtimePackDir, File runtimePackGameAssembly) {
		if (destDir == null) {
			return false;
		}

		if (selectedBranchRequiresRuntimePack()
			&& (runtimePackGameAssembly == null || !runtimePackGameAssembly.exists() || !runtimePackGameAssembly.isFile())) {
			Log.i(TAG, "Game assembly cache is not current because selected non-public branch has no usable runtime pack.");
			return false;
		}

		boolean hasAnyExpectedGameAssembly = false;
		if (runtimePackGameAssembly != null && runtimePackGameAssembly.exists() && runtimePackGameAssembly.isFile()) {
			File[] runtimePackFiles = runtimePackDir == null ? null : runtimePackDir.listFiles();
			if (runtimePackFiles == null || runtimePackFiles.length == 0) {
				return false;
			}

			for (File src : runtimePackFiles) {
				if (!src.isFile()) {
					continue;
				}
				if (!shouldCopyGameAssemblyFile(src.getName(), packagedBclNames)) {
					continue;
				}
				hasAnyExpectedGameAssembly = true;
				File dest = new File(destDir, src.getName());
				if (!filesMatch(src, dest)) {
					Log.i(TAG, "Runtime-pack assembly cache is stale or incomplete: " + src.getName());
					return false;
				}
			}
		}

		if (srcDir != null && srcDir.exists() && srcDir.isDirectory()) {
			File[] files = srcDir.listFiles();
			if (files != null) {
				for (File src : files) {
					if (!src.isFile()) {
						continue;
					}
					if (runtimePackGameAssembly != null && runtimePackGameAssembly.exists() && isBranchGameCodeAssembly(src.getName())) {
						continue;
					}
					if (!shouldCopyGameAssemblyFile(src.getName(), packagedBclNames)) {
						continue;
					}
					hasAnyExpectedGameAssembly = true;
					File dest = new File(destDir, src.getName());
					if (!filesMatch(src, dest)) {
						Log.i(TAG, "Game assembly cache is stale or incomplete: " + src.getName());
						return false;
					}
				}
			}
		}

		return hasAnyExpectedGameAssembly;
	}

	private boolean filesMatch(File expected, File current) {
		if (expected == null || current == null || !expected.exists() || !current.exists()) {
			return false;
		}

		if (expected.length() != current.length()) {
			return false;
		}

		try (InputStream expectedStream = new FileInputStream(expected);
				InputStream currentStream = new FileInputStream(current)) {
			byte[] expectedBuffer = new byte[8192];
			byte[] currentBuffer = new byte[8192];
			while (true) {
				int expectedRead = expectedStream.read(expectedBuffer);
				int currentRead = currentStream.read(currentBuffer);
				if (expectedRead != currentRead) {
					return false;
				}
				if (expectedRead < 0) {
					return true;
				}
				for (int i = 0; i < expectedRead; i++) {
					if (expectedBuffer[i] != currentBuffer[i]) {
						return false;
					}
				}
			}
		} catch (IOException e) {
			Log.w(TAG, "Could not compare game assembly cache file: " + expected.getName(), e);
			return false;
		}
	}

	private boolean isGamePckReady() {
		String branch = readSelectedBranch();
		File pck = new File(gameDir, PCK_FILE);
		if (!pck.exists() || !pck.isFile() || pck.length() < 96) {
			return false;
		}
		if (!isBranchMarkerReady(branch)) {
			return false;
		}

		try (RandomAccessFile raf = new RandomAccessFile(pck, "r")) {
			long magic = readUInt32LE(raf);
			if (magic != 0x43504447L) {
				return false;
			}

			readUInt32LE(raf); // format version
			readUInt32LE(raf); // major
			readUInt32LE(raf); // minor
			readUInt32LE(raf); // patch
			readUInt32LE(raf); // flags
			readLongLE(raf); // file base
			long dirBase = readLongLE(raf);
			if (dirBase <= 0 || dirBase + 4 > raf.length()) {
				Log.w(TAG, "Game PCK is not structurally ready: dirBase=" + dirBase + " fileSize=" + raf.length());
				return false;
			}

			raf.seek(dirBase);
			long fileCount = readUInt32LE(raf);
			if (fileCount <= 0) {
				Log.w(TAG, "Game PCK is not structurally ready: fileCount=" + fileCount);
				return false;
			}

			return true;
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect game PCK", e);
			return false;
		}
	}

	private boolean isRuntimeSlotEvidenceReadyForLaunch(String selectedBranch) {
		File marker = new File(getFilesDir(), CURRENT_RUNTIME_SLOT_MARKER);
		if (!marker.exists() || !marker.isFile()) {
			Log.w(TAG, "Blocking selected game startup because runtime slot evidence is missing: " + marker.getAbsolutePath());
			return false;
		}

		try {
			JSONObject json = new JSONObject(readSmallTextFile(marker, 64 * 1024));
			String markerBranch = json.optString("branch", "");
			boolean branchMatches = markerBranch.equalsIgnoreCase(selectedBranch);
			boolean filesReady = json.optBoolean("filesReady", false);
			boolean playable = json.optBoolean("playable", false);
			boolean runtimeCompatible = json.optBoolean("runtimeCompatible", false);
			boolean patchCompatible = json.optBoolean("patchCompatible", false);
			String markerPckSha256 = json.optString("pckSha256", "");
			String markerSourceAssemblySha256 = json.optString("sourceAssemblySha256", "");
			File selectedPck = new File(gameDir, PCK_FILE);
			String selectedPckSha256 = selectedPckSha256ForRuntimeSlotEvidence(selectedPck, marker, json, selectedBranch);
			File srcDir = findAssembliesDir();
			File selectedSourceAssembly = srcDir == null ? null : new File(srcDir, RUNTIME_PACK_ANDROID_ASSEMBLY);
			String selectedSourceAssemblySha256 = selectedSourceAssembly != null && selectedSourceAssembly.exists() && selectedSourceAssembly.isFile()
				? sha256Hex(selectedSourceAssembly)
				: "";
			File activeAndroidAssembly = activeAndroidAssemblyFile();
			String activeAndroidAssemblySha256 = activeAndroidAssembly.exists() && activeAndroidAssembly.isFile()
				? sha256Hex(activeAndroidAssembly)
				: "";
			boolean pckMatches = pckMatchesRuntimeSource(selectedPck, markerPckSha256, selectedPckSha256);
			boolean sourceAssemblyMatches = !markerSourceAssemblySha256.trim().isEmpty() && markerSourceAssemblySha256.equalsIgnoreCase(selectedSourceAssemblySha256);
			if (branchMatches && filesReady && playable && runtimeCompatible && patchCompatible && pckMatches && sourceAssemblyMatches) {
				Log.i(TAG, "Runtime slot evidence ready for selected game startup: slot=" + json.optString("runtimeSlotId", "") + " branch=" + markerBranch);
				return true;
			}

			boolean publicLegacyRuntimeReady = "public".equalsIgnoreCase(selectedBranch)
				&& !selectedSourceAssemblySha256.trim().isEmpty()
				&& selectedSourceAssemblySha256.equalsIgnoreCase(activeAndroidAssemblySha256);
			if (!branchMatches && publicLegacyRuntimeReady) {
				Log.i(
					TAG,
					"Runtime slot evidence marker belongs to '" + markerBranch
						+ "', but selected public runtime cache matches the public source assembly; allowing public legacy startup after branch switch."
				);
				return true;
			}

			Log.w(
				TAG,
				"Blocking selected game startup because runtime slot evidence is not playable: "
					+ "selectedBranch=" + selectedBranch
					+ " markerBranch=" + markerBranch
					+ " filesReady=" + filesReady
					+ " playable=" + playable
					+ " runtimeCompatible=" + runtimeCompatible
					+ " patchCompatible=" + patchCompatible
					+ " pckMatches=" + pckMatches
					+ " sourceAssemblyMatches=" + sourceAssemblyMatches
					+ " activeAndroidAssemblyMatchesPublic=" + publicLegacyRuntimeReady
					+ " readinessProblem=" + json.optString("readinessProblem", "")
					+ " runtimePackStatus=" + json.optString("runtimePackUsabilityStatus", "")
					+ " patchStatus=" + json.optString("patchCompatibilityStatus", "")
			);
			return false;
		} catch (Exception e) {
			Log.w(TAG, "Blocking selected game startup because runtime slot evidence is unreadable: " + marker.getAbsolutePath(), e);
			return false;
		}
	}

	private String selectedPckSha256ForRuntimeSlotEvidence(File selectedPck, File marker, JSONObject json, String selectedBranch) {
		if (selectedPck == null || !selectedPck.exists() || !selectedPck.isFile()) {
			return "";
		}

		String markerBranch = json.optString("branch", "");
		String markerPckSha256 = json.optString("pckSha256", "");
		if (
			markerBranch.equalsIgnoreCase(selectedBranch)
				&& !markerPckSha256.trim().isEmpty()
				&& markerIsFreshForFile(marker, selectedPck)
		) {
			Log.i(TAG, "Using current runtime slot marker PCK hash for startup readiness.");
			return markerPckSha256;
		}

		Log.i(TAG, "Current runtime slot marker PCK hash is unavailable or stale; hashing selected PCK for startup readiness.");
		return sha256Hex(selectedPck);
	}

	private File activeAndroidAssemblyFile() {
		return new File(
			new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir()),
			RUNTIME_PACK_ANDROID_ASSEMBLY
		);
	}

	private boolean isBranchMarkerReady(String branch) {
		File marker = new File(gameDir, BRANCH_MARKER_FILE);
		if (!marker.exists() || !marker.isFile()) {
			return "public".equalsIgnoreCase(branch);
		}

		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (!line.regionMatches(true, 0, "Branch:", 0, "Branch:".length())) {
					continue;
				}
				String markerBranch = line.substring("Branch:".length()).trim();
				boolean ready = markerBranch.equalsIgnoreCase(branch);
				if (!ready) {
					Log.w(TAG, "Steam branch marker mismatch: selected=" + branch + " marker=" + markerBranch);
				}
				return ready && ("public".equalsIgnoreCase(branch) || (hasInstallSlotProvenance(marker, branch) && hasDepotManifestProvenance(marker) && hasBranchIntegrityProvenance(marker)));
			}
			Log.w(TAG, "Steam branch marker has no Branch line: " + marker.getAbsolutePath());
		} catch (IOException e) {
			Log.w(TAG, "Failed to read Steam branch marker: " + marker.getAbsolutePath(), e);
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

		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (line.regionMatches(true, 0, prefix, 0, prefix.length())) {
					return line.substring(prefix.length()).trim();
				}
			}
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect Steam branch marker install slot provenance: " + marker.getAbsolutePath(), e);
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

		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			int count = 0;
			String line;
			while ((line = reader.readLine()) != null) {
				if (line.regionMatches(true, 0, "Depot manifest:", 0, "Depot manifest:".length())) {
					count++;
				}
			}
			return count;
		} catch (IOException e) {
			Log.w(TAG, "Failed to inspect Steam branch marker depot provenance: " + marker.getAbsolutePath(), e);
		}
		return 0;
	}

	private void copyFile(File src, File dest) throws IOException {
		try (InputStream in = new FileInputStream(src);
				OutputStream out = new FileOutputStream(dest)) {
			byte[] buf = new byte[8192];
			int len;
			while ((len = in.read(buf)) > 0) {
				out.write(buf, 0, len);
			}
		}
	}

	// Extracts a single file from APK assets to the files directory.
	private void extractAssetFile(String assetPath, String destName) {
		File dest = new File(getFilesDir(), destName);
		if (dest.exists())
			return;
		try (InputStream in = getAssets().open(assetPath);
				OutputStream out = new FileOutputStream(dest)) {
			byte[] buf = new byte[4096];
			int len;
			while ((len = in.read(buf)) > 0) {
				out.write(buf, 0, len);
			}
		} catch (IOException e) {
			Log.w(TAG, "Failed to extract " + assetPath, e);
		}
	}

	@Override
	public List<String> getCommandLine() {
		List<String> commands = new ArrayList<>(super.getCommandLine());
		setAndroidFilesDirMode();
		File pckFile = new File(gameDir, PCK_FILE);
		String selectedBranch = readSelectedBranch();
		boolean pendingGameLaunch = hasPendingGameLaunchRequest();
		File branchMarker = new File(gameDir, BRANCH_MARKER_FILE);
		Log.i(TAG, "Selected Steam branch for startup: " + selectedBranch);
		Log.i(TAG, "Selected Steam branch note for startup: " + SteamBranchInfo.selectorHelpText(selectedBranch));
		Log.i(TAG, "Selected game version slot kind for startup: " + SteamBranchInfo.installSlotKind(selectedBranch));
		Log.i(TAG, "Selected game version slot directory for startup: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), selectedBranch).getAbsolutePath());
		Log.i(TAG, "Resolved startup game directory: " + gameDir);
		Log.i(TAG, "Selected game PCK for startup: " + describeGamePck(pckFile, false));
		Log.i(TAG, "Steam branch marker install slot kind for startup: " + readMarkerValue(branchMarker, "Install slot kind:"));
		Log.i(TAG, "Steam branch marker expected install slot kind for startup: " + SteamBranchInfo.installSlotKind(selectedBranch));
		Log.i(TAG, "Steam branch marker install slot directory for startup: " + readMarkerValue(branchMarker, "Install slot directory:"));
		Log.i(TAG, "Steam branch marker expected install slot directory for startup: " + SteamBranchInfo.installSlotDirectory(getFilesDir(), selectedBranch).getAbsolutePath());
		Log.i(TAG, "Steam branch marker has matching install slot provenance for startup: " + hasInstallSlotProvenance(branchMarker, selectedBranch));
		Log.i(TAG, "Steam branch marker has depot manifests for startup: " + hasDepotManifestProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker has branch integrity provenance for startup: " + hasBranchIntegrityProvenance(branchMarker));
		Log.i(TAG, "Steam branch marker depot manifest entries for startup: " + depotManifestCount(branchMarker));
		boolean branchMarkerReady = isBranchMarkerReady(selectedBranch);
		boolean gamePckReady = isGamePckReady();
		Log.i(TAG, "Steam branch marker ready for startup: " + branchMarkerReady);
		boolean gameLaunchRequested = branchMarkerReady && gamePckReady && consumeGameLaunchRequest();
		boolean runtimeSlotReady = gameLaunchRequested && isRuntimeSlotEvidenceReadyForLaunch(selectedBranch);
		Log.i(TAG, "Runtime slot evidence ready for startup: " + runtimeSlotReady);
		boolean launchRequested = gameLaunchRequested && runtimeSlotReady;
		if (gamePckReady && !branchMarkerReady) {
			Log.w(TAG, "Blocking selected game version startup because branch marker provenance is missing or mismatched; returning to launcher instead of falling back to another branch.");
		}
		if (gameLaunchRequested && !runtimeSlotReady) {
			Log.w(TAG, "Blocking selected game version startup because runtime slot evidence is missing, stale, or not playable; returning to launcher instead of mounting the selected PCK.");
		}
		setAutoLaunchGameMode(launchRequested);
		setForcedCriticalPatchFailureMode();
		setSteamKitDebugLogMode();
		if (launchRequested) {
			setLauncherBootstrapMode(false);
			boolean safeLaunch = consumeSafeGameLaunchRequest();
			setAutoSafeLaunchMode(safeLaunch);
			boolean useDefaultRenderer = previousStartupPhaseWas("game startup completed");
			patchGamePckForAndroid(pckFile);
			if (!useDefaultRenderer) {
				commands.add("--rendering-driver");
				commands.add("opengl3");
				commands.add("--rendering-method");
				commands.add("gl_compatibility");
			} else {
				Log.i(TAG, safeLaunch
					? "Using default renderer for manual safe launch"
					: "Using default renderer because previous game startup completed but did not produce a usable screen");
			}
			if (safeLaunch && !useDefaultRenderer) {
				Log.i(TAG, "Manual safe launch keeps OpenGL compatibility renderer");
			}
			commands.add("--verbose");
			Log.i(TAG, "Enabled verbose Godot logging for downloaded game");
			if (isX86Runtime()) {
				commands.add("--audio-driver");
				commands.add("Dummy");
				Log.i(TAG, "Using dummy audio driver for x86 emulator");
			}
			commands.add("--main-pack");
			commands.add(pckFile.getAbsolutePath());
			if (!useDefaultRenderer) {
				Log.i(TAG, "Forcing OpenGL compatibility renderer for downloaded game");
			}
			Log.i(TAG, "Loading PCK from: " + pckFile.getAbsolutePath());
		} else {
			setAutoSafeLaunchMode(false);
			// Start in the launcher unless a one-shot game launch was requested; use bootstrap PCK so Godot can initialize for the
			// launcher.
			setLauncherBootstrapMode(true);
			setMinimalBootstrapUiMode();
			String bootstrapPck = extractBootstrapPck();
			if (bootstrapPck != null) {
				commands.add("--main-pack");
				commands.add(bootstrapPck);
				Log.i(TAG, "Using bootstrap PCK for launcher-only mode");
			}
		}
		return commands;
	}

	private void setLauncherBootstrapMode(boolean enabled) {
		try {
			android.system.Os.setenv(ENV_LAUNCHER_BOOTSTRAP, enabled ? "1" : "0", true);
			Log.i(TAG, "Launcher bootstrap mode: " + enabled);
		} catch (Exception e) {
			Log.w(TAG, "Failed to set launcher bootstrap mode", e);
		}
	}

	private boolean pckMatchesRuntimeSource(File selectedPck, String expectedSourcePckSha256, String selectedPckSha256) {
		if (expectedSourcePckSha256 == null || expectedSourcePckSha256.trim().isEmpty()) {
			return false;
		}
		if (selectedPckSha256 != null && expectedSourcePckSha256.equalsIgnoreCase(selectedPckSha256)) {
			return true;
		}
		if (selectedPck == null || !selectedPck.exists() || !selectedPck.isFile()) {
			return false;
		}
		File marker = new File(selectedPck.getParentFile(), PCK_ANDROID_PATCH_MARKER);
		if (!marker.exists() || !marker.isFile() || marker.lastModified() < selectedPck.lastModified()) {
			return false;
		}

		String markerSource = readPckPatchMarkerHash(marker, "sourcePckSha256");
		if (!markerSource.trim().isEmpty()) {
			boolean matched = expectedSourcePckSha256.equalsIgnoreCase(markerSource);
			if (!matched) {
				Log.w(TAG, "Android PCK patch marker source hash mismatch: expected=" + expectedSourcePckSha256 + " marker=" + markerSource);
			}
			return matched;
		}

		Log.i(TAG, "Accepting legacy Android-patched PCK marker for runtime source hash " + expectedSourcePckSha256);
		return true;
	}

	private String readPckPatchMarkerHash(File marker, String name) {
		try {
			if (marker == null || !marker.exists() || !marker.isFile() || marker.length() <= 0) {
				return "";
			}
			JSONObject json = new JSONObject(readSmallTextFile(marker, 16 * 1024));
			return json.optString(name, "").trim();
		} catch (Exception e) {
			return "";
		}
	}

	private void setAndroidFilesDirMode() {
		try {
			String filesDir = getFilesDir().getAbsolutePath();
			android.system.Os.setenv(ENV_ANDROID_FILES_DIR, filesDir, true);
			Log.i(TAG, "Android files dir env: " + filesDir);
		} catch (Exception e) {
			Log.w(TAG, "Failed to set Android files dir env", e);
		}
	}

	private void setMinimalBootstrapUiMode() {
		int mode = 0;
		try {
			mode = android.provider.Settings.Global.getInt(getContentResolver(), "sts2_bootstrap_ui_mode", 0);
			boolean enabled = mode == 1 || android.provider.Settings.Global.getInt(getContentResolver(), "sts2_minimal_bootstrap_ui", 0) == 1;
			android.system.Os.setenv(ENV_BOOTSTRAP_UI_MODE, Integer.toString(enabled && mode == 0 ? 1 : mode), true);
			android.system.Os.setenv(ENV_MINIMAL_BOOTSTRAP_UI, enabled ? "1" : "0", true);
			Log.i(TAG, "Bootstrap UI diagnostic mode: " + mode);
			Log.i(TAG, "Minimal bootstrap UI mode: " + enabled);
		} catch (Exception e) {
			Log.w(TAG, "Failed to set minimal bootstrap UI mode", e);
			try {
				android.system.Os.setenv(ENV_BOOTSTRAP_UI_MODE, "0", true);
				android.system.Os.setenv(ENV_MINIMAL_BOOTSTRAP_UI, "0", true);
			} catch (Exception ignored) {
			}
		}
	}

	private void setAutoLaunchGameMode(boolean enabled) {
		try {
			android.system.Os.setenv(ENV_AUTO_LAUNCH_GAME, enabled ? "1" : "0", true);
			Log.i(TAG, "Auto-launch game mode: " + enabled);
		} catch (Exception e) {
			Log.w(TAG, "Failed to set auto-launch game mode", e);
		}
	}

	private void setAutoSafeLaunchMode(boolean enabled) {
		try {
			android.system.Os.setenv(ENV_AUTO_SAFE_LAUNCH, enabled ? "1" : "0", true);
			Log.i(TAG, "Auto-safe-launch mode: " + enabled);
		} catch (Exception e) {
			Log.w(TAG, "Failed to set auto-safe-launch mode", e);
		}
	}

	private void setForcedCriticalPatchFailureMode() {
		boolean enabled = false;
		try {
			enabled = android.provider.Settings.Global.getInt(getContentResolver(), "sts2_force_critical_patch_failure", 0) == 1;
			android.system.Os.setenv(ENV_FORCE_CRITICAL_PATCH_FAILURE, enabled ? "1" : "0", true);
			if (enabled) {
				Log.w(TAG, "Forced critical patch failure mode enabled by Android global setting");
			}
		} catch (Exception e) {
			Log.w(TAG, "Failed to set forced critical patch failure mode", e);
			try {
				android.system.Os.setenv(ENV_FORCE_CRITICAL_PATCH_FAILURE, "0", true);
			} catch (Exception ignored) {
			}
		}
	}

	private boolean previousStartupPhaseWas(String expectedPhase) {
		File marker = new File(getFilesDir(), "last_game_start_incomplete");
		if (!marker.exists() || expectedPhase == null) {
			return false;
		}

		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			reader.readLine();
			String phase = reader.readLine();
			return expectedPhase.equalsIgnoreCase(phase == null ? "" : phase.trim());
		} catch (IOException e) {
			Log.w(TAG, "Failed to read previous startup marker", e);
			return false;
		}
	}

	private boolean consumeGameLaunchRequest() {
		Intent intent = getIntent();
		if (intent != null && intent.getBooleanExtra(EXTRA_LAUNCH_GAME_ON_START, false)) {
			intent.removeExtra(EXTRA_LAUNCH_GAME_ON_START);
			Log.i(TAG, "Consuming intent game launch request");
			return true;
		}

		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		boolean requested = prefs.getBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, false);
		if (!requested) {
			Log.i(TAG, "Downloaded game is ready; starting launcher first. Press PLAY to boot the game.");
			return false;
		}

		prefs.edit().remove(KEY_LAUNCH_GAME_ON_NEXT_START).apply();
		Log.i(TAG, "Consuming one-shot game launch request");
		return true;
	}

	private boolean hasPendingGameLaunchRequest() {
		Intent intent = getIntent();
		if (intent != null && intent.getBooleanExtra(EXTRA_LAUNCH_GAME_ON_START, false)) {
			return true;
		}

		return getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
			.getBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, false);
	}

	private boolean consumeSafeGameLaunchRequest() {
		Intent intent = getIntent();
		if (intent != null && intent.getBooleanExtra(EXTRA_SAFE_LAUNCH_ON_START, false)) {
			intent.removeExtra(EXTRA_SAFE_LAUNCH_ON_START);
			Log.i(TAG, "Consuming intent safe launch request");
			return true;
		}

		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		boolean requested = prefs.getBoolean(KEY_SAFE_LAUNCH_ON_NEXT_START, false);
		if (!requested) {
			return false;
		}

		prefs.edit().remove(KEY_SAFE_LAUNCH_ON_NEXT_START).apply();
		Log.i(TAG, "Consuming one-shot safe launch request");
		return true;
	}

	private void patchGamePckForAndroid(File pckFile) {
		File marker = new File(gameDir, PCK_ANDROID_PATCH_MARKER);
		if (marker.exists() && marker.lastModified() >= pckFile.lastModified()) {
			return;
		}

		String sourcePckSha256 = pckFile.exists() && pckFile.isFile() ? sha256Hex(pckFile) : "";
		try (RandomAccessFile raf = new RandomAccessFile(pckFile, "rw")) {
			long magic = readUInt32LE(raf);
			if (magic != 0x43504447L) {
				return;
			}

			readUInt32LE(raf); // format version
			readUInt32LE(raf); // major
			readUInt32LE(raf); // minor
			readUInt32LE(raf); // patch
			long flags = readUInt32LE(raf);
			long fileBase = readLongLE(raf);
			long dirBase = readLongLE(raf);
			raf.seek(raf.getFilePointer() + 16L * 4L);

			boolean relativeOffsets = (flags & 0x02L) != 0;
			raf.seek(dirBase);
			long fileCount = readUInt32LE(raf);
			boolean patched = false;

			for (long i = 0; i < fileCount; i++) {
				long pathLen = readUInt32LE(raf);
				if (pathLen <= 0 || pathLen > 8192) {
					Log.w(TAG, "PCK startup patch skipped: invalid path length " + pathLen);
					return;
				}

				byte[] pathBytes = new byte[(int)pathLen];
				raf.readFully(pathBytes);
				String path = new String(pathBytes, "UTF-8").replace("\u0000", "");
				long offset = readLongLE(raf);
				long size = readLongLE(raf);
				byte[] md5 = new byte[16];
				raf.readFully(md5);
				readUInt32LE(raf); // entry flags

				long absOffset = relativeOffsets ? fileBase + offset : offset;
				dumpPckEntryForDiagnostics(path, raf, absOffset, size);
				if (isPckPath(path, "project.binary")) {
					patched |= patchPckBinaryProjectEntry(raf, absOffset, size);
				} else if (isPckPath(path, "project.godot")) {
					patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
						"SentryInit=\"*res://addons/sentry/SentryInit.gd\"",
						"FmodManager=\"*res://addons/fmod/FmodManager.gd\""
					});
				} else if (isPckPath(path, ".godot/extension_list.cfg")) {
					patched |= patchPckTextEntryReplacements(raf, absOffset, size, new String[][] {
						{ spacesFor("res://addons/fmod/fmod.gdextension"), "res://addons/fmod/fmod.gdextension" }
					});
					if (isX86Runtime()) {
						patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
							"res://addons/sentry/sentry.gdextension",
							"res://addons/spine/spine_godot_extension.gdextension"
						});
					} else {
						patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
							"res://addons/sentry/sentry.gdextension"
						});
					}
				} else if (isPckPath(path, "scenes/game.tscn")) {
					patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
						"[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]",
						"[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]",
						"bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]",
						"script = ExtResource(\"3_xfu11\")",
						"[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]"
					});
				}
			}

			if (patched) {
				Log.i(TAG, "Patched game PCK startup data for Android");
			}
		} catch (Exception e) {
			Log.w(TAG, "PCK startup patch failed", e);
		}

		try {
			String androidPckSha256 = pckFile.exists() && pckFile.isFile() ? sha256Hex(pckFile) : "";
			JSONObject json = new JSONObject();
			json.put("markerVersion", 1);
			json.put("sourcePckSha256", sourcePckSha256);
			json.put("androidPckSha256", androidPckSha256);
			json.put("pckBytes", pckFile.exists() ? pckFile.length() : -1);
			json.put("utcMillis", System.currentTimeMillis());
			try (OutputStream out = new FileOutputStream(marker, false)) {
				out.write(json.toString(2).getBytes(StandardCharsets.UTF_8));
			}
			marker.setLastModified(System.currentTimeMillis());
		} catch (Exception e) {
			Log.w(TAG, "Failed to write PCK Android patch marker", e);
		}
	}

	private void dumpPckEntryForDiagnostics(String path, RandomAccessFile raf, long offset, long size) {
		if (!isPckDiagnosticsDumpEnabled() || !isPckDiagnosticsPath(path)) {
			return;
		}
		if (offset < 0 || size < 0 || size > 4L * 1024L * 1024L) {
			Log.w(TAG, "PCK diagnostic dump skipped for " + path + ": size=" + size);
			return;
		}

		try {
			File dir = getExternalFilesDir("pck-diagnostics");
			if (dir == null) {
				Log.w(TAG, "PCK diagnostic dump skipped: external files dir unavailable");
				return;
			}
			if (!dir.exists() && !dir.mkdirs()) {
				Log.w(TAG, "PCK diagnostic dump skipped: failed to create " + dir);
				return;
			}

			long saved = raf.getFilePointer();
			raf.seek(offset);
			byte[] content = new byte[(int)size];
			raf.readFully(content);
			raf.seek(saved);

			String fileName = path.replace("res://", "").replace("/", "__").replace("\\", "__");
			File out = new File(dir, fileName);
			try (java.io.FileOutputStream stream = new java.io.FileOutputStream(out)) {
				stream.write(content);
			}
			Log.i(TAG, "PCK diagnostic entry dumped: " + path + " -> " + out.getAbsolutePath() + " bytes=" + size);
		} catch (Exception e) {
			Log.w(TAG, "PCK diagnostic dump failed for " + path, e);
		}
	}

	private boolean isPckDiagnosticsDumpEnabled() {
		try {
			return android.provider.Settings.Global.getInt(getContentResolver(), "sts2_dump_pck_diagnostics", 0) == 1;
		} catch (Exception e) {
			return false;
		}
	}

	private boolean isPckDiagnosticsPath(String path) {
		return isPckPath(path, ".godot/extension_list.cfg")
			|| isPckPath(path, "addons/fmod/fmod.gdextension")
			|| isPckPath(path, "addons/fmod/FmodManager.gd")
			|| isPckPath(path, "addons/fmod/FmodManager.gdc")
			|| isPckPath(path, "src/gdscript/music_controller_proxy.gd")
			|| isPckPath(path, "src/gdscript/music_controller_proxy.gdc")
			|| isPckPath(path, "src/gdscript/audio_manager_proxy.gd")
			|| isPckPath(path, "src/gdscript/audio_manager_proxy.gdc")
			|| isPckPath(path, "scenes/game.tscn")
			|| isPckPath(path, "project.godot");
	}

	private boolean isPckPath(String path, String expected) {
		return expected.equals(path) || ("res://" + expected).equals(path);
	}

	private boolean patchPckBinaryProjectEntry(RandomAccessFile raf, long offset, long size) throws IOException {
		if (offset < 0 || size < 0 || size > 8L * 1024L * 1024L || offset + size > raf.length()) {
			return false;
		}

		long saved = raf.getFilePointer();
		raf.seek(offset);
		byte[] content = new byte[(int)size];
		raf.readFully(content);

		boolean patched = false;
		patched |= replacePckEntryBytes(content, "autoload/SentryInit", "disabled/SentryInit");
		patched |= replacePckEntryBytes(content, "autoload/FmodManager", "disabled/FmodManager");

		if (patched) {
			raf.seek(offset);
			raf.write(content);
		}

		raf.seek(saved);
		return patched;
	}

	private void patchRawPckReferences(File pckFile, String[] references) {
		try (RandomAccessFile raf = new RandomAccessFile(pckFile, "rw")) {
			boolean patched = false;
			for (String reference : references) {
				patched |= overwriteRawBytes(raf, reference.getBytes("UTF-8"));
				raf.seek(0);
			}

			if (patched) {
				Log.i(TAG, "Raw-patched Android-incompatible PCK plugin references");
			}
		} catch (Exception e) {
			Log.w(TAG, "Raw PCK reference patch failed", e);
		}
	}

	private boolean overwriteRawBytes(RandomAccessFile raf, byte[] needle) throws IOException {
		if (needle.length == 0) {
			return false;
		}

		final int chunkSize = 1024 * 1024;
		final int overlap = needle.length - 1;
		byte[] buffer = new byte[chunkSize + overlap];
		long position = 0;
		int carried = 0;
		boolean patched = false;

		while (position < raf.length()) {
			raf.seek(position);
			int read = raf.read(buffer, carried, chunkSize);
			if (read <= 0) {
				break;
			}

			int limit = carried + read;
			for (int i = 0; i <= limit - needle.length; i++) {
				boolean match = true;
				for (int j = 0; j < needle.length; j++) {
					if (buffer[i + j] != needle[j]) {
						match = false;
						break;
					}
				}
				if (match) {
					long absolute = position - carried + i;
					raf.seek(absolute);
					for (int j = 0; j < needle.length; j++) {
						raf.writeByte(' ');
					}
					patched = true;
				}
			}

			carried = Math.min(overlap, limit);
			if (carried > 0) {
				System.arraycopy(buffer, limit - carried, buffer, 0, carried);
			}
			position += read;
		}

		return patched;
	}

	private boolean patchPckTextEntryReplacements(RandomAccessFile raf, long offset, long size, String[][] replacements) throws IOException {
		if (offset < 0 || size < 0 || size > 8L * 1024L * 1024L || offset + size > raf.length()) {
			return false;
		}

		long saved = raf.getFilePointer();
		raf.seek(offset);
		byte[] content = new byte[(int)size];
		raf.readFully(content);
		boolean patched = false;

		for (String[] replacement : replacements) {
			patched |= replacePckEntryBytes(content, replacement[0], replacement[1]);
		}

		if (patched) {
			raf.seek(offset);
			raf.write(content);
		}

		raf.seek(saved);
		return patched;
	}

	private String spacesFor(String value) {
		char[] chars = new char[value.length()];
		java.util.Arrays.fill(chars, ' ');
		return new String(chars);
	}

	private boolean patchPckTextEntry(RandomAccessFile raf, long offset, long size, String[] needles) throws IOException {
		if (offset < 0 || size < 0 || size > 8L * 1024L * 1024L || offset + size > raf.length()) {
			return false;
		}

		long saved = raf.getFilePointer();
		raf.seek(offset);
		byte[] content = new byte[(int)size];
		raf.readFully(content);
		boolean patched = false;

		for (String needle : needles) {
			byte[] search = needle.getBytes("UTF-8");
			int idx = indexOf(content, search);
			if (idx < 0) {
				continue;
			}

			if (needle.indexOf("://") >= 0 && !needle.endsWith(".gd\"")) {
				for (int i = 0; i < search.length; i++) {
					content[idx + i] = (byte)' ';
				}
			} else {
				content[idx] = (byte)';';
			}
			patched = true;
		}

		if (patched) {
			raf.seek(offset);
			raf.write(content);
		}

		raf.seek(saved);
		return patched;
	}

	private boolean replacePckEntryBytes(byte[] content, String searchText, String replacementText) throws IOException {
		byte[] search = searchText.getBytes("UTF-8");
		byte[] replacement = replacementText.getBytes("UTF-8");
		if (search.length != replacement.length) {
			throw new IOException("PCK replacement length mismatch for " + searchText);
		}

		boolean patched = false;
		int idx = indexOf(content, search);
		while (idx >= 0) {
			System.arraycopy(replacement, 0, content, idx, replacement.length);
			patched = true;
			idx = indexOf(content, search);
		}

		return patched;
	}

	private int indexOf(byte[] haystack, byte[] needle) {
		for (int i = 0; i <= haystack.length - needle.length; i++) {
			boolean match = true;
			for (int j = 0; j < needle.length; j++) {
				if (haystack[i + j] != needle[j]) {
					match = false;
					break;
				}
			}
			if (match) {
				return i;
			}
		}
		return -1;
	}

	private long readUInt32LE(RandomAccessFile raf) throws IOException {
		long b0 = raf.readUnsignedByte();
		long b1 = raf.readUnsignedByte();
		long b2 = raf.readUnsignedByte();
		long b3 = raf.readUnsignedByte();
		return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
	}

	private long readLongLE(RandomAccessFile raf) throws IOException {
		long value = 0;
		for (int i = 0; i < 8; i++) {
			value |= ((long)raf.readUnsignedByte()) << (8 * i);
		}
		return value;
	}

	private String extractBootstrapPck() {
		File dest = new File(getFilesDir(), "bootstrap.pck");
		try (InputStream in = getAssets().open("bootstrap.pck");
				OutputStream out = new FileOutputStream(dest)) {
			byte[] buf = new byte[4096];
			int len;
			while ((len = in.read(buf)) > 0) {
				out.write(buf, 0, len);
			}
			return dest.getAbsolutePath();
		} catch (IOException e) {
			Log.e(TAG, "Failed to extract bootstrap PCK", e);
			return null;
		}
	}

	@Override
	protected void onDestroy() {
		clearSteamLoginCredentialPanel();
		if (multicastLock != null && multicastLock.isHeld()) {
			multicastLock.release();
			Log.i(TAG, "WiFi MulticastLock released");
		}
		super.onDestroy();
	}

	public static GodotApp getInstance() {
		return instance;
	}

	public String getGameDir() {
		return gameDir;
	}

	public String getVersionName() {
		return BuildConfig.VERSION_NAME;
	}

    public void launchGameOnRestart() {
        Log.i(TAG, "Scheduling one-shot game launch on restart");
        boolean saved = getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
            .edit()
            .remove(KEY_SAFE_LAUNCH_ON_NEXT_START)
            .putBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, true)
            .commit();
        Log.i(TAG, "One-shot game launch request saved: " + saved);
        if (!saved) {
            Log.e(TAG, "Failed to persist one-shot game launch request; not restarting");
            return;
        }
        restartApp();
    }

    public void launchGameSafelyOnRestart() {
        Log.i(TAG, "Scheduling one-shot safe game launch on restart");
        boolean saved = getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
            .edit()
            .putBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, true)
            .putBoolean(KEY_SAFE_LAUNCH_ON_NEXT_START, true)
            .commit();
        Log.i(TAG, "One-shot safe game launch request saved: " + saved);
        if (!saved) {
            Log.e(TAG, "Failed to persist one-shot safe game launch request; not restarting");
            return;
        }
        restartApp();
    }

	public void restartApp() {
		Log.i(TAG, "Restarting app...");
		Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
		if (intent != null) {
			intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
			startActivity(intent);
		}
		Runtime.getRuntime().exit(0);
	}

	// AES-256-GCM encryption via Android Keystore (hardware-backed TEE).
	private SecretKey getOrCreateKeystoreKey() throws Exception {
		KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
		keyStore.load(null);

		if (keyStore.containsAlias(KEYSTORE_ALIAS)) {
			return ((KeyStore.SecretKeyEntry) keyStore.getEntry(KEYSTORE_ALIAS, null)).getSecretKey();
		}

		KeyGenerator keyGen = KeyGenerator.getInstance(
				android.security.keystore.KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
		keyGen.init(new android.security.keystore.KeyGenParameterSpec.Builder(
				KEYSTORE_ALIAS,
				android.security.keystore.KeyProperties.PURPOSE_ENCRYPT
						| android.security.keystore.KeyProperties.PURPOSE_DECRYPT)
				.setBlockModes(android.security.keystore.KeyProperties.BLOCK_MODE_GCM)
				.setEncryptionPaddings(android.security.keystore.KeyProperties.ENCRYPTION_PADDING_NONE)
				.setKeySize(256)
				.build());
		return keyGen.generateKey();
	}

	public String encryptString(String plaintext) {
		try {
			SecretKey key = getOrCreateKeystoreKey();
			Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
			cipher.init(Cipher.ENCRYPT_MODE, key);
			byte[] iv = cipher.getIV();
			byte[] ciphertext = cipher.doFinal(plaintext.getBytes("UTF-8"));

			// Format: [iv_length (1 byte)] [iv] [ciphertext]
			byte[] result = new byte[1 + iv.length + ciphertext.length];
			result[0] = (byte) iv.length;
			System.arraycopy(iv, 0, result, 1, iv.length);
			System.arraycopy(ciphertext, 0, result, 1 + iv.length, ciphertext.length);
			return Base64.encodeToString(result, Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "Encryption failed", e);
			return null;
		}
	}

	public String decryptString(String encrypted) {
		try {
			byte[] blob = Base64.decode(encrypted, Base64.NO_WRAP);
			int ivLength = blob[0] & 0xFF;
			byte[] iv = new byte[ivLength];
			System.arraycopy(blob, 1, iv, 0, ivLength);
			byte[] ciphertext = new byte[blob.length - 1 - ivLength];
			System.arraycopy(blob, 1 + ivLength, ciphertext, 0, ciphertext.length);

			SecretKey key = getOrCreateKeystoreKey();
			Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
			cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, iv));
			byte[] plaintext = cipher.doFinal(ciphertext);
			return new String(plaintext, "UTF-8");
		} catch (Exception e) {
			Log.e(TAG, "Decryption failed", e);
			return null;
		}
	}

	public String getExternalFilesDirPath() {
		File dir = getExternalFilesDir(null);
		return dir != null ? dir.getAbsolutePath() : null;
	}

	public String getInternalFilesDirPath() {
		File dir = getFilesDir();
		return dir != null ? dir.getAbsolutePath() : null;
	}

	public boolean shareTextFile(String path) {
		try {
			if (path == null || path.isEmpty()) {
				return false;
			}

			File file = new File(path);
			if (!file.exists() || !file.isFile()) {
				Log.w(TAG, "Diagnostics file does not exist for sharing: " + path);
				return false;
			}

			Uri uri = FileProvider.getUriForFile(this, getPackageName() + ".fileprovider", file);
			Intent intent = new Intent(Intent.ACTION_SEND);
			intent.setType("text/plain");
			intent.putExtra(Intent.EXTRA_STREAM, uri);
			intent.putExtra(Intent.EXTRA_SUBJECT, "StS2 Mobile diagnostics");
			intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
			startActivity(Intent.createChooser(intent, "Share diagnostics"));
			return true;
		} catch (Exception e) {
			Log.e(TAG, "Failed to share diagnostics file", e);
			return false;
		}
	}

	public String getLogcatTail(int lineCount) {
		int boundedLineCount = Math.max(50, Math.min(lineCount, 800));
		Process process = null;
		try {
			StringBuilder output = new StringBuilder();
			File lastAndroidException = new File(getFilesDir(), LAST_ANDROID_EXCEPTION_FILE);
			if (lastAndroidException.exists() && lastAndroidException.isFile()) {
				output.append("Last persisted Android uncaught exception:\n");
				output.append(readSmallTextFile(lastAndroidException, 32 * 1024));
				output.append("\n\n");
			}

			String[] command = new String[] {
				"logcat",
				"-d",
				"-t",
				String.valueOf(boundedLineCount),
				"-v",
				"time",
				"STS2Mobile:I",
				"Godot:I",
				"godot:I",
				"Mono:I",
				"mono-rt:E",
				"AndroidRuntime:E",
				"crash_dump64:E",
				"libc:F",
				"DEBUG:E",
				"*:S"
			};
			process = Runtime.getRuntime().exec(command);

			try (BufferedReader reader = new BufferedReader(new java.io.InputStreamReader(process.getInputStream()))) {
				String line;
				while ((line = reader.readLine()) != null) {
					output.append(line).append('\n');
				}
			}

			try (BufferedReader reader = new BufferedReader(new java.io.InputStreamReader(process.getErrorStream()))) {
				String line;
				while ((line = reader.readLine()) != null) {
					output.append("[stderr] ").append(line).append('\n');
				}
			}

			int exitCode = process.waitFor();
			if (exitCode != 0) {
				output.append("[logcat exited ").append(exitCode).append("]\n");
			}

			return output.length() == 0 ? "<empty>" : output.toString();
		} catch (Exception e) {
			Log.w(TAG, "Failed to collect logcat tail", e);
			return "Failed to collect logcat tail: " + e;
		} finally {
			if (process != null) {
			process.destroy();
			}
		}
	}

	private String readSmallTextFile(File file, int maxBytes) {
		try (FileInputStream in = new FileInputStream(file)) {
			ByteArrayOutputStream out = new ByteArrayOutputStream();
			byte[] buffer = new byte[4096];
			int remaining = maxBytes;
			while (remaining > 0) {
				int read = in.read(buffer, 0, Math.min(buffer.length, remaining));
				if (read <= 0) {
					break;
				}
				out.write(buffer, 0, read);
				remaining -= read;
			}
			if (in.read() >= 0) {
				out.write("\n[truncated]\n".getBytes("UTF-8"));
			}
			return out.toString("UTF-8");
		} catch (Exception e) {
			return "Failed to read " + file.getAbsolutePath() + ": " + e;
		}
	}

	public long getUsableSpaceBytes(String path) {
		try {
			File target = (path == null || path.isEmpty()) ? getFilesDir() : new File(path);
			File probe = target;
			while (probe != null && !probe.exists()) {
				probe = probe.getParentFile();
			}
			if (probe == null) {
				probe = getFilesDir();
			}
			return probe.getUsableSpace();
		} catch (Exception e) {
			Log.w(TAG, "Failed to query usable space for " + path, e);
			return -1L;
		}
	}

	// Returns true if the app has permission to write to shared external storage.
	public boolean hasStoragePermission() {
		if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.R) {
			return android.os.Environment.isExternalStorageManager();
		}
		return checkSelfPermission(
				android.Manifest.permission.WRITE_EXTERNAL_STORAGE) == android.content.pm.PackageManager.PERMISSION_GRANTED;
	}

	// Requests external storage permission. On Android 11+, opens the system
	// settings
	// page for "All files access". On older versions, shows the runtime permission
	// dialog.
	public void requestStoragePermission() {
		if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.R) {
			try {
				Intent intent = new Intent(android.provider.Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION);
				intent.setData(android.net.Uri.parse("package:" + getPackageName()));
				startActivity(intent);
			} catch (Exception e) {
				Log.w(TAG, "Failed to open app-specific storage settings, trying general", e);
				Intent intent = new Intent(android.provider.Settings.ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION);
				startActivity(intent);
			}
		} else {
			requestPermissions(new String[] { android.Manifest.permission.WRITE_EXTERNAL_STORAGE }, 1);
		}
	}

	public void showSteamLoginCredentialPanel() {
		runOnUiThread(() -> {
			ensureSteamLoginCredentialPanel();
			reflowSteamLoginCredentialPanelForCurrentWindow();
			if (steamLoginCredentialOverlay == null) {
				Log.w(TAG, "Native Steam login panel unavailable");
				return;
			}
			steamLoginCredentialOverlay.setVisibility(View.VISIBLE);
			updateSteamLoginCredentialKeyboardInsets();
			setSteamLoginCredentialPanelEnabled(true);
			setSteamLoginCredentialStatus(steamLoginCredentialShownStatusText());
			steamLoginCredentialUsernameField.requestFocus();
			scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialUsernameField);
			InputMethodManager inputMethodManager = (InputMethodManager) getSystemService(Context.INPUT_METHOD_SERVICE);
			if (inputMethodManager != null) {
				inputMethodManager.showSoftInput(steamLoginCredentialUsernameField, InputMethodManager.SHOW_IMPLICIT);
			}
			requestSteamLoginCredentialAutofill();
			Log.i(TAG, "Native Steam login credential panel shown");
		});
	}

	public void hideSteamLoginCredentialPanel() {
		runOnUiThread(() -> {
			if (steamLoginCredentialOverlay != null) {
				hideKeyboardForSteamLoginCredentialPanel();
				clearSteamLoginCredentialPanelSensitiveFields();
				steamLoginCredentialOverlay.setVisibility(View.GONE);
			}
		});
	}

	@Override
	public void onBackPressed() {
		if (isSteamLoginCredentialPanelVisible()) {
			dismissSteamLoginCredentialPanelFromBack();
			return;
		}

		super.onBackPressed();
	}

	@Override
	public boolean dispatchKeyEvent(KeyEvent event) {
		if (event.getKeyCode() == KeyEvent.KEYCODE_BACK
				&& event.getAction() == KeyEvent.ACTION_UP
				&& isSteamLoginCredentialPanelVisible()) {
			dismissSteamLoginCredentialPanelFromBack();
			return true;
		}

		return super.dispatchKeyEvent(event);
	}

	@Override
	public void onConfigurationChanged(Configuration newConfig) {
		super.onConfigurationChanged(newConfig);
		reflowSteamLoginCredentialPanelForCurrentWindow();
	}

	public String consumeSteamLoginCredentialResult() {
		synchronized (steamLoginCredentialLock) {
			if (pendingSteamLoginCredentialExpiresAtMs > 0L && System.currentTimeMillis() > pendingSteamLoginCredentialExpiresAtMs) {
				clearPendingSteamLoginCredentialsLocked();
				return "";
			}

			if (pendingSteamLoginCredentialUsername.isEmpty() && pendingSteamLoginCredentialPassword.isEmpty()) {
				return "";
			}

			String result = base64Utf8(pendingSteamLoginCredentialUsername) + "\n" + base64Utf8(pendingSteamLoginCredentialPassword);
			clearPendingSteamLoginCredentialsLocked();
			return result;
		}
	}

	private String base64Utf8(String value) {
		String safeValue = value == null ? "" : value;
		return Base64.encodeToString(safeValue.getBytes(StandardCharsets.UTF_8), Base64.NO_WRAP);
	}

	public boolean isSteamLoginCredentialPanelVisible() {
		return steamLoginCredentialOverlay != null && steamLoginCredentialOverlay.getVisibility() == View.VISIBLE;
	}

	private void hideKeyboardForSteamLoginCredentialPanel() {
		View focusedView = getCurrentFocus();
		if (focusedView == null && steamLoginCredentialOverlay != null) {
			focusedView = steamLoginCredentialOverlay;
		}
		if (focusedView == null) {
			return;
		}

		InputMethodManager inputMethodManager = (InputMethodManager) getSystemService(Context.INPUT_METHOD_SERVICE);
		if (inputMethodManager != null) {
			inputMethodManager.hideSoftInputFromWindow(focusedView.getWindowToken(), 0);
		}
		focusedView.clearFocus();
	}

	private void dismissSteamLoginCredentialPanelFromBack() {
		hideKeyboardForSteamLoginCredentialPanel();
		cancelSteamLoginCredentialAutofillSession();
		setSteamLoginCredentialStatus("Steam login cancelled. No password was stored.");
		hideSteamLoginCredentialPanel();
	}

	private void ensureSteamLoginCredentialPanel() {
		if (steamLoginCredentialOverlay != null) {
			return;
		}

		steamLoginCredentialWideLayout = useSteamLoginCredentialWideLayout();
		boolean wideCredentialLayout = steamLoginCredentialWideLayout;
		steamLoginCredentialShortHeightLayout = wideCredentialLayout && useSteamLoginCredentialShortHeightLayout();
		boolean shortHeightCredentialLayout = steamLoginCredentialShortHeightLayout;
		int horizontalPadding = wideCredentialLayout ? (shortHeightCredentialLayout ? 20 : 24) : 22;
		int verticalPadding = wideCredentialLayout ? (shortHeightCredentialLayout ? 12 : 16) : 14;
		int fieldGroupMarginDp = shortHeightCredentialLayout ? 6 : 8;

		FrameLayout overlay = new FrameLayout(this);
		overlay.setVisibility(View.GONE);
		overlay.setClickable(true);
		overlay.setBackgroundColor(Color.argb(172, 5, 8, 14));
		overlay.getViewTreeObserver().addOnGlobalLayoutListener(this::updateSteamLoginCredentialKeyboardInsets);

		LinearLayout card = new LinearLayout(this);
		card.setOrientation(LinearLayout.VERTICAL);
		card.setPadding(
			dp(horizontalPadding),
			dp(verticalPadding),
			dp(horizontalPadding),
			dp(verticalPadding)
		);
		GradientDrawable cardBackground = new GradientDrawable(
			GradientDrawable.Orientation.TL_BR,
			new int[] { Color.rgb(17, 24, 35), Color.rgb(8, 11, 17) }
		);
		cardBackground.setCornerRadius(dp(22));
		cardBackground.setStroke(dp(1), Color.rgb(35, 225, 240));
		card.setBackground(cardBackground);

		TextView title = new TextView(this);
		title.setText("Steam login");
		title.setTextColor(Color.WHITE);
		title.setTextSize(shortHeightCredentialLayout ? 20 : 22);
		title.setTypeface(Typeface.DEFAULT_BOLD);
		card.addView(title);

		TextView subtitle = new TextView(this);
		subtitle.setText(shortHeightCredentialLayout
			? "Use Android password suggestions here. Credentials clear after one Steam handoff."
			: "Choose a saved Steam credential if Android, Samsung, or Google offers it here. Credentials are handed to SteamKit once, then cleared.");
		subtitle.setTextColor(Color.rgb(188, 201, 213));
		subtitle.setTextSize(shortHeightCredentialLayout ? 11 : 12);
		subtitle.setPadding(0, dp(shortHeightCredentialLayout ? 2 : 4), 0, dp(shortHeightCredentialLayout ? 4 : 8));
		card.addView(subtitle);

		TextView trust = new TextView(this);
		trust.setText(shortHeightCredentialLayout ? "Not stored by StS2 Mobile." : "Steam password is never stored by StS2 Mobile.");
		trust.setTextColor(Color.rgb(35, 225, 240));
		trust.setTextSize(12);
		trust.setTypeface(Typeface.DEFAULT_BOLD);
		trust.setPadding(0, 0, 0, dp(shortHeightCredentialLayout ? 3 : 6));
		card.addView(trust);

		steamLoginCredentialStatusText = new TextView(this);
		steamLoginCredentialStatusText.setText(steamLoginCredentialDefaultStatusText());
		steamLoginCredentialStatusText.setTextColor(Color.rgb(155, 178, 188));
		steamLoginCredentialStatusText.setTextSize(shortHeightCredentialLayout ? 11 : 12);
		steamLoginCredentialStatusText.setPadding(0, 0, 0, dp(shortHeightCredentialLayout ? 4 : 6));
		card.addView(steamLoginCredentialStatusText);

		steamLoginCredentialUsernameField = new SteamLoginCredentialEditText(this, STEAM_CREDENTIAL_WEB_DOMAIN_STORE);
		steamLoginCredentialUsernameField.setContentDescription("Steam username");
		steamLoginCredentialUsernameField.setHint("Steam username or email");
		steamLoginCredentialUsernameField.setSingleLine(true);
		steamLoginCredentialUsernameField.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS);
		steamLoginCredentialUsernameField.setImeOptions(EditorInfo.IME_ACTION_NEXT);
		steamLoginCredentialUsernameField.setOnEditorActionListener((v, actionId, event) -> {
			if (actionId == EditorInfo.IME_ACTION_NEXT) {
				steamLoginCredentialPasswordField.requestFocus();
				scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialPasswordField);
				requestSteamLoginCredentialAutofillField(steamLoginCredentialPasswordField);
				return true;
			}
			return false;
		});
		steamLoginCredentialUsernameField.setOnFocusChangeListener((v, hasFocus) -> {
			if (hasFocus) {
				scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialUsernameField);
				requestSteamLoginCredentialAutofillField(steamLoginCredentialUsernameField);
			}
		});
		configureCredentialField(steamLoginCredentialUsernameField, View.AUTOFILL_HINT_USERNAME);

		steamLoginCredentialNextPasswordButton = new Button(this);
		steamLoginCredentialNextPasswordButton.setText("Next");
		steamLoginCredentialNextPasswordButton.setContentDescription("Move to Steam password field");
		styleSteamLoginCredentialButton(steamLoginCredentialNextPasswordButton, false);
		steamLoginCredentialNextPasswordButton.setOnClickListener(v -> focusSteamLoginPasswordField());

		LinearLayout usernameRow = createSteamLoginCredentialInputRow(wideCredentialLayout);
		usernameRow.addView(steamLoginCredentialUsernameField, credentialFieldLayoutParams(wideCredentialLayout));
		usernameRow.addView(steamLoginCredentialNextPasswordButton, credentialInlineActionLayoutParams(wideCredentialLayout, 4));
		card.addView(usernameRow, credentialGroupLayoutParams(fieldGroupMarginDp));

		steamLoginCredentialPasswordField = new SteamLoginCredentialEditText(this, STEAM_CREDENTIAL_WEB_DOMAIN_STORE);
		steamLoginCredentialPasswordField.setContentDescription("Steam password");
		steamLoginCredentialPasswordField.setHint("Steam password");
		steamLoginCredentialPasswordField.setSingleLine(true);
		steamLoginCredentialPasswordField.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
		steamLoginCredentialPasswordField.setImeOptions(EditorInfo.IME_ACTION_DONE);
		steamLoginCredentialPasswordField.setOnEditorActionListener((v, actionId, event) -> {
			if (actionId == EditorInfo.IME_ACTION_DONE) {
				submitSteamLoginCredentials();
				return true;
			}
			return false;
		});
		steamLoginCredentialPasswordField.setOnFocusChangeListener((v, hasFocus) -> {
			if (hasFocus) {
				scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialPasswordField);
				requestSteamLoginCredentialAutofillField(steamLoginCredentialPasswordField);
			}
		});
		configureCredentialField(steamLoginCredentialPasswordField, View.AUTOFILL_HINT_PASSWORD);

		steamLoginCredentialPasswordVisibilityButton = new Button(this);
		steamLoginCredentialPasswordVisibilityButton.setText("Show password");
		steamLoginCredentialPasswordVisibilityButton.setContentDescription("Show or hide Steam password while typing");
		styleSteamLoginCredentialButton(steamLoginCredentialPasswordVisibilityButton, false);
		steamLoginCredentialPasswordVisibilityButton.setOnClickListener(v -> toggleSteamLoginCredentialPasswordVisibility());

		LinearLayout passwordRow = createSteamLoginCredentialInputRow(wideCredentialLayout);
		passwordRow.addView(steamLoginCredentialPasswordField, credentialFieldLayoutParams(wideCredentialLayout));
		passwordRow.addView(steamLoginCredentialPasswordVisibilityButton, credentialInlineActionLayoutParams(wideCredentialLayout, 6));
		card.addView(passwordRow, credentialGroupLayoutParams(fieldGroupMarginDp));

		LinearLayout buttons = new LinearLayout(this);
		buttons.setOrientation(wideCredentialLayout ? LinearLayout.HORIZONTAL : LinearLayout.VERTICAL);
		buttons.setGravity(wideCredentialLayout ? Gravity.CENTER_VERTICAL : Gravity.CENTER_HORIZONTAL);
		buttons.setPadding(0, dp(shortHeightCredentialLayout ? 8 : (wideCredentialLayout ? 10 : 8)), 0, 0);

		steamLoginCredentialSubmitButton = new Button(this);
		steamLoginCredentialSubmitButton.setText("Sign in with Steam");
		steamLoginCredentialSubmitButton.setContentDescription("Sign in with Steam");
		styleSteamLoginCredentialButton(steamLoginCredentialSubmitButton, true);
		steamLoginCredentialSubmitButton.setOnClickListener(v -> submitSteamLoginCredentials());
		buttons.addView(steamLoginCredentialSubmitButton, credentialSubmitButtonLayoutParams(wideCredentialLayout));

		steamLoginCredentialCancelButton = new Button(this);
		steamLoginCredentialCancelButton.setText("Cancel");
		steamLoginCredentialCancelButton.setContentDescription("Cancel Steam login");
		styleSteamLoginCredentialButton(steamLoginCredentialCancelButton, false);
		steamLoginCredentialCancelButton.setOnClickListener(v -> {
			cancelSteamLoginCredentialAutofillSession();
			setSteamLoginCredentialStatus("Steam login cancelled. No password was stored.");
			hideSteamLoginCredentialPanel();
		});
		buttons.addView(steamLoginCredentialCancelButton, credentialCancelButtonLayoutParams(wideCredentialLayout));
		card.addView(buttons);

		ScrollView scroll = new ScrollView(this);
		scroll.setFillViewport(false);
		scroll.setClipToPadding(false);
		scroll.setPadding(0, dp(shortHeightCredentialLayout ? 4 : 8), 0, dp(shortHeightCredentialLayout ? 12 : 18));
		steamLoginCredentialScrollView = scroll;

		FrameLayout.LayoutParams cardParams = new FrameLayout.LayoutParams(
			steamLoginCredentialPanelWidth(wideCredentialLayout),
			FrameLayout.LayoutParams.WRAP_CONTENT,
			Gravity.TOP | Gravity.CENTER_HORIZONTAL
		);
		cardParams.topMargin = dp(shortHeightCredentialLayout ? 4 : (wideCredentialLayout ? 8 : 10));
		cardParams.bottomMargin = dp(shortHeightCredentialLayout ? 12 : 18);
		scroll.addView(card, cardParams);

		overlay.addView(
			scroll,
			new FrameLayout.LayoutParams(
				FrameLayout.LayoutParams.MATCH_PARENT,
				FrameLayout.LayoutParams.MATCH_PARENT
			)
		);

		addContentView(
			overlay,
			new FrameLayout.LayoutParams(
				FrameLayout.LayoutParams.MATCH_PARENT,
				FrameLayout.LayoutParams.MATCH_PARENT
			)
		);
		steamLoginCredentialOverlay = overlay;
	}

	private void reflowSteamLoginCredentialPanelForCurrentWindow() {
		if (steamLoginCredentialOverlay == null) {
			return;
		}

		boolean wideCredentialLayout = useSteamLoginCredentialWideLayout();
		boolean shortHeightCredentialLayout = wideCredentialLayout && useSteamLoginCredentialShortHeightLayout();
		if (wideCredentialLayout == steamLoginCredentialWideLayout
				&& shortHeightCredentialLayout == steamLoginCredentialShortHeightLayout) {
			updateSteamLoginCredentialKeyboardInsets();
			return;
		}

		boolean wasVisible = isSteamLoginCredentialPanelVisible();
		boolean wasEnabled = steamLoginCredentialSubmitButton == null || steamLoginCredentialSubmitButton.isEnabled();
		boolean usernameFocused = steamLoginCredentialUsernameField != null && steamLoginCredentialUsernameField.hasFocus();
		boolean passwordFocused = steamLoginCredentialPasswordField != null && steamLoginCredentialPasswordField.hasFocus();
		boolean passwordVisible = steamLoginCredentialPasswordVisible;
		String status = steamLoginCredentialStatusText == null ? "" : steamLoginCredentialStatusText.getText().toString();
		status = translateSteamLoginCredentialStatusForLayout(status, shortHeightCredentialLayout);
		String username = steamLoginCredentialUsernameField == null ? "" : steamLoginCredentialUsernameField.getText().toString();
		String password = steamLoginCredentialPasswordField == null ? "" : steamLoginCredentialPasswordField.getText().toString();

		clearSteamLoginCredentialVisibleFieldText();
		ViewGroup parent = (ViewGroup)steamLoginCredentialOverlay.getParent();
		if (parent != null) {
			parent.removeView(steamLoginCredentialOverlay);
		}
		clearSteamLoginCredentialViewReferences();

		ensureSteamLoginCredentialPanel();
		if (steamLoginCredentialOverlay == null) {
			return;
		}

		if (steamLoginCredentialUsernameField != null) {
			steamLoginCredentialUsernameField.setText(username);
		}
		if (steamLoginCredentialPasswordField != null) {
			steamLoginCredentialPasswordField.setText(password);
			setSteamLoginCredentialPasswordVisibilityState(passwordVisible);
			steamLoginCredentialPasswordField.setSelection(steamLoginCredentialPasswordField.length());
		}

		setSteamLoginCredentialPanelEnabled(wasEnabled);
		setSteamLoginCredentialStatus(status);
		steamLoginCredentialOverlay.setVisibility(wasVisible ? View.VISIBLE : View.GONE);
		updateSteamLoginCredentialKeyboardInsets();

		if (wasVisible) {
			View focusTarget = passwordFocused ? steamLoginCredentialPasswordField : (usernameFocused ? steamLoginCredentialUsernameField : null);
			if (focusTarget != null) {
				focusTarget.requestFocus();
				scheduleSteamLoginCredentialFocusScroll(focusTarget);
				if (focusTarget instanceof EditText) {
					requestSteamLoginCredentialAutofillField((EditText)focusTarget);
				}
			}
		}
	}

	private boolean useSteamLoginCredentialWideLayout() {
		int width = getResources().getDisplayMetrics().widthPixels;
		int height = getResources().getDisplayMetrics().heightPixels;
		return width > height && width >= dp(640);
	}

	private boolean useSteamLoginCredentialShortHeightLayout() {
		int width = getResources().getDisplayMetrics().widthPixels;
		int height = steamLoginCredentialUsableHeightPixels();
		return width > height && height < dp(430);
	}

	private int steamLoginCredentialUsableHeightPixels() {
		int height = getResources().getDisplayMetrics().heightPixels;
		try {
			Window window = getWindow();
			View decorView = window == null ? null : window.getDecorView();
			if (decorView != null) {
				android.graphics.Rect visibleFrame = new android.graphics.Rect();
				decorView.getWindowVisibleDisplayFrame(visibleFrame);
				if (visibleFrame.height() > 0) {
					height = Math.min(height, visibleFrame.height());
				}
			}
		} catch (Exception e) {
			Log.w(TAG, "Unable to read Steam login usable height", e);
		}
		return height;
	}

	private String steamLoginCredentialDefaultStatusText() {
		return steamLoginCredentialDefaultStatusText(useSteamLoginCredentialWideLayout() && useSteamLoginCredentialShortHeightLayout());
	}

	private String steamLoginCredentialDefaultStatusText(boolean shortHeightLayout) {
		return shortHeightLayout
			? "Password-manager suggestions requested for both fields."
			: "Password-manager suggestions are requested for both fields when the provider supports Steam.";
	}

	private String steamLoginCredentialShownStatusText() {
		return steamLoginCredentialShownStatusText(useSteamLoginCredentialWideLayout() && useSteamLoginCredentialShortHeightLayout());
	}

	private String steamLoginCredentialShownStatusText(boolean shortHeightLayout) {
		return shortHeightLayout
			? "Android password suggestions may appear here."
			: "Android password suggestions may appear when your provider recognizes Steam.";
	}

	private String translateSteamLoginCredentialStatusForLayout(String status, boolean shortHeightLayout) {
		if (steamLoginCredentialDefaultStatusText(true).equals(status)
				|| steamLoginCredentialDefaultStatusText(false).equals(status)) {
			return steamLoginCredentialDefaultStatusText(shortHeightLayout);
		}
		if (steamLoginCredentialShownStatusText(true).equals(status)
				|| steamLoginCredentialShownStatusText(false).equals(status)) {
			return steamLoginCredentialShownStatusText(shortHeightLayout);
		}
		return status;
	}

	private int steamLoginCredentialPanelWidth(boolean wideLayout) {
		int screenWidth = getResources().getDisplayMetrics().widthPixels;
		int sideMargins = dp(wideLayout ? 48 : 40);
		int maxWidth = dp(wideLayout ? 720 : 540);
		int availableWidth = Math.max(dp(280), screenWidth - sideMargins);
		return Math.min(availableWidth, maxWidth);
	}

	private LinearLayout createSteamLoginCredentialInputRow(boolean wideLayout) {
		LinearLayout row = new LinearLayout(this);
		row.setOrientation(wideLayout ? LinearLayout.HORIZONTAL : LinearLayout.VERTICAL);
		row.setGravity(wideLayout ? Gravity.CENTER_VERTICAL : Gravity.CENTER_HORIZONTAL);
		return row;
	}

	private void configureCredentialField(EditText field, String autofillHint) {
		field.setTextColor(Color.WHITE);
		field.setHintTextColor(Color.rgb(136, 151, 166));
		field.setTextSize(17);
		field.setSaveEnabled(false);
		field.setFocusableInTouchMode(true);
		field.setSelectAllOnFocus(false);
		field.setMinHeight(dp(56));
		field.setPadding(dp(14), 0, dp(14), 0);
		GradientDrawable background = new GradientDrawable();
		background.setShape(GradientDrawable.RECTANGLE);
		background.setColor(Color.rgb(8, 15, 23));
		background.setCornerRadius(dp(8));
		background.setStroke(dp(1), Color.rgb(62, 126, 148));
		field.setBackground(background);
		if (android.os.Build.VERSION.SDK_INT >= 26) {
			field.setAutofillHints(autofillHint);
			field.setImportantForAutofill(View.IMPORTANT_FOR_AUTOFILL_YES);
		}
	}

	private void styleSteamLoginCredentialButton(Button button, boolean primary) {
		if (button == null) {
			return;
		}

		GradientDrawable background = new GradientDrawable(
			GradientDrawable.Orientation.LEFT_RIGHT,
			primary
				? new int[] { Color.rgb(255, 126, 16), Color.rgb(255, 184, 42) }
				: new int[] { Color.rgb(16, 30, 42), Color.rgb(9, 16, 24) }
		);
		background.setCornerRadius(dp(14));
		background.setStroke(
			dp(1),
			primary ? Color.rgb(255, 208, 78) : Color.rgb(35, 225, 240)
		);
		button.setBackground(background);
		button.setTextColor(primary ? Color.rgb(5, 8, 14) : Color.rgb(232, 248, 250));
		button.setTextSize(14);
		button.setTypeface(Typeface.DEFAULT_BOLD);
		button.setAllCaps(false);
		button.setMinHeight(dp(primary ? 60 : 56));
		button.setPadding(dp(12), dp(6), dp(12), dp(6));
	}

	private LinearLayout.LayoutParams fieldLayoutParams() {
		LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
			LinearLayout.LayoutParams.MATCH_PARENT,
			LinearLayout.LayoutParams.WRAP_CONTENT
		);
		params.setMargins(0, dp(8), 0, 0);
		return params;
	}

	private LinearLayout.LayoutParams credentialGroupLayoutParams(int topMarginDp) {
		LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
			LinearLayout.LayoutParams.MATCH_PARENT,
			LinearLayout.LayoutParams.WRAP_CONTENT
		);
		params.setMargins(0, dp(topMarginDp), 0, 0);
		return params;
	}

	private LinearLayout.LayoutParams credentialFieldLayoutParams(boolean wideLayout) {
		if (!wideLayout) {
			return new LinearLayout.LayoutParams(
				LinearLayout.LayoutParams.MATCH_PARENT,
				LinearLayout.LayoutParams.WRAP_CONTENT
			);
		}

		return new LinearLayout.LayoutParams(
			0,
			LinearLayout.LayoutParams.WRAP_CONTENT,
			1f
		);
	}

	private LinearLayout.LayoutParams credentialInlineActionLayoutParams(boolean wideLayout, int stackedTopMarginDp) {
		LinearLayout.LayoutParams params = wideLayout
			? new LinearLayout.LayoutParams(dp(178), LinearLayout.LayoutParams.WRAP_CONTENT)
			: new LinearLayout.LayoutParams(
				LinearLayout.LayoutParams.MATCH_PARENT,
				LinearLayout.LayoutParams.WRAP_CONTENT
			);

		params.setMargins(wideLayout ? dp(10) : 0, wideLayout ? 0 : dp(stackedTopMarginDp), 0, 0);
		return params;
	}

	private LinearLayout.LayoutParams credentialSubmitButtonLayoutParams(boolean wideLayout) {
		if (!wideLayout) {
			return buttonLayoutParams(0);
		}

		return new LinearLayout.LayoutParams(
			0,
			LinearLayout.LayoutParams.WRAP_CONTENT,
			1.35f
		);
	}

	private LinearLayout.LayoutParams credentialCancelButtonLayoutParams(boolean wideLayout) {
		if (!wideLayout) {
			return buttonLayoutParams(6);
		}

		LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
			0,
			LinearLayout.LayoutParams.WRAP_CONTENT,
			0.85f
		);
		params.setMargins(dp(10), 0, 0, 0);
		return params;
	}

	private LinearLayout.LayoutParams buttonLayoutParams(int topMarginDp) {
		LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
			LinearLayout.LayoutParams.MATCH_PARENT,
			LinearLayout.LayoutParams.WRAP_CONTENT
		);
		params.setMargins(0, dp(topMarginDp), 0, 0);
		return params;
	}

	private void submitSteamLoginCredentials() {
		String username = steamLoginCredentialUsernameField == null ? "" : steamLoginCredentialUsernameField.getText().toString().trim();
		String password = steamLoginCredentialPasswordField == null ? "" : steamLoginCredentialPasswordField.getText().toString();
		if (username.isEmpty()) {
			setSteamLoginCredentialStatus("Enter your Steam username to continue.");
			steamLoginCredentialUsernameField.setError("Enter your Steam username");
			steamLoginCredentialUsernameField.requestFocus();
			scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialUsernameField);
			return;
		}
		if (password.isEmpty()) {
			setSteamLoginCredentialStatus("Enter your Steam password to continue.");
			steamLoginCredentialPasswordField.setError("Enter your Steam password");
			steamLoginCredentialPasswordField.requestFocus();
			scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialPasswordField);
			return;
		}

		setSteamLoginCredentialStatus("Submitting to Steam. StS2 Mobile is clearing these fields now.");
		cancelSteamLoginCredentialAutofillSession();
		synchronized (steamLoginCredentialLock) {
			pendingSteamLoginCredentialUsername = username;
			pendingSteamLoginCredentialPassword = password;
			pendingSteamLoginCredentialExpiresAtMs = System.currentTimeMillis() + STEAM_LOGIN_CREDENTIAL_RESULT_TTL_MS;
		}
		clearSteamLoginCredentialPanelSensitiveFields();
		setSteamLoginCredentialPanelEnabled(false);
		if (steamLoginCredentialOverlay != null) {
			steamLoginCredentialOverlay.setVisibility(View.GONE);
		}
		Log.i(TAG, "Native Steam login credentials submitted to managed login flow");
	}

	private void cancelSteamLoginCredentialAutofillSession() {
		if (android.os.Build.VERSION.SDK_INT < 26) {
			return;
		}

		AutofillManager autofillManager = getSystemService(AutofillManager.class);
		if (autofillManager != null) {
			autofillManager.cancel();
		}
	}

	private void toggleSteamLoginCredentialPasswordVisibility() {
		if (steamLoginCredentialPasswordField == null || steamLoginCredentialPasswordVisibilityButton == null) {
			return;
		}

		int cursor = steamLoginCredentialPasswordField.getSelectionStart();
		setSteamLoginCredentialPasswordVisibilityState(!steamLoginCredentialPasswordVisible);
		steamLoginCredentialPasswordField.setSelection(Math.max(0, Math.min(cursor, steamLoginCredentialPasswordField.length())));
	}

	private void setSteamLoginCredentialPasswordVisibilityState(boolean visible) {
		steamLoginCredentialPasswordVisible = visible;
		if (steamLoginCredentialPasswordField != null) {
			steamLoginCredentialPasswordField.setInputType(
				visible
					? InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD
					: InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD
			);
		}
		if (steamLoginCredentialPasswordVisibilityButton != null) {
			steamLoginCredentialPasswordVisibilityButton.setText(visible ? "Hide password" : "Show password");
			steamLoginCredentialPasswordVisibilityButton.setContentDescription(visible ? "Hide Steam password" : "Show Steam password while typing");
		}
	}

	private void requestSteamLoginCredentialAutofill() {
		if (android.os.Build.VERSION.SDK_INT < 26) {
			return;
		}

		requestSteamLoginCredentialAutofillField(steamLoginCredentialUsernameField);
		requestSteamLoginCredentialAutofillField(steamLoginCredentialPasswordField);
	}

	private void requestSteamLoginCredentialAutofillField(EditText field) {
		if (android.os.Build.VERSION.SDK_INT < 26 || field == null) {
			return;
		}

		AutofillManager autofillManager = getSystemService(AutofillManager.class);
		if (autofillManager == null) {
			return;
		}

		autofillManager.requestAutofill(field);
	}

	private void focusSteamLoginPasswordField() {
		if (steamLoginCredentialPasswordField == null) {
			return;
		}

		steamLoginCredentialPasswordField.requestFocus();
		scheduleSteamLoginCredentialFocusScroll(steamLoginCredentialPasswordField);
		InputMethodManager inputMethodManager = (InputMethodManager) getSystemService(Context.INPUT_METHOD_SERVICE);
		if (inputMethodManager != null) {
			inputMethodManager.showSoftInput(steamLoginCredentialPasswordField, InputMethodManager.SHOW_IMPLICIT);
		}
		requestSteamLoginCredentialAutofillField(steamLoginCredentialPasswordField);
		setSteamLoginCredentialStatus("Enter your Steam password. StS2 Mobile will clear it after handoff.");
	}

	private void setSteamLoginCredentialStatus(String text) {
		if (steamLoginCredentialStatusText != null) {
			steamLoginCredentialStatusText.setText(text == null ? "" : text);
		}
	}

	private void updateSteamLoginCredentialKeyboardInsets() {
		if (steamLoginCredentialOverlay == null || steamLoginCredentialScrollView == null) {
			return;
		}

		android.graphics.Rect visibleFrame = new android.graphics.Rect();
		steamLoginCredentialOverlay.getWindowVisibleDisplayFrame(visibleFrame);
		View rootView = steamLoginCredentialOverlay.getRootView();
		int rootHeight = rootView == null ? steamLoginCredentialOverlay.getHeight() : rootView.getHeight();
		int keyboardHeight = Math.max(0, rootHeight - visibleFrame.bottom);
		boolean visibleWideLayout = useSteamLoginCredentialWideLayout();
		boolean visibleShortHeightLayout = visibleWideLayout && useSteamLoginCredentialShortHeightLayout();
		if (isSteamLoginCredentialPanelVisible()
				&& (visibleWideLayout != steamLoginCredentialWideLayout
					|| visibleShortHeightLayout != steamLoginCredentialShortHeightLayout)) {
			reflowSteamLoginCredentialPanelForCurrentWindow();
			return;
		}

		int bottomPadding = Math.max(dp(18), keyboardHeight + dp(18));
		if (steamLoginCredentialScrollView.getPaddingBottom() == bottomPadding) {
			return;
		}

		steamLoginCredentialScrollView.setPadding(
			steamLoginCredentialScrollView.getPaddingLeft(),
			steamLoginCredentialScrollView.getPaddingTop(),
			steamLoginCredentialScrollView.getPaddingRight(),
			bottomPadding
		);
	}

	private void scheduleSteamLoginCredentialFocusScroll(View target) {
		if (target == null || steamLoginCredentialScrollView == null) {
			return;
		}

		steamLoginCredentialScrollView.postDelayed(() -> {
			updateSteamLoginCredentialKeyboardInsets();
			if (steamLoginCredentialScrollView != null && target.isShown()) {
				int targetScroll = target == steamLoginCredentialUsernameField ? 0 : Math.max(0, target.getBottom() - dp(96));
				steamLoginCredentialScrollView.smoothScrollTo(0, targetScroll);
			}
		}, 180L);
	}

	private void setSteamLoginCredentialPanelEnabled(boolean enabled) {
		if (steamLoginCredentialUsernameField != null) {
			steamLoginCredentialUsernameField.setEnabled(enabled);
		}
		if (steamLoginCredentialPasswordField != null) {
			steamLoginCredentialPasswordField.setEnabled(enabled);
		}
		if (steamLoginCredentialNextPasswordButton != null) {
			steamLoginCredentialNextPasswordButton.setEnabled(enabled);
		}
		if (steamLoginCredentialSubmitButton != null) {
			steamLoginCredentialSubmitButton.setEnabled(enabled);
		}
		if (steamLoginCredentialCancelButton != null) {
			steamLoginCredentialCancelButton.setEnabled(enabled);
		}
		if (steamLoginCredentialPasswordVisibilityButton != null) {
			steamLoginCredentialPasswordVisibilityButton.setEnabled(enabled);
		}
	}

	private void clearSteamLoginCredentialPanel() {
		synchronized (steamLoginCredentialLock) {
			clearPendingSteamLoginCredentialsLocked();
		}
		clearSteamLoginCredentialPanelSensitiveFields();
	}

	private void clearPendingSteamLoginCredentialsLocked() {
		pendingSteamLoginCredentialUsername = "";
		pendingSteamLoginCredentialPassword = "";
		pendingSteamLoginCredentialExpiresAtMs = 0L;
	}

	private void clearSteamLoginCredentialPanelSensitiveFields() {
		clearSteamLoginCredentialVisibleFieldText();
		setSteamLoginCredentialPasswordVisibilityState(false);
		setSteamLoginCredentialStatus(steamLoginCredentialDefaultStatusText());
	}

	private void clearSteamLoginCredentialVisibleFieldText() {
		if (steamLoginCredentialUsernameField != null) {
			steamLoginCredentialUsernameField.setText("");
			steamLoginCredentialUsernameField.setError(null);
		}
		if (steamLoginCredentialPasswordField != null) {
			steamLoginCredentialPasswordField.setText("");
			steamLoginCredentialPasswordField.setError(null);
		}
	}

	private void clearSteamLoginCredentialViewReferences() {
		steamLoginCredentialOverlay = null;
		steamLoginCredentialScrollView = null;
		steamLoginCredentialUsernameField = null;
		steamLoginCredentialPasswordField = null;
		steamLoginCredentialStatusText = null;
		steamLoginCredentialSubmitButton = null;
		steamLoginCredentialCancelButton = null;
		steamLoginCredentialNextPasswordButton = null;
		steamLoginCredentialPasswordVisibilityButton = null;
		steamLoginCredentialPasswordVisible = false;
		steamLoginCredentialWideLayout = false;
		steamLoginCredentialShortHeightLayout = false;
	}

	private static void setSteamCredentialWebDomain(ViewStructure structure, String webDomain) {
		if (android.os.Build.VERSION.SDK_INT >= 26 && structure != null) {
			structure.setWebDomain(webDomain);
		}
	}

	private static final class SteamLoginCredentialEditText extends EditText {
		private final String webDomain;

		SteamLoginCredentialEditText(Context context, String webDomain) {
			super(context);
			this.webDomain = webDomain;
		}

		@Override
		public void onProvideAutofillStructure(ViewStructure structure, int flags) {
			super.onProvideAutofillStructure(structure, flags);
			setSteamCredentialWebDomain(structure, webDomain);
			if (android.os.Build.VERSION.SDK_INT >= 26 && structure != null) {
				CharSequence description = getContentDescription();
				if (description != null) {
					structure.setHint(description);
				}
			}
		}
	}

	private int dp(int value) {
		return (int)(value * getResources().getDisplayMetrics().density + 0.5f);
	}

	private void setSteamKitDebugLogMode() {
		boolean enabled = false;
		try {
			enabled = android.provider.Settings.Global.getInt(getContentResolver(), "sts2_steamkit_debug_logs", 0) == 1;
			android.system.Os.setenv(ENV_STEAMKIT_DEBUG_LOGS, enabled ? "1" : "0", true);
			if (enabled) {
				Log.i(TAG, "Sanitized SteamKit debug logging enabled by Android global setting");
			}
		} catch (Exception e) {
			Log.w(TAG, "Failed to set SteamKit debug log mode", e);
			try {
				android.system.Os.setenv(ENV_STEAMKIT_DEBUG_LOGS, "0", true);
			} catch (Exception ignored) {
			}
		}
	}

	public void deleteKeystoreKey() {
		try {
			KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
			keyStore.load(null);
			keyStore.deleteEntry(KEYSTORE_ALIAS);
		} catch (Exception e) {
			Log.e(TAG, "Failed to delete keystore key", e);
		}
	}

	private final java.util.concurrent.atomic.AtomicInteger httpRequestIds =
		new java.util.concurrent.atomic.AtomicInteger();
	private static final long ASYNC_HTTP_RESPONSE_TTL_MS = 5L * 60L * 1000L;
	private final java.util.concurrent.ConcurrentHashMap<String, AsyncHttpResponse> httpRequestResponses =
		new java.util.concurrent.ConcurrentHashMap<>();
	private static final long CANCELED_HTTP_REQUEST_TTL_MS = 5L * 60L * 1000L;
	private final java.util.concurrent.ConcurrentHashMap<String, Long> canceledHttpRequests =
		new java.util.concurrent.ConcurrentHashMap<>();

	private static final class AsyncHttpResponse {
		final String body;
		final long completedAt;

		AsyncHttpResponse(String body) {
			this.body = body;
			this.completedAt = System.currentTimeMillis();
		}
	}

	public String httpRequestAsyncStart(String method, String urlString, String headersJson, String bodyBase64, int timeoutMs) {
		final String requestId = Integer.toString(httpRequestIds.incrementAndGet());
		Thread worker = new Thread(() -> {
			String response;
			try {
				response = performHttpRequest(method, urlString, headersJson, bodyBase64, timeoutMs);
			} catch (Throwable t) {
				Log.e(TAG, "Async HTTP bridge request failed unexpectedly: " + method + " " + sanitizeUrlForLog(urlString), t);
				response = "{\"error\":\"HTTP bridge request failed unexpectedly\"}";
			}
			if (canceledHttpRequests.remove(requestId) != null) {
				return;
			}
			cleanupAsyncHttpResponses();
			httpRequestResponses.put(requestId, new AsyncHttpResponse(response));
		}, "STS2-http-bridge-" + requestId);
		worker.setDaemon(true);
		worker.start();
		return requestId;
	}

	public String httpRequestAsyncPoll(String requestId) {
		cleanupAsyncHttpResponses();
		AsyncHttpResponse response = httpRequestResponses.remove(requestId);
		return response == null ? "" : response.body;
	}

	public void httpRequestAsyncCancel(String requestId) {
		httpRequestResponses.remove(requestId);
		cleanupCanceledHttpRequests();
		canceledHttpRequests.put(requestId, System.currentTimeMillis());
	}

	private void cleanupCanceledHttpRequests() {
		long cutoff = System.currentTimeMillis() - CANCELED_HTTP_REQUEST_TTL_MS;
		for (Map.Entry<String, Long> entry : canceledHttpRequests.entrySet()) {
			Long canceledAt = entry.getValue();
			if (canceledAt == null) {
				canceledHttpRequests.remove(entry.getKey());
			} else if (canceledAt < cutoff) {
				canceledHttpRequests.remove(entry.getKey(), canceledAt);
			}
		}
	}

	private void cleanupAsyncHttpResponses() {
		long cutoff = System.currentTimeMillis() - ASYNC_HTTP_RESPONSE_TTL_MS;
		for (Map.Entry<String, AsyncHttpResponse> entry : httpRequestResponses.entrySet()) {
			AsyncHttpResponse response = entry.getValue();
			if (response == null) {
				httpRequestResponses.remove(entry.getKey());
			} else if (response.completedAt < cutoff) {
				httpRequestResponses.remove(entry.getKey(), response);
			}
		}
	}

	public String httpRequest(String method, String urlString, String headersJson, String bodyBase64, int timeoutMs) {
		if (android.os.Looper.myLooper() == android.os.Looper.getMainLooper()) {
			return httpRequestOffMainThread(method, urlString, headersJson, bodyBase64, timeoutMs);
		}

		return performHttpRequest(method, urlString, headersJson, bodyBase64, timeoutMs);
	}

	private String httpRequestOffMainThread(
			String method,
			String urlString,
			String headersJson,
			String bodyBase64,
			int timeoutMs) {
		final java.util.concurrent.atomic.AtomicReference<String> result =
			new java.util.concurrent.atomic.AtomicReference<>();
		final java.util.concurrent.CountDownLatch completed = new java.util.concurrent.CountDownLatch(1);
		Thread worker = new Thread(() -> {
			try {
				result.set(performHttpRequest(method, urlString, headersJson, bodyBase64, timeoutMs));
			} finally {
				completed.countDown();
			}
		}, "STS2-http-bridge");
		worker.setDaemon(true);
		worker.start();

		try {
			completed.await();
			return result.get();
		} catch (InterruptedException e) {
			Thread.currentThread().interrupt();
			return "{\"error\":\"HTTP bridge request interrupted\"}";
		}
	}

	private String performHttpRequest(String method, String urlString, String headersJson, String bodyBase64, int timeoutMs) {
		HttpURLConnection connection = null;
		try {
			URL url = new URL(urlString);
			connection = (HttpURLConnection) url.openConnection();
			connection.setRequestMethod(method);
			connection.setConnectTimeout(timeoutMs);
			connection.setReadTimeout(timeoutMs);
			connection.setInstanceFollowRedirects(false);
			connection.setUseCaches(false);
			connection.setRequestProperty("Accept-Encoding", "identity");

			if (headersJson != null && !headersJson.isEmpty()) {
				JSONObject headers = new JSONObject(headersJson);
				Iterator<String> keys = headers.keys();
				while (keys.hasNext()) {
					String name = keys.next();
					if (
						"Content-Length".equalsIgnoreCase(name)
							|| "Host".equalsIgnoreCase(name)
							|| "Accept-Encoding".equalsIgnoreCase(name)
					) {
						continue;
					}
					JSONArray values = headers.optJSONArray(name);
					if (values == null) {
						continue;
					}
					for (int i = 0; i < values.length(); i++) {
						connection.addRequestProperty(name, values.optString(i, ""));
					}
				}
			}

			byte[] body = (bodyBase64 == null || bodyBase64.isEmpty())
				? new byte[0]
				: Base64.decode(bodyBase64, Base64.NO_WRAP);
			if (body.length > 0) {
				connection.setDoOutput(true);
				try (OutputStream out = connection.getOutputStream()) {
					out.write(body);
				}
			}

			int status = connection.getResponseCode();
			InputStream stream = status >= 400 ? connection.getErrorStream() : connection.getInputStream();
			long contentLength = connection.getContentLengthLong();

			JSONObject response = new JSONObject();
			response.put("status", status);
			response.put("reason", connection.getResponseMessage());

			if (shouldStreamHttpResponseToFile(method, urlString, status, contentLength) && stream != null) {
				File bodyFile = createHttpResponseTempFile();
				try (InputStream in = stream; OutputStream out = new FileOutputStream(bodyFile)) {
					copyStream(in, out);
				} catch (IOException e) {
					if (!bodyFile.delete()) {
						Log.w(TAG, "Could not delete failed CDN response file: " + bodyFile.getAbsolutePath());
					}
					throw e;
				}
				response.put("bodyFile", bodyFile.getAbsolutePath());
			} else {
				byte[] responseBody;
				if (stream == null) {
					responseBody = new byte[0];
				} else {
					try (InputStream in = stream) {
						responseBody = readFullyLimited(in, MAX_BUFFERED_HTTP_RESPONSE_BYTES);
					}
				}
				response.put("body", Base64.encodeToString(responseBody, Base64.NO_WRAP));
			}

			JSONObject responseHeaders = new JSONObject();
			Map<String, List<String>> headerFields = connection.getHeaderFields();
			if (headerFields != null) {
				for (Map.Entry<String, List<String>> entry : headerFields.entrySet()) {
					if (entry == null || entry.getKey() == null || entry.getValue() == null) {
						continue;
					}
					JSONArray values = new JSONArray();
					for (String value : entry.getValue()) {
						if (value != null) {
							values.put(value);
						}
					}
					responseHeaders.put(entry.getKey(), values);
				}
			}
			response.put("headers", responseHeaders);

			return response.toString();
		} catch (Exception e) {
			Log.e(TAG, "HTTP bridge request failed: " + method + " " + sanitizeUrlForLog(urlString), e);
			try {
				JSONObject response = new JSONObject();
				response.put("error", sanitizeErrorForBridge(e, urlString));
				return response.toString();
			} catch (Exception ignored) {
				return "{\"error\":\"HTTP bridge request failed\"}";
			}
		} finally {
			if (connection != null) {
				connection.disconnect();
			}
		}
	}

	private byte[] readFullyLimited(InputStream in, int maxBytes) throws IOException {
		try (ByteArrayOutputStream out = new ByteArrayOutputStream()) {
			byte[] buffer = new byte[8192];
			int total = 0;
			int read;
			while ((read = in.read(buffer)) != -1) {
				if (read > maxBytes - total) {
					throw new IOException("HTTP response body exceeds buffered limit: " + maxBytes);
				}
				total += read;
				out.write(buffer, 0, read);
			}
			return out.toByteArray();
		}
	}

	private String sanitizeUrlForLog(String urlString) {
		if (urlString == null) {
			return "<null>";
		}

		int queryIndex = urlString.indexOf('?');
		int fragmentIndex = urlString.indexOf('#');
		int cutIndex = -1;
		if (queryIndex >= 0 && fragmentIndex >= 0) {
			cutIndex = Math.min(queryIndex, fragmentIndex);
		} else if (queryIndex >= 0) {
			cutIndex = queryIndex;
		} else if (fragmentIndex >= 0) {
			cutIndex = fragmentIndex;
		}

		return cutIndex >= 0 ? urlString.substring(0, cutIndex) : urlString;
	}

	private String sanitizeErrorForBridge(Exception error, String urlString) {
		String message = error == null ? "unknown" : error.toString();
		if (urlString != null && !urlString.isEmpty()) {
			message = message.replace(urlString, sanitizeUrlForLog(urlString));
		}
		return redactUrlSuffixes(message);
	}

	private String redactUrlSuffixes(String message) {
		if (message == null || (message.indexOf('?') < 0 && message.indexOf('#') < 0)) {
			return message;
		}

		StringBuilder builder = new StringBuilder(message.length());
		int index = 0;
		while (index < message.length()) {
			char current = message.charAt(index);
			if (current != '?' && current != '#') {
				builder.append(current);
				index++;
				continue;
			}
			if (!isUrlLikeSuffixMarker(message, index)) {
				builder.append(current);
				index++;
				continue;
			}

			builder.append(current).append("<redacted>");
			index++;
			while (index < message.length()) {
				char suffix = message.charAt(index);
				if (Character.isWhitespace(suffix) || suffix == '"' || suffix == '\'' || suffix == ')') {
					break;
				}
				index++;
			}
			if (index < message.length()) {
				builder.append(message.charAt(index));
				index++;
			}
		}

		return builder.toString();
	}

	private boolean isUrlLikeSuffixMarker(String message, int markerIndex) {
		int tokenStart = markerIndex - 1;
		while (tokenStart >= 0) {
			char value = message.charAt(tokenStart);
			if (Character.isWhitespace(value) || value == '"' || value == '\'' || value == '(' || value == ')') {
				break;
			}
			tokenStart--;
		}

		String tokenPrefix = message.substring(tokenStart + 1, markerIndex);
		return tokenPrefix.startsWith("/")
			|| tokenPrefix.indexOf("://") >= 0
			|| tokenPrefix.regionMatches(true, 0, "http://", 0, 7)
			|| tokenPrefix.regionMatches(true, 0, "https://", 0, 8);
	}

	private void copyStream(InputStream in, OutputStream out) throws IOException {
		byte[] buffer = new byte[65536];
		int read;
		while ((read = in.read(buffer)) != -1) {
			out.write(buffer, 0, read);
		}
	}

	private boolean shouldStreamHttpResponseToFile(String method, String urlString, int status, long contentLength) {
		if (!"GET".equalsIgnoreCase(method) || urlString == null) {
			return false;
		}

		return urlString.contains("/chunk/")
			|| urlString.contains("/manifest/")
			|| contentLength >= STREAM_HTTP_RESPONSE_THRESHOLD_BYTES;
	}

	private File createHttpResponseTempFile() throws IOException {
		File cacheDir = getCacheDir();
		File dir = cacheDir != null ? cacheDir : new File(getFilesDir(), "tmp");
		if (!dir.exists() && !dir.mkdirs()) {
			throw new IOException("Could not create HTTP response temp directory: " + dir.getAbsolutePath());
		}
		cleanupStaleHttpResponseFilesDuringDownload(dir);
		return File.createTempFile("sts2_cdn_", ".bin", dir);
	}

	private void cleanupStaleHttpResponseFiles() {
		cleanupStaleHttpResponseFiles(getCacheDir(), 60L * 60L * 1000L);
		cleanupStaleHttpResponseFiles(new File(getFilesDir(), "tmp"), 60L * 60L * 1000L);
	}

	private void cleanupStaleHttpResponseFiles(File dir, long maxAgeMs) {
		try {
			File[] files = dir == null ? null : dir.listFiles();
			if (files == null) {
				return;
			}

			long cutoff = System.currentTimeMillis() - maxAgeMs;
			for (File file : files) {
				if (
					file != null
						&& file.isFile()
						&& file.getName().startsWith("sts2_cdn_")
						&& file.lastModified() < cutoff
						&& !file.delete()
				) {
					Log.w(TAG, "Could not delete stale CDN response file: " + file.getAbsolutePath());
				}
			}
		} catch (Exception e) {
			Log.w(TAG, "Failed to clean stale CDN response files", e);
		}
	}

	private void cleanupStaleHttpResponseFilesDuringDownload(File dir) {
		long now = System.currentTimeMillis();
		if (now - lastHttpResponseCleanupAt < 60L * 1000L) {
			return;
		}

		lastHttpResponseCleanupAt = now;
		cleanupStaleHttpResponseFiles(dir, 5L * 60L * 1000L);
	}

	public String randomBytesBase64(int count) {
		if (count < 0) {
			throw new IllegalArgumentException("count must be non-negative");
		}
		byte[] bytes = new byte[count];
		SECURE_RANDOM.nextBytes(bytes);
		return Base64.encodeToString(bytes, Base64.NO_WRAP);
	}

	public String rsaEncryptBase64(
			String publicKeyBase64,
			String modulusBase64,
			String exponentBase64,
			String dataBase64,
			String paddingName) {
		try {
			byte[] data = Base64.decode(dataBase64, Base64.NO_WRAP);
			KeyFactory keyFactory = KeyFactory.getInstance("RSA");

			PublicKey publicKey;
			if (publicKeyBase64 != null && !publicKeyBase64.isEmpty()) {
				byte[] publicKeyBytes = Base64.decode(publicKeyBase64, Base64.NO_WRAP);
				publicKey = keyFactory.generatePublic(new X509EncodedKeySpec(publicKeyBytes));
			} else {
				byte[] modulus = Base64.decode(modulusBase64, Base64.NO_WRAP);
				byte[] exponent = Base64.decode(exponentBase64, Base64.NO_WRAP);
				publicKey = keyFactory.generatePublic(new RSAPublicKeySpec(
					new BigInteger(1, modulus),
					new BigInteger(1, exponent)));
			}

			String transformation;
			if ("PKCS1".equals(paddingName)) {
				transformation = "RSA/ECB/PKCS1Padding";
			} else if ("OAEP-SHA1".equals(paddingName)) {
				transformation = "RSA/ECB/OAEPWithSHA-1AndMGF1Padding";
			} else {
				throw new IllegalArgumentException("Unsupported RSA padding: " + paddingName);
			}

			Cipher cipher = Cipher.getInstance(transformation);
			cipher.init(Cipher.ENCRYPT_MODE, publicKey);
			return Base64.encodeToString(cipher.doFinal(data), Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "RSA bridge encryption failed", e);
			return null;
		}
	}

	public String hmacSha1Base64(String keyBase64, String dataBase64) {
		try {
			byte[] key = Base64.decode(keyBase64, Base64.NO_WRAP);
			byte[] data = Base64.decode(dataBase64, Base64.NO_WRAP);
			Mac mac = Mac.getInstance("HmacSHA1");
			mac.init(new SecretKeySpec(key, "HmacSHA1"));
			return Base64.encodeToString(mac.doFinal(data), Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "HMAC-SHA1 bridge failed", e);
			return null;
		}
	}

	public String sha1Base64(String dataBase64) {
		try {
			byte[] data = Base64.decode(dataBase64, Base64.NO_WRAP);
			MessageDigest digest = MessageDigest.getInstance("SHA-1");
			return Base64.encodeToString(digest.digest(data), Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "SHA-1 bridge failed", e);
			return null;
		}
	}

	public String sha1FileBase64(String path) {
		try (InputStream in = new FileInputStream(new File(path))) {
			MessageDigest digest = MessageDigest.getInstance("SHA-1");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = in.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return Base64.encodeToString(digest.digest(), Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "File SHA-1 bridge failed", e);
			return null;
		}
	}

	public String sha256FileBase64(String path) {
		try (InputStream in = new FileInputStream(new File(path))) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = in.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return Base64.encodeToString(digest.digest(), Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "File SHA-256 bridge failed", e);
			return null;
		}
	}

	private static String describeGamePck(File pckFile) {
		return describeGamePck(pckFile, true);
	}

	private static String describeGamePck(File pckFile, boolean includeSha256) {
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

	private static String sha256Hex(File file) {
		try (InputStream in = new FileInputStream(file)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = in.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return bytesToHex(digest.digest());
		} catch (Exception e) {
			Log.w(TAG, "Failed to compute game PCK SHA-256: " + file.getAbsolutePath(), e);
			return "<unavailable:" + e.getClass().getSimpleName() + ">";
		}
	}

	private static String bytesToHex(byte[] bytes) {
		char[] hex = new char[bytes.length * 2];
		final char[] alphabet = "0123456789abcdef".toCharArray();
		for (int i = 0; i < bytes.length; i++) {
			int value = bytes[i] & 0xff;
			hex[i * 2] = alphabet[value >>> 4];
			hex[i * 2 + 1] = alphabet[value & 0x0f];
		}
		return new String(hex);
	}

	public String aesCryptBase64(
			String operation,
			String mode,
			String paddingName,
			String keyBase64,
			String ivBase64,
			String dataBase64) {
		try {
			byte[] key = Base64.decode(keyBase64, Base64.NO_WRAP);
			byte[] data = Base64.decode(dataBase64, Base64.NO_WRAP);
			String padding;
			if ("None".equals(paddingName)) {
				padding = "NoPadding";
			} else if ("PKCS7".equals(paddingName)) {
				padding = "PKCS5Padding";
			} else {
				throw new IllegalArgumentException("Unsupported AES padding: " + paddingName);
			}

			String transformation = "AES/" + mode + "/" + padding;
			Cipher cipher = Cipher.getInstance(transformation);
			SecretKeySpec keySpec = new SecretKeySpec(key, "AES");
			int cipherMode;
			if ("encrypt".equals(operation)) {
				cipherMode = Cipher.ENCRYPT_MODE;
			} else if ("decrypt".equals(operation)) {
				cipherMode = Cipher.DECRYPT_MODE;
			} else {
				throw new IllegalArgumentException("Unsupported AES operation: " + operation);
			}

			if ("CBC".equals(mode)) {
				byte[] iv = Base64.decode(ivBase64, Base64.NO_WRAP);
				cipher.init(cipherMode, keySpec, new IvParameterSpec(iv));
			} else if ("ECB".equals(mode)) {
				cipher.init(cipherMode, keySpec);
			} else {
				throw new IllegalArgumentException("Unsupported AES mode: " + mode);
			}

			return Base64.encodeToString(cipher.doFinal(data), Base64.NO_WRAP);
		} catch (Exception e) {
			Log.e(TAG, "AES bridge failed", e);
			return null;
		}
	}
}
