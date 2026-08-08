package com.game.sts2launcher;

final class AndroidNativeRecoveryController {
	interface StateClearer {
		AndroidPendingLaunchState.ClearResult clear();
	}

	interface LauncherStarter {
		void start(AndroidPendingLaunchState.ClearResult clearedState);
	}

	private final AndroidStartupRouteGate routeGate =
		new AndroidStartupRouteGate();

	RestartResult restart(
		StateClearer stateClearer,
		LauncherStarter launcherStarter
	) {
		if (!routeGate.tryClaim()) {
			return RestartResult.duplicate();
		}

		AndroidPendingLaunchState.ClearResult clearedState;
		try {
			clearedState = stateClearer.clear();
		} catch (RuntimeException error) {
			clearedState = AndroidPendingLaunchState.failed(
				error.getClass().getSimpleName() + ": "
					+ String.valueOf(error.getMessage())
			);
		}
		launcherStarter.start(clearedState);
		return RestartResult.started(clearedState);
	}

	static final class RestartResult {
		private final boolean started;
		private final AndroidPendingLaunchState.ClearResult clearedState;

		private RestartResult(
			boolean started,
			AndroidPendingLaunchState.ClearResult clearedState
		) {
			this.started = started;
			this.clearedState = clearedState;
		}

		static RestartResult started(
			AndroidPendingLaunchState.ClearResult clearedState
		) {
			return new RestartResult(true, clearedState);
		}

		static RestartResult duplicate() {
			return new RestartResult(false, null);
		}

		boolean started() {
			return started;
		}

		AndroidPendingLaunchState.ClearResult clearedState() {
			return clearedState;
		}
	}
}
