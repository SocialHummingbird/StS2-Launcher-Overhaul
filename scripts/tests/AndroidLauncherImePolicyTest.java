package com.game.sts2launcher;

public final class AndroidLauncherImePolicyTest {
	public static void main(String[] args) {
		testIdleLauncherLifecycle();
		testCloudOperationsDoNotCreateEditorIntent();
		testGenuineTextEditing();
		testLauncherAndGameRouting();
		testBootCleanupAndLifecycleRearming();
		testDestroyIsTerminal();
		System.out.println("AndroidLauncherImePolicy tests passed");
	}

	private static void testIdleLauncherLifecycle() {
		AndroidLauncherImePolicy policy = new AndroidLauncherImePolicy(true);
		assertAction(
			AndroidLauncherImePolicy.Action.SUPPRESS,
			policy.onLauncherStartup(),
			"idle launcher startup"
		);
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			policy.onResume(),
			"startup suppression should cover initial resume"
		);
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			policy.onWindowFocusChanged(true),
			"startup suppression should cover initial focus"
		);
		assertTrue(policy.shouldSuppressIme(), "idle launcher must suppress the IME");
	}

	private static void testCloudOperationsDoNotCreateEditorIntent() {
		String[] cloudStates = {
			"pull-active",
			"pull-completed",
			"pull-cancelled"
		};
		for (String cloudState : cloudStates) {
			AndroidLauncherImePolicy policy = new AndroidLauncherImePolicy(true);
			policy.onLauncherStartup();
			policy.onPause();
			assertAction(
				AndroidLauncherImePolicy.Action.SUPPRESS,
				policy.onResume(),
				cloudState + " resume"
			);
			assertTrue(
				policy.shouldSuppressIme(),
				cloudState + " must remain a non-editor launcher state"
			);
		}
	}

	private static void testGenuineTextEditing() {
		AndroidLauncherImePolicy policy = new AndroidLauncherImePolicy(true);
		policy.onLauncherStartup();
		assertAction(
			AndroidLauncherImePolicy.Action.ALLOW,
			policy.onTextEditingRequested(true),
			"real launcher text field"
		);
		assertTrue(policy.isTextEditingRequested(), "editing intent must be retained");
		policy.onPause();
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			policy.onResume(),
			"resume while a real editor remains focused"
		);
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			policy.onWindowFocusChanged(true),
			"window focus must not suppress a real editor"
		);
		assertAction(
			AndroidLauncherImePolicy.Action.SUPPRESS,
			policy.onTextEditingRequested(false),
			"editing completion"
		);
		assertTrue(policy.shouldSuppressIme(), "editing completion must restore suppression");
	}

	private static void testLauncherAndGameRouting() {
		AndroidLauncherImePolicy gamePolicy = new AndroidLauncherImePolicy(false);
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			gamePolicy.onLauncherStartup(),
			"game startup"
		);
		assertFalse(gamePolicy.shouldSuppressIme(), "game mode must not suppress text input");
		assertAction(
			AndroidLauncherImePolicy.Action.SUPPRESS,
			gamePolicy.setLauncherUiActive(true),
			"in-game launcher shown"
		);
		assertAction(
			AndroidLauncherImePolicy.Action.ALLOW,
			gamePolicy.setLauncherUiActive(false),
			"launcher handoff to game"
		);
		assertFalse(gamePolicy.shouldSuppressIme(), "game handoff must release IME policy");
	}

	private static void testBootCleanupAndLifecycleRearming() {
		AndroidLauncherImePolicy policy = new AndroidLauncherImePolicy(true);
		policy.onLauncherStartup();
		assertAction(
			AndroidLauncherImePolicy.Action.SUPPRESS,
			policy.onBootTransitionCleanup(),
			"boot overlay cleanup must force a post-removal hide"
		);
		policy.onWindowFocusChanged(false);
		assertAction(
			AndroidLauncherImePolicy.Action.SUPPRESS,
			policy.onWindowFocusChanged(true),
			"rotation, split-screen, lock return, or Home resume"
		);
		policy.onPause();
		assertAction(
			AndroidLauncherImePolicy.Action.SUPPRESS,
			policy.onResume(),
			"subsequent lifecycle resume"
		);
	}

	private static void testDestroyIsTerminal() {
		AndroidLauncherImePolicy policy = new AndroidLauncherImePolicy(true);
		policy.onLauncherStartup();
		policy.onDestroy();
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			policy.onResume(),
			"destroyed activity resume callback"
		);
		assertAction(
			AndroidLauncherImePolicy.Action.NONE,
			policy.onTextEditingRequested(true),
			"destroyed activity editor callback"
		);
		assertFalse(policy.shouldSuppressIme(), "destroyed policy must be inert");
	}

	private static void assertAction(
		AndroidLauncherImePolicy.Action expected,
		AndroidLauncherImePolicy.Action actual,
		String message
	) {
		if (expected != actual) {
			throw new AssertionError(message + ": expected=" + expected + " actual=" + actual);
		}
	}

	private static void assertTrue(boolean condition, String message) {
		if (!condition) {
			throw new AssertionError(message);
		}
	}

	private static void assertFalse(boolean condition, String message) {
		assertTrue(!condition, message);
	}
}
