package com.game.sts2launcher;

import android.animation.Animator;
import android.animation.AnimatorListenerAdapter;
import android.animation.AnimatorSet;
import android.animation.ValueAnimator;
import android.app.Activity;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.provider.Settings;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.LinearInterpolator;
import android.view.inputmethod.InputMethodManager;
import android.widget.FrameLayout;

import androidx.core.splashscreen.SplashScreen;

final class AndroidBootTransitionController {
	private static final String TAG = "STS2Mobile";
	private static final long WATCHDOG_MS = 30_000L;
	private static final int FALLBACK_SPLASH_CONTAINER_DP = 198;
	private static boolean processTransitionConsumed;

	interface TimelineLogger {
		void record(String phase, String detail);
	}

	private final Activity activity;
	private final TimelineLogger logger;
	private final Handler mainHandler = new Handler(Looper.getMainLooper());
	private final AndroidBootTransitionPolicy.Decision decision;
	private final AndroidBootTransitionPolicy.ReadinessGate readinessGate =
		new AndroidBootTransitionPolicy.ReadinessGate();
	private final boolean reducedMotion;
	private final AndroidBootTransitionSoundSession soundSession;
	private final Runnable watchdog = this::handleWatchdog;

	private FrameLayout overlay;
	private AndroidBootIdentityView identityView;
	private AnimatorSet animation;
	private AndroidBootSequence.Phase activeSequencePhase;
	private float registrationOffsetPx;
	private boolean destroyed;
	private boolean launcherReadyLogged;

	static AndroidBootTransitionController install(
		Activity activity,
		SplashScreen splashScreen,
		boolean pendingNormalLaunch,
		boolean pendingSafeLaunch,
		boolean explicitSkip,
		TimelineLogger logger
	) {
		AndroidBootTransitionPolicy.Decision decision;
		synchronized (AndroidBootTransitionController.class) {
			decision = AndroidBootTransitionPolicy.resolve(
				pendingNormalLaunch,
				pendingSafeLaunch,
				explicitSkip,
				processTransitionConsumed
			);
			if (decision.shouldPlay()) {
				processTransitionConsumed = true;
			}
		}

		AndroidBootTransitionController controller = new AndroidBootTransitionController(
			activity,
			decision,
			logger,
			new SilentBootTransitionSound()
		);
		controller.installSplashExitListener(splashScreen);
		return controller;
	}

	private AndroidBootTransitionController(
		Activity activity,
		AndroidBootTransitionPolicy.Decision decision,
		TimelineLogger logger,
		AndroidBootTransitionSound sound
	) {
		this.activity = activity;
		this.decision = decision;
		this.logger = logger;
		this.reducedMotion = detectReducedMotion(activity);
		this.soundSession = new AndroidBootTransitionSoundSession(
			sound,
			decision.shouldPlay() && !reducedMotion
		);
	}

	private void installSplashExitListener(SplashScreen splashScreen) {
		if (!decision.shouldPlay()) {
			soundSession.close();
			logger.record("boot transition skipped", decision.reason());
			return;
		}

		logger.record(
			"boot transition installed",
			"policy=" + decision.reason() + "; reducedMotion=" + reducedMotion
		);
		if (reducedMotion) {
			logger.record("boot transition reduced-motion", "directFadeMs=200");
		}
		splashScreen.setOnExitAnimationListener(provider -> {
			try {
				if (destroyed || readinessGate.isTerminal()) {
					logger.record(
						"boot transition splash-exit ignored",
						destroyed ? "activity-destroyed" : "transition-terminal"
					);
					return;
				}
				int splashContainerSize = 0;
				try {
					View systemIcon = provider.getIconView();
					if (systemIcon != null) {
						splashContainerSize = Math.min(systemIcon.getWidth(), systemIcon.getHeight());
					}
				} catch (Throwable error) {
					logger.record(
						"boot transition sizing fallback",
						"provider-icon-unavailable:" + error.getClass().getSimpleName()
					);
				}
				attachOverlay(splashContainerSize);
			} catch (Throwable error) {
				mainHandler.removeCallbacks(watchdog);
				soundSession.close();
				removeOverlay();
				Log.e(TAG, "Boot transition overlay installation failed", error);
				logger.record(
					"boot transition skipped",
					"overlay-install-failed:" + error.getClass().getSimpleName()
				);
			} finally {
				provider.remove();
			}
		});
	}

