package com.game.sts2launcher;

import android.app.Activity;
import android.os.Build;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.view.View;
import android.view.Window;
import android.view.WindowInsets;
import android.view.WindowInsetsController;
import android.view.WindowManager;
import android.view.inputmethod.InputMethodManager;

final class AndroidLauncherImeController {
	private static final long DELAYED_SUPPRESSION_MS = 240L;
	private static final long EDITOR_EXIT_GRACE_MS = 80L;

	interface TimelineLogger {
		void record(String phase, String detail);
	}

	private final Activity activity;
	private final TimelineLogger logger;
	private final AndroidLauncherImePolicy policy;
	private final Handler mainHandler = new Handler(Looper.getMainLooper());
	private final Runnable immediateSuppression = () -> applySuppression(true);
	private final Runnable delayedSuppression = () -> applySuppression(false);

	private boolean destroyed;
	private String pendingSuppressionReason = "";

	AndroidLauncherImeController(
		Activity activity,
		boolean launcherUiActive,
		TimelineLogger logger
	) {
		this.activity = activity;
		this.logger = logger;
		this.policy = new AndroidLauncherImePolicy(launcherUiActive);
	}

	void onLauncherStartup() {
		handleAction(policy.onLauncherStartup(), "launcher-startup", 0L);
	}

	void setLauncherUiActive(boolean active, String source) {
		handleAction(
			policy.setLauncherUiActive(active),
			(active ? "launcher-ui-active:" : "launcher-ui-inactive:") + source,
			0L
		);
	}

	void onResume() {
		handleAction(policy.onResume(), "activity-resume", 0L);
	}

	void onPause() {
		synchronized (this) {
			policy.onPause();
			cancelPendingSuppressionLocked();
		}
	}

	void onWindowFocusChanged(boolean hasFocus) {
		AndroidLauncherImePolicy.Action action;
		synchronized (this) {
			action = policy.onWindowFocusChanged(hasFocus);
			if (!hasFocus) {
				cancelPendingSuppressionLocked();
				return;
			}
		}
		handleAction(action, "window-focus-gained", 0L);
	}

	void onBootTransitionCleanup() {
		handleAction(policy.onBootTransitionCleanup(), "boot-transition-cleanup", 0L);
	}

	void onTextEditingRequested(boolean requested, String source) {
		handleAction(
			policy.onTextEditingRequested(requested),
			(requested ? "editor-requested:" : "editor-released:") + source,
			requested ? 0L : EDITOR_EXIT_GRACE_MS
		);
	}

	void destroy() {
		synchronized (this) {
			if (destroyed) {
				return;
			}
			destroyed = true;
			policy.onDestroy();
			cancelPendingSuppressionLocked();
		}
	}

	private void handleAction(
		AndroidLauncherImePolicy.Action action,
		String reason,
		long suppressionDelayMs
	) {
		synchronized (this) {
			if (destroyed || action == AndroidLauncherImePolicy.Action.NONE) {
				return;
			}
			cancelPendingSuppressionLocked();
			if (action == AndroidLauncherImePolicy.Action.ALLOW) {
				mainHandler.post(() -> applySoftInputState(false, reason));
				return;
			}
			pendingSuppressionReason = reason;
			if (suppressionDelayMs > 0L) {
				mainHandler.postDelayed(immediateSuppression, suppressionDelayMs);
			} else {
				mainHandler.post(immediateSuppression);
			}
		}
	}

	private void applySuppression(boolean scheduleVerification) {
		String reason;
		synchronized (this) {
			if (destroyed || !policy.shouldSuppressIme()) {
				return;
			}
			reason = pendingSuppressionReason;
		}

		applySoftInputState(true, reason);
		Window window = activity.getWindow();
		View decorView = window.getDecorView();
		View focusedView = activity.getCurrentFocus();
		IBinder windowToken = focusedView != null
			? focusedView.getWindowToken()
			: decorView.getWindowToken();
		InputMethodManager inputMethodManager =
			activity.getSystemService(InputMethodManager.class);
		if (inputMethodManager != null && windowToken != null) {
			inputMethodManager.hideSoftInputFromWindow(windowToken, 0);
		}
		if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
			WindowInsetsController insetsController = decorView.getWindowInsetsController();
			if (insetsController != null) {
				insetsController.hide(WindowInsets.Type.ime());
			}
		}

		if (scheduleVerification) {
			logger.record(
				"launcher ime suppressed",
				"reason=" + reason + "; delayedVerificationMs=" + DELAYED_SUPPRESSION_MS
			);
			synchronized (this) {
				if (!destroyed && policy.shouldSuppressIme()) {
					mainHandler.removeCallbacks(delayedSuppression);
					mainHandler.postDelayed(
						delayedSuppression,
						DELAYED_SUPPRESSION_MS
					);
				}
			}
		}
	}

	private void applySoftInputState(boolean suppress, String reason) {
		synchronized (this) {
			if (destroyed
				|| (suppress && !policy.shouldSuppressIme())
				|| (!suppress && policy.shouldSuppressIme())) {
				return;
			}
		}
		Window window = activity.getWindow();
		WindowManager.LayoutParams attributes = window.getAttributes();
		int state = suppress
			? WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_HIDDEN
			: WindowManager.LayoutParams.SOFT_INPUT_STATE_UNSPECIFIED;
		int softInputMode =
			(attributes.softInputMode
				& ~WindowManager.LayoutParams.SOFT_INPUT_MASK_STATE)
			| state;
		if ((softInputMode & WindowManager.LayoutParams.SOFT_INPUT_MASK_ADJUST) == 0) {
			softInputMode |= WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE;
		}
		window.setSoftInputMode(softInputMode);
		if (!suppress) {
			logger.record("launcher ime allowed", "reason=" + reason);
		}
	}

	private void cancelPendingSuppressionLocked() {
		mainHandler.removeCallbacks(immediateSuppression);
		mainHandler.removeCallbacks(delayedSuppression);
		pendingSuppressionReason = "";
	}
}
