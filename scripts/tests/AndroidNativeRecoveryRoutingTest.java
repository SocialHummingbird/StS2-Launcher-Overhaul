package com.game.sts2launcher;

import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

public final class AndroidNativeRecoveryRoutingTest {
	public static void main(String[] args) throws Exception {
		if (args.length != 2) {
			throw new IllegalArgumentException(
				"Expected paths to NativeFallbackActivity.java and "
					+ "LauncherActivity.java."
			);
		}

		String fallbackSource = Files.readString(
			Path.of(args[0]),
			StandardCharsets.UTF_8
		);
		String launcherSource = Files.readString(
			Path.of(args[1]),
			StandardCharsets.UTF_8
		);

		testOrdinaryLaunchFailure();
		testSafeStartFailure();
		testPendingPayloadIsRemoved();
		testRepeatedRestartTapsStartExactlyOnce();
		testActivityRecreationCanRecover();
		testRecoveryAfterProcessDeath();
		testRecoveryPreservesUnrelatedState();
		verifyFallbackRecoveryIsGuarded(fallbackSource);
		verifyFallbackClearsPendingStateBeforeStarting(fallbackSource);
		verifyFallbackUsesRecoveryHandoff(fallbackSource);
		verifyLauncherConsumesRecoveryBeforeRouting(launcherSource);

		System.out.println("Android native recovery routing tests passed.");
	}

	private static void testOrdinaryLaunchFailure() {
		FakePendingState state = new FakePendingState();
		state.preferenceGame = true;
		StartCounter starter = new StartCounter();

		AndroidNativeRecoveryController.RestartResult result =
			new AndroidNativeRecoveryController().restart(state, starter);

		assertTrue("ordinary recovery starts launcher", result.started());
		assertFalse("ordinary game preference cleared", state.preferenceGame);
		assertFalse("ordinary safe preference remains clear", state.preferenceSafe);
		assertTrue("ordinary clear result", result.clearedState().cleared());
		assertEquals("ordinary launcher start count", 1, starter.count);
	}

	private static void testSafeStartFailure() {
		FakePendingState state = new FakePendingState();
		state.preferenceGame = true;
		state.preferenceSafe = true;
		StartCounter starter = new StartCounter();

		AndroidNativeRecoveryController.RestartResult result =
			new AndroidNativeRecoveryController().restart(state, starter);

		assertTrue("Safe Start recovery starts launcher", result.started());
		assertFalse("Safe Start game preference cleared", state.preferenceGame);
		assertFalse("Safe Start preference cleared", state.preferenceSafe);
		assertTrue("Safe Start clear result", result.clearedState().cleared());
		assertEquals("Safe Start launcher start count", 1, starter.count);
	}

	private static void testPendingPayloadIsRemoved() {
		FakePendingState state = new FakePendingState();
		state.payloadGame = true;
		state.payloadSafe = true;

		AndroidPendingLaunchState.ClearResult result =
			AndroidPendingLaunchState.clearForRecovery(state, state);

		assertTrue("payload recovery clears all state", result.cleared());
		assertFalse("game payload removed", state.payloadGame);
		assertFalse("safe payload removed", state.payloadSafe);
	}

	private static void testRepeatedRestartTapsStartExactlyOnce() {
		FakePendingState state = new FakePendingState();
		state.preferenceGame = true;
		StartCounter starter = new StartCounter();
		AndroidNativeRecoveryController controller =
			new AndroidNativeRecoveryController();

		assertTrue("first Restart tap starts", controller.restart(state, starter).started());
		assertFalse(
			"second Restart tap blocked",
			controller.restart(state, starter).started()
		);
		assertFalse(
			"third Restart tap blocked",
			controller.restart(state, starter).started()
		);
		assertEquals("repeated taps launcher start count", 1, starter.count);
	}

	private static void testActivityRecreationCanRecover() {
		FakePendingState state = new FakePendingState();
		state.preferenceGame = true;
		StartCounter starter = new StartCounter();

		AndroidNativeRecoveryController recreatedActivityController =
			new AndroidNativeRecoveryController();

		assertTrue(
			"recreated activity recovers",
			recreatedActivityController.restart(state, starter).started()
		);
		assertTrue(
			"recreated activity clears state",
			!state.preferenceGame && !state.preferenceSafe
		);
		assertEquals("recreated activity launcher start count", 1, starter.count);
	}

	private static void testRecoveryAfterProcessDeath() {
		FakePendingState persistentState = new FakePendingState();
		persistentState.preferenceGame = true;
		persistentState.preferenceSafe = true;
		persistentState.payloadGame = true;
		persistentState.payloadSafe = true;

		StartCounter interruptedStarter = new StartCounter();
		AndroidNativeRecoveryController.RestartResult interrupted =
			new AndroidNativeRecoveryController().restart(
				() -> {
					throw new IllegalStateException("process interrupted");
				},
				interruptedStarter
			);
		assertTrue(
			"interrupted process attempted one launcher handoff",
			interrupted.started()
		);
		assertEquals(
			"interrupted process launcher start count",
			1,
			interruptedStarter.count
		);
		assertTrue(
			"pending state survived interrupted process",
			persistentState.preferenceGame
				&& persistentState.preferenceSafe
				&& persistentState.payloadGame
				&& persistentState.payloadSafe
		);

		StartCounter starter = new StartCounter();
		AndroidNativeRecoveryController restoredProcess =
			new AndroidNativeRecoveryController();
		AndroidNativeRecoveryController.RestartResult result =
			restoredProcess.restart(persistentState, starter);

		assertTrue("restored process recovers", result.started());
		assertTrue("restored process clears pending state", result.clearedState().cleared());
		assertEquals("restored process launcher start count", 1, starter.count);
	}