	void notifyLauncherReady() {
		mainHandler.post(() -> {
			if (destroyed || launcherReadyLogged) {
				return;
			}
			launcherReadyLogged = true;
			suppressInputMethod();
			logger.record(
				"boot transition launcher-ready",
				decision.shouldPlay() ? "readiness cached or consumed" : "transition inactive"
			);
			if (decision.shouldPlay()
				&& readinessGate.onLauncherReady() == AndroidBootTransitionPolicy.Event.START) {
				startAnimation();
			}
		});
	}

	void pauseSound() {
		soundSession.pause();
	}

	void resumeSound() {
		soundSession.resume();
	}

	void destroy() {
		destroyed = true;
		mainHandler.removeCallbacks(watchdog);
		cancelAnimation();
		removeOverlay();
	}

	private void attachOverlay(int splashContainerSize) {
		if (destroyed || readinessGate.isTerminal() || overlay != null) {
			return;
		}

		overlay = new FrameLayout(activity);
		overlay.setBackgroundColor(activity.getColor(R.color.sts2_launch_background));
		overlay.setClickable(true);
		overlay.setFocusable(true);
		overlay.setFocusableInTouchMode(true);
		overlay.setOnTouchListener((view, event) -> true);
		overlay.setElevation(1000.0f);

		if (splashContainerSize <= 0) {
			splashContainerSize = Math.round(
				FALLBACK_SPLASH_CONTAINER_DP * activity.getResources().getDisplayMetrics().density
			);
		}
		identityView = new AndroidBootIdentityView(activity);
		identityView.setGodotContainerSizePx(splashContainerSize);
		identityView.resetVisualState();
		registrationOffsetPx = AndroidBootSequence.REGISTRATION_OFFSET_DP
			* activity.getResources().getDisplayMetrics().density;
		overlay.addView(
			identityView,
			new FrameLayout.LayoutParams(
				ViewGroup.LayoutParams.MATCH_PARENT,
				ViewGroup.LayoutParams.MATCH_PARENT
			)
		);

		ViewGroup decor = (ViewGroup) activity.getWindow().getDecorView();
		decor.addView(
			overlay,
			new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MATCH_PARENT,
				ViewGroup.LayoutParams.MATCH_PARENT
			)
		);
		overlay.bringToFront();
		suppressInputMethod();
		mainHandler.post(this::suppressInputMethod);
		mainHandler.postDelayed(watchdog, WATCHDOG_MS);

