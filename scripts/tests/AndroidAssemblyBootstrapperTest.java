package com.game.sts2launcher;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public final class AndroidAssemblyBootstrapperTest {
	private static final String[] REQUIRED_ASSEMBLIES = {
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

	public static void main(String[] args) throws Exception {
		testLauncherPreparationWithoutGodotActivity();
		testCorruptedActiveAssemblyIsRepaired();
		testMissingPackagedAssemblyIsRejected();
		testInsufficientStorageFailsBeforeCopy();
		testFailedPreparationDiscardsStagingAndRetriesOnce();
		testTerminalFailureReturnsDiagnostics();
		testValidatedReplacementRemovesStaleFiles();
		testPartialCopyPreservesExistingValidCache();
		testInterruptedReplacementRestoresBackup();
		testFailedPromotionsPreserveExistingValidCache();
		testInvalidGameAssemblyPreservesExistingValidGameCache();
		System.out.println("Android assembly bootstrapper tests passed.");
	}

	private static void testCorruptedActiveAssemblyIsRepaired()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-corrupt-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			assertTrue(
				"initial preparation succeeds",
				prepare(environment).isSuccess()
			);
			Path assembly = publishDirectory(root).resolve("STS2Mobile.dll");
			Files.writeString(
				assembly,
				"corrupted-active-assembly",
				StandardCharsets.UTF_8
			);

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertTrue("corrupted cache is repaired", result.isSuccess());
			assertEquals(
				"packaged assembly restored",
				environment.assetText("STS2Mobile.dll"),
				Files.readString(assembly, StandardCharsets.UTF_8)
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testMissingPackagedAssemblyIsRejected()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-missing-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			environment.omitListedAsset("GodotSharp.dll");

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertFalse("missing assembly is rejected", result.isSuccess());
			assertContains(
				"missing assembly validation operation",
				result.diagnostics(),
				"Original failed operation: validate required staged assemblies"
			);
			assertContains(
				"missing assembly retry operation",
				result.diagnostics(),
				"Retry failed operation: validate required staged assemblies"
			);
			assertContains(
				"missing assembly reason",
				result.diagnostics(),
				"Missing required Mono/cache assemblies after copy"
			);
			assertFalse(
				"missing assembly staging removed",
				Files.exists(stagingDirectory(root))
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testInsufficientStorageFailsBeforeCopy()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-storage-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			environment.availableStorageBytes = 0L;

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertFalse("insufficient storage is rejected", result.isSuccess());
			assertContains(
				"storage operation",
				result.diagnostics(),
				"Original failed operation: check assembly cache storage"
			);
			assertContains(
				"storage retry operation",
				result.diagnostics(),
				"Retry failed operation: check assembly cache storage"
			);
			assertContains(
				"storage value",
				result.diagnostics(),
				"Available storage bytes: 0"
			);
			assertContains(
				"storage reason",
				result.diagnostics(),
				"Insufficient storage for staged assembly cache"
			);
			assertFalse(
				"storage failure creates no staging cache",
				Files.exists(stagingDirectory(root))
			);
			assertFalse(
				"storage failure creates no active cache",
				Files.exists(publishDirectory(root))
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testLauncherPreparationWithoutGodotActivity()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-success-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			RecordingTimeline timeline = new RecordingTimeline();
			AndroidAssemblyBootstrapper bootstrapper =
				new AndroidAssemblyBootstrapper(environment, false, timeline);

			AndroidAssemblyBootstrapper.Result result = bootstrapper.prepare();

			assertTrue("preparation succeeds", result.isSuccess());
			assertEquals(
				"preparation starts once",
				1,
				timeline.count("native assembly setup")
			);
			assertEquals(
				"preparation completes once",
				1,
				timeline.count("native assembly setup complete")
			);
			assertEquals(
				"no retry",
				0,
				timeline.count("native assembly setup retry")
			);
			for (String name : REQUIRED_ASSEMBLIES) {
				Path copied = publishDirectory(root).resolve(name);
				assertTrue(name + " copied", Files.isRegularFile(copied));
				assertEquals(
					name + " contents",
					environment.assetText(name),
					Files.readString(copied, StandardCharsets.UTF_8)
				);
			}
			assertEquals(
				"cache schema",
				25,
				environment.cacheState.schema
			);
			assertEquals(
				"cache branch",
				"public",
				environment.cacheState.branch
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testFailedPreparationDiscardsStagingAndRetriesOnce()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-retry-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			environment.remainingAssetFailures = 1;
			RecordingTimeline timeline = new RecordingTimeline();
			AndroidAssemblyBootstrapper bootstrapper =
				new AndroidAssemblyBootstrapper(environment, false, timeline);

			AndroidAssemblyBootstrapper.Result result = bootstrapper.prepare();

			assertTrue("retry succeeds", result.isSuccess());
			assertEquals(
				"first failure recorded",
				1,
				timeline.count("native assembly setup failed")
			);
			assertEquals(
				"retry recorded",
				1,
				timeline.count("native assembly setup retry")
			);
			assertEquals(
				"retry completion recorded",
				1,
				timeline.count("native assembly setup retry complete")
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testTerminalFailureReturnsDiagnostics()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-failure-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			environment.remainingAssetFailures = Integer.MAX_VALUE;
			RecordingTimeline timeline = new RecordingTimeline();
			AndroidAssemblyBootstrapper bootstrapper =
				new AndroidAssemblyBootstrapper(environment, false, timeline);

			AndroidAssemblyBootstrapper.Result result = bootstrapper.prepare();

			assertFalse("terminal failure returned", result.isSuccess());
			assertContains(
				"diagnostics package",
				result.diagnostics(),
				"Package: com.example.test"
			);
			assertContains(
				"diagnostics branch",
				result.diagnostics(),
				"Selected branch: public"
			);
			assertContains(
				"diagnostics storage",
				result.diagnostics(),
				"Available storage bytes: 123456"
			);
			assertContains(
				"diagnostics runtime-pack state",
				result.diagnostics(),
				"Runtime-pack state"
			);
			assertContains(
				"diagnostics runtime-pack usability",
				result.diagnostics(),
				"Runtime pack usable: false"
			);
			assertContains(
				"diagnostics cache state",
				result.diagnostics(),
				"Cache state"
			);
			assertContains(
				"diagnostics active cache",
				result.diagnostics(),
				"Active cache:"
			);
			assertContains(
				"diagnostics original operation",
				result.diagnostics(),
				"Original failed operation: copy packaged assembly"
			);
			assertContains(
				"diagnostics original exception",
				result.diagnostics(),
				"Original exception: java.io.IOException: forced asset read failure"
			);
			assertContains(
				"diagnostics retry result",
				result.diagnostics(),
				"Retry result: failed"
			);
			assertContains(
				"diagnostics retry operation",
				result.diagnostics(),
				"Retry failed operation: copy packaged assembly"
			);
			assertNotContains(
				"secondary lifecycle exception absent",
				result.diagnostics(),
				"SuperNotCalledException"
			);
			assertEquals(
				"fatal phase recorded",
				1,
				timeline.count("native assembly setup fatal")
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testValidatedReplacementRemovesStaleFiles()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-stale-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			assertTrue(
				"initial preparation succeeds",
				prepare(environment).isSuccess()
			);
			Path active = publishDirectory(root);
			Path stale = active.resolve("removed-from-new-cache.dll");
			Files.writeString(stale, "stale", StandardCharsets.UTF_8);
			environment.setAssetText(
				"STS2Mobile.dll",
				"updated-launcher-assembly"
			);

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertTrue("replacement succeeds", result.isSuccess());
			assertFalse(
				"stale active file removed by clean replacement",
				Files.exists(stale)
			);
			assertEquals(
				"updated assembly promoted",
				"updated-launcher-assembly",
				Files.readString(
					active.resolve("STS2Mobile.dll"),
					StandardCharsets.UTF_8
				)
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testPartialCopyPreservesExistingValidCache()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-partial-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			assertTrue(
				"initial preparation succeeds",
				prepare(environment).isSuccess()
			);
			Map<String, String> original = readRequiredAssemblies(root);
			AndroidAssemblyBootstrapper.CacheState originalState =
				environment.cacheState;
			environment.setAssetText("STS2Mobile.dll", "uncommitted-update");
			environment.remainingAssetFailures = Integer.MAX_VALUE;

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertFalse("partial preparation fails", result.isSuccess());
			assertRequiredAssembliesEqual(
				"partial copy preserved active cache",
				root,
				original
			);
			assertFalse(
				"failed staging removed",
				Files.exists(stagingDirectory(root))
			);
			assertFalse(
				"no backup left behind",
				Files.exists(backupDirectory(root))
			);
			assertEquals(
				"valid active runtime identity preserved",
				originalState.runtimeId,
				environment.cacheState.runtimeId
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testInterruptedReplacementRestoresBackup()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-recovery-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			assertTrue(
				"initial preparation succeeds",
				prepare(environment).isSuccess()
			);
			Map<String, String> original = readRequiredAssemblies(root);
			Files.move(publishDirectory(root), backupDirectory(root));
			Files.createDirectories(stagingDirectory(root));
			Files.writeString(
				stagingDirectory(root).resolve("partial.dll"),
				"partial",
				StandardCharsets.UTF_8
			);

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertTrue("interrupted replacement recovered", result.isSuccess());
			assertRequiredAssembliesEqual(
				"backup restored as active cache",
				root,
				original
			);
			assertFalse(
				"interrupted staging discarded",
				Files.exists(stagingDirectory(root))
			);
			assertFalse(
				"restored backup consumed",
				Files.exists(backupDirectory(root))
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testFailedPromotionsPreserveExistingValidCache()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-promotion-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			assertTrue(
				"initial preparation succeeds",
				prepare(environment).isSuccess()
			);
			Map<String, String> original = readRequiredAssemblies(root);
			environment.setAssetText("STS2Mobile.dll", "replacement");
			environment.remainingStagingPromotionFailures = Integer.MAX_VALUE;

			AndroidAssemblyBootstrapper.Result result = prepare(environment);

			assertFalse("both promotions fail", result.isSuccess());
			assertContains(
				"promotion operation reported",
				result.diagnostics(),
				"Original failed operation: promote staged assembly cache"
			);
			assertContains(
				"promotion path reported",
				result.diagnostics(),
				"Original failed path:"
			);
			assertContains(
				"promotion retry reported",
				result.diagnostics(),
				"Retry failed operation: promote staged assembly cache"
			);
			assertRequiredAssembliesEqual(
				"failed promotions restored active cache",
				root,
				original
			);
			assertFalse(
				"failed promotion staging removed",
				Files.exists(stagingDirectory(root))
			);
			assertFalse(
				"rollback backup restored",
				Files.exists(backupDirectory(root))
			);
		} finally {
			deleteTree(root);
		}
	}

	private static void testInvalidGameAssemblyPreservesExistingValidGameCache()
		throws Exception {
		Path root = Files.createTempDirectory("sts2-assembly-game-");
		try {
			FakeEnvironment environment = FakeEnvironment.create(root);
			Path sourceDirectory = environment.makePublicGameReady();
			assertTrue(
				"initial game preparation succeeds",
				prepare(environment, true).isSuccess()
			);
			String originalActiveAssembly = Files.readString(
				publishDirectory(root).resolve("Steamworks.NET.dll"),
				StandardCharsets.UTF_8
			);
			Files.write(
				sourceDirectory.resolve("Steamworks.NET.dll"),
				new byte[0]
			);

			AndroidAssemblyBootstrapper.Result result =
				prepare(environment, true);

			assertFalse("empty required game assembly rejected", result.isSuccess());
			assertEquals(
				"valid active game assembly preserved",
				originalActiveAssembly,
				Files.readString(
					publishDirectory(root).resolve("Steamworks.NET.dll"),
					StandardCharsets.UTF_8
				)
			);
			assertFalse(
				"invalid game staging removed",
				Files.exists(stagingDirectory(root))
			);
		} finally {
			deleteTree(root);
		}
	}

	private static AndroidAssemblyBootstrapper.Result prepare(
		FakeEnvironment environment
	) {
		return prepare(environment, false);
	}

	private static AndroidAssemblyBootstrapper.Result prepare(
		FakeEnvironment environment,
		boolean pendingGameLaunch
	) {
		return new AndroidAssemblyBootstrapper(
			environment,
			pendingGameLaunch,
			new RecordingTimeline()
		).prepare();
	}

	private static Map<String, String> readRequiredAssemblies(Path root)
		throws IOException {
		Map<String, String> contents = new LinkedHashMap<>();
		for (String name : REQUIRED_ASSEMBLIES) {
			contents.put(
				name,
				Files.readString(
					publishDirectory(root).resolve(name),
					StandardCharsets.UTF_8
				)
			);
		}
		return contents;
	}

	private static void assertRequiredAssembliesEqual(
		String label,
		Path root,
		Map<String, String> expected
	) throws IOException {
		Map<String, String> actual = readRequiredAssemblies(root);
		assertEquals(label, expected, actual);
	}

	private static Path publishDirectory(Path root) {
		return root.resolve(".godot/mono/publish/arm64");
	}

	private static Path stagingDirectory(Path root) {
		return publishDirectory(root).resolveSibling("arm64.staging");
	}

	private static Path backupDirectory(Path root) {
		return publishDirectory(root).resolveSibling("arm64.backup");
	}

	private static void deleteTree(Path root) throws IOException {
		if (!Files.exists(root)) {
			return;
		}
		try (var paths = Files.walk(root)) {
			paths
				.sorted(Comparator.reverseOrder())
				.map(Path::toFile)
				.forEach(File::delete);
		}
	}

	private static void assertTrue(String label, boolean value) {
		if (!value) {
			throw new AssertionError(label + ": expected true");
		}
	}

	private static void assertFalse(String label, boolean value) {
		if (value) {
			throw new AssertionError(label + ": expected false");
		}
	}

	private static void assertEquals(
		String label,
		Object expected,
		Object actual
	) {
		if (!expected.equals(actual)) {
			throw new AssertionError(
				label + ": expected=" + expected + " actual=" + actual
			);
		}
	}

	private static void assertContains(
		String label,
		String actual,
		String expected
	) {
		if (!actual.contains(expected)) {
			throw new AssertionError(
				label + ": expected to contain " + expected
					+ " but was " + actual
			);
		}
	}

	private static void assertNotContains(
		String label,
		String actual,
		String unexpected
	) {
		if (actual.contains(unexpected)) {
			throw new AssertionError(
				label + ": did not expect to contain " + unexpected
					+ " but was " + actual
			);
		}
	}

	private static final class RecordingTimeline
		implements AndroidAssemblyBootstrapper.Timeline {
		private final List<String> phases = new ArrayList<>();

		@Override
		public void record(String phase, String detail) {
			phases.add(phase);
		}

		int count(String expected) {
			int count = 0;
			for (String phase : phases) {
				if (phase.equals(expected)) {
					count++;
				}
			}
			return count;
		}
	}

	private static final class FakeEnvironment
		implements AndroidAssemblyBootstrapper.Environment {
		private final Path root;
		private final Path game;
		private final Map<String, byte[]> assets;
		private final List<String> listedAssets;
		private AndroidAssemblyBootstrapper.CacheState cacheState =
			AndroidAssemblyBootstrapper.CacheState.empty();
		private int remainingAssetFailures;
		private int remainingStagingPromotionFailures;
		private long availableStorageBytes = 123_456L;

		private FakeEnvironment(
			Path root,
			Path game,
			Map<String, byte[]> assets
		) {
			this.root = root;
			this.game = game;
			this.assets = assets;
			this.listedAssets = new ArrayList<>();
			for (String path : assets.keySet()) {
				listedAssets.add(path.substring("dotnet_bcl/".length()));
			}
		}

		static FakeEnvironment create(Path root) throws IOException {
			Path game = Files.createDirectories(root.resolve("game"));
			Map<String, byte[]> assets = new LinkedHashMap<>();
			for (String name : REQUIRED_ASSEMBLIES) {
				assets.put(
					"dotnet_bcl/" + name,
					("asset-" + name).getBytes(StandardCharsets.UTF_8)
				);
			}
			return new FakeEnvironment(root, game, assets);
		}

		String assetText(String name) {
			return new String(
				assets.get("dotnet_bcl/" + name),
				StandardCharsets.UTF_8
			);
		}

		void setAssetText(String name, String contents) {
			assets.put(
				"dotnet_bcl/" + name,
				contents.getBytes(StandardCharsets.UTF_8)
			);
		}

		void omitListedAsset(String name) {
			listedAssets.remove(name);
		}

		Path makePublicGameReady() throws IOException {
			byte[] pck = new byte[96];
			pck[0] = 0x47;
			pck[1] = 0x44;
			pck[2] = 0x50;
			pck[3] = 0x43;
			writeLongLittleEndian(pck, 32, 80L);
			pck[80] = 1;
			Files.write(game.resolve("SlayTheSpire2.pck"), pck);

			Path source = Files.createDirectories(game.resolve("data_android"));
			for (String name : GAME_REQUIRED_ASSEMBLIES) {
				Files.writeString(
					source.resolve(name),
					"game-" + name,
					StandardCharsets.UTF_8
				);
			}
			return source;
		}

		private static void writeLongLittleEndian(
			byte[] destination,
			int offset,
			long value
		) {
			for (int index = 0; index < Long.BYTES; index++) {
				destination[offset + index] =
					(byte) ((value >>> (index * 8)) & 0xff);
			}
		}

		@Override
		public File filesDirectory() {
			return root.toFile();
		}

		@Override
		public File gameDirectory() {
			return game.toFile();
		}

		@Override
		public String selectedBranch() {
			return "public";
		}

		@Override
		public String packageName() {
			return "com.example.test";
		}

		@Override
		public String versionName() {
			return "test-version";
		}

		@Override
		public int versionCode() {
			return 42;
		}

		@Override
		public String runtimeGodotArchDirectory() {
			return "arm64";
		}

		@Override
		public String nativeLibraryDirectory() {
			return "/test/lib/arm64";
		}

		@Override
		public String[] listAssets(String path) {
			return listedAssets.toArray(new String[0]);
		}

		@Override
		public long assetLengthBytes(String path) throws IOException {
			byte[] contents = assets.get(path);
			if (contents == null) {
				throw new IOException("missing fake asset: " + path);
			}
			return contents.length;
		}

		@Override
		public InputStream openAsset(String path) throws IOException {
			if (remainingAssetFailures > 0) {
				remainingAssetFailures--;
				throw new IOException("forced asset read failure");
			}
			byte[] contents = assets.get(path);
			if (contents == null) {
				throw new IOException("missing fake asset: " + path);
			}
			return new ByteArrayInputStream(contents);
		}

		@Override
		public AndroidAssemblyBootstrapper.CacheState readCacheState() {
			return cacheState;
		}

		@Override
		public void writeCacheState(
			AndroidAssemblyBootstrapper.CacheState state
		) {
			cacheState = state;
		}

		@Override
		public boolean renameDirectory(File source, File destination) {
			if (
				source.getName().endsWith(".staging")
					&& destination.toPath().equals(publishDirectory(root))
					&& remainingStagingPromotionFailures > 0
			) {
				remainingStagingPromotionFailures--;
				return false;
			}
			return source.renameTo(destination);
		}

		@Override
		public void writeInternalTextFile(String name, String text)
			throws IOException {
			Files.writeString(
				root.resolve(name),
				text,
				StandardCharsets.UTF_8
			);
		}

		@Override
		public long currentTimeMillis() {
			return 1_000L;
		}

		@Override
		public long usableSpaceBytes() {
			return availableStorageBytes;
		}

		@Override
		public void info(String message) {
		}

		@Override
		public void warn(String message) {
		}

		@Override
		public void warn(String message, Throwable error) {
		}

		@Override
		public void error(String message, Throwable error) {
		}

		@Override
		public String stackTrace(Throwable error) {
			return error.getClass().getName() + ": " + error.getMessage();
		}
	}
}