	private static void testRecoveryPreservesUnrelatedState() {
		FakePendingState state = new FakePendingState();
		state.preferenceGame = true;
		state.preferenceSafe = true;
		state.installedGame = "installed-game";
		state.saves = "profile-and-saves";
		state.credentials = "encrypted-credentials";
		state.settings = "launcher-settings";
		state.assemblyCache = "valid-assembly-cache";

		new AndroidNativeRecoveryController().restart(
			state,
			new StartCounter()
		);

		assertEquals(
			"installed game preserved",
			"installed-game",
			state.installedGame
		);
		assertEquals("saves preserved", "profile-and-saves", state.saves);
		assertEquals(
			"credentials preserved",
			"encrypted-credentials",
			state.credentials
		);
		assertEquals(
			"settings preserved",
			"launcher-settings",
			state.settings
		);
		assertEquals(
			"assembly cache preserved",
			"valid-assembly-cache",
			state.assemblyCache
		);
	}

	private static void verifyFallbackRecoveryIsGuarded(String source) {
		requireContains(
			source,
			"recoveryController.restart(",
			"Fallback restart is not protected by an exact-once controller."
		);
	}

	private static void verifyFallbackClearsPendingStateBeforeStarting(
		String source
	) {
		requireOrder(
			source,
			"AndroidPendingLaunchStateAdapter.clearForRecovery(",
			"startLauncherAfterNativeRecovery(",
			"Fallback starts LauncherActivity before clearing pending launch state."
		);
	}

	private static void verifyFallbackUsesRecoveryHandoff(String source) {
		requireContains(
			source,
			"AndroidPendingLaunchState.NATIVE_RECOVERY_INTENT_EXTRA",
			"Fallback restart does not identify the native recovery handoff."
		);
		requireContains(
			source,
			"AndroidBootTransitionPolicy.SKIP_INTENT_EXTRA",
			"Fallback restart does not skip the boot transition."
		);
		requireContains(
			source,
			"Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK",
			"Fallback restart does not replace the failed activity task."
		);
	}

	private static void verifyLauncherConsumesRecoveryBeforeRouting(
		String source
	) {
		requireOrder(
			source,
			"consumeNativeRecoveryRequest();",
			"hasPendingGameLaunchRequest();",
			"LauncherActivity evaluates the failed launch before recovery state."
		);
		requireContains(
			source,
			"AndroidPendingLaunchStateAdapter.clearForRecovery(",
			"LauncherActivity does not defensively clear a recovery handoff."
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

	private static void requireOrder(
		String source,
		String first,
		String second,
		String message
	) {
		int firstIndex = source.indexOf(first);
		int secondIndex = source.indexOf(
			second,
			Math.max(0, firstIndex) + first.length()
		);
		if (firstIndex < 0 || secondIndex < 0) {
			throw new AssertionError(message);
		}
	}

	private static void assertEquals(
		String label,
		int expected,
		int actual
	) {
		if (expected != actual) {
			throw new AssertionError(
				label + ": expected=" + expected + ", actual=" + actual
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

	private static void assertEquals(
		String label,
		String expected,
		String actual
	) {
		if (!expected.equals(actual)) {
			throw new AssertionError(
				label + ": expected=" + expected + ", actual=" + actual
			);
		}
	}

	private static final class FakePendingState
		implements
			AndroidPendingLaunchState.Preferences,
			AndroidPendingLaunchState.Payload,
			AndroidNativeRecoveryController.StateClearer {
		boolean preferenceGame;
		boolean preferenceSafe;
		boolean payloadGame;
		boolean payloadSafe;
		String installedGame;
		String saves;
		String credentials;
		String settings;
		String assemblyCache;

		@Override
		public boolean gameLaunchPreferencePending() {
			return preferenceGame;
		}

		@Override
		public boolean safeLaunchPreferencePending() {
			return preferenceSafe;
		}

		@Override
		public boolean clearPendingPreferences() {
			preferenceGame = false;
			preferenceSafe = false;
			return true;
		}

		@Override
		public boolean gameLaunchPayloadPending() {
			return payloadGame;
		}

		@Override
		public boolean safeLaunchPayloadPending() {
			return payloadSafe;
		}

		@Override
		public void clearPendingPayload() {
			payloadGame = false;
			payloadSafe = false;
		}

		@Override
		public AndroidPendingLaunchState.ClearResult clear() {
			return AndroidPendingLaunchState.clearForRecovery(this, this);
		}
	}

	private static final class StartCounter
		implements AndroidNativeRecoveryController.LauncherStarter {
		int count;

		@Override
		public void start(AndroidPendingLaunchState.ClearResult clearedState) {
			count++;
		}
	}
}
