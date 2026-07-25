package com.game.sts2launcher;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.RandomAccessFile;
import java.io.StringWriter;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

final class AndroidAssemblyBootstrapper {
	private static final String TAG = "STS2Mobile";
	private static final String PREFS_NAME = "sts2mobile";
	private static final String KEY_INSTALLED_VERSION_CODE = "installed_version_code";
	private static final String KEY_INSTALLED_PACKAGE_NAME = "installed_package_name";
	private static final String KEY_ASSEMBLY_CACHE_SCHEMA = "assembly_cache_schema";
	private static final String KEY_ASSEMBLY_CACHE_BRANCH = "assembly_cache_branch";
	private static final String KEY_ASSEMBLY_CACHE_RUNTIME_ID = "assembly_cache_runtime_id";
	private static final int ASSEMBLY_CACHE_SCHEMA = 25;
	private static final String PCK_FILE = "SlayTheSpire2.pck";
	private static final String PCK_ANDROID_PATCH_MARKER = ".android_pck_patch_v35";
	private static final String BRANCH_MARKER_FILE = "steam_branch.txt";
	private static final String RUNTIME_PACKS_DIRECTORY = "runtime_packs";
	private static final String RUNTIME_PACK_ANDROID_ASSEMBLY = "sts2.dll";
	private static final String RUNTIME_PACK_COMPATIBILITY_MANIFEST = "compatibility.json";
	private static final String RUNTIME_PACK_PATCH_VALIDATION_REPORT = "patch_validation.json";
	private static final String CURRENT_RUNTIME_SLOT_MARKER = "current_runtime_slot.json";
	private static final String CURRENT_RUNTIME_CACHE_MARKER = "current_runtime_cache.txt";
	private static final String CACHE_STAGING_SUFFIX = ".staging";
	private static final String CACHE_BACKUP_SUFFIX = ".backup";
	private static final long STAGING_STORAGE_RESERVE_BYTES = 64L * 1024L;
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

	interface Timeline {
		void record(String phase, String detail);
	}

	interface Environment {
		File filesDirectory();
		File gameDirectory();
		String selectedBranch();
		String packageName();
		String versionName();
		int versionCode();
		String runtimeGodotArchDirectory();
		String nativeLibraryDirectory();
		String[] listAssets(String path) throws IOException;
		long assetLengthBytes(String path) throws IOException;
		InputStream openAsset(String path) throws IOException;
		CacheState readCacheState();
		void writeCacheState(CacheState state);
		boolean renameDirectory(File source, File destination);
		void writeInternalTextFile(String name, String text) throws IOException;
		long currentTimeMillis();
		long usableSpaceBytes();
		void info(String message);
		void warn(String message);
		void warn(String message, Throwable error);
		void error(String message, Throwable error);
		String stackTrace(Throwable error);
	}

	static final class CacheState {
		final int schema;
		final int versionCode;
		final String packageName;
		final String branch;
		final String runtimeId;

		CacheState(
			int schema,
			int versionCode,
			String packageName,
			String branch,
			String runtimeId
		) {
			this.schema = schema;
			this.versionCode = versionCode;
			this.packageName = packageName == null ? "" : packageName;
			this.branch = branch == null ? "" : branch;
			this.runtimeId = runtimeId == null ? "" : runtimeId;
		}

		static CacheState empty() {
			return new CacheState(-1, -1, "", "", "");
		}
	}

	private static final class PreparationFailure extends RuntimeException {
		private final String operation;
		private final String path;

		PreparationFailure(
			String operation,
			String path,
			String message,
			Throwable cause
		) {
			super(message, cause);
			this.operation = operation == null ? "<unknown>" : operation;
			this.path = path == null ? "<unknown>" : path;
		}
	}

	static final class Result {
		private final boolean success;
		private final String title;
		private final String message;
		private final String diagnostics;

		private Result(
			boolean success,
			String title,
			String message,
			String diagnostics
		) {
			this.success = success;
			this.title = title;
			this.message = message;
			this.diagnostics = diagnostics;
		}

		static Result success() {
			return new Result(true, "", "", "");
		}

		static Result failure(String diagnostics) {
			return new Result(
				false,
				"StS2 Launcher diagnostics",
				"The launcher could not prepare the Android .NET assemblies "
					+ "required by native Godot.\n\nNative Godot was not started, "
					+ "because continuing would only trigger the generic "
					+ "'.NET assemblies not found' failure.",
				diagnostics
			);
		}

		boolean isSuccess() {
			return success;
		}

		String title() {
			return title;
		}

		String message() {
			return message;
		}

		String diagnostics() {
			return diagnostics;
		}
	}

	private final Environment environment;
	private final boolean pendingGameLaunch;
	private final Timeline timeline;
	private File cachedRuntimePackDirectory;
	private String cachedRuntimePackValidationKey = "";

	AndroidAssemblyBootstrapper(
		Context context,
		File gameDirectory,
		String selectedBranch,
		boolean pendingGameLaunch,
		Timeline timeline
	) {
		this(
			new ContextEnvironment(context, gameDirectory, selectedBranch),
			pendingGameLaunch,
			timeline
		);
	}

	AndroidAssemblyBootstrapper(
		Environment environment,
		boolean pendingGameLaunch,
		Timeline timeline
	) {
		this.environment = environment;
		this.pendingGameLaunch = pendingGameLaunch;
		this.timeline = timeline == null ? (phase, detail) -> { } : timeline;
	}

	void logStartupFreshnessProbe() {
		try {
			CacheState state = environment.readCacheState();
			String arch = environment.runtimeGodotArchDirectory();
			File destination = publishDirectory();
			File patcher = new File(destination, "STS2Mobile.dll");
			environment.info(
				"Android startup freshness:"
					+ " package=" + environment.packageName()
					+ " versionName=" + environment.versionName()
					+ " versionCode=" + environment.versionCode()
					+ " schema=" + ASSEMBLY_CACHE_SCHEMA
					+ " storedSchema=" + state.schema
					+ " storedVersionCode=" + state.versionCode
					+ " storedPackage=" + state.packageName
					+ " storedRuntimeId=" + state.runtimeId
					+ " currentRuntimeId=" + (
						pendingGameLaunch
							? currentRuntimeCacheId()
							: bootstrapRuntimeCacheId()
					)
					+ " arch=" + arch
					+ " cacheDirExists=" + destination.isDirectory()
					+ " sts2MobileBytes=" + (patcher.exists() ? patcher.length() : -1)
			);
		} catch (Exception error) {
			environment.warn("Android startup freshness probe failed", error);
		}
	}

	Result prepare() {
		RuntimeException firstFailure;
		try {
			timeline.record(
				"native assembly setup",
				"Preparing Mono/.NET assembly cache"
			);
			prepareOnce();
			timeline.record(
				"native assembly setup complete",
				"Assembly cache prepared"
			);
			return Result.success();
		} catch (RuntimeException error) {
			firstFailure = error;
			timeline.record("native assembly setup failed", error.getMessage());
			environment.error(
				"Assembly setup failed, discarding staging and retrying once",
				error
			);
		}

		discardFailedStaging();
		try {
			timeline.record(
				"native assembly setup retry",
				"Failed staging discarded"
			);
			prepareOnce();
			timeline.record(
				"native assembly setup retry complete",
				"Assembly cache prepared after reset"
			);
			return Result.success();
		} catch (RuntimeException secondFailure) {
			timeline.record(
				"native assembly setup fatal",
				secondFailure.getMessage()
			);
			environment.error(
				"Assembly setup failed after recovery",
				secondFailure
			);
			discardFailedStaging();
			return Result.failure(
				buildFailureDiagnostics(firstFailure, secondFailure)
			);
		}
	}

	private String buildFailureDiagnostics(
		RuntimeException firstFailure,
		RuntimeException secondFailure
	) {
		File destination = publishDirectory();
		File source = findAssembliesDirectory();
		File runtimePackCandidate = runtimePackCandidateDirectory();
		File usableRuntimePack = findRuntimePackDirectory();
		CacheState cacheState = environment.readCacheState();
		PreparationFailure originalContext =
			findPreparationFailure(firstFailure);
		PreparationFailure retryContext =
			findPreparationFailure(secondFailure);
		Throwable originalCause = rootCause(firstFailure);
		Throwable retryCause = rootCause(secondFailure);
		StringBuilder diagnostics = new StringBuilder();
		diagnostics.append("Android assembly bootstrap failure\n");
		diagnostics.append("Package: ").append(environment.packageName()).append('\n');
		diagnostics.append("Version: ")
			.append(environment.versionName())
			.append(" (")
			.append(environment.versionCode())
			.append(")\n");
		diagnostics.append("Selected branch: ")
			.append(environment.selectedBranch())
			.append('\n');
		diagnostics.append("Pending game launch: ")
			.append(pendingGameLaunch)
			.append('\n');
		diagnostics.append("Runtime architecture: ")
			.append(environment.runtimeGodotArchDirectory())
			.append('\n');
		diagnostics.append("Files directory: ")
			.append(environment.filesDirectory().getAbsolutePath())
			.append('\n');
		diagnostics.append("Game directory: ")
			.append(environment.gameDirectory().getAbsolutePath())
			.append('\n');
		diagnostics.append("Source assemblies: ")
			.append(source == null ? "<none>" : source.getAbsolutePath())
			.append('\n');
		diagnostics.append("Available storage bytes: ")
			.append(environment.usableSpaceBytes())
			.append('\n');

		diagnostics.append("\nRuntime-pack state\n");
		diagnostics.append("Runtime pack required: ")
			.append(selectedBranchRequiresRuntimePack())
			.append('\n');
		diagnostics.append("Runtime pack candidate: ")
			.append(runtimePackCandidate.getAbsolutePath())
			.append('\n');
		diagnostics.append("Runtime pack directory exists: ")
			.append(runtimePackCandidate.isDirectory())
			.append('\n');
		appendFileDiagnostic(
			diagnostics,
			"Runtime pack compatibility manifest",
			new File(runtimePackCandidate, RUNTIME_PACK_COMPATIBILITY_MANIFEST)
		);
		appendFileDiagnostic(
			diagnostics,
			"Runtime pack validation report",
			new File(runtimePackCandidate, RUNTIME_PACK_PATCH_VALIDATION_REPORT)
		);
		appendFileDiagnostic(
			diagnostics,
			"Runtime pack Android assembly",
			new File(runtimePackCandidate, RUNTIME_PACK_ANDROID_ASSEMBLY)
		);
		diagnostics.append("Runtime pack usable: ")
			.append(usableRuntimePack != null)
			.append('\n');

		diagnostics.append("\nCache state\n");
		diagnostics.append("Cache metadata schema: ")
			.append(cacheState.schema)
			.append('\n');
		diagnostics.append("Cache metadata version: ")
			.append(cacheState.versionCode)
			.append('\n');
		diagnostics.append("Cache metadata package: ")
			.append(emptyAsNone(cacheState.packageName))
			.append('\n');
		diagnostics.append("Cache metadata branch: ")
			.append(emptyAsNone(cacheState.branch))
			.append('\n');
		diagnostics.append("Cache metadata runtime ID: ")
			.append(emptyAsNone(cacheState.runtimeId))
			.append('\n');
		appendCacheDirectoryDiagnostic(diagnostics, "Active cache", destination);
		appendCacheDirectoryDiagnostic(
			diagnostics,
			"Staging cache",
			stagingDirectory()
		);
		appendCacheDirectoryDiagnostic(
			diagnostics,
			"Backup cache",
			backupDirectory()
		);

		diagnostics.append("\nPreparation attempts\n");
		appendFailureDiagnostic(
			diagnostics,
			"Original",
			firstFailure,
			originalContext,
			originalCause
		);
		appendFailureDiagnostic(
			diagnostics,
			"Retry",
			secondFailure,
			retryContext,
			retryCause
		);
		return diagnostics.toString();
	}

