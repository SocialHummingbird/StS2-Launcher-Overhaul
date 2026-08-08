package com.game.sts2launcher;

import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

public final class AndroidStartupRoutingRegressionTest {
	private static final String PREPARE =
		"assemblyBootstrapper.prepare();";
	private static final String FAILURE_CONDITION =
		"if (!assemblyResult.isSuccess())";
	private static final String FAILURE_ROUTE =
		"routeOnce(NativeFallbackActivity.class, assemblyResult);";
	private static final String SUCCESS_ROUTE =
		"routeOnce(GodotApp.class, null);";
	private static final String START_ACTIVITY =
		"startActivity(intent);";

	public static void main(String[] args) throws Exception {
		if (args.length != 2) {
			throw new IllegalArgumentException(
				"Expected paths to LauncherActivity.java and GodotApp.java."
			);
		}

		String launcherSource = Files.readString(
			Path.of(args[0]),
			StandardCharsets.UTF_8
		);
		String godotAppSource = Files.readString(
			Path.of(args[1]),
			StandardCharsets.UTF_8
		);

		verifyRouteGateAllowsExactlyOneRoute();
		verifyLauncherOwnsPreparation(launcherSource);
		verifyAssemblyResultDeterminesRoute(launcherSource);
		verifyRouteIsStartedOnce(launcherSource);
		verifyGodotDoesNotPrepareOrRouteFailure(godotAppSource);

		System.out.println("Android startup routing regression tests passed.");
	}

	private static void verifyRouteGateAllowsExactlyOneRoute() {
		AndroidStartupRouteGate gate = new AndroidStartupRouteGate();
		assertTrue("first route claimed", gate.tryClaim());
		assertFalse("second route blocked", gate.tryClaim());
		assertFalse("third route blocked", gate.tryClaim());
	}

	private static void verifyLauncherOwnsPreparation(String source) {
		requireContains(
			source,
			"new AndroidAssemblyBootstrapper(",
			"LauncherActivity does not construct the assembly bootstrapper."
		);
		requireOrder(
			source,
			"super.onCreate(savedInstanceState);",
			PREPARE,
			"LauncherActivity must call Activity.super.onCreate() before preparation."
		);
	}

	private static void verifyAssemblyResultDeterminesRoute(String source) {
		requireOrder(
			source,
			PREPARE,
			FAILURE_CONDITION,
			"LauncherActivity does not inspect the preparation result."
		);
		requireOrder(
			source,
			FAILURE_CONDITION,
			FAILURE_ROUTE,
			"Assembly preparation failure does not route to NativeFallbackActivity."
		);
		requireOrder(
			source,
			FAILURE_ROUTE,
			"return;",
			"Assembly preparation failure can continue into the success route."
		);
		requireOrder(
			source,
			FAILURE_ROUTE,
			SUCCESS_ROUTE,
			"GodotApp must only be selected after the failure route exits."
		);
		assertEquals(
			"assembly failure route count",
			1,
			countOccurrences(source, FAILURE_ROUTE)
		);
		assertEquals(
			"assembly success route count",
			1,
			countOccurrences(source, SUCCESS_ROUTE)
		);
	}

	private static void verifyRouteIsStartedOnce(String source) {
		assertEquals(
			"direct activity start count",
			1,
			countOccurrences(source, START_ACTIVITY)
		);
		assertEquals(
			"route gate claim count",
			1,
			countOccurrences(source, "routeGate.tryClaim()")
		);
		requireOrder(
			source,
			"if (!routeGate.tryClaim())",
			START_ACTIVITY,
			"The exact-once route guard is missing."
		);
	}

	private static void verifyGodotDoesNotPrepareOrRouteFailure(String source) {
		requireAbsent(
			source,
			"AndroidAssemblyBootstrapper",
			"GodotApp still owns assembly preparation."
		);
		requireAbsent(
			source,
			"showNativeFailure(",
			"GodotApp still contains the pre-super failure route."
		);
		requireContains(
			source,
			"super.onCreate(savedInstanceState);",
			"GodotApp does not call GodotActivity.super.onCreate()."
		);
	}

	private static void requireContains(
		String source,
		String expected,
		String message
	) {
		if (!source.contains(expected)) {
			throw new AssertionError(message);
		}
	}

	private static void requireAbsent(
		String source,
		String unexpected,
		String message
	) {
		if (source.contains(unexpected)) {
			throw new AssertionError(message);
		}
	}

	private static void requireOrder(
		String source,
		String first,
		String second,
		String message
	) {
		int firstIndex = source.indexOf(first);
		int secondIndex = source.indexOf(second, firstIndex + first.length());
		if (firstIndex < 0 || secondIndex < 0) {
			throw new AssertionError(message);
		}
	}

	private static int countOccurrences(String source, String expected) {
		int count = 0;
		int index = 0;
		while ((index = source.indexOf(expected, index)) >= 0) {
			count++;
			index += expected.length();
		}
		return count;
	}

	private static void assertEquals(String label, int expected, int actual) {
		if (expected != actual) {
			throw new AssertionError(
				label + ": expected " + expected + ", actual " + actual
			);
		}
	}

	private static void assertTrue(String label, boolean condition) {
		if (!condition) {
			throw new AssertionError(label);
		}
	}

	private static void assertFalse(String label, boolean condition) {
		if (condition) {
			throw new AssertionError(label);
		}
	}
}
