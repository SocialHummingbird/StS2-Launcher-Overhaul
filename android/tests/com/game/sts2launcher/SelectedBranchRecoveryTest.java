package com.game.sts2launcher;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;

import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public final class SelectedBranchRecoveryTest {
	private File root;

	@Before
	public void setUp() throws Exception {
		root = Files.createTempDirectory("sts2-native-recovery-").toFile();
	}

	@After
	public void tearDown() throws Exception {
		deleteTree(root);
	}

	@Test
	public void selectedPublicBetaCleanupRemovesOnlyOwnedArtifacts() throws Exception {
		select("PUBLIC-BETA");
		File betaSlot = LauncherArtifactLayout.versionSlotDirectory(root, "public-beta");
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck"), "beta-pck");
		write(new File(LauncherArtifactLayout.downloadStateDirectory(root, "public-beta"), "manifest.bin"), "download");
		write(LauncherArtifactLayout.pckIdentityCacheFile(root, "public-beta"), "cache");
		write(new File(betaSlot, LauncherArtifactLayout.INSTALLATION_STATE_FILE), readyState("public-beta"));
		File pack = LauncherArtifactLayout.runtimePackDirectory(root, "public-beta");
		write(new File(pack, LauncherArtifactLayout.RUNTIME_PACK_COMPATIBILITY_MANIFEST), "bad-pack");
		write(new File(pack.getParentFile(), pack.getName() + ".staging.tx/partial"), "partial");
		write(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE), "{\"branch\":\"public-beta\"}");
		write(new File(root, LauncherArtifactLayout.LAST_RUNTIME_PATCH_VALIDATION_EVIDENCE), "{\"selectedBranch\":\"public-beta\"}");
		write(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE), "Active branch: public-beta\n");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertEquals("public-beta", result.branch());
		assertFalse(betaSlot.exists());
		assertFalse(pack.exists());
		assertFalse(new File(pack.getParentFile(), pack.getName() + ".staging.tx").exists());
		assertFalse(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE).exists());
		assertFalse(new File(root, LauncherArtifactLayout.LAST_RUNTIME_PATCH_VALIDATION_EVIDENCE).exists());
		assertFalse(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE).exists());
	}

	@Test
	public void publicAndOtherBranchesArePreservedDuringBetaCleanup() throws Exception {
		select("public-beta");
		File publicPck = new File(LauncherArtifactLayout.gameDirectory(root, "public"), "SlayTheSpire2.pck");
		File otherPck = new File(LauncherArtifactLayout.gameDirectory(root, "experimental"), "SlayTheSpire2.pck");
		File publicPack = new File(LauncherArtifactLayout.runtimePackDirectory(root, "public"), "sts2.dll");
		File otherPack = new File(LauncherArtifactLayout.runtimePackDirectory(root, "experimental"), "sts2.dll");
		write(publicPck, "public");
		write(otherPck, "other");
		write(publicPack, "public-pack");
		write(otherPack, "other-pack");
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck"), "beta");
		write(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE), "Active branch: public\n");
		write(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE), "{\"branch\":\"public\"}");
		write(new File(root, LauncherArtifactLayout.LAST_RUNTIME_PATCH_VALIDATION_EVIDENCE), "{\"selectedBranch\":\"public\"}");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertEquals("public", read(publicPck));
		assertEquals("other", read(otherPck));
		assertEquals("public-pack", read(publicPack));
		assertEquals("other-pack", read(otherPack));
		assertTrue(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE).isFile());
		assertTrue(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE).isFile());
		assertTrue(new File(root, LauncherArtifactLayout.LAST_RUNTIME_PATCH_VALIDATION_EVIDENCE).isFile());
	}

	@Test
	public void publicCleanupPreservesBetaInstallation() throws Exception {
		select("public");
		File publicPck = new File(LauncherArtifactLayout.gameDirectory(root, "public"), "SlayTheSpire2.pck");
		File betaPck = new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck");
		File betaPack = new File(LauncherArtifactLayout.runtimePackDirectory(root, "public-beta"), "sts2.dll");
		write(publicPck, "public");
		write(betaPck, "beta");
		write(betaPack, "beta-pack");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertFalse(publicPck.exists());
		assertEquals("beta", read(betaPck));
		assertEquals("beta-pack", read(betaPack));
	}

	@Test
	public void savesAndUnrelatedApplicationStateArePreservedByteForByte() throws Exception {
		select("public-beta");
		byte[] saveBytes = new byte[] { 0, 1, 2, (byte) 0xff, 7 };
		byte[] credentialBytes = new byte[] { 9, 8, 7, 0, 6 };
		File save = new File(root, "saves/profile-1.save");
		File credentials = new File(root, "steam_credentials/account.bin");
		write(save, saveBytes);
		write(credentials, credentialBytes);
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck"), "beta");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertArrayEquals(saveBytes, Files.readAllBytes(save.toPath()));
		assertArrayEquals(credentialBytes, Files.readAllBytes(credentials.toPath()));
	}

	@Test
	public void unusableSelectedRuntimePackIsRemovedForRegeneration() throws Exception {
		select("public-beta");
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck"), "beta");
		File pack = LauncherArtifactLayout.runtimePackDirectory(root, "public-beta");
		write(new File(pack, LauncherArtifactLayout.RUNTIME_PACK_COMPATIBILITY_MANIFEST), "{corrupt");
		write(new File(pack, LauncherArtifactLayout.RUNTIME_PACK_VALIDATION_REPORT), "{\"wrong\":true}");
		write(new File(pack, "sts2.dll"), "stale-patched-assembly");
		write(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE), "{truncated");
		write(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE), "owner missing");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertFalse(pack.exists());
		assertFalse(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE).exists());
		assertFalse(new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE).exists());
	}

	@Test
	public void corruptSelectionFailsClosedWithoutDeletingAnyBranch() throws Exception {
		write(LauncherArtifactLayout.selectedBranchFile(root), new byte[] { (byte) 0xc3, 0x28 });
		File publicPck = new File(LauncherArtifactLayout.gameDirectory(root, "public"), "SlayTheSpire2.pck");
		File betaPck = new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck");
		write(publicPck, "public");
		write(betaPck, "beta");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertFalse(result.summary(), result.succeeded());
		assertEquals("public", read(publicPck));
		assertEquals("beta", read(betaPck));
	}

	@Test
	public void repeatedCleanupIsIdempotent() throws Exception {
		select("public-beta");
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck"), "beta");

		SelectedBranchRecovery.Result first = SelectedBranchRecovery.recover(root);
		SelectedBranchRecovery.Result second = SelectedBranchRecovery.recover(root);

		assertTrue(first.summary(), first.succeeded());
		assertTrue(second.summary(), second.succeeded());
		assertTrue(LauncherArtifactLayout.selectedBranchFile(root).isFile());
	}

	@Test
	public void updatingSelectedBranchIsRecoveredWithoutBecomingReady() throws Exception {
		select("public-beta");
		File state = LauncherArtifactLayout.installationStateFile(root, "public-beta");
		write(
			state,
			"{\"schemaVersion\":1,\"branch\":\"public-beta\",\"status\":\"updating\""
				+ ",\"transactionId\":\"22222222-2222-2222-2222-222222222222\""
				+ ",\"phase\":\"replacing-files\",\"startedUtc\":\"2026-01-01T00:00:00.0000000Z\""
				+ ",\"targetDepots\":[],\"lastError\":\"interrupted\"}"
		);
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "partial.tmp"), "partial");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertFalse(state.exists());
		assertFalse(LauncherArtifactLayout.versionSlotDirectory(root, "public-beta").exists());
	}

	@Test
	public void missingSelectionRecoversOnlyDetectableLegacyPublicInstall() throws Exception {
		File publicPck = new File(LauncherArtifactLayout.gameDirectory(root, "public"), "SlayTheSpire2.pck");
		File betaPck = new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck");
		File betaPack = new File(LauncherArtifactLayout.runtimePackDirectory(root, "public-beta"), "sts2.dll");
		write(publicPck, "legacy-public");
		write(betaPck, "beta");
		write(betaPack, "beta-pack");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertTrue(result.usedLegacySelection());
		assertEquals("public", result.branch());
		assertFalse(publicPck.exists());
		assertEquals("beta", read(betaPck));
		assertEquals("beta-pack", read(betaPack));
	}

	@Test
	public void recoveryNeverPerformsBroadApplicationDataDeletion() throws Exception {
		select("public-beta");
		File godotCache = new File(root, ".godot/mono/publish/arm64/known-good.dll");
		File saves = new File(root, "saves/slot.save");
		File workshop = new File(root, "workshop/mod.bin");
		File arbitrary = new File(root, "application-settings.json");
		File otherBranch = new File(LauncherArtifactLayout.gameDirectory(root, "nightly"), "SlayTheSpire2.pck");
		write(godotCache, "active-cache");
		write(saves, "save");
		write(workshop, "mod");
		write(arbitrary, "settings");
		write(otherBranch, "nightly");
		write(new File(LauncherArtifactLayout.gameDirectory(root, "public-beta"), "SlayTheSpire2.pck"), "beta");

		SelectedBranchRecovery.Result result = SelectedBranchRecovery.recover(root);

		assertTrue(result.summary(), result.succeeded());
		assertEquals("active-cache", read(godotCache));
		assertEquals("save", read(saves));
		assertEquals("mod", read(workshop));
		assertEquals("settings", read(arbitrary));
		assertEquals("nightly", read(otherBranch));
	}

	private void select(String branch) throws IOException {
		write(LauncherArtifactLayout.selectedBranchFile(root), branch);
	}

	private static String readyState(String branch) {
		String hash = "a".repeat(64);
		return "{\"schemaVersion\":1,\"branch\":\"" + branch
			+ "\",\"status\":\"ready\",\"transactionId\":\"11111111-1111-1111-1111-111111111111\""
			+ ",\"completedUtc\":\"2026-01-01T00:00:00.0000000Z\",\"depots\":[],\"pckPreparationVersion\":\"1\""
			+ ",\"gameIdentity\":{\"schemaVersion\":1,\"branch\":\"" + branch
			+ "\",\"installGeneration\":\"" + hash + "\",\"pckSha256\":\"" + hash
			+ "\",\"sourceAssemblySha256\":\"" + hash + "\"},\"runtimePack\":null}";
	}

	private static void write(File file, String value) throws IOException {
		write(file, value.getBytes(StandardCharsets.UTF_8));
	}

	private static void write(File file, byte[] value) throws IOException {
		File parent = file.getParentFile();
		if (parent != null && !parent.isDirectory() && !parent.mkdirs() && !parent.isDirectory()) {
			throw new IOException("Could not create " + parent);
		}
		Files.write(file.toPath(), value);
	}

	private static String read(File file) throws IOException {
		return new String(Files.readAllBytes(file.toPath()), StandardCharsets.UTF_8);
	}

	private static void deleteTree(File target) throws IOException {
		if (target == null || !target.exists()) {
			return;
		}
		File[] children = target.listFiles();
		if (children != null) {
			for (File child : children) {
				deleteTree(child);
			}
		}
		Files.deleteIfExists(target.toPath());
	}
}
