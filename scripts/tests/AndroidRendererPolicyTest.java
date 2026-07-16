package com.game.sts2launcher;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public final class AndroidRendererPolicyTest {
	public static void main(String[] args) {
		assertPlan("auto", false, "auto");
		assertPlan("unknown", false, "auto");
		assertPlan(" VULKAN ", false, "vulkan", "--rendering-driver", "vulkan", "--rendering-method", "mobile");
		assertPlan("OpenGL", false, "opengl", "--rendering-driver", "opengl3", "--rendering-method", "gl_compatibility");

		AndroidRendererPolicy.Plan safe = AndroidRendererPolicy.resolve("opengl", true);
		List<String> safeCommands = new ArrayList<>();
		safe.appendCommandLine(safeCommands);
		assertEquals("safe preference", "opengl", safe.preference());
		assertEquals("safe effective mode", "auto", safe.effectiveMode());
		assertTrue("safe override", safe.safeLaunchOverride());
		assertEquals("safe arguments", List.of(), safeCommands);

		System.out.println("Android renderer policy tests passed.");
	}

	private static void assertPlan(String preference, boolean safe, String effective, String... expectedArguments) {
		AndroidRendererPolicy.Plan plan = AndroidRendererPolicy.resolve(preference, safe);
		List<String> commands = new ArrayList<>();
		plan.appendCommandLine(commands);
		assertEquals("effective mode", effective, plan.effectiveMode());
		assertEquals("arguments", Arrays.asList(expectedArguments), commands);
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
