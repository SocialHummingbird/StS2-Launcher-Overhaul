package com.game.sts2launcher;

import java.io.File;
import java.util.Locale;

final class SteamBranchInfo {
	private static final String PUBLIC_BRANCH = "public";
	private static final String BETA_BRANCH = "beta";

	private SteamBranchInfo() {
	}

	static String selectorHelpText(String branch) {
		String normalized = normalize(branch);
		if (PUBLIC_BRANCH.equalsIgnoreCase(normalized)) {
			return "Default/public Steam branch. Choose a game version from the dropdown. Account-visible branch options refresh after Steam app-info is available; beta password entry is still being hardened.";
		}

		return "Steam branch '" + normalized + "' selected from the game version dropdown. Private/password-protected branches may be inaccessible because beta password entry is not supported. Failed downloads do not change Steam Cloud saves. Save compatibility is unproven.";
	}

	static String installSlotKind(String branch) {
		return PUBLIC_BRANCH.equalsIgnoreCase(normalize(branch)) ? "public legacy" : "side-by-side branch cache";
	}

	static File installSlotDirectory(File filesDir, String branch) {
		if (PUBLIC_BRANCH.equalsIgnoreCase(normalize(branch))) {
			return filesDir;
		}
		return new File(new File(filesDir, "game_versions"), stateDirectoryName(branch));
	}

	static File gameDirectory(File filesDir, String branch) {
		return new File(installSlotDirectory(filesDir, branch), "game");
	}

	static String stateDirectoryName(String branch) {
		String normalized = storageIdentity(branch);
		if (PUBLIC_BRANCH.equalsIgnoreCase(normalized)) {
			return PUBLIC_BRANCH;
		}
		if (BETA_BRANCH.equalsIgnoreCase(normalized)) {
			return BETA_BRANCH;
		}

		StringBuilder safe = new StringBuilder();
		for (int i = 0; i < normalized.length(); i++) {
			char ch = normalized.charAt(i);
			if (Character.isLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') {
				safe.append(ch);
			} else {
				safe.append('_');
			}
		}

		String safePrefix = safe.length() == 0 ? "branch" : safe.toString();
		if (safePrefix.length() > 48) {
			safePrefix = trimTrailingUnsafe(safePrefix.substring(0, 48));
		}
		if (safePrefix.length() == 0) {
			safePrefix = "branch";
		}

		return safePrefix + "-" + stableBranchHash(normalized);
	}

	private static String storageIdentity(String branch) {
		return normalize(branch).toLowerCase(Locale.ROOT);
	}

	private static String normalize(String branch) {
		if (branch == null) {
			return PUBLIC_BRANCH;
		}

		String normalized = branch.trim();
		return normalized.isEmpty() ? PUBLIC_BRANCH : normalized;
	}

	private static String trimTrailingUnsafe(String value) {
		int end = value.length();
		while (end > 0) {
			char ch = value.charAt(end - 1);
			if (ch != '.' && ch != '-' && ch != '_') {
				break;
			}
			end--;
		}

		return value.substring(0, end);
	}

	private static String stableBranchHash(String branch) {
		int hash = 0x811c9dc5;
		for (int i = 0; i < branch.length(); i++) {
			hash ^= branch.charAt(i);
			hash *= 0x01000193;
		}

		return String.format(Locale.ROOT, "%08x", hash);
	}
}
