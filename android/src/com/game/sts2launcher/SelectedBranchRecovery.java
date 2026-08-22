package com.game.sts2launcher;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.ByteBuffer;
import java.nio.charset.CharacterCodingException;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.nio.channels.FileChannel;
import java.nio.channels.FileLock;
import java.nio.channels.OverlappingFileLockException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;
import java.util.UUID;

/**
 * The sole native owner of full selected-branch recovery. It deliberately
 * mirrors SelectedBranchRecovery.Execute(..., FullRedownload) on the managed
 * side: readiness is revoked first and only selected-branch artifacts are
 * removed. Saves and arbitrary application data are never enumerated.
 */
final class SelectedBranchRecovery {
	private static final int MAX_SELECTION_BYTES = 4096;
	private static final int MAX_EVIDENCE_BYTES = 256 * 1024;
	private static final String ACTIVE_BRANCH_PREFIX = "Active branch:";
	private static final String SELECTED_BRANCH_PREFIX = "Selected branch:";

	private SelectedBranchRecovery() {
	}

	static Result recover(File filesDirectory) {
		if (filesDirectory == null) {
			return Result.failure("<unknown>", "launcher files directory is missing");
		}

		final File root;
		try {
			root = filesDirectory.getCanonicalFile();
		} catch (IOException error) {
			return Result.failure("<unknown>", "launcher files directory is unreadable: " + error.getMessage());
		}
		if (!root.isDirectory()) {
			return Result.failure("<unknown>", "launcher files directory does not exist");
		}

		Selection selection = readSelection(root);
		if (!selection.valid) {
			return Result.failure(selection.branch, selection.problem);
		}

		Recovery recovery = new Recovery(root, selection.branch, selection.legacy);
		return recovery.run();
	}

	private static Selection readSelection(File root) {
		File selectionFile = LauncherArtifactLayout.selectedBranchFile(root);
		if (!selectionFile.exists()) {
			if (hasLegacyPublicInstallation(root)) {
				return Selection.valid("public", true);
			}
			return Selection.invalid(
				"<missing>",
				"selected Steam branch is missing and no legacy public installation can establish ownership"
			);
		}
		if (!selectionFile.isFile()) {
			return Selection.invalid("<invalid>", "selected Steam branch evidence is not a regular file");
		}

		try {
			byte[] bytes = readSmallFile(selectionFile, MAX_SELECTION_BYTES);
			String raw = decodeUtf8Strict(bytes);
			String branch = raw.trim();
			if (branch.isEmpty()) {
				return Selection.invalid("<empty>", "selected Steam branch evidence is empty");
			}
			for (int index = 0; index < branch.length(); index++) {
				char value = branch.charAt(index);
				if (Character.isISOControl(value)) {
					return Selection.invalid("<invalid>", "selected Steam branch evidence contains control characters");
				}
			}
			return Selection.valid(SteamBranchInfo.storageIdentity(branch), false);
		} catch (Exception error) {
			return Selection.invalid(
				"<unreadable>",
				"selected Steam branch evidence is unreadable: " + error.getClass().getSimpleName()
			);
		}
	}

	private static boolean hasLegacyPublicInstallation(File root) {
		return LauncherArtifactLayout.gameDirectory(root, "public").exists()
			|| LauncherArtifactLayout.downloadStateDirectory(root, "public").exists()
			|| LauncherArtifactLayout.installationStateFile(root, "public").exists()
			|| LauncherArtifactLayout.pckIdentityCacheFile(root, "public").exists()
			|| LauncherArtifactLayout.runtimePackDirectory(root, "public").exists();
	}

	private static byte[] readSmallFile(File file, int maxBytes) throws IOException {
		long length = file.length();
		if (length < 1 || length > maxBytes) {
			throw new IOException("invalid file size " + length);
		}
		try (
			FileInputStream input = new FileInputStream(file);
			ByteArrayOutputStream output = new ByteArrayOutputStream((int) length)
		) {
			byte[] buffer = new byte[4096];
			int total = 0;
			while (true) {
				int read = input.read(buffer);
				if (read < 0) {
					break;
				}
				total += read;
				if (total > maxBytes) {
					throw new IOException("file exceeds size limit");
				}
				output.write(buffer, 0, read);
			}
			return output.toByteArray();
		}
	}

