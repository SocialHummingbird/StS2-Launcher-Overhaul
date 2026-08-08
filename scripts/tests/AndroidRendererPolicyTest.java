package com.game.sts2launcher;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public final class AndroidRendererPolicyTest {
	public static void main(String[] args) {
		assertPlan("auto", false, "Adreno 830", "auto");
		assertPlan(
			"auto",
			false,
			"Adapter name: Adreno (TM) 830\nAdapter vendor: Qualcomm\nPowerVR compatibility required: False\n",
			"auto"
		);
		assertPlan("unknown", false, "Mali-G78", "auto");
		assertPlan(" VULKAN ", false, "Adreno 830", "vulkan", "--rendering-driver", "vulkan", "--rendering-method", "mobile");
		assertPlan("OpenGL", false, "Adreno 830", "opengl", "--rendering-driver", "opengl3", "--rendering-method", "gl_compatibility");

		AndroidRendererPolicy.Plan safe = AndroidRendererPolicy.resolve("opengl", true, "Adreno 830");
		List<String> safeCommands = new ArrayList<>();
		safe.appendCommandLine(safeCommands);
		assertEquals("safe preference", "opengl", safe.preference());
		assertEquals("safe effective mode", "auto", safe.effectiveMode());
		assertTrue("safe override", safe.safeLaunchOverride());
		assertEquals("safe arguments", List.of(), safeCommands);

		assertPowerVrPlan("auto", false);
		assertPowerVrPlan("vulkan", false);
		assertPowerVrPlan("opengl", false);
		assertPowerVrPlan("auto", true);
		assertTrue("ImgTec vendor detection", AndroidRendererPolicy.isPowerVr("ImgTec DXT-48"));
		assertTrue("Imagination vendor detection", AndroidRendererPolicy.isPowerVr("Imagination Technologies"));
		assertTrue(
			"explicit PowerVR marker",
			AndroidRendererPolicy.isPowerVr(
				"Adapter name: DXT-48-1536 MC1\nPowerVR compatibility required: True\n"
			)
		);

		System.out.println("Android renderer policy tests passed.");
	}

	private static void assertPlan(String preference, boolean safe, String graphicsDevice, String effective, String... expectedArguments) {
		AndroidRendererPolicy.Plan plan = AndroidRendererPolicy.resolve(preference, safe, graphicsDevice);
		List<String> commands = new ArrayList<>();
		plan.appendCommandLine(commands);
		assertEquals("effective mode", effective, plan.effectiveMode());
		assertEquals("arguments", Arrays.asList(expectedArguments), commands);
	}

	private static void assertPowerVrPlan(String preference, boolean safe) {
		AndroidRendererPolicy.Plan plan = AndroidRendererPolicy.resolve(
			preference,
			safe,
			"Adapter name: PowerVR D-Series DXT-48-1536 MC1"
		);
		List<String> commands = new ArrayList<>();
		plan.appendCommandLine(commands);
		assertEquals("PowerVR requested preference", preference, plan.preference());
		assertEquals("PowerVR effective mode", "opengl", plan.effectiveMode());
		assertEquals(
			"PowerVR arguments",
			Arrays.asList("--rendering-driver", "opengl3", "--rendering-method", "gl_compatibility"),
			commands
		);
		assertTrue("PowerVR compatibility", plan.powerVrCompatibility());
	}

	private static void assertTrue(String label, boolean value) {
		if (!value) {
			throw new AssertionError(label + ": expected true");
		}
	}

	private static void assertEquals(String label, Object expected, Object actual) {
		if (!expected.equals(actual)) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}
}