		if (readinessGate.onOverlayAttached() == AndroidBootTransitionPolicy.Event.START) {
			startAnimation();
		}
	}

	private void startAnimation() {
		if (destroyed || overlay == null || animation != null) {
			return;
		}

		animation = reducedMotion ? createReducedMotionAnimation() : createFullAnimation();
		animation.addListener(new AnimatorListenerAdapter() {
			@Override
			public void onAnimationCancel(Animator animator) {
				soundSession.close();
			}

			@Override
			public void onAnimationEnd(Animator animator) {
				completeAnimation();
			}
		});
		animation.start();
	}

	private AnimatorSet createReducedMotionAnimation() {
		ValueAnimator fade = ValueAnimator.ofInt(
			0,
			(int) AndroidBootSequence.REDUCED_MOTION_DURATION_MS
		);
		fade.setDuration(AndroidBootSequence.REDUCED_MOTION_DURATION_MS);
		fade.setInterpolator(new LinearInterpolator());
		fade.addUpdateListener(animator -> {
			if (overlay != null) {
				overlay.setAlpha(AndroidBootSequence.reducedMotionOverlayOpacity(
					(Integer) animator.getAnimatedValue()
				));
			}
		});
		AnimatorSet result = new AnimatorSet();
		result.play(fade);
		return result;
	}

	private AnimatorSet createFullAnimation() {
		activeSequencePhase = null;
		applySequenceFrame(AndroidBootSequence.frameAt(0L));
		ValueAnimator timeline = ValueAnimator.ofInt(0, (int) AndroidBootSequence.FULL_DURATION_MS);
		timeline.setDuration(AndroidBootSequence.FULL_DURATION_MS);
		timeline.setInterpolator(new LinearInterpolator());
		timeline.addUpdateListener(animator -> applySequenceFrame(
			AndroidBootSequence.frameAt((Integer) animator.getAnimatedValue())
		));
		AnimatorSet result = new AnimatorSet();
		result.play(timeline);
		return result;
	}

	private void applySequenceFrame(AndroidBootSequence.Frame frame) {
		if (identityView == null || overlay == null) {
			return;
		}
		if (activeSequencePhase != frame.phase()) {
			activeSequencePhase = frame.phase();
			soundSession.enterPhase(frame.phase());
			logger.record(
				"boot transition phase",
				frame.phase().timelineName() + "; timelineMs=" + frame.phase().startMs()
			);
		}

		identityView.setGodotOpacity(frame.godotOpacity());
		identityView.setGodotScale(frame.godotScale());
		identityView.setIdentityOpacity(frame.identityOpacity());
		identityView.setIdentityScale(frame.identityScale());
		identityView.setCyanOpacity(frame.cyanOpacity());
		identityView.setOrangeOpacity(frame.orangeOpacity());
		identityView.setResolvedOpacity(frame.resolvedOpacity());
		identityView.setWordmarkOpacity(frame.wordmarkOpacity());
		identityView.setCyanOffsetX(frame.cyanOffsetX() * registrationOffsetPx);
		identityView.setCyanOffsetY(frame.cyanOffsetY() * registrationOffsetPx);
		identityView.setOrangeOffsetX(frame.orangeOffsetX() * registrationOffsetPx);
		identityView.setOrangeOffsetY(frame.orangeOffsetY() * registrationOffsetPx);
		identityView.setRevealProgress(frame.revealProgress());
		identityView.setPulseStrength(frame.pulseStrength());
		overlay.setAlpha(frame.overlayOpacity());
	}

	private void completeAnimation() {
		if (readinessGate.onCompleted() != AndroidBootTransitionPolicy.Event.COMPLETED) {
			return;
		}
		mainHandler.removeCallbacks(watchdog);
		soundSession.close();
		removeOverlay();
		animation = null;
		activeSequencePhase = null;
		logger.record(
			"boot transition completed",
			reducedMotion
				? "reduced-motion; durationMs=" + AndroidBootSequence.REDUCED_MOTION_DURATION_MS
				: "full-sequence; durationMs=" + AndroidBootSequence.FULL_DURATION_MS
		);
	}

	private void handleWatchdog() {
		if (destroyed
			|| readinessGate.onTimeout() != AndroidBootTransitionPolicy.Event.TIMED_OUT) {
			return;
		}
		cancelAnimation();
		removeOverlay();
		logger.record("boot transition timeout", "watchdogMs=" + WATCHDOG_MS);
	}

	private void cancelAnimation() {
		soundSession.close();
		if (animation == null) {
			return;
		}
		animation.removeAllListeners();
		animation.cancel();
		animation = null;
	}

	private void removeOverlay() {
		if (overlay != null) {
			ViewParentCompat.removeFromParent(overlay);
		}
		overlay = null;
		identityView = null;
		activeSequencePhase = null;
	}

	private void suppressInputMethod() {
		if (overlay == null) {
			return;
		}
		overlay.requestFocus();
		InputMethodManager inputMethodManager = activity.getSystemService(InputMethodManager.class);
		if (inputMethodManager != null && overlay.getWindowToken() != null) {
			inputMethodManager.hideSoftInputFromWindow(overlay.getWindowToken(), 0);
		}
	}

	private static boolean detectReducedMotion(Activity activity) {
		float animatorScale = 1.0f;
		try {
			animatorScale = Settings.Global.getFloat(
				activity.getContentResolver(),
				Settings.Global.ANIMATOR_DURATION_SCALE,
				1.0f
			);
		} catch (Exception ignored) {
			// ValueAnimator remains the authoritative fallback.
		}
		boolean animatorsEnabled = Build.VERSION.SDK_INT < Build.VERSION_CODES.O
			|| ValueAnimator.areAnimatorsEnabled();
		return AndroidBootTransitionPolicy.isReducedMotion(animatorScale, animatorsEnabled);
	}

	private static final class ViewParentCompat {
		private ViewParentCompat() {
		}

		static void removeFromParent(View view) {
			if (view.getParent() instanceof ViewGroup) {
				((ViewGroup) view.getParent()).removeView(view);
			}
		}
	}
}