	private static String decodeUtf8Strict(byte[] bytes) throws CharacterCodingException {
		return StandardCharsets.UTF_8.newDecoder()
			.onMalformedInput(CodingErrorAction.REPORT)
			.onUnmappableCharacter(CodingErrorAction.REPORT)
			.decode(ByteBuffer.wrap(bytes))
			.toString();
	}

	private static final class Recovery {
		private final File root;
		private final String branch;
		private final boolean legacySelection;
		private final List<String> failures = new ArrayList<>();
		private int removed;

		Recovery(File root, String branch, boolean legacySelection) {
			this.root = root;
			this.branch = branch;
			this.legacySelection = legacySelection;
		}

		Result run() {
			File slot = LauncherArtifactLayout.versionSlotDirectory(root, branch);
			if (!slot.exists() && !slot.mkdirs() && !slot.isDirectory()) {
				return Result.failure(branch, "could not create the selected branch slot for recovery state");
			}

			File lockPath = LauncherArtifactLayout.installationStateLockFile(root, branch);
			try (
				RandomAccessFile lockFile = new RandomAccessFile(lockPath, "rw");
				FileChannel channel = lockFile.getChannel();
				FileLock lock = tryLock(channel)
			) {
				if (lock == null) {
					return Result.failure(branch, "selected branch is busy updating");
				}

				writeUpdatingState();
				removeSelectedInstallPayload();
				removeDerivedArtifacts();
				removeStateTemporaryFiles();

				// Removing this updating record is last. Earlier interruption or
				// failure leaves the selected branch durably non-launchable.
				if (failures.isEmpty()) {
					deleteFile(LauncherArtifactLayout.installationStateFile(root, branch));
				}
			} catch (IOException | RuntimeException error) {
				failures.add("selected-branch recovery failed: " + error.getMessage());
			}

			if (failures.isEmpty()) {
				removeUnusedSlotScaffolding();
			}
			return new Result(
				failures.isEmpty(),
				branch,
				legacySelection,
				removed,
				new ArrayList<>(failures)
			);
		}

		private FileLock tryLock(FileChannel channel) throws IOException {
			try {
				return channel.tryLock();
			} catch (OverlappingFileLockException error) {
				return null;
			}
		}

		private void writeUpdatingState() throws IOException {
			File state = owned(LauncherArtifactLayout.installationStateFile(root, branch));
			File parent = state.getParentFile();
			if (!parent.isDirectory() && !parent.mkdirs() && !parent.isDirectory()) {
				throw new IOException("could not create installation-state directory");
			}

			String transactionId = UUID.randomUUID().toString();
			byte[] payloadBytes;
			try {
				JSONObject payload = new JSONObject();
				payload.put("schemaVersion", 1);
				payload.put("branch", branch);
				payload.put("status", "updating");
				payload.put("transactionId", transactionId);
				payload.put("phase", "selected-native-recovery");
				payload.put("startedUtc", roundTripUtcNow());
				payload.put("targetDepots", new JSONArray());
				payload.put("lastError", "");
				payloadBytes = payload.toString(2).getBytes(StandardCharsets.UTF_8);
			} catch (JSONException error) {
				throw new IOException("could not serialize updating installation state", error);
			}

			File staging = owned(new File(parent, state.getName() + "." + transactionId + ".tmp"));
			File backup = owned(new File(parent, state.getName() + ".native-recovery.backup"));
			try (FileOutputStream output = new FileOutputStream(staging)) {
				output.write(payloadBytes);
				output.flush();
				output.getFD().sync();
			}

			if (backup.exists() && !backup.delete()) {
				throw new IOException("could not remove stale installation-state backup");
			}
			if (state.exists() && !state.renameTo(backup)) {
				throw new IOException("could not revoke the previous ready installation state");
			}
			if (!staging.renameTo(state)) {
				// Never restore the old ready record: a failed recovery has already
				// begun and must stay fail-closed.
				throw new IOException("could not atomically publish updating installation state");
			}
			if (backup.exists() && !backup.delete()) {
				throw new IOException("could not retire revoked installation-state backup");
			}
		}

