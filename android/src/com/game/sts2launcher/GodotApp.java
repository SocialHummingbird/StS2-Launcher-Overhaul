package com.game.sts2launcher;

import org.godotengine.godot.GodotActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.activity.EdgeToEdge;
import androidx.core.splashscreen.SplashScreen;

import android.content.SharedPreferences;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.security.KeyStore;
import java.util.ArrayList;
import java.util.List;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

import android.content.Context;
import android.net.wifi.WifiManager;
import android.util.Base64;

// Main activity for the mobile launcher. Handles .NET assembly setup, PCK loading,
// LAN multicast, and Android Keystore encryption for credentials.
public class GodotApp extends GodotActivity {
	static {
		// Required for TLS/SSL (SteamKit2 WebSocket, HTTPS).
		System.loadLibrary("System.Security.Cryptography.Native.Android");
	}

	private static GodotApp instance;
	private WifiManager.MulticastLock multicastLock;
	private String gameDir;
	private static final String TAG = "STS2Mobile";
	private static final String KEYSTORE_ALIAS = "sts2mobile_credentials";
	private static final String PCK_FILE = "SlayTheSpire2.pck";
	private static final String PREFS_NAME = "sts2mobile";
	private static final String KEY_INSTALLED_VERSION_CODE = "installed_version_code";
	private static final String KEY_INSTALLED_PACKAGE_NAME = "installed_package_name";
	private static final String KEY_ASSEMBLY_CACHE_SCHEMA = "assembly_cache_schema";
	private static final int ASSEMBLY_CACHE_SCHEMA = 4;
	private static final String[] BOOTSTRAP_REQUIRED_ASSEMBLIES = {
		"STS2Mobile.dll",
		"SteamKit2.dll",
		"0Harmony.dll",
		"protobuf-net.dll",
		"protobuf-net.Core.dll",
		"System.IO.Hashing.dll",
		"ZstdSharp.dll",
		"GodotSharp.dll"
	};
	private static final String GAME_REQUIRED_ASSEMBLY = "sts2.dll";

	@Override
	public void onCreate(Bundle savedInstanceState) {
		instance = this;
		gameDir = new File(getFilesDir(), "game").getAbsolutePath();

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
				Log.e(TAG, "Assembly setup failed after recovery. Continuing with existing cache.", ex2);
			}
		}
		extractAssetFile("FMOD_LOGOS/FMOD Logo White - Transparent Background.png", "fmod_logo.png");

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
			File destDir = new File(getFilesDir(), ".godot/mono/publish/arm64");
			return !hasRequiredCacheFiles(destDir);
		}

		return true;
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
		File destDir = new File(getFilesDir(), ".godot/mono/publish/arm64");
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

	// Copies .NET BCL from APK assets and game assemblies from the download
	// directory
	// into the location Godot expects. Skips if already done unless the APK version
	// changed.
	private void setupAssemblies() {
		File srcDir = findAssembliesDir();
		File destDir = new File(getFilesDir(), ".godot/mono/publish/arm64");
		int currentVersion = BuildConfig.VERSION_CODE;
		boolean requiresGameAssemblies = hasGameAssemblies(srcDir);

		boolean refreshCache = shouldRefreshAssemblyCache();

		File patcherMarker = new File(destDir, "STS2Mobile.dll");
		File sts2Marker = new File(destDir, "sts2.dll");
		if (sts2Marker.exists() && patcherMarker.exists() && !refreshCache) {
			Log.i(TAG, "Assemblies already set up at: " + destDir.getAbsolutePath());
			markAssemblyCacheStateAsCurrent(currentVersion);
			return;
		}

		if (refreshCache) {
			Log.i(TAG, "New version detected, re-copying all assemblies");
			clearAssemblyCache(destDir);
		}

		destDir.mkdirs();

		try {
			String[] bclFiles = getAssets().list("dotnet_bcl");
			if (bclFiles != null) {
				int count = 0;
				for (String name : bclFiles) {
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
						if (name.endsWith(".so")) {
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
			String mode = requiresGameAssemblies ? "game" : "launcher-only";
			throw new RuntimeException("Missing required Mono/cache assemblies after copy for " + mode + " mode.");
		}

		markAssemblyCacheStateAsCurrent(currentVersion);
	}

	private File findAssembliesDir() {
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
		if (pckFile.exists()) {
			commands.add("--main-pack");
			commands.add(pckFile.getAbsolutePath());
			Log.i(TAG, "Loading PCK from: " + pckFile.getAbsolutePath());
		} else {
			// No game files yet; use bootstrap PCK so Godot can initialize for the
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

	private String extractBootstrapPck() {
		File dest = new File(getFilesDir(), "bootstrap.pck");
		if (dest.exists()) {
			return dest.getAbsolutePath();
		}
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
}
