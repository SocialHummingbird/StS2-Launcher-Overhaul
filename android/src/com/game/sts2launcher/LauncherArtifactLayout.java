package com.game.sts2launcher;

import java.io.File;

/**
 * Native view of launcher-owned storage. These names are the Java counterpart
 * of SteamGameInstallPaths and the managed runtime-evidence owners. Native
 * recovery must resolve every destructive target through this class.
 */
final class LauncherArtifactLayout {
	static final String SELECTED_BRANCH_FILE = "game_branch";
	static final String GAME_DIRECTORY = "game";
	static final String GAME_VERSIONS_DIRECTORY = "game_versions";
	static final String DOWNLOAD_STATE_DIRECTORY = "download_state";
	static final String BRANCH_MARKER_FILE = "steam_branch.txt";
	static final String INSTALLATION_STATE_FILE = "installation_state.json";
	static final String INSTALLATION_STATE_LOCK_FILE = "installation_state.lock";
	static final String PCK_IDENTITY_CACHE_FILE = "pck_identity_cache.json";

	static final String RUNTIME_PACKS_DIRECTORY = "runtime_packs";
	static final String RUNTIME_PACK_COMPATIBILITY_MANIFEST = "compatibility.json";
	static final String RUNTIME_PACK_VALIDATION_REPORT = "patch_validation.json";
	static final String CURRENT_RUNTIME_SLOT_EVIDENCE = "current_runtime_slot.json";
	static final String CURRENT_RUNTIME_CACHE_EVIDENCE = "current_runtime_cache.txt";
	static final String LAST_RUNTIME_PATCH_VALIDATION_EVIDENCE =
		"last_runtime_patch_validation.json";

	static final String LEGACY_GAME_PATCH_VALIDATION =
		".android_patch_validation.json";
	static final String LEGACY_PCK_PATCH_MARKER_PREFIX =
		".android_pck_patch_v";
	static final String LEGACY_REDOWNLOAD_EVIDENCE =
		"last_game_version_redownload.txt";
	static final String LEGACY_CACHE_CLEANUP_EVIDENCE =
		"last_game_version_cache_cleanup.txt";

	private LauncherArtifactLayout() {
	}

	static File selectedBranchFile(File filesDirectory) {
		return new File(filesDirectory, SELECTED_BRANCH_FILE);
	}

	static File versionSlotDirectory(File filesDirectory, String branch) {
		return SteamBranchInfo.installSlotDirectory(filesDirectory, branch);
	}

	static File gameDirectory(File filesDirectory, String branch) {
		return new File(versionSlotDirectory(filesDirectory, branch), GAME_DIRECTORY);
	}

	static File downloadStateDirectory(File filesDirectory, String branch) {
		return new File(
			versionSlotDirectory(filesDirectory, branch),
			DOWNLOAD_STATE_DIRECTORY
		);
	}

	static File installationStateFile(File filesDirectory, String branch) {
		return new File(
			versionSlotDirectory(filesDirectory, branch),
			INSTALLATION_STATE_FILE
		);
	}

	static File installationStateLockFile(File filesDirectory, String branch) {
		return new File(
			versionSlotDirectory(filesDirectory, branch),
			INSTALLATION_STATE_LOCK_FILE
		);
	}

	static File pckIdentityCacheFile(File filesDirectory, String branch) {
		return new File(
			versionSlotDirectory(filesDirectory, branch),
			PCK_IDENTITY_CACHE_FILE
		);
	}

	static File runtimePacksDirectory(File filesDirectory) {
		return new File(filesDirectory, RUNTIME_PACKS_DIRECTORY);
	}

	static File runtimePackDirectory(File filesDirectory, String branch) {
		return new File(
			runtimePacksDirectory(filesDirectory),
			SteamBranchInfo.stateDirectoryName(branch)
		);
	}
}
