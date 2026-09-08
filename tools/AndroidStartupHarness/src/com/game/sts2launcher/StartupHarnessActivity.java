package com.game.sts2launcher;

import android.app.Activity;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.Button;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/** Isolated UI owner exercising the production preparation/lifecycle helper. */
public final class StartupHarnessActivity extends Activity {
    final ExecutorService worker = Executors.newSingleThreadExecutor();
    final Handler main = new Handler(Looper.getMainLooper());
    final CountDownLatch entered = new CountDownLatch(1);
    final CountDownLatch release = new CountDownLatch(1);
    final CountDownLatch completed = new CountDownLatch(1);
    final AtomicInteger routes = new AtomicInteger();
    final AtomicInteger clicks = new AtomicInteger();
    volatile boolean preparedOnMain;
    volatile boolean routedOnMain;
    volatile String routeValue;
    AndroidStartupPreparation<String> preparation;
    Button button;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        button = new Button(this);
        button.setText("Tap while startup worker is blocked");
        button.setOnClickListener(view -> clicks.incrementAndGet());
        setContentView(button);
        preparation = new AndroidStartupPreparation<>(worker, main::post, value -> {
            routedOnMain = Looper.myLooper() == Looper.getMainLooper();
            routeValue = value;
            routes.incrementAndGet();
        });
    }

    void begin(String result) {
        if (!preparation.start(() -> {
            preparedOnMain = Looper.myLooper() == Looper.getMainLooper();
            entered.countDown();
            try {
                if (!release.await(15, TimeUnit.SECONDS)) {
                    throw new AssertionError("Harness did not release the worker within 15 seconds");
                }
                return result;
            } catch (InterruptedException error) {
                Thread.currentThread().interrupt();
                return "interrupted";
            } finally {
                completed.countDown();
            }
        })) {
            throw new AssertionError("First preparation was rejected");
        }
    }

    @Override public void onResume() { super.onResume(); preparation.onResume(); }
    @Override public void onPause() { preparation.onPause(); super.onPause(); }
    @Override public void onDestroy() {
        preparation.onDestroy();
        release.countDown();
        worker.shutdown();
        super.onDestroy();
    }
}
