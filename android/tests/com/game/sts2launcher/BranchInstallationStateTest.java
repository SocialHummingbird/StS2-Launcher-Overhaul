package com.game.sts2launcher;

import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

import org.junit.Test;

import java.io.File;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;

public final class BranchInstallationStateTest {
	private static final String SHA256 =
		"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

	@Test
	public void missingManagedPckPreparationEvidenceIsRejected() throws Exception {
		File filesDirectory = Files.createTempDirectory("sts2-state-").toFile();
		writeState(filesDirectory, "");

		BranchInstallationState.Result result =
			BranchInstallationState.inspect(filesDirectory, "public");

		assertFalse(result.isReady());
		assertTrue(result.summary(), result.summary().contains("evidence is missing"));
		assertTrue(result.summary(), result.summary().contains("Update selected version"));
	}

	@Test
	public void corruptManagedPckPreparationEvidenceIsRejected() throws Exception {
		File filesDirectory = Files.createTempDirectory("sts2-state-").toFile();
		writeState(filesDirectory, ",\"pckPreparationVersion\":37");

		BranchInstallationState.Result result =
			BranchInstallationState.inspect(filesDirectory, "public");

		assertFalse(result.isReady());
		assertTrue(result.summary(), result.summary().contains("evidence is missing"));
		assertTrue(result.summary(), result.summary().contains("android-pck-v2"));
	}

	@Test
	public void staleManagedPckPreparationEvidenceIsRejected() throws Exception {
		File filesDirectory = Files.createTempDirectory("sts2-state-").toFile();
		writeState(
			filesDirectory,
			",\"pckPreparationVersion\":\"android-pck-v1\""
		);

		BranchInstallationState.Result result =
			BranchInstallationState.inspect(filesDirectory, "public");

		assertFalse(result.isReady());
		assertTrue(result.summary(), result.summary().contains("evidence is missing"));
		assertTrue(result.summary(), result.summary().contains("Update selected version"));
	}

	@Test
	public void currentManagedPckPreparationEvidenceIsAccepted() throws Exception {
		File filesDirectory = Files.createTempDirectory("sts2-state-").toFile();
		writeState(
			filesDirectory,
			",\"pckPreparationVersion\":\"android-pck-v2\""
		);

		BranchInstallationState.Result result =
			BranchInstallationState.inspect(filesDirectory, "public");

		assertTrue(result.summary(), result.isReady());
	}

	private static void writeState(File filesDirectory, String preparationEvidence)
		throws Exception {
		File stateFile = LauncherArtifactLayout.installationStateFile(
			filesDirectory,
			"public"
		);
		Files.createDirectories(stateFile.getParentFile().toPath());
		String state = "{\"schemaVersion\":1,\"branch\":\"public\",\"status\":\"ready\""
			+ preparationEvidence
			+ ",\"gameIdentity\":{\"schemaVersion\":1,\"branch\":\"public\""
			+ ",\"installGeneration\":\"" + SHA256 + "\""
			+ ",\"pckSha256\":\"" + SHA256 + "\""
			+ ",\"sourceAssemblySha256\":\"" + SHA256 + "\"}}";
		Files.write(stateFile.toPath(), state.getBytes(StandardCharsets.UTF_8));
	}
}