		private String roundTripUtcNow() {
			SimpleDateFormat format = new SimpleDateFormat(
				"yyyy-MM-dd'T'HH:mm:ss.SSS'0000Z'",
				Locale.ROOT
			);
			format.setTimeZone(TimeZone.getTimeZone("UTC"));
			return format.format(new Date());
		}

		private void removeSelectedInstallPayload() throws IOException {
			if (!"public".equals(branch)) {
				File slot = owned(LauncherArtifactLayout.versionSlotDirectory(root, branch));
				File[] children = slot.listFiles();
				if (children == null) {
					if (slot.exists()) {
						failures.add("could not enumerate selected branch slot " + slot.getAbsolutePath());
					}
					return;
				}
				for (File child : children) {
					String name = child.getName();
					if (LauncherArtifactLayout.INSTALLATION_STATE_FILE.equals(name)
						|| LauncherArtifactLayout.INSTALLATION_STATE_LOCK_FILE.equals(name)) {
						continue;
					}
					deletePath(child);
				}
				return;
			}

			deleteDirectory(LauncherArtifactLayout.gameDirectory(root, branch));
			deleteDirectory(LauncherArtifactLayout.downloadStateDirectory(root, branch));
			deleteFile(LauncherArtifactLayout.pckIdentityCacheFile(root, branch));
			deleteMatchingFiles(
				LauncherArtifactLayout.versionSlotDirectory(root, branch),
				LauncherArtifactLayout.PCK_IDENTITY_CACHE_FILE + ".",
				".tmp"
			);
		}

		private void removeDerivedArtifacts() throws IOException {
			deleteRuntimePackArtifacts();
			deleteFile(new File(
				LauncherArtifactLayout.gameDirectory(root, branch),
				LauncherArtifactLayout.LEGACY_GAME_PATCH_VALIDATION
			));
			deleteMatchingFiles(
				LauncherArtifactLayout.gameDirectory(root, branch),
				LauncherArtifactLayout.LEGACY_PCK_PATCH_MARKER_PREFIX,
				""
			);

			deleteJsonEvidenceWhenOwnedOrInvalid(
				new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_SLOT_EVIDENCE),
				"branch"
			);
			deleteJsonEvidenceWhenOwnedOrInvalid(
				new File(root, LauncherArtifactLayout.LAST_RUNTIME_PATCH_VALIDATION_EVIDENCE),
				"selectedBranch"
			);
			deleteTextEvidenceWhenOwnedOrInvalid(
				new File(root, LauncherArtifactLayout.CURRENT_RUNTIME_CACHE_EVIDENCE),
				ACTIVE_BRANCH_PREFIX
			);
			deleteTextEvidenceWhenOwnedOrInvalid(
				new File(root, LauncherArtifactLayout.LEGACY_REDOWNLOAD_EVIDENCE),
				SELECTED_BRANCH_PREFIX
			);
			deleteTextEvidenceWhenOwnedOrInvalid(
				new File(root, LauncherArtifactLayout.LEGACY_CACHE_CLEANUP_EVIDENCE),
				SELECTED_BRANCH_PREFIX
			);
		}

		private void deleteRuntimePackArtifacts() throws IOException {
			File finalPack = owned(LauncherArtifactLayout.runtimePackDirectory(root, branch));
			File parent = finalPack.getParentFile();
			if (!parent.isDirectory()) {
				return;
			}
			File[] candidates = parent.listFiles();
			if (candidates == null) {
				failures.add("could not enumerate runtime-pack directory " + parent.getAbsolutePath());
				return;
			}
			String finalName = finalPack.getName();
			for (File candidate : candidates) {
				if (isOwnedRuntimePackArtifact(candidate.getName(), finalName)) {
					deletePath(candidate);
				}
			}
		}