	private void appendFailureDiagnostic(
		StringBuilder diagnostics,
		String label,
		RuntimeException failure,
		PreparationFailure context,
		Throwable cause
	) {
		diagnostics.append(label).append(" result: failed\n");
		diagnostics.append(label).append(" failed operation: ")
			.append(context == null ? "<unknown>" : context.operation)
			.append('\n');
		diagnostics.append(label).append(" failed path: ")
			.append(context == null ? "<unknown>" : context.path)
			.append('\n');
		diagnostics.append(label).append(" exception: ")
			.append(cause.getClass().getName())
			.append(": ")
			.append(emptyAsNone(cause.getMessage()))
			.append('\n');
		diagnostics.append(label).append(" stack trace:\n")
			.append(environment.stackTrace(failure))
			.append('\n');
	}

	private void appendFileDiagnostic(
		StringBuilder diagnostics,
		String label,
		File file
	) {
		diagnostics.append(label)
			.append(": ")
			.append(file.getAbsolutePath())
			.append(" exists=")
			.append(file.isFile())
			.append(" bytes=")
			.append(file.isFile() ? file.length() : 0)
			.append('\n');
	}

	private void appendCacheDirectoryDiagnostic(
		StringBuilder diagnostics,
		String label,
		File directory
	) {
		int dllCount = 0;
		int bootstrapCount = 0;
		int gameCount = 0;
		if (directory.isDirectory()) {
			File[] dlls = directory.listFiles(
				(parent, name) ->
					name.toLowerCase(java.util.Locale.ROOT).endsWith(".dll")
			);
			dllCount = dlls == null ? 0 : dlls.length;
			bootstrapCount = countPresentFiles(
				directory,
				BOOTSTRAP_REQUIRED_ASSEMBLIES
			);
			gameCount = countPresentFiles(directory, GAME_REQUIRED_ASSEMBLIES);
		}
		diagnostics.append(label)
			.append(": ")
			.append(directory.getAbsolutePath())
			.append(" exists=")
			.append(directory.exists())
			.append(" directory=")
			.append(directory.isDirectory())
			.append(" dllCount=")
			.append(dllCount)
			.append(" bootstrapRequired=")
			.append(bootstrapCount)
			.append('/')
			.append(BOOTSTRAP_REQUIRED_ASSEMBLIES.length)
			.append(" gameRequired=")
			.append(gameCount)
			.append('/')
			.append(GAME_REQUIRED_ASSEMBLIES.length)
			.append('\n');
	}

	private int countPresentFiles(File directory, String[] names) {
		int count = 0;
		for (String name : names) {
			File file = new File(directory, name);
			if (file.isFile() && file.length() > 0) {
				count++;
			}
		}
		return count;
	}

	private PreparationFailure findPreparationFailure(Throwable failure) {
		Throwable current = failure;
		while (current != null) {
			if (current instanceof PreparationFailure) {
				return (PreparationFailure) current;
			}
			current = current.getCause();
		}
		return null;
	}

	private Throwable rootCause(Throwable failure) {
		Throwable current = failure;
		while (current.getCause() != null && current.getCause() != current) {
			current = current.getCause();
		}
		return current;
	}

	private String emptyAsNone(String value) {
		return value == null || value.trim().isEmpty() ? "<none>" : value;
	}

	private PreparationFailure preparationFailure(
		String operation,
		String path,
		String message,
		Throwable cause
	) {
		return new PreparationFailure(operation, path, message, cause);
	}

	private void prepareOnce() {
		recoverInterruptedCacheReplacement();
		File sourceDirectory = pendingGameLaunch
			? findAssembliesDirectory()
			: null;
		File runtimePackDirectory = pendingGameLaunch
			? findRuntimePackDirectory()
			: null;
		File runtimePackGameAssembly =
			runtimePackGameAssembly(runtimePackDirectory);
		File destination = publishDirectory();
		int currentVersion = environment.versionCode();
		boolean gameReady = isGamePckReady();
		boolean hasRuntimePackGameAssembly =
			runtimePackGameAssembly != null
				&& runtimePackGameAssembly.exists()
				&& runtimePackGameAssembly.isFile();
		boolean selectedBranchRequiresRuntimePack =
			gameReady && selectedBranchRequiresRuntimePack();
		boolean runtimePackCandidatePresent =
			hasRuntimePackCandidateEvidence();
		boolean launchRequiresUsableRuntimePack =
			gameReady
				&& (
					selectedBranchRequiresRuntimePack
						|| runtimePackCandidatePresent
				);
		boolean requiresGameAssemblies =
			gameReady
				&& pendingGameLaunch
				&& (
					hasGameAssemblies(sourceDirectory)
						|| hasRuntimePackGameAssembly
				)
				&& (
					!launchRequiresUsableRuntimePack
						|| hasRuntimePackGameAssembly
				);
		Set<String> packagedBclNames = getPackagedBclNames();
		String runtimeId = pendingGameLaunch
			? currentRuntimeCacheId()
			: bootstrapRuntimeCacheId();

		boolean bootstrapHasGameCode =
			!requiresGameAssemblies
				&& hasStaleCachedBranchGameCodeAssemblies(destination);
		boolean refreshCache =
			shouldRefreshAssemblyCache(runtimeId)
				|| shouldRefreshAssemblyCacheForSelectedBranch()
				|| bootstrapHasGameCode;
		logAssemblyCacheState(
			"before-copy",
			destination,
			sourceDirectory,
			requiresGameAssemblies,
			packagedBclNames,
			runtimeId
		);
		environment.info(
			"Runtime pack directory: "
				+ (
					runtimePackDirectory == null
						? "<none>"
						: runtimePackDirectory.getAbsolutePath()
				)
		);
		environment.info(
			"Runtime pack game assembly: "
				+ (
					hasRuntimePackGameAssembly
						? runtimePackGameAssembly.getAbsolutePath()
						: "<none>"
				)
		);

		if (
			launchRequiresUsableRuntimePack
				&& !hasRuntimePackGameAssembly
				&& pendingGameLaunch
		) {
			logAssemblyCacheState(
				"blocked-no-runtime-pack",
				destination,
				sourceDirectory,
				requiresGameAssemblies,
				packagedBclNames,
				runtimeId
			);
			throw preparationFailure(
				"validate selected-branch runtime pack",
				runtimePackCandidateDirectory().getAbsolutePath(),
				"Selected Steam branch '" + environment.selectedBranch()
					+ "' has a required or installed Android runtime pack, but "
					+ "its manifest, validation report, or patched assembly "
					+ "identity is unusable. Native Godot startup was blocked "
					+ "to avoid substituting unpatched or stale game code.",
				null
			);
		}

		File cachedGameAssembly = new File(destination, "sts2.dll");
		if (!gameReady && cachedGameAssembly.exists()) {
			environment.warn(
				"Game PCK is not ready; clearing stale game assembly cache"
			);
			refreshCache = true;
		}

		if (
			!refreshCache
				&& hasRequiredCacheFiles(destination, requiresGameAssemblies)
				&& hasCurrentPackagedRequiredAssemblies(destination)
				&& (
					!requiresGameAssemblies
						|| hasCachedGameAssemblies(
							destination,
							sourceDirectory,
							packagedBclNames,
							runtimePackDirectory,
							runtimePackGameAssembly
						)
				)
		) {
			environment.info(
				"Assemblies already set up at: " + destination.getAbsolutePath()
			);
			logAssemblyCacheState(
				"cache-hit",
				destination,
				sourceDirectory,
				requiresGameAssemblies,
				packagedBclNames,
				runtimeId
			);
			markAssemblyCacheStateAsCurrent(
				currentVersion,
				runtimeId,
				pendingGameLaunch
			);
			discardObsoleteBackup();
			return;
		}

		if (refreshCache) {
			environment.info(
				"Assembly cache refresh required, re-copying all assemblies"
			);
		}

		ensureStagingStorageAvailable(
			packagedBclNames,
			sourceDirectory,
			runtimePackDirectory,
			hasRuntimePackGameAssembly,
			selectedBranchRequiresRuntimePack
		);
		File staging = prepareStagingDirectory();
		if (!staging.mkdirs() && !staging.isDirectory()) {
			throw preparationFailure(
				"create assembly cache staging directory",
				staging.getAbsolutePath(),
				"Failed to create staged Mono/cache assembly directory: "
					+ staging.getAbsolutePath(),
				new IOException("File.mkdirs returned false")
			);
		}

		copyPackagedAssemblies(staging, packagedBclNames);
		copyGameAssemblies(
			sourceDirectory,
			staging,
			packagedBclNames,
			runtimePackDirectory,
			hasRuntimePackGameAssembly,
			selectedBranchRequiresRuntimePack
		);
		copyRuntimePackAssemblies(
			runtimePackDirectory,
			runtimePackGameAssembly,
			staging,
			packagedBclNames
		);

		if (!hasRequiredCacheFiles(staging, requiresGameAssemblies)) {
			logAssemblyCacheState(
				"missing-after-copy",
				staging,
				sourceDirectory,
				requiresGameAssemblies,
				packagedBclNames,
				runtimeId
			);
			String mode = requiresGameAssemblies ? "game" : "launcher-only";
			throw preparationFailure(
				"validate required staged assemblies",
				staging.getAbsolutePath(),
				"Missing required Mono/cache assemblies after copy for "
					+ mode + " mode.",
				null
			);
		}

		if (!hasCurrentPackagedRequiredAssemblies(staging)) {
			logAssemblyCacheState(
				"stale-packaged-after-copy",
				staging,
				sourceDirectory,
				requiresGameAssemblies,
				packagedBclNames,
				runtimeId
			);
			throw preparationFailure(
				"validate packaged staged assemblies",
				staging.getAbsolutePath(),
				"Packaged launcher assemblies are stale after cache copy; "
					+ "refusing to mark assembly cache current.",
				null
			);
		}

		if (
			requiresGameAssemblies
				&& !hasCachedGameAssemblies(
					staging,
					sourceDirectory,
					packagedBclNames,
					runtimePackDirectory,
					runtimePackGameAssembly
				)
		) {
			logAssemblyCacheState(
				"stale-game-after-copy",
				staging,
				sourceDirectory,
				true,
				packagedBclNames,
				runtimeId
			);
			throw preparationFailure(
				"validate staged game assemblies",
				staging.getAbsolutePath(),
				"Game assemblies are stale or incomplete after staged cache copy; "
					+ "refusing to replace the active assembly cache.",
				null
			);
		}

		logAssemblyCacheState(
			"staging-validated",
			staging,
			sourceDirectory,
			requiresGameAssemblies,
			packagedBclNames,
			runtimeId
		);
		replaceActiveCache(staging, destination);
		markAssemblyCacheStateAsCurrent(
			currentVersion,
			runtimeId,
			pendingGameLaunch
		);
	}

