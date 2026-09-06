package com.game.sts2launcher;

import java.io.File;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;

final class ManagedPckPreparationValidator {
	private static final long PCK_MAGIC = 0x43504447L;
	private static final int MAX_PATH_BYTES = 4096;
	private static final int MAX_VALIDATED_ENTRY_BYTES = 8 * 1024 * 1024;
	private static final long MAX_FILE_COUNT = 2_000_000L;

	private static final String PROJECT_BINARY = "project.binary";
	private static final String PROJECT_GODOT = "project.godot";
	private static final String EXTENSION_LIST = ".godot/extension_list.cfg";
	private static final String GAME_SCENE = "scenes/game.tscn";

	private static final String FMOD_PROJECT_SETTING =
		"FmodManager=\"*res://addons/fmod/FmodManager.gd\"";
	private static final String DISABLED_FMOD_PROJECT_SETTING =
		";modManager=\"*res://addons/fmod/FmodManager.gd\"";
	private static final String FMOD_BINARY_AUTOLOAD = "autoload/FmodManager";
	private static final String DISABLED_FMOD_BINARY_AUTOLOAD =
		"disabled/FmodManager";
	private static final String FMOD_EXTENSION =
		"res://addons/fmod/fmod.gdextension";
	private static final String[] FMOD_SCENE_SETTINGS = new String[] {
		"[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]",
		"[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]",
		"bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]",
		"script = ExtResource(\"3_xfu11\")",
		"[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]",
	};

	private ManagedPckPreparationValidator() {
	}

	static Result inspect(File pck, boolean x86) {
		if (pck == null || !pck.isFile()) {
			return Result.invalid("selected game PCK is missing");
		}

		try (RandomAccessFile file = new RandomAccessFile(pck, "r")) {
			Map<String, byte[]> entries = readRequiredEntries(file);
			Result extensionResult = requireState(
				entries.get(EXTENSION_LIST),
				EXTENSION_LIST,
				FMOD_EXTENSION,
				true
			);
			if (!extensionResult.isValid()) {
				return extensionResult;
			}

			byte[] projectGodot = entries.get(PROJECT_GODOT);
			if (projectGodot != null) {
				Result projectGodotResult = requireExclusiveState(
					projectGodot,
					PROJECT_GODOT,
					FMOD_PROJECT_SETTING,
					DISABLED_FMOD_PROJECT_SETTING,
					!x86
				);
				if (!projectGodotResult.isValid()) {
					return projectGodotResult;
				}
			}

			Result projectBinaryResult = requireExclusiveState(
				entries.get(PROJECT_BINARY),
				PROJECT_BINARY,
				FMOD_BINARY_AUTOLOAD,
				DISABLED_FMOD_BINARY_AUTOLOAD,
				!x86
			);
			if (!projectBinaryResult.isValid()) {
				return projectBinaryResult;
			}

			byte[] gameScene = entries.get(GAME_SCENE);
			for (String setting : FMOD_SCENE_SETTINGS) {
				String disabledSetting = ";" + setting.substring(1);
				Result sceneResult = requireExclusiveState(
					gameScene,
					GAME_SCENE,
					setting,
					disabledSetting,
					!x86
				);
				if (!sceneResult.isValid()) {
					return sceneResult;
				}
			}

			return Result.valid();
		} catch (Exception error) {
			return Result.invalid(
				"selected game PCK is unreadable or corrupt: "
					+ error.getClass().getSimpleName()
					+ (error.getMessage() == null ? "" : " (" + error.getMessage() + ")")
			);
		}
	}

