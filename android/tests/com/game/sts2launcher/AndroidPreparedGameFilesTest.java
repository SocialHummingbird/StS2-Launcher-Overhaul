package com.game.sts2launcher;

import org.junit.Test;

import java.io.File;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.attribute.FileTime;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public final class AndroidPreparedGameFilesTest {
	@Test
	public void preparedFilesReuseVerifiedHashesAndPckInspection() throws Exception {
		File game = fixture();
		File source = new File(game, "data_android/sts2.dll");
		AndroidPreparedGameFiles files = AndroidPreparedGameFiles.prepare(game, "public", false);
		assertTrue(files.matches(game, "public"));
		assertTrue(files.pckPreparation().problem(), files.pckPreparation().isValid());
		assertEquals("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", files.sha256(source));
		assertEquals("", files.sha256(new File(game, "unverified.dll")));
		assertFalse(files.matches(game, "public-beta"));
		assertFalse(files.matches(new File(game, "other"), "public"));
	}

	@Test
	public void fileReplacementInvalidatesPreparedHashInsteadOfReadingNewDataOnCaller() throws Exception {
		File game = fixture();
		File source = new File(game, "data_android/sts2.dll");
		AndroidPreparedGameFiles files = AndroidPreparedGameFiles.prepare(game, "public", false);
		Files.write(source.toPath(), "new source bytes".getBytes(StandardCharsets.UTF_8));
		assertEquals("", files.sha256(source));
	}

	@Test
	public void sameSizeEditWithChangedTimestampInvalidatesPreparedHash() throws Exception {
		File game = fixture();
		File source = new File(game, "data_android/sts2.dll");
		long before = source.lastModified();
		AndroidPreparedGameFiles files = AndroidPreparedGameFiles.prepare(game, "public", false);
		Files.write(source.toPath(), "xyz".getBytes(StandardCharsets.UTF_8));
		Files.setLastModifiedTime(source.toPath(), FileTime.fromMillis(before + 2000));
		assertEquals("", files.sha256(source));
	}

	@Test
	public void changedOrMissingPckCannotReuseSuccessfulStructureInspection() throws Exception {
		File game = fixture();
		File pck = new File(game, "SlayTheSpire2.pck");
		AndroidPreparedGameFiles files = AndroidPreparedGameFiles.prepare(game, "public", false);
		Files.write(pck.toPath(), "corrupt replacement".getBytes(StandardCharsets.UTF_8));
		assertFalse(files.pckPreparation().isValid());
		assertEquals("", files.sha256(pck));
		Files.delete(pck.toPath());
		assertFalse(files.pckPreparation().isValid());
	}

	private static File fixture() throws Exception {
		File game = Files.createTempDirectory("sts2-prepared-").toFile();
		File pck = ManagedPckPreparationValidatorTest.writePck(ManagedPckPreparationValidatorTest.arm64Entries());
		Files.copy(pck.toPath(), new File(game, "SlayTheSpire2.pck").toPath());
		File source = new File(game, "data_android/sts2.dll");
		Files.createDirectories(source.toPath().getParent());
		Files.write(source.toPath(), "abc".getBytes(StandardCharsets.UTF_8));
		Files.write(new File(game, LauncherArtifactLayout.BRANCH_MARKER_FILE).toPath(), "Branch: public".getBytes(StandardCharsets.UTF_8));
		return game;
	}
}
