package com.game.sts2launcher;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.security.MessageDigest;
import java.util.HashMap;
import java.util.Map;

/** A launch-local snapshot. Hashing and PCK inspection run before creating Godot's activity. */
final class AndroidPreparedGameFiles {
	private final File gameDirectory;
	private final String branch;
	private final File pck;
	private final Map<File, HashedFile> hashes = new HashMap<>();
	private ManagedPckPreparationValidator.Result preparation;

	private AndroidPreparedGameFiles(File gameDirectory, String branch) {
		this.gameDirectory = gameDirectory.getAbsoluteFile();
		this.branch = branch;
		this.pck = new File(gameDirectory, "SlayTheSpire2.pck");
	}

	static AndroidPreparedGameFiles prepare(File gameDirectory, String branch, boolean x86) {
		AndroidPreparedGameFiles result = new AndroidPreparedGameFiles(gameDirectory, branch);
		result.hash(result.pck);
		result.hash(new File(gameDirectory, LauncherArtifactLayout.BRANCH_MARKER_FILE));
		File[] directories = gameDirectory.listFiles();
		if (directories != null) {
			for (File directory : directories) {
				if (directory.isDirectory() && directory.getName().startsWith("data_")) {
					result.hash(new File(directory, "sts2.dll"));
				}
			}
		}
		result.preparation = ManagedPckPreparationValidator.inspect(result.pck, x86);
		return result;
	}

	boolean matches(File gameDirectory, String branch) {
		return this.gameDirectory.equals(gameDirectory.getAbsoluteFile())
			&& this.branch.equalsIgnoreCase(branch);
	}

	String sha256(File file) {
		HashedFile hashed = hashes.get(file.getAbsoluteFile());
		return hashed != null && hashed.unchanged(file) ? hashed.sha256 : "";
	}

	ManagedPckPreparationValidator.Result pckPreparation() {
		return sha256(pck).isEmpty()
			? ManagedPckPreparationValidator.Result.invalid("selected game PCK changed after launch preparation")
			: preparation;
	}

	private void hash(File file) {
		if (!file.isFile()) {
			return;
		}
		long length = file.length();
		long modified = file.lastModified();
		try (FileInputStream input = new FileInputStream(file)) {
			MessageDigest digest = MessageDigest.getInstance("SHA-256");
			byte[] buffer = new byte[256 * 1024];
			int count;
			while ((count = input.read(buffer)) != -1) {
				digest.update(buffer, 0, count);
			}
			StringBuilder hex = new StringBuilder(64);
			for (byte value : digest.digest()) {
				hex.append(Character.forDigit((value & 0xff) >>> 4, 16));
				hex.append(Character.forDigit(value & 0xf, 16));
			}
			HashedFile hashed = new HashedFile(length, modified, hex.toString());
			if (hashed.unchanged(file)) {
				hashes.put(file.getAbsoluteFile(), hashed);
			}
		} catch (IOException | java.security.NoSuchAlgorithmException error) {
			// Missing/unreadable evidence fails closed; never rehash it on the UI thread.
		}
	}

	private static final class HashedFile {
		final long length;
		final long modified;
		final String sha256;

		HashedFile(long length, long modified, String sha256) {
			this.length = length;
			this.modified = modified;
			this.sha256 = sha256;
		}

		boolean unchanged(File file) {
			return file.isFile() && file.length() == length && file.lastModified() == modified;
		}
	}
}
