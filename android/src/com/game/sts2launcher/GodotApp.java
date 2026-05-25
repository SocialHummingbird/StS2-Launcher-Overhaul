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
import java.io.ByteArrayOutputStream;
import java.security.KeyStore;
import java.security.KeyFactory;
import java.security.MessageDigest;
import java.security.PublicKey;
import java.security.SecureRandom;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

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
	private static final int ASSEMBLY_CACHE_SCHEMA = 4;
	private static final long STREAM_HTTP_RESPONSE_THRESHOLD_BYTES = 256L * 1024L;
	private static final int MAX_BUFFERED_HTTP_RESPONSE_BYTES = 1024 * 1024;
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
		gameDir = new File(getFilesDir(), "game").getAbsolutePath();
		configureTempDirectory();
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
				Log.e(TAG, "Assembly setup failed after recovery. Continuing with existing cache.", ex2);
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

	public String getExternalFilesDirPath() {
		File dir = getExternalFilesDir(null);
		return dir != null ? dir.getAbsolutePath() : null;
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