	private static Map<String, byte[]> readRequiredEntries(RandomAccessFile file)
		throws IOException {
		if (file.length() < 104 || readUInt32LittleEndian(file) != PCK_MAGIC) {
			throw new IOException("invalid PCK header");
		}

		readUInt32LittleEndian(file); // format version
		readUInt32LittleEndian(file); // major
		readUInt32LittleEndian(file); // minor
		readUInt32LittleEndian(file); // patch
		long headerFlags = readUInt32LittleEndian(file);
		long fileBase = readLongLittleEndian(file);
		long directoryBase = readLongLittleEndian(file);
		if (directoryBase <= 0 || directoryBase + 4 > file.length()) {
			throw new IOException("invalid PCK directory offset " + directoryBase);
		}

		file.seek(directoryBase);
		long fileCount = readUInt32LittleEndian(file);
		if (fileCount < 1 || fileCount > MAX_FILE_COUNT) {
			throw new IOException("invalid PCK file count " + fileCount);
		}

		boolean relativeOffsets = (headerFlags & 0x02L) != 0;
		Map<String, byte[]> entries = new HashMap<>();
		for (long index = 0; index < fileCount; index++) {
			long pathLength = readUInt32LittleEndian(file);
			if (pathLength < 1 || pathLength > MAX_PATH_BYTES) {
				throw new IOException("invalid PCK path length " + pathLength);
			}
			if (file.getFilePointer() + pathLength + 36L > file.length()) {
				throw new IOException("truncated PCK directory entry");
			}

			byte[] pathBytes = new byte[(int)pathLength];
			file.readFully(pathBytes);
			String path = trimTrailingNulls(
				new String(pathBytes, StandardCharsets.UTF_8)
			);
			long offset = readLongLittleEndian(file);
			long size = readLongLittleEndian(file);
			file.skipBytes(16); // MD5
			readUInt32LittleEndian(file); // entry flags
			long nextDirectoryPosition = file.getFilePointer();

			String requiredPath = requiredPath(path);
			if (requiredPath != null) {
				if (entries.containsKey(requiredPath)) {
					throw new IOException("duplicate PCK entry " + requiredPath);
				}
				long absoluteOffset = relativeOffsets
					? checkedAdd(fileBase, offset)
					: offset;
				if (
					size < 0
						|| size > MAX_VALIDATED_ENTRY_BYTES
						|| absoluteOffset < 0
						|| absoluteOffset > file.length()
						|| size > file.length() - absoluteOffset
				) {
					throw new IOException("invalid PCK entry bounds for " + requiredPath);
				}

				byte[] content = new byte[(int)size];
				file.seek(absoluteOffset);
				file.readFully(content);
				entries.put(requiredPath, content);
				file.seek(nextDirectoryPosition);
			}
		}

		for (String required : new String[] {
			PROJECT_BINARY,
			EXTENSION_LIST,
			GAME_SCENE,
		}) {
			if (!entries.containsKey(required)) {
				throw new IOException("required PCK entry is missing: " + required);
			}
		}
		return entries;
	}

	private static Result requireExclusiveState(
		byte[] content,
		String path,
		String enabledValue,
		String disabledValue,
		boolean shouldBeEnabled
	) {
		Result expected = requireState(
			content,
			path,
			shouldBeEnabled ? enabledValue : disabledValue,
			true
		);
		if (!expected.isValid()) {
			return expected;
		}
		return requireState(
			content,
			path,
			shouldBeEnabled ? disabledValue : enabledValue,
			false
		);
	}

	private static Result requireState(
		byte[] content,
		String path,
		String value,
		boolean expectedPresent
	) {
		if (content == null) {
			return Result.invalid("required PCK entry is missing: " + path);
		}
		boolean present = contains(
			content,
			value.getBytes(StandardCharsets.UTF_8)
		);
		if (present == expectedPresent) {
			return Result.valid();
		}
		return Result.invalid(
			path + " has an invalid managed FMOD preparation state for " + value
		);
	}

	private static boolean contains(byte[] content, byte[] search) {
		for (int index = 0; index <= content.length - search.length; index++) {
			boolean match = true;
			for (int offset = 0; offset < search.length; offset++) {
				if (content[index + offset] != search[offset]) {
					match = false;
					break;
				}
			}
			if (match) {
				return true;
			}
		}
		return false;
	}

	private static String requiredPath(String path) {
		String normalized = path.startsWith("res://") ? path.substring(6) : path;
		if (
			PROJECT_BINARY.equals(normalized)
				|| PROJECT_GODOT.equals(normalized)
				|| EXTENSION_LIST.equals(normalized)
				|| GAME_SCENE.equals(normalized)
		) {
			return normalized;
		}
		return null;
	}

	private static String trimTrailingNulls(String value) {
		int length = value.length();
		while (length > 0 && value.charAt(length - 1) == '\0') {
			length--;
		}
		return value.substring(0, length);
	}

	private static long checkedAdd(long left, long right) throws IOException {
		long result = left + right;
		if (((left ^ result) & (right ^ result)) < 0) {
			throw new IOException("PCK entry offset overflow");
		}
		return result;
	}

	private static long readUInt32LittleEndian(RandomAccessFile file)
		throws IOException {
		return ((long)file.readUnsignedByte())
			| ((long)file.readUnsignedByte() << 8)
			| ((long)file.readUnsignedByte() << 16)
			| ((long)file.readUnsignedByte() << 24);
	}

	private static long readLongLittleEndian(RandomAccessFile file)
		throws IOException {
		return readUInt32LittleEndian(file)
			| (readUInt32LittleEndian(file) << 32);
	}

	static final class Result {
		private final boolean valid;
		private final String problem;

		private Result(boolean valid, String problem) {
			this.valid = valid;
			this.problem = problem;
		}

		static Result valid() {
			return new Result(true, "");
		}

		static Result invalid(String problem) {
			return new Result(false, problem);
		}

		boolean isValid() {
			return valid;
		}

		String problem() {
			return problem;
		}
	}
}