		private boolean isOwnedRuntimePackArtifact(String name, String finalName) {
			return name.equals(finalName)
				|| name.startsWith(finalName + ".staging.")
				|| name.startsWith(finalName + ".backup.")
				|| name.startsWith(finalName + ".rejected.")
				|| name.equals(finalName + ".promotion.lock");
		}

		private void removeStateTemporaryFiles() throws IOException {
			deleteMatchingFiles(
				LauncherArtifactLayout.versionSlotDirectory(root, branch),
				LauncherArtifactLayout.INSTALLATION_STATE_FILE + ".",
				".tmp"
			);
			deleteFile(new File(
				LauncherArtifactLayout.versionSlotDirectory(root, branch),
				LauncherArtifactLayout.INSTALLATION_STATE_FILE + ".native-recovery.backup"
			));
		}

		private void deleteJsonEvidenceWhenOwnedOrInvalid(File evidence, String branchProperty) throws IOException {
			deleteEvidenceWhenOwnedOrInvalid(evidence, candidate -> readJsonOwner(candidate, branchProperty));
			deleteMatchingEvidenceTemporaryFiles(evidence, candidate -> readJsonOwner(candidate, branchProperty));
		}

		private void deleteTextEvidenceWhenOwnedOrInvalid(File evidence, String prefix) throws IOException {
			deleteEvidenceWhenOwnedOrInvalid(evidence, candidate -> readTextOwner(candidate, prefix));
		}

		private void deleteMatchingEvidenceTemporaryFiles(File finalEvidence, OwnerReader ownerReader) throws IOException {
			File parent = finalEvidence.getParentFile();
			File[] candidates = parent.listFiles((directory, name) ->
				name.startsWith(finalEvidence.getName() + ".") && name.endsWith(".tmp")
			);
			if (candidates == null) {
				return;
			}
			for (File candidate : candidates) {
				deleteEvidenceWhenOwnedOrInvalid(candidate, ownerReader);
			}
		}

		private void deleteEvidenceWhenOwnedOrInvalid(File evidence, OwnerReader ownerReader) throws IOException {
			if (!evidence.exists()) {
				return;
			}
			String owner = null;
			try {
				owner = ownerReader.read(evidence);
			} catch (Exception ignored) {
				// Unreadable derived evidence cannot safely authorize any branch.
			}
			if (owner == null || owner.equals(branch)) {
				deleteFile(evidence);
			}
		}

		private String readJsonOwner(File evidence, String branchProperty) throws Exception {
			JSONObject json = new JSONObject(new String(
				readSmallFile(evidence, MAX_EVIDENCE_BYTES),
				StandardCharsets.UTF_8
			));
			if (!json.has(branchProperty) || json.isNull(branchProperty)) {
				return null;
			}
			String value = json.getString(branchProperty).trim();
			return value.isEmpty() ? null : SteamBranchInfo.storageIdentity(value);
		}

		private String readTextOwner(File evidence, String prefix) throws Exception {
			String text = decodeUtf8Strict(readSmallFile(evidence, MAX_EVIDENCE_BYTES));
			for (String line : text.split("\\r?\\n")) {
				if (!line.regionMatches(true, 0, prefix, 0, prefix.length())) {
					continue;
				}
				String value = line.substring(prefix.length()).trim();
				return value.isEmpty() ? null : SteamBranchInfo.storageIdentity(value);
			}
			return null;
		}

		private void deleteMatchingFiles(File directory, String prefix, String suffix) throws IOException {
			File ownedDirectory = ownedAllowRoot(directory);
			if (!ownedDirectory.isDirectory()) {
				return;
			}
			File[] candidates = ownedDirectory.listFiles((parent, name) ->
				name.startsWith(prefix) && name.endsWith(suffix)
			);
			if (candidates == null) {
				failures.add("could not enumerate " + ownedDirectory.getAbsolutePath());
				return;
			}
			for (File candidate : candidates) {
				deleteFile(candidate);
			}
		}