	private void ensureStagingStorageAvailable(
		Set<String> packagedBclNames,
		File sourceDirectory,
		File runtimePackDirectory,
		boolean hasRuntimePackGameAssembly,
		boolean selectedBranchRequiresRuntimePack
	) {
		long requiredBytes = STAGING_STORAGE_RESERVE_BYTES;
		for (String name : packagedBclNames) {
			long length = packagedAssetLength("dotnet_bcl/" + name);
			if (length > 0) {
				requiredBytes = saturatingAdd(requiredBytes, length);
			}
		}

		if (sourceDirectory != null && sourceDirectory.isDirectory()) {
			File[] files = sourceDirectory.listFiles();
			if (files != null) {
				for (File source : files) {
					if (!source.isFile()) {
						continue;
					}
					String name = source.getName();
					if (
						hasRuntimePackGameAssembly
							&& isBranchGameCodeAssembly(name)
					) {
						continue;
					}
					if (
						selectedBranchRequiresRuntimePack
							&& !hasRuntimePackGameAssembly
							&& isBranchGameCodeAssembly(name)
					) {
						continue;
					}
					if (shouldCopyGameAssemblyFile(name, packagedBclNames)) {
						requiredBytes =
							saturatingAdd(requiredBytes, source.length());
					}
				}
			}
		}

		if (
			hasRuntimePackGameAssembly
				&& runtimePackDirectory != null
				&& runtimePackDirectory.isDirectory()
		) {
			Set<String> declaredAssemblies =
				runtimePackDeclaredAssemblyNames(runtimePackDirectory);
			File[] files = runtimePackDirectory.listFiles();
			if (files != null) {
				for (File source : files) {
					if (
						source.isFile()
							&& declaredAssemblies.contains(
								source.getName().toLowerCase(
									java.util.Locale.ROOT
								)
							)
							&& shouldCopyGameAssemblyFile(
								source.getName(),
								packagedBclNames
							)
					) {
						requiredBytes =
							saturatingAdd(requiredBytes, source.length());
					}
				}
			}
		}

		long availableBytes = environment.usableSpaceBytes();
		environment.info(
			"Assembly cache staging storage: required=" + requiredBytes
				+ " available=" + availableBytes
		);
		if (availableBytes < requiredBytes) {
			throw preparationFailure(
				"check assembly cache storage",
				stagingDirectory().getAbsolutePath(),
				"Insufficient storage for staged assembly cache: required "
					+ requiredBytes + " bytes, available " + availableBytes
					+ " bytes.",
				new IOException("Insufficient storage for assembly cache staging")
			);
		}
	}

	private long saturatingAdd(long left, long right) {
		if (right <= 0) {
			return left;
		}
		if (left > Long.MAX_VALUE - right) {
			return Long.MAX_VALUE;
		}
		return left + right;
	}

	private void copyPackagedAssemblies(
		File destination,
		Set<String> packagedBclNames
	) {
		if (packagedBclNames.isEmpty()) {
			return;
		}

		int count = 0;
		for (String name : packagedBclNames) {
			String assetPath = "dotnet_bcl/" + name;
			File target = new File(destination, name);
			try (
				InputStream input = environment.openAsset(assetPath);
				OutputStream output = new FileOutputStream(target)
			) {
				copyStream(input, output);
				count++;
			} catch (Exception error) {
				environment.error(
					"Failed to copy packaged assembly: " + assetPath,
					error
				);
				throw preparationFailure(
					"copy packaged assembly",
					assetPath + " -> " + target.getAbsolutePath(),
					"Failed to copy packaged assembly " + name,
					error
				);
			}
		}
		environment.info(
			"Copied " + count + " BCL assemblies from assets"
		);
	}

	private void copyGameAssemblies(
		File sourceDirectory,
		File destination,
		Set<String> packagedBclNames,
		File runtimePackDirectory,
		boolean hasRuntimePackGameAssembly,
		boolean selectedBranchRequiresRuntimePack
	) {
		if (
			sourceDirectory == null
				|| !sourceDirectory.exists()
				|| !sourceDirectory.isDirectory()
		) {
			environment.warn(
				"Game assemblies source directory not found: "
					+ (
						sourceDirectory == null
							? "<none>"
							: sourceDirectory.getAbsolutePath()
					)
			);
			return;
		}

		File[] files = sourceDirectory.listFiles();
		if (files == null) {
			return;
		}

		int count = 0;
		Set<String> runtimePackOverrides =
			hasRuntimePackGameAssembly
				? runtimePackDeclaredAssemblyNames(runtimePackDirectory)
				: java.util.Collections.emptySet();
		for (File source : files) {
			if (!source.isFile()) {
				continue;
			}
			String name = source.getName();
			if (
				hasRuntimePackGameAssembly
					&& runtimePackOverrides.contains(
						name.toLowerCase(java.util.Locale.ROOT)
					)
			) {
				continue;
			}
			if (
				selectedBranchRequiresRuntimePack
					&& !hasRuntimePackGameAssembly
					&& isBranchGameCodeAssembly(name)
			) {
				environment.warn(
					"Skipping selected-game branch code assembly without usable "
						+ "runtime pack: " + name
				);
				continue;
			}
			if (!shouldCopyGameAssemblyFile(name, packagedBclNames)) {
				continue;
			}
			try {
				File target = new File(destination, name);
				copyFile(source, target);
				count++;
			} catch (Exception error) {
				environment.error("Failed to copy game assembly: " + name, error);
				throw preparationFailure(
					"copy game assembly",
					source.getAbsolutePath()
						+ " -> " + new File(destination, name).getAbsolutePath(),
					"Failed to copy game assembly " + name,
					error
				);
			}
		}
		environment.info("Copied " + count + " game assembly files");
	}

	private void copyRuntimePackAssemblies(
		File runtimePackDirectory,
		File runtimePackGameAssembly,
		File destination,
		Set<String> packagedBclNames
	) {
		if (runtimePackGameAssembly == null) {
			return;
		}

		Set<String> declaredAssemblies =
			runtimePackDeclaredAssemblyNames(runtimePackDirectory);
		File[] files = runtimePackDirectory.listFiles();
		if (files == null) {
			return;
		}

		int count = 0;
		for (File source : files) {
			if (!source.isFile()) {
				continue;
			}
			if (
				!declaredAssemblies.contains(
					source.getName().toLowerCase(java.util.Locale.ROOT)
				)
			) {
				continue;
			}
			if (
				!shouldCopyGameAssemblyFile(
					source.getName(),
					packagedBclNames
				)
			) {
				continue;
			}
			try {
				File target = new File(destination, source.getName());
				copyFile(source, target);
				count++;
			} catch (Exception error) {
				environment.error(
					"Failed to copy runtime-pack assembly: " + source.getName(),
					error
				);
				throw preparationFailure(
					"copy runtime-pack assembly",
					source.getAbsolutePath()
						+ " -> "
						+ new File(destination, source.getName()).getAbsolutePath(),
					"Failed to copy runtime-pack assembly " + source.getName(),
					error
				);
			}
		}
		environment.info("Copied " + count + " runtime-pack assembly files");
	}

	private boolean shouldRefreshAssemblyCache(String runtimeId) {
		CacheState state = environment.readCacheState();
		if (
			state.schema == ASSEMBLY_CACHE_SCHEMA
				&& state.versionCode == environment.versionCode()
				&& environment.packageName().equals(state.packageName)
		) {
			if (!runtimeId.equals(state.runtimeId)) {
				environment.info(
					"Assembly cache runtime changed from "
						+ state.runtimeId + " to " + runtimeId
				);
				return true;
			}
			return !hasRequiredCacheFiles(publishDirectory(), false);
		}
		return true;
	}

	private boolean shouldRefreshAssemblyCacheForSelectedBranch() {
		CacheState state = environment.readCacheState();
		String selectedBranch = environment.selectedBranch();
		if (state.branch.trim().isEmpty()) {
			if (isGamePckReady()) {
				environment.info(
					"Assembly cache has no selected-branch marker; refreshing "
						+ "for branch: " + selectedBranch
				);
				return true;
			}
			return false;
		}
		if (!state.branch.trim().equalsIgnoreCase(selectedBranch)) {
			environment.info(
				"Assembly cache branch changed from " + state.branch
					+ " to " + selectedBranch + "; refreshing game assemblies"
			);
			return true;
		}
		return false;
	}

	private String bootstrapRuntimeCacheId() {
		return "bootstrap"
			+ "|package=" + environment.packageName()
			+ "|version=" + environment.versionCode()
			+ "|schema=" + ASSEMBLY_CACHE_SCHEMA
			+ "|arch=" + environment.runtimeGodotArchDirectory();
	}

	private String currentRuntimeCacheId() {
		File pck = selectedPck();
		File sourceDirectory = findAssembliesDirectory();
		File runtimePackDirectory = findRuntimePackDirectory();
		File runtimePackAssembly =
			runtimePackGameAssembly(runtimePackDirectory);
		boolean requiresRuntimePack = selectedBranchRequiresRuntimePack();
		File sourceGameAssembly =
			runtimePackAssembly != null && runtimePackAssembly.exists()
				? runtimePackAssembly
				: requiresRuntimePack
					? null
					: sourceDirectory == null
						? null
						: new File(sourceDirectory, RUNTIME_PACK_ANDROID_ASSEMBLY);
		String sourceHash =
			sourceGameAssembly != null && sourceGameAssembly.exists()
				? sha256Hex(sourceGameAssembly)
				: "<no-source-sts2>";
		String runtimeSource =
			runtimePackAssembly != null && runtimePackAssembly.exists()
				? "runtime-pack"
				: requiresRuntimePack
					? "no-usable-runtime"
					: "selected-game";
		return environment.selectedBranch()
			+ "|pck=" + fileIdentity(pck)
			+ "|runtimeSource=" + runtimeSource
			+ "|runtimePack=" + runtimePackIdentity(runtimePackDirectory)
			+ "|sts2=" + sourceHash;
	}

	private File publishDirectory() {
		return new File(
			environment.filesDirectory(),
			".godot/mono/publish/"
				+ environment.runtimeGodotArchDirectory()
		);
	}

	private File stagingDirectory() {
		File active = publishDirectory();
		return new File(active.getParentFile(), active.getName() + CACHE_STAGING_SUFFIX);
	}

	private File backupDirectory() {
		File active = publishDirectory();
		return new File(active.getParentFile(), active.getName() + CACHE_BACKUP_SUFFIX);
	}

	private File runtimePackCandidateDirectory() {
		return new File(
			new File(environment.filesDirectory(), RUNTIME_PACKS_DIRECTORY),
			SteamBranchInfo.stateDirectoryName(environment.selectedBranch())
		);
	}

	private boolean hasRuntimePackCandidateEvidence() {
		File directory = runtimePackCandidateDirectory();
		if (!directory.isDirectory()) {
			return false;
		}
		return new File(
			directory,
			RUNTIME_PACK_COMPATIBILITY_MANIFEST
		).exists()
			|| new File(
				directory,
				RUNTIME_PACK_PATCH_VALIDATION_REPORT
			).exists()
			|| new File(
				directory,
				RUNTIME_PACK_ANDROID_ASSEMBLY
			).exists();
	}

	private File selectedPck() {
		return new File(environment.gameDirectory(), PCK_FILE);
	}

	private boolean hasRequiredCacheFiles(
		File destination,
		boolean requireGameAssemblies
	) {
		if (
			destination == null
				|| !destination.exists()
				|| !destination.isDirectory()
		) {
			return false;
		}

		ArrayList<String> required = new ArrayList<>(
			java.util.Arrays.asList(BOOTSTRAP_REQUIRED_ASSEMBLIES)
		);
		if (requireGameAssemblies) {
			required.addAll(
				java.util.Arrays.asList(GAME_REQUIRED_ASSEMBLIES)
			);
		}
		for (String name : required) {
			File file = new File(destination, name);
			if (!file.isFile() || file.length() <= 0) {
				environment.warn(
					"Missing or empty required cache file: "
						+ file.getAbsolutePath()
				);
				return false;
			}
		}
		return true;
	}

