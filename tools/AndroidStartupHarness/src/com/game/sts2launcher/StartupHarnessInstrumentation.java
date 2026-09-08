package com.game.sts2launcher;

import android.app.Activity;
import android.app.Instrumentation;
import android.content.Intent;
import android.os.Bundle;
import android.os.SystemClock;
import android.view.MotionEvent;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

/** No test hooks are added to the launcher and no launcher data is accessed. */
public final class StartupHarnessInstrumentation extends Instrumentation {
    private final StringBuilder report = new StringBuilder();

    @Override public void onCreate(Bundle arguments) { super.onCreate(arguments); start(); }

    @Override public void onStart() {
        Bundle result = new Bundle();
        try {
            blockedWorkerKeepsInputResponsive();
            pausedCompletionWaitsForResume();
            destroyedActivityDiscardsCompletion();
            failedPreparationResultRoutesOnce();
            result.putString("stream", report + "\nPASS: 4 Android main-Looper startup tests\n");
            finish(Activity.RESULT_OK, result);
        } catch (Throwable error) {
            result.putString("stream", report + "\nFAIL: " + android.util.Log.getStackTraceString(error));
            finish(Activity.RESULT_CANCELED, result);
        }
    }

    private StartupHarnessActivity launch() throws Exception {
        Intent intent = new Intent(getTargetContext(), StartupHarnessActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        StartupHarnessActivity activity = (StartupHarnessActivity) startActivitySync(intent);
        waitForIdleSync();
        AtomicBoolean focused = new AtomicBoolean();
        long deadline = SystemClock.elapsedRealtime() + 20000;
        do {
            onMain(() -> focused.set(activity.hasWindowFocus() && activity.button.isLaidOut()));
            if (focused.get()) { return activity; }
            Thread.sleep(50);
        } while (SystemClock.elapsedRealtime() < deadline);
        check(false, "Harness Activity did not acquire input focus within 20 seconds");
        return activity;
    }

    private void onMain(Runnable action) throws Exception {
        CountDownLatch done = new CountDownLatch(1);
        Throwable[] error = new Throwable[1];
        new android.os.Handler(android.os.Looper.getMainLooper()).post(() -> {
            try { action.run(); } catch (Throwable failure) { error[0] = failure; }
            finally { done.countDown(); }
        });
        check(done.await(2, TimeUnit.SECONDS), "Main Looper was blocked for over two seconds");
        if (error[0] != null) { throw new AssertionError("Main action failed", error[0]); }
    }

    private void awaitRoute(StartupHarnessActivity activity) throws Exception {
        long deadline = SystemClock.elapsedRealtime() + 3000;
        while (activity.routes.get() == 0 && SystemClock.elapsedRealtime() < deadline) {
            onMain(() -> {});
            Thread.sleep(10);
        }
        check(activity.routes.get() == 1, "Expected exactly one route");
    }

    private void blockedWorkerKeepsInputResponsive() throws Exception {
        StartupHarnessActivity activity = launch();
        try {
            onMain(() -> activity.begin("ready"));
            check(activity.entered.await(2, TimeUnit.SECONDS), "Worker did not begin");
            int[] location = new int[2];
            onMain(() -> {
                activity.button.getLocationOnScreen(location);
                location[0] += activity.button.getWidth() / 2;
                location[1] += activity.button.getHeight() / 2;
            });
            long start = SystemClock.uptimeMillis();
            sendPointerSync(MotionEvent.obtain(start, start, MotionEvent.ACTION_DOWN, location[0], location[1], 0));
            sendPointerSync(MotionEvent.obtain(start, SystemClock.uptimeMillis(), MotionEvent.ACTION_UP, location[0], location[1], 0));
            onMain(() -> {});
            check(activity.clicks.get() == 1, "Touch did not reach the button while worker was blocked");
            check(!activity.preparedOnMain, "Preparation ran on the main Looper");
            check(activity.routes.get() == 0, "Route occurred before preparation completed");
            activity.release.countDown();
            awaitRoute(activity);
            check(activity.routedOnMain, "Route did not run on the main Looper");
            report.append("PASS blocked worker: real touch input responded in ")
                .append(SystemClock.uptimeMillis() - start).append(" ms; worker/main thread separation verified\n");
        } finally { onMain(activity::finish); }
    }

    private void pausedCompletionWaitsForResume() throws Exception {
        StartupHarnessActivity activity = launch();
        try {
            onMain(() -> { activity.begin("ready"); callActivityOnPause(activity); });
            check(activity.entered.await(2, TimeUnit.SECONDS), "Worker did not begin");
            activity.release.countDown();
            check(activity.completed.await(2, TimeUnit.SECONDS), "Preparation did not finish");
            activity.worker.shutdown();
            check(activity.worker.awaitTermination(2, TimeUnit.SECONDS), "Completion was not posted");
            onMain(() -> {});
            check(activity.routes.get() == 0, "Paused Activity routed");
            onMain(() -> callActivityOnResume(activity));
            awaitRoute(activity);
            onMain(() -> activity.preparation.onResume());
            check(activity.routes.get() == 1, "Resume delivered a duplicate route");
            report.append("PASS paused completion: deferred until resume, delivered once\n");
        } finally { onMain(activity::finish); }
    }

    private void destroyedActivityDiscardsCompletion() throws Exception {
        StartupHarnessActivity activity = launch();
        onMain(() -> activity.begin("stale"));
        check(activity.entered.await(2, TimeUnit.SECONDS), "Worker did not begin");
        onMain(activity::finish);
        waitForIdleSync();
        check(activity.completed.await(2, TimeUnit.SECONDS), "Worker did not finish after destroy");
        check(activity.worker.awaitTermination(2, TimeUnit.SECONDS), "Worker did not terminate");
        onMain(() -> {});
        check(activity.isDestroyed(), "Activity was not destroyed");
        check(activity.routes.get() == 0, "Destroyed Activity received stale completion");
        report.append("PASS destroyed Activity: worker completion discarded\n");
    }

    private void failedPreparationResultRoutesOnce() throws Exception {
        StartupHarnessActivity activity = launch();
        try {
            onMain(() -> activity.begin("bootstrap_failed"));
            AtomicBoolean duplicateAccepted = new AtomicBoolean(true);
            onMain(() -> duplicateAccepted.set(activity.preparation.start(() -> "duplicate")));
            check(!duplicateAccepted.get(), "Duplicate startup accepted");
            activity.release.countDown();
            awaitRoute(activity);
            check("bootstrap_failed".equals(activity.routeValue), "Failure result was lost");
            check(activity.routedOnMain, "Failure result routed off the main Looper");
            report.append("PASS failed preparation result: routed once on main Looper; duplicate start rejected\n");
        } finally { onMain(activity::finish); }
    }

    private static void check(boolean condition, String message) {
        if (!condition) { throw new AssertionError(message); }
    }
}
