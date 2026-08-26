package com.game.sts2launcher;

import org.json.JSONArray;
import org.json.JSONObject;
import org.junit.Test;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public final class AndroidAssemblyBootstrapperRuntimePackValidationTest {
	@Test
	public void exactManagedSchemaPackIsAcceptedAndReadyStateMismatchIsRejected()
		throws Exception {
		try (Fixture fixture = Fixture.create()) {
			assertTrue(
				"The native bootstrap validator must accept an exact ready N+1 pack.",
				fixture.bootstrapper.isRuntimePackManifestUsableForTesting(
					fixture.runtimePackDirectory
				)
			);

			fixture.writeReadyState(
				fixture.installGeneration,
				zeros(),
				fixture.sourceAssemblySha256
			);
			assertFalse(
				"A syntactically ready state from another generation must fail closed.",
				fixture.bootstrapper.isRuntimePackManifestUsableForTesting(
					fixture.runtimePackDirectory
				)
			);
		}
	}

	@Test
	public void mixedGenerationAndCorruptRuntimePacksFailClosed()
		throws Exception {
		try (Fixture fixture = Fixture.create()) {
			JSONObject report = new JSONObject(
				readText(fixture.validationReport)
			);
			report.put("sourceAssemblySha256", zeros());
			writeText(fixture.validationReport, report.toString(2));
			assertFalse(
				"A mixed-generation report and compatibility manifest must be rejected.",
				fixture.bootstrapper.isRuntimePackManifestUsableForTesting(
					fixture.runtimePackDirectory
				)
			);

			writeText(fixture.validationReport, "{\"schemaVersion\":3");
			assertFalse(
				"A truncated validation report must be rejected.",
				fixture.bootstrapper.isRuntimePackManifestUsableForTesting(
					fixture.runtimePackDirectory
				)
			);
		}
	}

	private static final class Fixture implements AutoCloseable {
		final File root;
		final String branch = "public-beta";
		final File gameDirectory;
		final File runtimePackDirectory;
		final File validationReport;
		final FakeEnvironment environment;
		final AndroidAssemblyBootstrapper bootstrapper;
		String installGeneration;
		String pckSha256;
		String sourceAssemblySha256;

		private Fixture(File root) {
			this.root = root;
			this.gameDirectory = LauncherArtifactLayout.gameDirectory(root, branch);
			this.runtimePackDirectory =
				LauncherArtifactLayout.runtimePackDirectory(root, branch);
			this.validationReport = new File(
				runtimePackDirectory,
				LauncherArtifactLayout.RUNTIME_PACK_VALIDATION_REPORT
			);
			this.environment = new FakeEnvironment(root, gameDirectory, branch);
			this.bootstrapper = new AndroidAssemblyBootstrapper(
				environment,
				true,
				(phase, detail) -> { }
			);
		}

		static Fixture create() throws Exception {
			Fixture fixture = new Fixture(
				Files.createTempDirectory("sts2-native-runtime-pack-test").toFile()
			);
			fixture.seed();
			return fixture;
		}

		private void seed() throws Exception {
			if (!gameDirectory.mkdirs() && !gameDirectory.isDirectory()) {
				throw new IOException("Could not create game fixture directory.");
			}
			File pck = new File(gameDirectory, "SlayTheSpire2.pck");
			byte[] pckBytes = new byte[160];
			ByteBuffer pckBuffer = ByteBuffer.wrap(pckBytes)
				.order(ByteOrder.LITTLE_ENDIAN);
			pckBuffer.putInt(0x43504447);
			pckBuffer.putInt(3);
			pckBuffer.putInt(4);
			pckBuffer.putInt(5);
			pckBuffer.putInt(0);
			pckBuffer.putInt(0);
			pckBuffer.putLong(0);
			pckBuffer.putLong(96);
			pckBuffer.position(96);
			pckBuffer.putInt(1);
			writeBytes(pck, pckBytes);

			File sourceDirectory = new File(
				gameDirectory,
				"data_sts2_windows_x86_64"
			);
			if (!sourceDirectory.mkdirs() && !sourceDirectory.isDirectory()) {
				throw new IOException("Could not create source assembly directory.");
			}
			File sourceAssembly = new File(sourceDirectory, "sts2.dll");
			writeText(sourceAssembly, "authoritative-source-generation-N+1");

			File marker = new File(
				gameDirectory,
				LauncherArtifactLayout.BRANCH_MARKER_FILE
			);
			writeText(
				marker,
				"Branch: " + branch + "\n"
					+ "Install slot kind: "
					+ SteamBranchInfo.installSlotKind(branch) + "\n"
					+ "Install slot directory: "
					+ SteamBranchInfo.installSlotDirectory(root, branch).getAbsolutePath()
					+ "\nDepot manifest count: 1\n"
					+ "Depot manifests matching public count: 0\n"
					+ "Depot manifests differing from public count: 1\n"
					+ "Depot manifests without public comparison count: 0\n"
					+ "Depot manifests inherited from public count: 0\n"
					+ "Depot manifests missing selected branch manifest count: 0\n"
					+ "Depot manifest: depot=123 manifest=1002 branch=public-beta manifestSource=selected\n"
			);

			installGeneration = sha256(marker);
			pckSha256 = sha256(pck);
			sourceAssemblySha256 = sha256(sourceAssembly);
			writeReadyState(
				installGeneration,
				pckSha256,
				sourceAssemblySha256
			);

			if (!runtimePackDirectory.mkdirs()
				&& !runtimePackDirectory.isDirectory()) {
				throw new IOException("Could not create runtime-pack directory.");
			}
			File patchedAssembly = new File(runtimePackDirectory, "sts2.dll");
			writeText(patchedAssembly, "patched-android-generation-N+1");
			String patchedAssemblySha256 = sha256(patchedAssembly);
			String gameIdentityId = gameIdentityId(
				branch,
				installGeneration,
				pckSha256,
				sourceAssemblySha256
			);
			String packId = "public-beta-native-contract-N+1";
			String patchSetVersion =
				"startup-orchestrator-v2-android-harmony-publicizer";
			String validationSurface =
				"critical-startup-save-platform-model-v1";

			JSONObject identity = new JSONObject();
			identity.put("schemaVersion", 1);
			identity.put("branch", branch);
			identity.put("installGeneration", installGeneration);
			identity.put("pckSha256", pckSha256);
			identity.put("sourceAssemblySha256", sourceAssemblySha256);

			JSONObject manifest = new JSONObject();
			manifest.put("schemaVersion", 3);
			manifest.put("packId", packId);
			manifest.put("gameIdentity", identity);
			manifest.put("gameIdentityId", gameIdentityId);
			manifest.put("sourceBranch", branch);
			manifest.put("installGeneration", installGeneration);
			manifest.put("sourcePckSha256", pckSha256);
			manifest.put("sourceAssemblySha256", sourceAssemblySha256);
			manifest.put("androidAssemblySha256", patchedAssemblySha256);
			manifest.put("patchValidationStatus", "passed");
			manifest.put("patchSetVersion", patchSetVersion);
			manifest.put("validationSurfaceVersion", validationSurface);
			manifest.put("generatedFromCleanDirectory", true);
			manifest.put("supportAssemblies", new JSONArray());
			manifest.put("supportAssemblySha256", new JSONObject());
			writeText(
				new File(
					runtimePackDirectory,
					LauncherArtifactLayout.RUNTIME_PACK_COMPATIBILITY_MANIFEST
				),
				manifest.toString(2)
			);

			JSONObject report = new JSONObject();
			report.put("schemaVersion", 3);
			report.put("status", "passed");
			report.put("runtimePackId", packId);
			report.put("gameIdentity", identity);
			report.put("gameIdentityId", gameIdentityId);
			report.put("branch", branch);
			report.put("installGeneration", installGeneration);
			report.put("pckSha256", pckSha256);
			report.put("sourceAssemblySha256", sourceAssemblySha256);
			report.put("androidAssemblySha256", patchedAssemblySha256);
			report.put("patchSetVersion", patchSetVersion);
			report.put("validationSurfaceVersion", validationSurface);
			report.put("generatedFromCleanDirectory", true);
			report.put("supportAssemblies", new JSONArray());
			report.put("supportAssemblySha256", new JSONObject());
			writeText(validationReport, report.toString(2));
		}

		void writeReadyState(
			String generation,
			String pckHash,
			String sourceHash
		) throws Exception {
			JSONObject identity = new JSONObject();
			identity.put("schemaVersion", 1);
			identity.put("branch", branch);
			identity.put("installGeneration", generation);
			identity.put("pckSha256", pckHash);
			identity.put("sourceAssemblySha256", sourceHash);

			JSONObject state = new JSONObject();
			state.put("schemaVersion", 1);
			state.put("branch", branch);
			state.put("status", "ready");
			state.put("transactionId", UUID.randomUUID().toString());
			state.put(
				"pckPreparationVersion",
				BranchInstallationState.REQUIRED_PCK_PREPARATION_VERSION
			);
			state.put("gameIdentity", identity);
			writeText(
				LauncherArtifactLayout.installationStateFile(root, branch),
				state.toString(2)
			);
		}

		@Override
		public void close() {
			deleteTree(root);
		}
	}

	private static final class FakeEnvironment
		implements AndroidAssemblyBootstrapper.Environment {
		private final File filesDirectory;
		private final File gameDirectory;
		private final String branch;
		private final List<String> warnings = new ArrayList<>();

		FakeEnvironment(File filesDirectory, File gameDirectory, String branch) {
			this.filesDirectory = filesDirectory;
			this.gameDirectory = gameDirectory;
			this.branch = branch;
		}

		@Override public File filesDirectory() { return filesDirectory; }
		@Override public File gameDirectory() { return gameDirectory; }
		@Override public String selectedBranch() { return branch; }
		@Override public String packageName() { return "com.game.sts2launcher.test"; }
		@Override public String versionName() { return "test"; }
		@Override public int versionCode() { return 1; }
		@Override public String runtimeGodotArchDirectory() { return "arm64"; }
		@Override public String nativeLibraryDirectory() { return "arm64"; }
		@Override public String[] listAssets(String path) { return new String[0]; }
		@Override public long assetLengthBytes(String path) { return 0; }
		@Override public InputStream openAsset(String path) {
			return new ByteArrayInputStream(new byte[0]);
		}
		@Override public AndroidAssemblyBootstrapper.CacheState readCacheState() {
			return AndroidAssemblyBootstrapper.CacheState.empty();
		}
		@Override public void writeCacheState(AndroidAssemblyBootstrapper.CacheState state) { }
		@Override public boolean renameDirectory(File source, File destination) {
			return source.renameTo(destination);
		}
		@Override public void writeInternalTextFile(String name, String text)
			throws IOException {
			writeText(new File(filesDirectory, name), text);
		}
		@Override public long currentTimeMillis() { return System.currentTimeMillis(); }
		@Override public long usableSpaceBytes() { return Long.MAX_VALUE; }
		@Override public void info(String message) { }
		@Override public void warn(String message) { warnings.add(message); }
		@Override public void warn(String message, Throwable error) {
			warnings.add(message + ": " + error.getMessage());
		}
		@Override public void error(String message, Throwable error) {
			warnings.add(message + ": " + error.getMessage());
		}
		@Override public String stackTrace(Throwable error) { return error.toString(); }
	}

	private static String gameIdentityId(
		String branch,
		String installGeneration,
		String pckSha256,
		String sourceAssemblySha256
	) throws Exception {
		return sha256(
			("game-identity-v1\n"
				+ "branch=" + branch + "\n"
				+ "installGeneration=" + installGeneration + "\n"
				+ "pckSha256=" + pckSha256 + "\n"
				+ "sourceAssemblySha256=" + sourceAssemblySha256)
				.getBytes(StandardCharsets.UTF_8)
		);
	}

	private static String zeros() {
		return new String(new char[64]).replace('\0', '0');
	}

	private static String sha256(File file) throws Exception {
		try (InputStream input = new FileInputStream(file)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[8192];
			int read;
			while ((read = input.read(buffer)) >= 0) {
				digest.update(buffer, 0, read);
			}
			return hex(digest.digest());
		}
	}

	private static String sha256(byte[] bytes) throws Exception {
		return hex(MessageDigest.getInstance("SHA-256").digest(bytes));
	}

	private static String hex(byte[] bytes) {
		StringBuilder value = new StringBuilder(bytes.length * 2);
		for (byte item : bytes) {
			value.append(String.format(java.util.Locale.ROOT, "%02x", item & 0xff));
		}
		return value.toString();
	}

	private static String readText(File file) throws IOException {
		return new String(Files.readAllBytes(file.toPath()), StandardCharsets.UTF_8);
	}

	private static void writeText(File file, String text) throws IOException {
		writeBytes(file, text.getBytes(StandardCharsets.UTF_8));
	}

	private static void writeBytes(File file, byte[] bytes) throws IOException {
		File parent = file.getParentFile();
		if (parent != null && !parent.isDirectory()
			&& !parent.mkdirs() && !parent.isDirectory()) {
			throw new IOException("Could not create " + parent.getAbsolutePath());
		}
		try (FileOutputStream output = new FileOutputStream(file)) {
			output.write(bytes);
		}
	}

	private static void deleteTree(File file) {
		if (file == null || !file.exists()) {
			return;
		}
		File[] children = file.listFiles();
		if (children != null) {
			for (File child : children) {
				deleteTree(child);
			}
		}
		file.delete();
	}
}
