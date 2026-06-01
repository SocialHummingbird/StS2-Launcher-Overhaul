package com.game.sts2launcher;

import org.godotengine.godot.GodotActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.activity.EdgeToEdge;
import androidx.core.content.FileProvider;
import androidx.core.splashscreen.SplashScreen;

import android.content.SharedPreferences;

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
import android.net.Uri;
import android.net.wifi.WifiManager;
import android.util.Base64;

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
	private static final String KEYSTORE_ALIAS = "sts2mobile_credentials";
	private static final String PCK_FILE = "SlayTheSpire2.pck";
	private static final String PREFS_NAME = "sts2mobile";
	private static final String KEY_INSTALLED_VERSION_CODE = "installed_version_code";
	private static final String KEY_INSTALLED_PACKAGE_NAME = "installed_package_name";
	private static final String KEY_ASSEMBLY_CACHE_SCHEMA = "assembly_cache_schema";
	private static final String KEY_LAUNCH_GAME_ON_NEXT_START = "launch_game_on_next_start";
	private static final String KEY_SAFE_LAUNCH_ON_NEXT_START = "safe_launch_on_next_start";
	private static final int ASSEMBLY_CACHE_SCHEMA = 9;
	private static final String PCK_ANDROID_PATCH_MARKER = ".android_pck_patch_v26";
	private static final String LAST_ANDROID_EXCEPTION_FILE = "last_android_uncaught_exception.txt";
	private static final long STREAM_HTTP_RESPONSE_THRESHOLD_BYTES = 256L * 1024L;
	private static final int MAX_BUFFERED_HTTP_RESPONSE_BYTES = 1024 * 1024;
	private static boolean exceptionHandlerInstalled;
	private long lastHttpResponseCleanupAt;
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
	private static final String GAME_REQUIRED_ASSEMBLY = "sts2.dll";

	@Override
	public void onCreate(Bundle savedInstanceState) {
		instance = this;
		installAndroidExceptionHandler();
		gameDir = new File(getFilesDir(), "game").getAbsolutePath();
		configureTempDirectory();
		configureMonoForEmulator();
		cleanupStaleHttpResponseFiles();

		SplashScreen.installSplashScreen(this);
		EdgeToEdge.enable(this);

		try {
			setupAssemblies();
		} catch (RuntimeException ex) {
			Log.e(TAG, "Assembly setup failed, attempting one-time cache reset", ex);
			resetAssemblyCacheState();
			try {
				setupAssemblies();
			} catch (RuntimeException ex2) {
				Log.e(TAG, "Assembly setup failed after recovery. Routing to native diagnostics instead of starting Godot.", ex2);
				showNativeFailure(
					"StS2 Launcher diagnostics",
					"The launcher could not prepare the Android .NET assemblies required by native Godot.\n\nNative Godot was not started, because continuing would only trigger the generic '.NET assemblies not found' failure.",
					Log.getStackTraceString(ex2)
				);
				return;
			}
		}
		super.onCreate(savedInstanceState);

		// Android WiFi power saving drops broadcast packets without a MulticastLock.
		try {
			WifiManager wifiMgr = (WifiManager) getApplicationContext().getSystemService(Context.WIFI_SERVICE);
			multicastLock = wifiMgr.createMulticastLock("sts2_lan_discovery");
			multicastLock.setReferenceCounted(false);
			multicastLock.acquire();
			Log.i(TAG, "WiFi MulticastLock acquired for LAN discovery");
		} catch (Exception e) {
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

	private void writeInternalTextFile(String name, String text) {
		try (FileOutputStream out = new FileOutputStream(new File(getFilesDir(), name))) {
			out.write(text.getBytes("UTF-8"));
		} catch (Exception e) {
			Log.w(TAG, "Failed to write " + name, e);
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

	private boolean shouldRefreshAssemblyCache() {
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
			return !hasRequiredCacheFiles(destDir);
		}

		return true;
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
		if (requireGameAssemblies) {
			required.add(GAME_REQUIRED_ASSEMBLY);
		}

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

	private void logAssemblyCacheState(String phase, File destDir, File srcDir, boolean requireGameAssemblies, Set<String> packagedBclNames) {
		StringBuilder sb = new StringBuilder();
		sb.append("Assembly cache diagnostics [").append(phase).append("]");
		sb.append(" arch=").append(getRuntimeGodotArchDir());
		sb.append(" nativeLibraryDir=").append(getApplicationInfo().nativeLibraryDir);
		sb.append(" filesDir=").append(getFilesDir().getAbsolutePath());
		sb.append(" dest=").append(destDir == null ? "<none>" : destDir.getAbsolutePath());
		sb.append(" destExists=").append(destDir != null && destDir.exists());
		sb.append(" destIsDir=").append(destDir != null && destDir.isDirectory());
		sb.append(" src=").append(srcDir == null ? "<none>" : srcDir.getAbsolutePath());
		sb.append(" srcExists=").append(srcDir != null && srcDir.exists());
		sb.append(" packagedBclCount=").append(packagedBclNames == null ? 0 : packagedBclNames.size());
		sb.append(" requireGameAssemblies=").append(requireGameAssemblies);
		Log.i(TAG, sb.toString());

		ArrayList<String> required = new ArrayList<>(java.util.Arrays.asList(BOOTSTRAP_REQUIRED_ASSEMBLIES));
		if (requireGameAssemblies) {
			required.add(GAME_REQUIRED_ASSEMBLY);
		}
		for (String name : required.toArray(new String[0])) {
			File file = destDir == null ? null : new File(destDir, name);
			Log.i(TAG, "Assembly cache required file [" + phase + "]: " + name
				+ " exists=" + (file != null && file.exists())
				+ " bytes=" + (file != null && file.exists() ? file.length() : 0));
		}
	}

	private void markAssemblyCacheStateAsCurrent(int currentVersion) {
		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		prefs.edit()
			.putInt(KEY_ASSEMBLY_CACHE_SCHEMA, ASSEMBLY_CACHE_SCHEMA)
			.putInt(KEY_INSTALLED_VERSION_CODE, currentVersion)
			.putString(KEY_INSTALLED_PACKAGE_NAME, getPackageName())
			.apply();
	}

	private void resetAssemblyCacheState() {
		SharedPreferences prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
		prefs.edit().remove(KEY_ASSEMBLY_CACHE_SCHEMA).remove(KEY_INSTALLED_VERSION_CODE).remove(KEY_INSTALLED_PACKAGE_NAME).apply();
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
		File srcDir = findAssembliesDir();
		File destDir = new File(getFilesDir(), ".godot/mono/publish/" + getRuntimeGodotArchDir());
		int currentVersion = BuildConfig.VERSION_CODE;
		boolean gameReady = isGamePckReady();
		boolean requiresGameAssemblies = gameReady && hasGameAssemblies(srcDir);
		Set<String> packagedBclNames = getPackagedBclNames();

		boolean refreshCache = shouldRefreshAssemblyCache();
		logAssemblyCacheState("before-copy", destDir, srcDir, requiresGameAssemblies, packagedBclNames);

		File patcherMarker = new File(destDir, "STS2Mobile.dll");
		File sts2Marker = new File(destDir, "sts2.dll");
		if (!gameReady && sts2Marker.exists()) {
			Log.w(TAG, "Game PCK is not ready; clearing stale game assembly cache");
			refreshCache = true;
		}
		if (
			!refreshCache
				&& hasRequiredCacheFiles(destDir, requiresGameAssemblies)
				&& (!requiresGameAssemblies || hasCachedGameAssemblies(destDir, srcDir, packagedBclNames))
		) {
			Log.i(TAG, "Assemblies already set up at: " + destDir.getAbsolutePath());
			logAssemblyCacheState("cache-hit", destDir, srcDir, requiresGameAssemblies, packagedBclNames);
			markAssemblyCacheStateAsCurrent(currentVersion);
			return;
		}

		if (refreshCache) {
			Log.i(TAG, "New version detected, re-copying all assemblies");
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

		if (!hasRequiredCacheFiles(destDir, requiresGameAssemblies)) {
			logAssemblyCacheState("missing-after-copy", destDir, srcDir, requiresGameAssemblies, packagedBclNames);
			String mode = requiresGameAssemblies ? "game" : "launcher-only";
			throw new RuntimeException("Missing required Mono/cache assemblies after copy for " + mode + " mode.");
		}

		logAssemblyCacheState("after-copy", destDir, srcDir, requiresGameAssemblies, packagedBclNames);
		markAssemblyCacheStateAsCurrent(currentVersion);
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
		if (name == null || packagedBclNames.contains(name)) {
			return false;
		}

		String lower = name.toLowerCase(java.util.Locale.ROOT);
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
			|| lower.equals("steamworks.net.dll")
			|| lower.equals("sentry.dll")
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

	private boolean hasGameAssemblies(File srcDir) {
		if (srcDir == null || !srcDir.exists() || !srcDir.isDirectory()) {
			return false;
		}
		return new File(srcDir, GAME_REQUIRED_ASSEMBLY).exists();
	}

	private boolean containsAssemblies(File dir) {
		if (dir == null || !dir.exists() || !dir.isDirectory()) {
			return false;
		}
		File[] files = dir.listFiles((file, name) -> name.endsWith(".dll"));
		return files != null && files.length > 0;
	}

	private boolean hasCachedGameAssemblies(File destDir, File srcDir, Set<String> packagedBclNames) {
		if (destDir == null || srcDir == null || !srcDir.exists() || !srcDir.isDirectory()) {
			return false;
		}

		File[] files = srcDir.listFiles((file, name) -> name.endsWith(".dll"));
		if (files == null || files.length == 0) {
			return false;
		}

		for (File src : files) {
			if (packagedBclNames.contains(src.getName())) {
				continue;
			}
			File dest = new File(destDir, src.getName());
			if (!dest.exists() || dest.length() != src.length()) {
				Log.i(TAG, "Game assembly cache is incomplete: " + src.getName());
				return false;
			}
		}

		return true;
	}

	private boolean isGamePckReady() {
		File pck = new File(gameDir, PCK_FILE);
		if (!pck.exists() || !pck.isFile() || pck.length() < 96) {
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
		File pckFile = new File(gameDir, PCK_FILE);
		if (isGamePckReady() && consumeGameLaunchRequest()) {
			boolean safeLaunch = consumeSafeGameLaunchRequest();
			boolean useDefaultRenderer = safeLaunch || previousStartupPhaseWas("game startup completed");
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
			// Start in the launcher unless a one-shot game launch was requested; use bootstrap PCK so Godot can initialize for the
			// launcher.
			String bootstrapPck = extractBootstrapPck();
			if (bootstrapPck != null) {
				commands.add("--main-pack");
				commands.add(bootstrapPck);
				Log.i(TAG, "Using bootstrap PCK for launcher-only mode");
			}
		}
		return commands;
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

	private boolean consumeSafeGameLaunchRequest() {
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
				if (isPckPath(path, "project.binary")) {
					patched |= patchPckBinaryProjectEntry(raf, absOffset, size);
				} else if (isPckPath(path, "project.godot")) {
					patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
						"SentryInit=\"*res://addons/sentry/SentryInit.gd\"",
						"FmodManager=\"*res://addons/fmod/FmodManager.gd\""
					});
				} else if (isPckPath(path, ".godot/extension_list.cfg")) {
					if (isX86Runtime()) {
						patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
							"res://addons/fmod/fmod.gdextension",
							"res://addons/sentry/sentry.gdextension",
							"res://addons/spine/spine_godot_extension.gdextension"
						});
					} else {
						patched |= patchPckTextEntry(raf, absOffset, size, new String[] {
							"res://addons/fmod/fmod.gdextension",
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
			if (!marker.exists()) {
				marker.createNewFile();
			}
			marker.setLastModified(System.currentTimeMillis());
		} catch (Exception e) {
			Log.w(TAG, "Failed to write PCK Android patch marker", e);
		}
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
		getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
			.edit()
			.putBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, true)
			.apply();
		restartApp();
	}

	public void launchGameSafelyOnRestart() {
		Log.i(TAG, "Scheduling one-shot safe game launch on restart");
		getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
			.edit()
			.putBoolean(KEY_LAUNCH_GAME_ON_NEXT_START, true)
			.putBoolean(KEY_SAFE_LAUNCH_ON_NEXT_START, true)
			.apply();
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
			intent.putExtra(Intent.EXTRA_SUBJECT, "StS2 Launcher diagnostics");
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

	public void deleteKeystoreKey() {
		try {
			KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
			keyStore.load(null);
			keyStore.deleteEntry(KEYSTORE_ALIAS);
		} catch (Exception e) {
			Log.e(TAG, "Failed to delete keystore key", e);
		}
	}

	public String httpRequest(String method, String urlString, String headersJson, String bodyBase64, int timeoutMs) {
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
