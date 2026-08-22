package com.game.sts2launcher;

import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;

final class BranchInstallationState {
	private static final int SCHEMA_VERSION = 1;
	private static final int MAX_BYTES = 256 * 1024;

	private BranchInstallationState() {
	}

	static Result inspect(File filesDirectory, String branch) {
		String normalizedBranch = normalizeBranch(branch);
		File stateFile = LauncherArtifactLayout.installationStateFile(
			filesDirectory,
			normalizedBranch
		);
		if (!stateFile.isFile()) {
			return Result.blocked(stateFile, "installation state is missing");
		}

		try {
			JSONObject state = new JSONObject(readSmallFile(stateFile));
			if (state.optInt("schemaVersion", -1) != SCHEMA_VERSION) {
				return Result.blocked(stateFile, "installation state schema is missing or unknown");
			}
			if (!normalizedBranch.equals(state.optString("branch", ""))) {
				return Result.blocked(stateFile, "installation state belongs to another branch");
			}
			if (!"ready".equals(state.optString("status", ""))) {
				return Result.blocked(
					stateFile,
					"installation state is " + state.optString("status", "unknown")
				);
			}

			JSONObject identity = state.optJSONObject("gameIdentity");
			if (
				identity == null
					|| identity.optInt("schemaVersion", -1) != 1
					|| !normalizedBranch.equals(identity.optString("branch", ""))
					|| !isSha256(identity.optString("installGeneration", ""))
					|| !isSha256(identity.optString("pckSha256", ""))
					|| !isSha256(identity.optString("sourceAssemblySha256", ""))
			) {
				return Result.blocked(stateFile, "ready installation state has an incomplete game identity");
			}

			return Result.ready(
				stateFile,
				normalizedBranch,
				identity.optString("installGeneration", ""),
				identity.optString("pckSha256", ""),
				identity.optString("sourceAssemblySha256", "")
			);
		} catch (Exception error) {
			return Result.blocked(
				stateFile,
				"installation state is unreadable: " + error.getClass().getSimpleName()
			);
		}
	}

	private static String readSmallFile(File file) throws IOException {
		if (file.length() < 1 || file.length() > MAX_BYTES) {
			throw new IOException("invalid installation-state size " + file.length());
		}

		try (
			FileInputStream input = new FileInputStream(file);
			ByteArrayOutputStream output = new ByteArrayOutputStream((int) file.length())
		) {
			byte[] buffer = new byte[8192];
			int total = 0;
			while (true) {
				int read = input.read(buffer);
				if (read < 0) {
					break;
				}
				total += read;
				if (total > MAX_BYTES) {
					throw new IOException("installation state exceeds size limit");
				}
				output.write(buffer, 0, read);
			}
			return output.toString("UTF-8");
		}
	}

	private static String normalizeBranch(String branch) {
		return SteamBranchInfo.storageIdentity(branch);
	}

	private static boolean isSha256(String value) {
		if (value == null || value.length() != 64) {
			return false;
		}
		for (int index = 0; index < value.length(); index++) {
			char character = value.charAt(index);
			if (
				(character < '0' || character > '9')
					&& (character < 'a' || character > 'f')
					&& (character < 'A' || character > 'F')
			) {
				return false;
			}
		}
		return true;
	}

	static final class Result {
		private final File path;
		private final boolean ready;
		private final String problem;
		private final String branch;
		private final String installGeneration;
		private final String pckSha256;
		private final String sourceAssemblySha256;

		private Result(
			File path,
			boolean ready,
			String problem,
			String branch,
			String installGeneration,
			String pckSha256,
			String sourceAssemblySha256
		) {
			this.path = path;
			this.ready = ready;
			this.problem = problem;
			this.branch = branch;
			this.installGeneration = installGeneration;
			this.pckSha256 = pckSha256;
			this.sourceAssemblySha256 = sourceAssemblySha256;
		}

		static Result ready(
			File path,
			String branch,
			String installGeneration,
			String pckSha256,
			String sourceAssemblySha256
		) {
			return new Result(
				path,
				true,
				"",
				branch,
				installGeneration,
				pckSha256,
				sourceAssemblySha256
			);
		}

		static Result blocked(File path, String problem) {
			return new Result(path, false, problem, "", "", "", "");
		}

		boolean isReady() {
			return ready;
		}

		boolean matchesGameIdentity(
			String expectedBranch,
			String expectedInstallGeneration,
			String expectedPckSha256,
			String expectedSourceAssemblySha256
		) {
			return ready
				&& branch.equals(SteamBranchInfo.storageIdentity(expectedBranch))
				&& installGeneration.equalsIgnoreCase(expectedInstallGeneration)
				&& pckSha256.equalsIgnoreCase(expectedPckSha256)
				&& sourceAssemblySha256.equalsIgnoreCase(
					expectedSourceAssemblySha256
				);
		}

		String summary() {
			return ready
				? "ready at " + path.getAbsolutePath()
				: problem + " at " + path.getAbsolutePath();
		}
	}
}