		private void deletePath(File target) throws IOException {
			if (target.isDirectory()) {
				deleteDirectory(target);
			} else {
				deleteFile(target);
			}
		}

		private void deleteDirectory(File directory) throws IOException {
			File target = owned(directory);
			if (!target.exists()) {
				return;
			}
			if (!deleteTree(target)) {
				failures.add("could not remove directory " + target.getAbsolutePath());
				return;
			}
			removed++;
		}

		private boolean deleteTree(File target) throws IOException {
			File checked = owned(target);
			if (checked.isDirectory()) {
				File[] children = checked.listFiles();
				if (children == null) {
					return false;
				}
				for (File child : children) {
					if (!deleteTree(child)) {
						return false;
					}
				}
			}
			return !checked.exists() || checked.delete();
		}

		private void deleteFile(File file) throws IOException {
			File target = owned(file);
			if (!target.exists()) {
				return;
			}
			if (!target.isFile() || !target.delete()) {
				failures.add("could not remove file " + target.getAbsolutePath());
				return;
			}
			removed++;
		}

		private File owned(File file) throws IOException {
			File target = file.getCanonicalFile();
			String rootPath = root.getPath();
			String targetPath = target.getPath();
			if (targetPath.equals(rootPath)
				|| !targetPath.startsWith(rootPath + File.separator)) {
				throw new IOException("refusing recovery outside launcher files directory: " + targetPath);
			}
			return target;
		}

		private File ownedAllowRoot(File file) throws IOException {
			File target = file.getCanonicalFile();
			if (target.equals(root)) {
				return target;
			}
			return owned(target);
		}

		private void removeUnusedSlotScaffolding() {
			File lock = LauncherArtifactLayout.installationStateLockFile(root, branch);
			if (lock.isFile() && lock.delete()) {
				removed++;
			}
			if ("public".equals(branch)) {
				return;
			}
			File slot = LauncherArtifactLayout.versionSlotDirectory(root, branch);
			File[] remaining = slot.listFiles();
			if (remaining != null && remaining.length == 0 && slot.delete()) {
				removed++;
			}
		}
	}

	private interface OwnerReader {
		String read(File file) throws Exception;
	}

	private static final class Selection {
		final boolean valid;
		final String branch;
		final boolean legacy;
		final String problem;

		private Selection(boolean valid, String branch, boolean legacy, String problem) {
			this.valid = valid;
			this.branch = branch;
			this.legacy = legacy;
			this.problem = problem;
		}

		static Selection valid(String branch, boolean legacy) {
			return new Selection(true, branch, legacy, "");
		}

		static Selection invalid(String branch, String problem) {
			return new Selection(false, branch, false, problem);
		}
	}

	static final class Result {
		private final boolean succeeded;
		private final String branch;
		private final boolean legacySelection;
		private final int removedArtifactCount;
		private final List<String> failures;

		private Result(
			boolean succeeded,
			String branch,
			boolean legacySelection,
			int removedArtifactCount,
			List<String> failures
		) {
			this.succeeded = succeeded;
			this.branch = branch;
			this.legacySelection = legacySelection;
			this.removedArtifactCount = removedArtifactCount;
			this.failures = failures;
		}

		static Result failure(String branch, String problem) {
			List<String> failures = new ArrayList<>();
			failures.add(problem);
			return new Result(false, branch, false, 0, failures);
		}

		boolean succeeded() {
			return succeeded;
		}

		String branch() {
			return branch;
		}

		boolean usedLegacySelection() {
			return legacySelection;
		}

		int removedArtifactCount() {
			return removedArtifactCount;
		}

		String summary() {
			return "branch=" + branch
				+ " legacySelection=" + legacySelection
				+ " removed=" + removedArtifactCount
				+ " failures=" + failures.size()
				+ (failures.isEmpty() ? "" : " details=" + String.join(" | ", failures));
		}
	}
}