	private void logAssemblyCacheState(
		String phase,
		File destination,
		File sourceDirectory,
		boolean requireGameAssemblies,
		Set<String> packagedBclNames,
		String runtimeId
	) {
		environment.info(
			"Assembly cache diagnostics [" + phase + "]"
				+ " schema=" + ASSEMBLY_CACHE_SCHEMA
				+ " arch=" + environment.runtimeGodotArchDirectory()
				+ " nativeLibraryDir=" + environment.nativeLibraryDirectory()
				+ " filesDir="
				+ environment.filesDirectory().getAbsolutePath()
				+ " dest="
				+ (
					destination == null
						? "<none>"
						: destination.getAbsolutePath()
				)
				+ " destExists="
				+ (destination != null && destination.exists())
				+ " src="
				+ (
					sourceDirectory == null
						? "<none>"
						: sourceDirectory.getAbsolutePath()
				)
				+ " srcExists="
				+ (sourceDirectory != null && sourceDirectory.exists())
				+ " runtimeId=" + runtimeId
				+ " packagedBclCount="
				+ (packagedBclNames == null ? 0 : packagedBclNames.size())
				+ " requireGameAssemblies=" + requireGameAssemblies
		);

		ArrayList<String> required = new ArrayList<>(
			java.util.Arrays.asList(BOOTSTRAP_REQUIRED_ASSEMBLIES)
		);
		if (requireGameAssemblies) {
			required.addAll(
				java.util.Arrays.asList(GAME_REQUIRED_ASSEMBLIES)
			);
		}
		File runtimePackDirectory = pendingGameLaunch
			? findRuntimePackDirectory()
			: null;
		Map<String, RuntimePackAssemblyExpectation>
			runtimePackExpectations =
				runtimePackAssemblyExpectations(runtimePackDirectory);
		for (String name : required) {
			File cached = destination == null
				? null
				: new File(destination, name);
			File source = sourceDirectory == null
				? null
				: new File(sourceDirectory, name);
			RuntimePackAssemblyExpectation runtimePackExpectation =
				runtimePackExpectations.get(
					name.toLowerCase(java.util.Locale.ROOT)
				);
			boolean branchCodeBlocked =
				selectedBranchRequiresRuntimePack()
					&& runtimePackExpectation == null
					&& isBranchGameCodeAssembly(name);
			boolean branchCode =
				isBranchGameCodeAssembly(name)
					&& source != null
					&& source.exists()
					&& !branchCodeBlocked
					&& runtimePackExpectation == null;
			boolean packaged =
				packagedBclNames != null
					&& packagedBclNames.contains(name)
					&& !branchCode;
			long expectedBytes = runtimePackExpectation != null
				? runtimePackExpectation.source.length()
				: branchCodeBlocked
				? 0
				: branchCode
					? source.length()
					: packaged
						? packagedAssetLength("dotnet_bcl/" + name)
						: source != null && source.exists()
							? source.length()
							: packagedAssetLength("dotnet_bcl/" + name);
			String expectedSource = runtimePackExpectation != null
				? "runtime-pack"
				: branchCodeBlocked
				? "no-usable-runtime"
				: branchCode
					? "selected-game"
					: packaged
						? "packaged-bcl"
						: source != null && source.exists()
							? "game"
							: "packaged-bcl-fallback";
			environment.info(
				"Assembly cache required file [" + phase + "]: " + name
					+ " exists=" + (cached != null && cached.exists())
					+ " bytes="
					+ (
						cached != null && cached.exists()
							? cached.length()
							: 0
					)
					+ " expectedSource=" + expectedSource
					+ " expectedBytes=" + expectedBytes
					+ (
						runtimePackExpectation == null
							? ""
							: " expectedSha256="
								+ runtimePackExpectation.sha256
					)
			);
		}
	}

	private long packagedAssetLength(String assetPath) {
		try {
			return environment.assetLengthBytes(assetPath);
		} catch (IOException error) {
			return -1;
		}
	}

	private void markAssemblyCacheStateAsCurrent(
		int currentVersion,
		String runtimeId,
		boolean writeRuntimeMarker
	) {
		environment.writeCacheState(
			new CacheState(
				ASSEMBLY_CACHE_SCHEMA,
				currentVersion,
				environment.packageName(),
				environment.selectedBranch(),
				runtimeId
			)
		);
		if (writeRuntimeMarker) {
			writeRuntimeCacheMarker(currentVersion, runtimeId);
		}
	}

	private void writeRuntimeCacheMarker(
		int currentVersion,
		String runtimeId
	) {
		try {
			File pck = selectedPck();
			File sourceDirectory = findAssembliesDirectory();
			File runtimePackDirectory = findRuntimePackDirectory();
			File runtimePackAssembly =
				runtimePackGameAssembly(runtimePackDirectory);
			File selectedSourceAssembly = sourceDirectory == null
				? null
				: new File(sourceDirectory, RUNTIME_PACK_ANDROID_ASSEMBLY);
			boolean requiresRuntimePack = selectedBranchRequiresRuntimePack();
			File activeSourceAssembly =
				runtimePackAssembly != null && runtimePackAssembly.exists()
					? runtimePackAssembly
					: requiresRuntimePack
						? null
						: selectedSourceAssembly;
			String runtimeSource =
				runtimePackAssembly != null && runtimePackAssembly.exists()
					? "runtime-pack"
					: requiresRuntimePack
						? "no-usable-runtime"
						: "selected-game";
			File publishDirectory = publishDirectory();
			String text =
				"UTC millis: " + environment.currentTimeMillis() + "\n"
				+ "Package: " + environment.packageName() + "\n"
				+ "Version name: " + environment.versionName() + "\n"
				+ "Version code: " + currentVersion + "\n"
				+ "Assembly cache schema: " + ASSEMBLY_CACHE_SCHEMA + "\n"
				+ "Selected branch: " + environment.selectedBranch() + "\n"
				+ "Runtime ID: " + runtimeId + "\n"
				+ "Runtime source: " + runtimeSource + "\n"
				+ "Runtime pack directory: "
				+ (
					runtimePackDirectory == null
						? "<none>"
						: runtimePackDirectory.getAbsolutePath()
				) + "\n"
				+ "Runtime pack game assembly: "
				+ (
					runtimePackAssembly == null
						? "<none>"
						: runtimePackAssembly.getAbsolutePath()
				) + "\n"
				+ "Selected branch requires runtime pack: "
				+ requiresRuntimePack + "\n"
				+ "Game directory: "
				+ environment.gameDirectory().getAbsolutePath() + "\n"
				+ "Selected PCK path: " + pck.getAbsolutePath() + "\n"
				+ "Selected PCK identity: " + fileIdentity(pck) + "\n"
				+ "Selected PCK SHA256: "
				+ (pck.isFile() ? sha256Hex(pck) : "<missing>") + "\n"
				+ "Selected source sts2.dll: "
				+ (
					selectedSourceAssembly == null
						? "<none>"
						: selectedSourceAssembly.getAbsolutePath()
				) + "\n"
				+ "Selected source sts2.dll SHA256: "
				+ (
					selectedSourceAssembly != null
						&& selectedSourceAssembly.isFile()
							? sha256Hex(selectedSourceAssembly)
							: "<missing>"
				) + "\n"
				+ "Active source sts2.dll: "
				+ (
					activeSourceAssembly == null
						? "<none>"
						: activeSourceAssembly.getAbsolutePath()
				) + "\n"
				+ "Active source sts2.dll SHA256: "
				+ (
					activeSourceAssembly != null
						&& activeSourceAssembly.isFile()
							? sha256Hex(activeSourceAssembly)
							: "<missing>"
				) + "\n"
				+ "Publish cache directory: "
				+ publishDirectory.getAbsolutePath() + "\n"
				+ "Publish cache active sts2.dll SHA256: "
				+ sha256Hex(
					new File(
						publishDirectory,
						RUNTIME_PACK_ANDROID_ASSEMBLY
					)
				) + "\n";
			environment.writeInternalTextFile(
				CURRENT_RUNTIME_CACHE_MARKER,
				text
			);
		} catch (Exception error) {
			environment.warn("Failed to write runtime cache marker", error);
		}
	}

	private void recoverInterruptedCacheReplacement() {
		File active = publishDirectory();
		File backup = backupDirectory();
		if (!active.exists() && backup.isDirectory()) {
			if (!renameDirectory(backup, active, "restore assembly cache backup")) {
				throw preparationFailure(
					"restore assembly cache backup",
					backup.getAbsolutePath() + " -> " + active.getAbsolutePath(),
					"Failed to restore assembly cache backup after interrupted "
						+ "replacement: " + backup.getAbsolutePath(),
					new IOException("renameDirectory returned false")
				);
			}
			environment.info(
				"Restored active assembly cache after interrupted replacement"
			);
		} else if (active.isDirectory() && backup.exists()) {
			deleteRecursivelyOrThrow(
				backup,
				"obsolete assembly cache backup"
			);
		}
		discardFailedStaging();
	}

	private File prepareStagingDirectory() {
		File staging = stagingDirectory();
		deleteRecursivelyOrThrow(staging, "stale assembly cache staging");
		return staging;
	}

	private void replaceActiveCache(File staging, File active) {
		File backup = backupDirectory();
		deleteRecursivelyOrThrow(backup, "stale assembly cache backup");

		boolean hadActive = active.isDirectory();
		if (active.exists() && !hadActive) {
			throw preparationFailure(
				"validate active assembly cache path",
				active.getAbsolutePath(),
				"Active assembly cache path is not a directory: "
					+ active.getAbsolutePath(),
				null
			);
		}

		if (
			hadActive
				&& !renameDirectory(
					active,
					backup,
					"preserve active assembly cache"
				)
		) {
			throw preparationFailure(
				"preserve active assembly cache",
				active.getAbsolutePath() + " -> " + backup.getAbsolutePath(),
				"Failed to preserve active assembly cache before replacement: "
					+ active.getAbsolutePath(),
				new IOException("renameDirectory returned false")
			);
		}

		if (
			!renameDirectory(
				staging,
				active,
				"promote staged assembly cache"
			)
		) {
			boolean restored =
				!hadActive
					|| renameDirectory(
						backup,
						active,
						"restore active assembly cache"
					);
			if (!restored) {
				environment.error(
					"Failed to restore active assembly cache after promotion failure",
					new IOException(
						"Backup remains at " + backup.getAbsolutePath()
					)
				);
			}
			throw preparationFailure(
				"promote staged assembly cache",
				staging.getAbsolutePath() + " -> " + active.getAbsolutePath(),
				"Failed to promote validated staged assembly cache. "
					+ (
						restored
							? "The previous active cache was restored."
							: "The previous cache remains in the backup directory "
								+ "for startup recovery."
					),
				new IOException("renameDirectory returned false")
			);
		}

		environment.info(
			"Promoted validated staged assembly cache: "
				+ active.getAbsolutePath()
		);
		discardObsoleteBackup();
	}

	private void discardObsoleteBackup() {
		File backup = backupDirectory();
		if (!backup.exists()) {
			return;
		}
		deleteRecursive(backup);
		if (backup.exists()) {
			environment.warn(
				"Could not remove obsolete assembly cache backup: "
					+ backup.getAbsolutePath()
			);
		}
	}

