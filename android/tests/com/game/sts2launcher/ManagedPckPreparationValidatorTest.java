package com.game.sts2launcher;

import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

import org.junit.Test;

import java.io.File;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.attribute.FileTime;
import java.util.LinkedHashMap;
import java.util.Map;

public final class ManagedPckPreparationValidatorTest {
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

	@Test
	public void validArm64PreparationIsAccepted() throws Exception {
		File pck = writePck(arm64Entries());

		ManagedPckPreparationValidator.Result result =
			ManagedPckPreparationValidator.inspect(pck, false);

		assertTrue(result.problem(), result.isValid());
	}

	@Test
	public void staleArm64PreparationWithDisabledFmodManagerIsRejected()
		throws Exception {
		Map<String, byte[]> textStateEntries = arm64Entries();
		textStateEntries.put("project.godot", bytes(DISABLED_FMOD_PROJECT_SETTING));
		File textStatePck = writePck(textStateEntries);

		ManagedPckPreparationValidator.Result textStateResult =
			ManagedPckPreparationValidator.inspect(textStatePck, false);

		assertFalse(textStateResult.isValid());
		assertTrue(
			textStateResult.problem(),
			textStateResult.problem().contains("project.godot")
		);

		Map<String, byte[]> binaryStateEntries = arm64Entries();
		binaryStateEntries.put(
			"project.binary",
			bytes(DISABLED_FMOD_BINARY_AUTOLOAD)
		);
		File binaryStatePck = writePck(binaryStateEntries);

		ManagedPckPreparationValidator.Result binaryStateResult =
			ManagedPckPreparationValidator.inspect(binaryStatePck, false);

		assertFalse(binaryStateResult.isValid());
		assertTrue(
			binaryStateResult.problem(),
			binaryStateResult.problem().contains("project.binary")
		);
		assertTrue(
			binaryStateResult.problem(),
			binaryStateResult.problem().contains("FmodManager")
		);
	}

	@Test
	public void validX86PreparationIsAccepted() throws Exception {
		File pck = writePck(x86Entries());

		ManagedPckPreparationValidator.Result result =
			ManagedPckPreparationValidator.inspect(pck, true);

		assertTrue(result.problem(), result.isValid());
	}

	@Test
	public void validationNeverMutatesThePck() throws Exception {
		File validPck = writePck(arm64Entries());
		File stalePck = writePck(x86Entries());
		byte[] validBefore = Files.readAllBytes(validPck.toPath());
		byte[] staleBefore = Files.readAllBytes(stalePck.toPath());
		FileTime validModifiedBefore = Files.getLastModifiedTime(validPck.toPath());
		FileTime staleModifiedBefore = Files.getLastModifiedTime(stalePck.toPath());

		assertTrue(
			ManagedPckPreparationValidator.inspect(validPck, false).isValid()
		);
		assertFalse(
			ManagedPckPreparationValidator.inspect(stalePck, false).isValid()
		);

		assertArrayEquals(validBefore, Files.readAllBytes(validPck.toPath()));
		assertArrayEquals(staleBefore, Files.readAllBytes(stalePck.toPath()));
		assertTrue(
			validModifiedBefore.equals(Files.getLastModifiedTime(validPck.toPath()))
		);
		assertTrue(
			staleModifiedBefore.equals(Files.getLastModifiedTime(stalePck.toPath()))
		);
	}

	private static Map<String, byte[]> arm64Entries() {
		Map<String, byte[]> entries = new LinkedHashMap<>();
		entries.put("project.binary", bytes(FMOD_BINARY_AUTOLOAD));
		entries.put("project.godot", bytes(FMOD_PROJECT_SETTING));
		entries.put(".godot/extension_list.cfg", bytes(FMOD_EXTENSION));
		entries.put("scenes/game.tscn", bytes(String.join("\n", FMOD_SCENE_SETTINGS)));
		return entries;
	}

	private static Map<String, byte[]> x86Entries() {
		Map<String, byte[]> entries = new LinkedHashMap<>();
		entries.put("project.binary", bytes(DISABLED_FMOD_BINARY_AUTOLOAD));
		entries.put("project.godot", bytes(DISABLED_FMOD_PROJECT_SETTING));
		entries.put(".godot/extension_list.cfg", bytes(FMOD_EXTENSION));
		String[] disabledSceneSettings = new String[FMOD_SCENE_SETTINGS.length];
		for (int index = 0; index < FMOD_SCENE_SETTINGS.length; index++) {
			disabledSceneSettings[index] = ";" + FMOD_SCENE_SETTINGS[index].substring(1);
		}
		entries.put("scenes/game.tscn", bytes(String.join("\n", disabledSceneSettings)));
		return entries;
	}

	private static File writePck(Map<String, byte[]> entries) throws Exception {
		int directoryBytes = 4;
		for (Map.Entry<String, byte[]> entry : entries.entrySet()) {
			directoryBytes += 4
				+ entry.getKey().getBytes(StandardCharsets.UTF_8).length
				+ 8 + 8 + 16 + 4;
		}
		int contentOffset = 104 + directoryBytes;
		int totalBytes = contentOffset;
		for (byte[] content : entries.values()) {
			totalBytes += content.length;
		}

		ByteBuffer pck = ByteBuffer.allocate(totalBytes).order(ByteOrder.LITTLE_ENDIAN);
		pck.putInt(0x43504447);
		pck.putInt(3); // format version
		pck.putInt(4);
		pck.putInt(5);
		pck.putInt(0);
		pck.putInt(0); // absolute offsets
		pck.putLong(0);
		pck.putLong(104);
		for (int index = 0; index < 16; index++) {
			pck.putInt(0);
		}

		pck.putInt(entries.size());
		int nextContentOffset = contentOffset;
		for (Map.Entry<String, byte[]> entry : entries.entrySet()) {
			byte[] path = entry.getKey().getBytes(StandardCharsets.UTF_8);
			byte[] content = entry.getValue();
			pck.putInt(path.length);
			pck.put(path);
			pck.putLong(nextContentOffset);
			pck.putLong(content.length);
			pck.put(new byte[16]);
			pck.putInt(0);
			nextContentOffset += content.length;
		}
		for (byte[] content : entries.values()) {
			pck.put(content);
		}

		File file = Files.createTempFile("sts2-managed-pck-", ".pck").toFile();
		Files.write(file.toPath(), pck.array());
		file.deleteOnExit();
		return file;
	}

	private static byte[] bytes(String value) {
		return value.getBytes(StandardCharsets.UTF_8);
	}
}