	private void discardFailedStaging() {
		File staging = stagingDirectory();
		if (!staging.exists()) {
			return;
		}
		deleteRecursive(staging);
		if (staging.exists()) {
			environment.warn(
				"Could not remove failed assembly cache staging: "
					+ staging.getAbsolutePath()
			);
		}
	}

	private void deleteRecursivelyOrThrow(File target, String description) {
		if (!target.exists()) {
			return;
		}
		deleteRecursive(target);
		if (target.exists()) {
			throw preparationFailure(
				"delete " + description,
				target.getAbsolutePath(),
				"Failed to remove " + description + ": "
					+ target.getAbsolutePath(),
				new IOException("File.delete returned false")
			);
		}
	}

	private boolean renameDirectory(
		File source,
		File destination,
		String operation
	) {
		try {
			return environment.renameDirectory(source, destination);
		} catch (RuntimeException error) {
			throw preparationFailure(
				operation,
				source.getAbsolutePath() + " -> " + destination.getAbsolutePath(),
				"Directory rename failed during " + operation,
				error
			);
		}
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
			environment.warn(
				"Could not delete cached file: " + target.getAbsolutePath()
			);
		}
	}

	private boolean hasStaleCachedBranchGameCodeAssemblies(
		File destination
	) {
		if (
			destination == null
				|| !destination.exists()
				|| !destination.isDirectory()
		) {
			return false;
		}
		for (String name : BRANCH_GAME_CODE_ASSEMBLIES) {
			File cached = new File(destination, name);
			if (
				cached.exists()
					&& cached.isFile()
					&& !matchesPackagedAsset(
						cached,
						"dotnet_bcl/" + name
					)
			) {
				environment.info(
					"Launcher bootstrap assembly cache contains stale branch game-code assembly; refreshing: "
						+ cached.getAbsolutePath()
				);
				return true;
			}
		}
		return false;
	}

	private boolean matchesPackagedAsset(File file, String assetPath) {
		if (
			file == null
				|| assetPath == null
				|| !file.exists()
				|| !file.isFile()
		) {
			return false;
		}
		long assetLength = packagedAssetLength(assetPath);
		if (assetLength < 0 || file.length() != assetLength) {
			return false;
		}
		try {
			return sha256Hex(file).equalsIgnoreCase(
				sha256HexAsset(assetPath)
			);
		} catch (Exception error) {
			environment.warn(
				"Failed to compare cached assembly with packaged asset: "
					+ assetPath,
				error
			);
			return false;
		}
	}

	private String sha256HexAsset(String assetPath) throws IOException {
		try (InputStream input = environment.openAsset(assetPath)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = input.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return bytesToHex(digest.digest());
		} catch (java.security.NoSuchAlgorithmException error) {
			throw new IOException("SHA-256 unavailable", error);
		}
	}

	private Set<String> getPackagedBclNames() {
		Set<String> names = new HashSet<>();
		try {
			String[] files = environment.listAssets("dotnet_bcl");
			if (files != null) {
				java.util.Collections.addAll(names, files);
			}
		} catch (IOException error) {
			environment.error(
				"Failed to list packaged BCL assemblies",
				error
			);
			throw preparationFailure(
				"list packaged assemblies",
				"dotnet_bcl",
				"Failed to list packaged BCL assemblies",
				error
			);
		}
		return names;
	}

	private boolean shouldCopyGameAssemblyFile(
		String name,
		Set<String> packagedBclNames
	) {
		if (name == null) {
			return false;
		}
		String lower = name.toLowerCase(java.util.Locale.ROOT);
		if (
			packagedBclNames.contains(name)
				&& !isBranchGameCodeAssembly(lower)
		) {
			return false;
		}
		if (!lower.endsWith(".dll") && !lower.endsWith(".json")) {
			return false;
		}
		return !lower.equals("coreclr.dll")
			&& !lower.equals("clrjit.dll")
			&& !lower.equals("clrgc.dll")
			&& !lower.equals("clrgcexp.dll")
			&& !lower.equals("clretwrc.dll")
			&& !lower.equals("createdump.exe")
			&& !lower.equals("hostfxr.dll")
			&& !lower.equals("hostpolicy.dll")
			&& !lower.equals("mscordaccore.dll")
			&& !lower.equals("mscordbi.dll")
			&& !lower.equals("mscorrc.dll")
			&& !lower.equals("msquic.dll")
			&& !lower.equals("steam_api64.dll")
			&& !lower.equals("sharpgen.runtime.dll")
			&& !lower.equals("sharpgen.runtime.com.dll")
			&& !lower.equals("system.io.compression.native.dll")
			&& !lower.equals("vortice.directx.dll")
			&& !lower.equals("vortice.dxgi.dll")
			&& !lower.equals("vortice.mathematics.dll")
			&& !lower.startsWith("mscordaccore_")
			&& !lower.startsWith("microsoft.diasymreader.native.");
	}

	private boolean isBranchGameCodeAssembly(String name) {
		if (name == null) {
			return false;
		}
		String lower = name.toLowerCase(java.util.Locale.ROOT);
		for (String gameCodeAssembly : BRANCH_GAME_CODE_ASSEMBLIES) {
			if (
				lower.equals(
					gameCodeAssembly.toLowerCase(java.util.Locale.ROOT)
				)
			) {
				return true;
			}
		}
		return false;
	}

	private boolean selectedBranchRequiresRuntimePack() {
		return !"public".equalsIgnoreCase(environment.selectedBranch());
	}

	private String runtimePackIdentity(File runtimePackDirectory) {
		if (
			runtimePackDirectory == null
				|| !runtimePackDirectory.exists()
				|| !runtimePackDirectory.isDirectory()
		) {
			return "<none>";
		}
		StringBuilder identity = new StringBuilder(
			"dir=" + runtimePackDirectory.getName()
		);
		appendRuntimePackFileIdentity(
			identity,
			new File(
				runtimePackDirectory,
				RUNTIME_PACK_COMPATIBILITY_MANIFEST
			)
		);
		appendRuntimePackFileIdentity(
			identity,
			new File(
				runtimePackDirectory,
				RUNTIME_PACK_PATCH_VALIDATION_REPORT
			)
		);
		File[] files = runtimePackDirectory.listFiles(
			(directory, name) ->
				name.toLowerCase(java.util.Locale.ROOT).endsWith(".dll")
		);
		if (files != null) {
			java.util.Arrays.sort(
				files,
				(left, right) ->
					left.getName().compareToIgnoreCase(right.getName())
			);
			for (File file : files) {
				appendRuntimePackFileIdentity(identity, file);
			}
		}
		return identity.toString();
	}

	private void appendRuntimePackFileIdentity(
		StringBuilder identity,
		File file
	) {
		identity
			.append("|")
			.append(file.getName())
			.append("=")
			.append(fileIdentity(file));
	}

	private String fileIdentity(File file) {
		if (file == null || !file.exists() || !file.isFile()) {
			return "<missing>";
		}
		return "bytes=" + file.length() + ",mtime=" + file.lastModified();
	}

	private File findAssembliesDirectory() {
		if (!isGamePckReady()) {
			return null;
		}
		File[] children = environment.gameDirectory().listFiles();
		if (children == null) {
			return null;
		}

		File fallback = null;
		for (File child : children) {
			if (!child.isDirectory() || !child.getName().startsWith("data_")) {
				continue;
			}
			environment.info(
				"Found assemblies directory candidate: " + child.getName()
			);
			if (
				child.getName().contains("android")
					&& containsAssemblies(child)
			) {
				return child;
			}
			if (fallback == null) {
				fallback = child;
			}
		}
		return fallback != null && containsAssemblies(fallback)
			? fallback
			: null;
	}

	private File findRuntimePackDirectory() {
		if (!isGamePckReady()) {
			return null;
		}
		File directory = runtimePackCandidateDirectory();
		if (!directory.exists() || !directory.isDirectory()) {
			return null;
		}

		String validationKey = runtimePackValidationCacheKey(directory);
		if (
			cachedRuntimePackDirectory != null
				&& cachedRuntimePackDirectory.equals(directory)
				&& validationKey.equals(cachedRuntimePackValidationKey)
		) {
			return cachedRuntimePackDirectory;
		}
		if (!isRuntimePackManifestUsable(directory)) {
			environment.warn(
				"Ignoring runtime pack with incomplete or mismatched manifest: "
					+ directory.getAbsolutePath()
			);
			cachedRuntimePackDirectory = null;
			cachedRuntimePackValidationKey = "";
			return null;
		}
		cachedRuntimePackDirectory = directory;
		cachedRuntimePackValidationKey = validationKey;
		return directory;
	}

	private String runtimePackValidationCacheKey(File runtimePackDirectory) {
		File sourceDirectory = findAssembliesDirectory();
		File selectedSourceAssembly = sourceDirectory == null
			? null
			: new File(sourceDirectory, RUNTIME_PACK_ANDROID_ASSEMBLY);
		return "branch=" + environment.selectedBranch()
			+ "|pck=" + fileIdentity(selectedPck())
			+ "|source=" + fileIdentity(selectedSourceAssembly)
			+ "|runtimePack=" + runtimePackIdentity(runtimePackDirectory);
	}

	private boolean isRuntimePackManifestUsable(
		File runtimePackDirectory
	) {
		if (
			runtimePackDirectory == null
				|| !runtimePackDirectory.exists()
				|| !runtimePackDirectory.isDirectory()
		) {
			return false;
		}

		File manifest = new File(
			runtimePackDirectory,
			RUNTIME_PACK_COMPATIBILITY_MANIFEST
		);
		File report = new File(
			runtimePackDirectory,
			RUNTIME_PACK_PATCH_VALIDATION_REPORT
		);
		File gameAssembly = new File(
			runtimePackDirectory,
			RUNTIME_PACK_ANDROID_ASSEMBLY
		);
		if (
			!manifest.isFile()
				|| !report.isFile()
				|| !gameAssembly.isFile()
		) {
			environment.warn(
				"Runtime pack is missing required manifest/report/assembly files: "
					+ runtimePackDirectory.getAbsolutePath()
			);
			return false;
		}

		try {
			JSONObject json = new JSONObject(
				readSmallTextFile(manifest, 64 * 1024)
			);
			String packId = json.optString("packId", "");
			String sourceRuntimeSlotId =
				json.optString("sourceRuntimeSlotId", "");
			String sourceBranch = json.optString("sourceBranch", "");
			String sourcePckSha256 =
				json.optString("sourcePckSha256", "");
			String sourceAssemblySha256 =
				json.optString("sourceAssemblySha256", "");
			String androidAssemblySha256 =
				json.optString("androidAssemblySha256", "");
			String patchValidationStatus =
				json.optString("patchValidationStatus", "");
			boolean generatedFromCleanDirectory =
				json.optBoolean("generatedFromCleanDirectory", false);
			JSONArray supportAssemblies =
				json.optJSONArray("supportAssemblies");
			JSONObject supportAssemblySha256 =
				json.optJSONObject("supportAssemblySha256");

			if (
				packId.trim().isEmpty()
					|| sourceRuntimeSlotId.trim().isEmpty()
					|| sourceBranch.trim().isEmpty()
					|| sourcePckSha256.trim().isEmpty()
					|| sourceAssemblySha256.trim().isEmpty()
					|| androidAssemblySha256.trim().isEmpty()
			) {
				environment.warn(
					"Runtime pack manifest is missing required identity/hash "
						+ "fields: " + manifest.getAbsolutePath()
				);
				return false;
			}
			if (!generatedFromCleanDirectory) {
				environment.warn(
					"Runtime pack was not generated from a clean directory: "
						+ manifest.getAbsolutePath()
				);
				return false;
			}
			if (
				!runtimePackSupportAssembliesUsable(
					runtimePackDirectory,
					supportAssemblies,
					supportAssemblySha256
				)
			) {
				return false;
			}
			if (
				!sourceBranch.equalsIgnoreCase(
					environment.selectedBranch()
				)
			) {
				environment.warn(
					"Runtime pack branch mismatch: declared=" + sourceBranch
						+ " selected=" + environment.selectedBranch()
				);
				return false;
			}

			File selectedPck = selectedPck();
			if (!selectedPck.isFile()) {
				environment.warn(
					"Runtime pack cannot be matched because selected PCK is "
						+ "missing: " + selectedPck.getAbsolutePath()
				);
				return false;
			}
			String selectedPckSha256 =
				selectedPckSha256ForRuntimePackValidation(
					selectedPck,
					sourcePckSha256
				);
			if (
				!pckMatchesRuntimeSource(
					selectedPck,
					sourcePckSha256,
					selectedPckSha256
				)
			) {
				environment.warn(
					"Runtime pack selected PCK hash mismatch: declared="
						+ sourcePckSha256 + " selected=" + selectedPckSha256
				);
				return false;
			}

			File sourceDirectory = findAssembliesDirectory();
			File selectedSourceAssembly = sourceDirectory == null
				? null
				: new File(
					sourceDirectory,
					RUNTIME_PACK_ANDROID_ASSEMBLY
				);
			if (
				selectedSourceAssembly == null
					|| !selectedSourceAssembly.isFile()
			) {
				environment.warn(
					"Runtime pack cannot be matched because selected source "
						+ "sts2.dll is missing: "
						+ (
							selectedSourceAssembly == null
								? "<none>"
								: selectedSourceAssembly.getAbsolutePath()
						)
				);
				return false;
			}
			String selectedSourceSha256 =
				sha256Hex(selectedSourceAssembly);
			if (
				!sourceAssemblySha256.equalsIgnoreCase(
					selectedSourceSha256
				)
			) {
				environment.warn(
					"Runtime pack selected source assembly hash mismatch: declared="
						+ sourceAssemblySha256
						+ " selected=" + selectedSourceSha256
				);
				return false;
			}
			if (!"passed".equalsIgnoreCase(patchValidationStatus)) {
				environment.warn(
					"Runtime pack manifest patch validation did not pass: "
						+ patchValidationStatus
				);
				return false;
			}

			JSONObject reportJson = new JSONObject(
				readSmallTextFile(report, 64 * 1024)
			);
			if (
				!"passed".equalsIgnoreCase(
					reportJson.optString("status", "")
				)
			) {
				environment.warn(
					"Runtime pack patch validation report did not pass: "
						+ reportJson.optString("status", "")
				);
				return false;
			}
			if (
				!packId.equalsIgnoreCase(
					reportJson.optString("runtimePackId", "")
				)
					|| !sourceRuntimeSlotId.equalsIgnoreCase(
						reportJson.optString("sourceRuntimeSlotId", "")
					)
					|| !sourceBranch.equalsIgnoreCase(
						reportJson.optString("branch", "")
					)
					|| !sourcePckSha256.equalsIgnoreCase(
						reportJson.optString("pckSha256", "")
					)
					|| !sourceAssemblySha256.equalsIgnoreCase(
						reportJson.optString("sourceAssemblySha256", "")
					)
					|| !androidAssemblySha256.equalsIgnoreCase(
						reportJson.optString("androidAssemblySha256", "")
					)
					|| !json.optString("patchSetVersion", "")
						.equalsIgnoreCase(
							reportJson.optString("patchSetVersion", "")
						)
					|| !json.optString("validationSurfaceVersion", "")
						.equalsIgnoreCase(
							reportJson.optString(
								"validationSurfaceVersion",
								""
							)
						)
					|| !jsonArrayStringsEqual(
						supportAssemblies,
						reportJson.optJSONArray("supportAssemblies")
					)
					|| !jsonStringObjectEqual(
						supportAssemblySha256,
						reportJson.optJSONObject("supportAssemblySha256")
					)
					|| reportJson.optBoolean(
						"generatedFromCleanDirectory",
						false
					) != generatedFromCleanDirectory
			) {
				environment.warn(
					"Runtime pack patch validation report does not match "
						+ "compatibility manifest."
				);
				return false;
			}
			String actualAndroidAssemblySha256 = sha256Hex(gameAssembly);
			if (
				!androidAssemblySha256.equalsIgnoreCase(
					actualAndroidAssemblySha256
				)
			) {
				environment.warn(
					"Runtime pack Android assembly hash mismatch: declared="
						+ androidAssemblySha256
						+ " actual=" + actualAndroidAssemblySha256
				);
				return false;
			}
			return true;
		} catch (Exception error) {
			environment.warn(
				"Failed to parse runtime pack manifest: "
					+ manifest.getAbsolutePath(),
				error
			);
			return false;
		}
	}

	private boolean runtimePackSupportAssembliesUsable(
		File runtimePackDirectory,
		JSONArray supportAssemblies,
		JSONObject supportAssemblySha256
	) {
		if (
			supportAssemblies == null
				|| supportAssemblySha256 == null
		) {
			environment.warn(
				"Runtime pack manifest is missing support assembly declarations."
			);
			return false;
		}

		HashSet<String> declaredDlls = new HashSet<>();
		HashSet<String> declaredSupportAssemblies = new HashSet<>();
		declaredDlls.add(
			RUNTIME_PACK_ANDROID_ASSEMBLY.toLowerCase(
				java.util.Locale.ROOT
			)
		);
		for (int index = 0; index < supportAssemblies.length(); index++) {
			String name = supportAssemblies.optString(index, "");
			String lower = name.toLowerCase(java.util.Locale.ROOT);
			if (
				name.trim().isEmpty()
					|| name.contains("/")
					|| name.contains("\\")
					|| !name.endsWith(".dll")
			) {
				environment.warn(
					"Runtime pack support assembly has unsafe name: " + name
				);
				return false;
			}
			if (
				lower.equals(
					RUNTIME_PACK_ANDROID_ASSEMBLY.toLowerCase(
						java.util.Locale.ROOT
					)
				)
			) {
				environment.warn(
					"Runtime pack support assemblies must not redeclare sts2.dll."
				);
				return false;
			}
			if (!declaredDlls.add(lower)) {
				environment.warn(
					"Runtime pack support assembly is duplicated: " + name
				);
				return false;
			}
			declaredSupportAssemblies.add(lower);

			File supportAssembly = new File(runtimePackDirectory, name);
			if (!supportAssembly.isFile()) {
				environment.warn(
					"Runtime pack support assembly is missing: "
						+ supportAssembly.getAbsolutePath()
				);
				return false;
			}
			String declaredSha256 =
				supportAssemblySha256.optString(name, "");
			if (declaredSha256.trim().isEmpty()) {
				environment.warn(
					"Runtime pack support assembly hash is missing: " + name
				);
				return false;
			}
			String actualSha256 = sha256Hex(supportAssembly);
			if (!declaredSha256.equalsIgnoreCase(actualSha256)) {
				environment.warn(
					"Runtime pack support assembly hash mismatch: " + name
						+ " declared=" + declaredSha256
						+ " actual=" + actualSha256
				);
				return false;
			}
		}
		if (
			supportAssemblySha256.length()
				!= declaredSupportAssemblies.size()
		) {
			environment.warn(
				"Runtime pack support assembly hash set does not match "
					+ "declared support assemblies."
			);
			return false;
		}
		Iterator<String> keys = supportAssemblySha256.keys();
		while (keys.hasNext()) {
			String key = keys.next();
			if (
				!declaredSupportAssemblies.contains(
					key.toLowerCase(java.util.Locale.ROOT)
				)
			) {
				environment.warn(
					"Runtime pack support assembly hash is undeclared: " + key
				);
				return false;
			}
		}

		File[] dlls = runtimePackDirectory.listFiles(
			(directory, name) ->
				name.toLowerCase(java.util.Locale.ROOT).endsWith(".dll")
		);
		if (dlls != null) {
			for (File dll : dlls) {
				if (
					!declaredDlls.contains(
						dll.getName().toLowerCase(java.util.Locale.ROOT)
					)
				) {
					environment.warn(
						"Runtime pack contains undeclared DLL: "
							+ dll.getAbsolutePath()
					);
					return false;
				}
			}
		}
		return true;
	}

	private String selectedPckSha256ForRuntimePackValidation(
		File selectedPck,
		String expectedSourcePckSha256
	) {
		if (selectedPck == null || !selectedPck.isFile()) {
			return "";
		}
		try {
			File marker = new File(
				environment.filesDirectory(),
				CURRENT_RUNTIME_SLOT_MARKER
			);
			if (
				marker.isFile()
					&& markerIsFreshForFile(marker, selectedPck)
			) {
				JSONObject json = new JSONObject(
					readSmallTextFile(marker, 64 * 1024)
				);
				String markerBranch = json.optString("branch", "");
				String markerPckSha256 =
					json.optString("pckSha256", "");
				if (
					markerBranch.equalsIgnoreCase(
						environment.selectedBranch()
					)
						&& !markerPckSha256.trim().isEmpty()
						&& pckMatchesRuntimeSource(
							selectedPck,
							expectedSourcePckSha256,
							markerPckSha256
						)
				) {
					environment.info(
						"Using current runtime slot marker PCK hash for native "
							+ "runtime-pack validation."
					);
					return markerPckSha256;
				}
			}
		} catch (Exception error) {
			environment.warn(
				"Runtime slot marker could not satisfy native runtime-pack "
					+ "validation; hashing selected PCK.",
				error
			);
		}
		return sha256Hex(selectedPck);
	}

	private boolean markerIsFreshForFile(File marker, File file) {
		return marker != null
			&& file != null
			&& marker.exists()
			&& file.exists()
			&& marker.lastModified() + 2000L >= file.lastModified();
	}

	private Set<String> runtimePackDeclaredAssemblyNames(
		File runtimePackDirectory
	) {
		Map<String, RuntimePackAssemblyExpectation> expectations =
			runtimePackAssemblyExpectations(runtimePackDirectory);
		if (!expectations.isEmpty()) {
			return new HashSet<>(expectations.keySet());
		}
		HashSet<String> fallback = new HashSet<>();
		fallback.add(
			RUNTIME_PACK_ANDROID_ASSEMBLY.toLowerCase(
				java.util.Locale.ROOT
			)
		);
		return fallback;
	}

	private Map<String, RuntimePackAssemblyExpectation>
		runtimePackAssemblyExpectations(File runtimePackDirectory) {
		LinkedHashMap<String, RuntimePackAssemblyExpectation> expectations =
			new LinkedHashMap<>();
		if (runtimePackDirectory == null) {
			return expectations;
		}

		File manifest = new File(
			runtimePackDirectory,
			RUNTIME_PACK_COMPATIBILITY_MANIFEST
		);
		try {
			JSONObject json = new JSONObject(
				readSmallTextFile(manifest, 64 * 1024)
			);
			addRuntimePackAssemblyExpectation(
				expectations,
				runtimePackDirectory,
				RUNTIME_PACK_ANDROID_ASSEMBLY,
				json.optString("androidAssemblySha256", "")
			);
			JSONArray supportAssemblies =
				json.optJSONArray("supportAssemblies");
			JSONObject supportHashes =
				json.optJSONObject("supportAssemblySha256");
			if (supportAssemblies != null && supportHashes != null) {
				for (
					int index = 0;
					index < supportAssemblies.length();
					index++
				) {
					String name =
						supportAssemblies.optString(index, "").trim();
					if (!name.isEmpty()) {
						addRuntimePackAssemblyExpectation(
							expectations,
							runtimePackDirectory,
							name,
							supportHashes.optString(name, "")
						);
					}
				}
			}
		} catch (Exception error) {
			environment.warn(
				"Failed to read manifest-declared runtime-pack assembly "
					+ "identities.",
				error
			);
			expectations.clear();
		}
		return expectations;
	}

	private void addRuntimePackAssemblyExpectation(
		Map<String, RuntimePackAssemblyExpectation> expectations,
		File runtimePackDirectory,
		String name,
		String sha256
	) {
		String normalizedName = name == null
			? ""
			: name.trim().toLowerCase(java.util.Locale.ROOT);
		String normalizedSha256 = sha256 == null ? "" : sha256.trim();
		if (normalizedName.isEmpty() || normalizedSha256.isEmpty()) {
			return;
		}
		expectations.put(
			normalizedName,
			new RuntimePackAssemblyExpectation(
				name.trim(),
				new File(runtimePackDirectory, name.trim()),
				normalizedSha256
			)
		);
	}

	private boolean jsonArrayStringsEqual(JSONArray left, JSONArray right) {
		if (
			left == null
				|| right == null
				|| left.length() != right.length()
		) {
			return false;
		}
		for (int index = 0; index < left.length(); index++) {
			if (
				!left.optString(index, "").equalsIgnoreCase(
					right.optString(index, "")
				)
			) {
				return false;
			}
		}
		return true;
	}

	private boolean jsonStringObjectEqual(
		JSONObject left,
		JSONObject right
	) {
		if (
			left == null
				|| right == null
				|| left.length() != right.length()
		) {
			return false;
		}
		Iterator<String> keys = left.keys();
		while (keys.hasNext()) {
			String key = keys.next();
			if (
				!left.optString(key, "").equalsIgnoreCase(
					right.optString(key, "")
				)
			) {
				return false;
			}
		}
		return true;
	}

	private File runtimePackGameAssembly(File runtimePackDirectory) {
		if (runtimePackDirectory == null) {
			return null;
		}
		File candidate = new File(
			runtimePackDirectory,
			RUNTIME_PACK_ANDROID_ASSEMBLY
		);
		return candidate.isFile() ? candidate : null;
	}

	private boolean hasGameAssemblies(File sourceDirectory) {
		return sourceDirectory != null
			&& sourceDirectory.isDirectory()
			&& new File(
				sourceDirectory,
				GAME_REQUIRED_ASSEMBLIES[0]
			).exists();
	}

	private boolean hasCurrentPackagedRequiredAssemblies(
		File destination
	) {
		if (destination == null || !destination.isDirectory()) {
			return false;
		}
		for (String name : BOOTSTRAP_REQUIRED_ASSEMBLIES) {
			File cached = new File(destination, name);
			if (
				!cached.exists()
					|| !packagedAssetMatchesFile(
						"dotnet_bcl/" + name,
						cached
					)
			) {
				environment.info(
					"Packaged assembly cache is stale: " + name
				);
				return false;
			}
		}
		return true;
	}

	private boolean packagedAssetMatchesFile(
		String assetPath,
		File cached
	) {
		try (
			InputStream asset = environment.openAsset(assetPath);
			InputStream current = new FileInputStream(cached)
		) {
			return streamsMatch(asset, current);
		} catch (IOException error) {
			environment.warn(
				"Could not compare packaged assembly asset: " + assetPath,
				error
			);
			return false;
		}
	}

	private boolean containsAssemblies(File directory) {
		if (directory == null || !directory.isDirectory()) {
			return false;
		}
		File[] files = directory.listFiles(
			(file, name) -> name.endsWith(".dll")
		);
		return files != null && files.length > 0;
	}

	private boolean hasCachedGameAssemblies(
		File destination,
		File sourceDirectory,
		Set<String> packagedBclNames,
		File runtimePackDirectory,
		File runtimePackGameAssembly
	) {
		if (destination == null) {
			return false;
		}
		if (
			selectedBranchRequiresRuntimePack()
				&& (
					runtimePackGameAssembly == null
						|| !runtimePackGameAssembly.isFile()
				)
		) {
			environment.info(
				"Game assembly cache is not current because selected non-public "
					+ "branch has no usable runtime pack."
			);
			return false;
		}

		boolean hasExpectedAssembly = false;
		Map<String, RuntimePackAssemblyExpectation>
			runtimePackExpectations =
				runtimePackAssemblyExpectations(runtimePackDirectory);
		if (
			runtimePackGameAssembly != null
				&& runtimePackGameAssembly.isFile()
		) {
			if (runtimePackExpectations.isEmpty()) {
				return false;
			}
			for (
				RuntimePackAssemblyExpectation expectation
					: runtimePackExpectations.values()
			) {
				hasExpectedAssembly = true;
				if (
					!runtimePackAssemblyMatchesExpectedIdentity(
						expectation,
						new File(destination, expectation.name)
					)
				) {
					environment.info(
						"Runtime-pack assembly cache is stale or incomplete: "
							+ expectation.name
					);
					return false;
				}
			}
		}

		if (sourceDirectory != null && sourceDirectory.isDirectory()) {
			File[] files = sourceDirectory.listFiles();
			if (files != null) {
				for (File source : files) {
					if (!source.isFile()) {
						continue;
					}
					if (
						runtimePackGameAssembly != null
							&& runtimePackGameAssembly.isFile()
							&& runtimePackExpectations.containsKey(
								source.getName().toLowerCase(
									java.util.Locale.ROOT
								)
							)
					) {
						continue;
					}
					if (
						!shouldCopyGameAssemblyFile(
							source.getName(),
							packagedBclNames
						)
					) {
						continue;
					}
					hasExpectedAssembly = true;
					if (
						!filesMatch(
							source,
							new File(destination, source.getName())
						)
					) {
						environment.info(
							"Game assembly cache is stale or incomplete: "
								+ source.getName()
						);
						return false;
					}
				}
			}
		}
		return hasExpectedAssembly;
	}

	private boolean runtimePackAssemblyMatchesExpectedIdentity(
		RuntimePackAssemblyExpectation expectation,
		File current
	) {
		if (
			expectation == null
				|| !expectation.source.isFile()
				|| current == null
				|| !current.isFile()
				|| expectation.source.length() != current.length()
		) {
			return false;
		}
		String sourceSha256 = sha256Hex(expectation.source);
		if (!expectation.sha256.equalsIgnoreCase(sourceSha256)) {
			environment.warn(
				"Runtime-pack source assembly no longer matches manifest: "
					+ expectation.name
					+ " declared=" + expectation.sha256
					+ " actual=" + sourceSha256
			);
			return false;
		}
		String currentSha256 = sha256Hex(current);
		if (!expectation.sha256.equalsIgnoreCase(currentSha256)) {
			environment.info(
				"Staged runtime-pack assembly hash mismatch: "
					+ expectation.name
					+ " declared=" + expectation.sha256
					+ " staged=" + currentSha256
			);
			return false;
		}
		return true;
	}

	private static final class RuntimePackAssemblyExpectation {
		final String name;
		final File source;
		final String sha256;

		RuntimePackAssemblyExpectation(
			String name,
			File source,
			String sha256
		) {
			this.name = name;
			this.source = source;
			this.sha256 = sha256;
		}
	}

	private boolean filesMatch(File expected, File current) {
		if (
			expected == null
				|| current == null
				|| !expected.exists()
				|| !current.exists()
				|| expected.length() != current.length()
		) {
			return false;
		}
		try (
			InputStream expectedStream = new FileInputStream(expected);
			InputStream currentStream = new FileInputStream(current)
		) {
			return streamsMatch(expectedStream, currentStream);
		} catch (IOException error) {
			environment.warn(
				"Could not compare game assembly cache file: "
					+ expected.getName(),
				error
			);
			return false;
		}
	}

	private boolean streamsMatch(
		InputStream expected,
		InputStream current
	) throws IOException {
		byte[] expectedBuffer = new byte[8192];
		byte[] currentBuffer = new byte[8192];
		while (true) {
			int expectedRead = expected.read(expectedBuffer);
			int currentRead = current.read(currentBuffer);
			if (expectedRead != currentRead) {
				return false;
			}
			if (expectedRead < 0) {
				return true;
			}
			for (int index = 0; index < expectedRead; index++) {
				if (expectedBuffer[index] != currentBuffer[index]) {
					return false;
				}
			}
		}
	}

	private boolean isGamePckReady() {
		File pck = selectedPck();
		if (!pck.isFile() || pck.length() < 96) {
			return false;
		}
		if (!isBranchMarkerReady()) {
			return false;
		}

		try (RandomAccessFile file = new RandomAccessFile(pck, "r")) {
			if (readUInt32LittleEndian(file) != 0x43504447L) {
				return false;
			}
			readUInt32LittleEndian(file);
			readUInt32LittleEndian(file);
			readUInt32LittleEndian(file);
			readUInt32LittleEndian(file);
			readUInt32LittleEndian(file);
			readLongLittleEndian(file);
			long directoryBase = readLongLittleEndian(file);
			if (
				directoryBase <= 0
					|| directoryBase + 4 > file.length()
			) {
				environment.warn(
					"Game PCK is not structurally ready: dirBase="
						+ directoryBase + " fileSize=" + file.length()
				);
				return false;
			}
			file.seek(directoryBase);
			long fileCount = readUInt32LittleEndian(file);
			if (fileCount <= 0) {
				environment.warn(
					"Game PCK is not structurally ready: fileCount="
						+ fileCount
				);
				return false;
			}
			return true;
		} catch (IOException error) {
			environment.warn("Failed to inspect game PCK", error);
			return false;
		}
	}

	private boolean isBranchMarkerReady() {
		String branch = environment.selectedBranch();
		File marker = new File(
			environment.gameDirectory(),
			BRANCH_MARKER_FILE
		);
		if (!marker.isFile()) {
			return "public".equalsIgnoreCase(branch);
		}

		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (
					!line.regionMatches(
						true,
						0,
						"Branch:",
						0,
						"Branch:".length()
					)
				) {
					continue;
				}
				String markerBranch =
					line.substring("Branch:".length()).trim();
				boolean ready = markerBranch.equalsIgnoreCase(branch);
				if (!ready) {
					environment.warn(
						"Steam branch marker mismatch: selected=" + branch
							+ " marker=" + markerBranch
					);
				}
				return ready
					&& (
						"public".equalsIgnoreCase(branch)
							|| (
								hasInstallSlotProvenance(marker, branch)
									&& depotManifestCount(marker) > 0
									&& hasBranchIntegrityProvenance(marker)
							)
					);
			}
			environment.warn(
				"Steam branch marker has no Branch line: "
					+ marker.getAbsolutePath()
			);
		} catch (IOException error) {
			environment.warn(
				"Failed to read Steam branch marker: "
					+ marker.getAbsolutePath(),
				error
			);
		}
		return false;
	}

	private boolean hasInstallSlotProvenance(
		File marker,
		String branch
	) {
		return SteamBranchInfo.installSlotKind(branch).equalsIgnoreCase(
			readMarkerValue(marker, "Install slot kind:")
		) && normalizeMarkerPath(
			SteamBranchInfo.installSlotDirectory(
				environment.filesDirectory(),
				branch
			).getAbsolutePath()
		).equalsIgnoreCase(
			normalizeMarkerPath(
				readMarkerValue(marker, "Install slot directory:")
			)
		);
	}

	private boolean hasBranchIntegrityProvenance(File marker) {
		return markerHasValue(
			marker,
			"Depot manifests matching public count:"
		) && markerHasValue(
			marker,
			"Depot manifests differing from public count:"
		) && markerHasValue(
			marker,
			"Depot manifests without public comparison count:"
		) && markerHasValue(
			marker,
			"Depot manifests inherited from public count:"
		) && markerHasValue(
			marker,
			"Depot manifests missing selected branch manifest count:"
		);
	}

	private boolean markerHasValue(File marker, String prefix) {
		return !readMarkerValue(marker, prefix).isEmpty();
	}

	private String readMarkerValue(File marker, String prefix) {
		if (marker == null || !marker.isFile()) {
			return "";
		}
		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			String line;
			while ((line = reader.readLine()) != null) {
				if (
					line.regionMatches(
						true,
						0,
						prefix,
						0,
						prefix.length()
					)
				) {
					return line.substring(prefix.length()).trim();
				}
			}
		} catch (IOException error) {
			environment.warn(
				"Failed to inspect Steam branch marker: "
					+ marker.getAbsolutePath(),
				error
			);
		}
		return "";
	}

	private int depotManifestCount(File marker) {
		if (marker == null || !marker.isFile()) {
			return 0;
		}
		try (BufferedReader reader = new BufferedReader(new FileReader(marker))) {
			int count = 0;
			String line;
			while ((line = reader.readLine()) != null) {
				if (
					line.regionMatches(
						true,
						0,
						"Depot manifest:",
						0,
						"Depot manifest:".length()
					)
				) {
					count++;
				}
			}
			return count;
		} catch (IOException error) {
			environment.warn(
				"Failed to inspect Steam branch marker depot provenance: "
					+ marker.getAbsolutePath(),
				error
			);
			return 0;
		}
	}

	private String normalizeMarkerPath(String path) {
		if (
			path == null
				|| path.trim().isEmpty()
				|| path.startsWith("<")
		) {
			return "";
		}
		String normalized = path.trim().replace('\\', '/');
		while (normalized.endsWith("/") && normalized.length() > 1) {
			normalized = normalized.substring(0, normalized.length() - 1);
		}
		String packageName = environment.packageName();
		String dataDataRoot = "/data/data/" + packageName;
		String dataUserRoot = "/data/user/0/" + packageName;
		if (
			normalized.equals(dataDataRoot)
				|| normalized.startsWith(dataDataRoot + "/")
		) {
			normalized =
				dataUserRoot + normalized.substring(dataDataRoot.length());
		}
		return normalized;
	}

	private boolean pckMatchesRuntimeSource(
		File selectedPck,
		String expectedSourcePckSha256,
		String selectedPckSha256
	) {
		if (
			expectedSourcePckSha256 == null
				|| expectedSourcePckSha256.trim().isEmpty()
		) {
			return false;
		}
		if (
			selectedPckSha256 != null
				&& expectedSourcePckSha256.equalsIgnoreCase(
					selectedPckSha256
				)
		) {
			return true;
		}
		if (selectedPck == null || !selectedPck.isFile()) {
			return false;
		}
		File marker = new File(
			selectedPck.getParentFile(),
			PCK_ANDROID_PATCH_MARKER
		);
		if (
			!marker.isFile()
				|| marker.lastModified() < selectedPck.lastModified()
		) {
			return false;
		}
		String markerSource = readPckPatchMarkerHash(
			marker,
			"sourcePckSha256"
		);
		if (!markerSource.isEmpty()) {
			boolean matched =
				expectedSourcePckSha256.equalsIgnoreCase(markerSource);
			if (!matched) {
				environment.warn(
					"Android PCK patch marker source hash mismatch: expected="
						+ expectedSourcePckSha256
						+ " marker=" + markerSource
				);
			}
			return matched;
		}
		environment.info(
			"Accepting legacy Android-patched PCK marker for runtime source "
				+ "hash " + expectedSourcePckSha256
		);
		return true;
	}

	private String readPckPatchMarkerHash(File marker, String name) {
		try {
			if (marker == null || !marker.isFile() || marker.length() <= 0) {
				return "";
			}
			JSONObject json = new JSONObject(
				readSmallTextFile(marker, 16 * 1024)
			);
			return json.optString(name, "").trim();
		} catch (Exception error) {
			return "";
		}
	}

	private String readSmallTextFile(File file, int maxBytes) {
		try (FileInputStream input = new FileInputStream(file)) {
			ByteArrayOutputStream output = new ByteArrayOutputStream();
			byte[] buffer = new byte[4096];
			int remaining = maxBytes;
			while (remaining > 0) {
				int read = input.read(
					buffer,
					0,
					Math.min(buffer.length, remaining)
				);
				if (read <= 0) {
					break;
				}
				output.write(buffer, 0, read);
				remaining -= read;
			}
			if (input.read() >= 0) {
				output.write(
					"\n[truncated]\n".getBytes(java.nio.charset.StandardCharsets.UTF_8)
				);
			}
			return output.toString(
				java.nio.charset.StandardCharsets.UTF_8
			);
		} catch (Exception error) {
			return "Failed to read " + file.getAbsolutePath()
				+ ": " + error;
		}
	}

	private long readUInt32LittleEndian(RandomAccessFile file)
		throws IOException {
		return (file.readUnsignedByte())
			| ((long) file.readUnsignedByte() << 8)
			| ((long) file.readUnsignedByte() << 16)
			| ((long) file.readUnsignedByte() << 24);
	}

	private long readLongLittleEndian(RandomAccessFile file)
		throws IOException {
		return readUInt32LittleEndian(file)
			| (readUInt32LittleEndian(file) << 32);
	}

	private void copyFile(File source, File destination)
		throws IOException {
		try (
			InputStream input = new FileInputStream(source);
			OutputStream output = new FileOutputStream(destination)
		) {
			copyStream(input, output);
		}
	}

	private void copyStream(InputStream input, OutputStream output)
		throws IOException {
		byte[] buffer = new byte[8192];
		int read;
		while ((read = input.read(buffer)) > 0) {
			output.write(buffer, 0, read);
		}
	}

	private String sha256Hex(File file) {
		try (InputStream input = new FileInputStream(file)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[65536];
			int read;
			while ((read = input.read(buffer)) != -1) {
				digest.update(buffer, 0, read);
			}
			return bytesToHex(digest.digest());
		} catch (Exception error) {
			environment.warn(
				"Failed to compute SHA-256: " + file.getAbsolutePath(),
				error
			);
			return "<unavailable:"
				+ error.getClass().getSimpleName() + ">";
		}
	}

	private String bytesToHex(byte[] bytes) {
		char[] hex = new char[bytes.length * 2];
		char[] alphabet = "0123456789abcdef".toCharArray();
		for (int index = 0; index < bytes.length; index++) {
			int value = bytes[index] & 0xff;
			hex[index * 2] = alphabet[value >>> 4];
			hex[index * 2 + 1] = alphabet[value & 0x0f];
		}
		return new String(hex);
	}

	private static final class ContextEnvironment implements Environment {
		private final Context context;
		private final File gameDirectory;
		private final String selectedBranch;

		ContextEnvironment(
			Context context,
			File gameDirectory,
			String selectedBranch
		) {
			this.context = context;
			this.gameDirectory = gameDirectory;
			this.selectedBranch =
				selectedBranch == null || selectedBranch.trim().isEmpty()
					? "public"
					: selectedBranch;
		}

		@Override
		public File filesDirectory() {
			return context.getFilesDir();
		}

		@Override
		public File gameDirectory() {
			return gameDirectory;
		}

		@Override
		public String selectedBranch() {
			return selectedBranch;
		}

		@Override
		public String packageName() {
			return context.getPackageName();
		}

		@Override
		public String versionName() {
			return BuildConfig.VERSION_NAME;
		}

		@Override
		public int versionCode() {
			return BuildConfig.VERSION_CODE;
		}

		@Override
		public String runtimeGodotArchDirectory() {
			String nativeLibraryDirectory = nativeLibraryDirectory();
			if (nativeLibraryDirectory.contains("x86_64")) {
				return "x86_64";
			}
			if (nativeLibraryDirectory.contains("arm64")) {
				return "arm64";
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

		@Override
		public String nativeLibraryDirectory() {
			String directory = context.getApplicationInfo().nativeLibraryDir;
			return directory == null ? "" : directory;
		}

		@Override
		public String[] listAssets(String path) throws IOException {
			return context.getAssets().list(path);
		}

		@Override
		public long assetLengthBytes(String path) throws IOException {
			try (InputStream asset = context.getAssets().open(path)) {
				long total = 0;
				byte[] buffer = new byte[8192];
				int read;
				while ((read = asset.read(buffer)) > 0) {
					total += read;
				}
				return total;
			}
		}

		@Override
		public InputStream openAsset(String path) throws IOException {
			return context.getAssets().open(path);
		}

		@Override
		public CacheState readCacheState() {
			SharedPreferences preferences = preferences();
			return new CacheState(
				preferences.getInt(KEY_ASSEMBLY_CACHE_SCHEMA, -1),
				preferences.getInt(KEY_INSTALLED_VERSION_CODE, -1),
				preferences.getString(KEY_INSTALLED_PACKAGE_NAME, ""),
				preferences.getString(KEY_ASSEMBLY_CACHE_BRANCH, ""),
				preferences.getString(KEY_ASSEMBLY_CACHE_RUNTIME_ID, "")
			);
		}

		@Override
		public void writeCacheState(CacheState state) {
			preferences().edit()
				.putInt(KEY_ASSEMBLY_CACHE_SCHEMA, state.schema)
				.putInt(KEY_INSTALLED_VERSION_CODE, state.versionCode)
				.putString(KEY_INSTALLED_PACKAGE_NAME, state.packageName)
				.putString(KEY_ASSEMBLY_CACHE_BRANCH, state.branch)
				.putString(KEY_ASSEMBLY_CACHE_RUNTIME_ID, state.runtimeId)
				.apply();
		}

		@Override
		public boolean renameDirectory(File source, File destination) {
			return source.renameTo(destination);
		}

		@Override
		public void writeInternalTextFile(String name, String text)
			throws IOException {
			try (
				FileOutputStream output = new FileOutputStream(
					new File(filesDirectory(), name)
				)
			) {
				output.write(
					text.getBytes(java.nio.charset.StandardCharsets.UTF_8)
				);
			}
		}

		@Override
		public long currentTimeMillis() {
			return System.currentTimeMillis();
		}

		@Override
		public long usableSpaceBytes() {
			return filesDirectory().getUsableSpace();
		}

		@Override
		public void info(String message) {
			Log.i(TAG, message);
		}

		@Override
		public void warn(String message) {
			Log.w(TAG, message);
		}

		@Override
		public void warn(String message, Throwable error) {
			Log.w(TAG, message, error);
		}

		@Override
		public void error(String message, Throwable error) {
			Log.e(TAG, message, error);
		}

		@Override
		public String stackTrace(Throwable error) {
			StringWriter output = new StringWriter();
			error.printStackTrace(new PrintWriter(output));
			return output.toString();
		}

		private SharedPreferences preferences() {
			return context.getSharedPreferences(
				PREFS_NAME,
				Context.MODE_PRIVATE
			);
		}
	}
}
