package com.game.sts2launcher;

import java.util.concurrent.Executor;
import java.util.function.Consumer;
import java.util.function.Supplier;

/** Runs file preparation on a worker and routes only a live, resumed activity. */
final class AndroidStartupPreparation<T> {
	private final Executor worker;
	private final Executor main;
	private Consumer<T> route;
	private volatile boolean destroyed;
	// Everything except the destroyed check in the worker belongs to the main thread.
	private boolean started;
	private boolean resumed;
	private boolean ready;
	private T result;

	AndroidStartupPreparation(Executor worker, Executor main, Consumer<T> route) {
		this.worker = worker;
		this.main = main;
		this.route = route;
	}

	boolean start(Supplier<T> prepare) {
		if (started || destroyed) {
			return false;
		}
		started = true;
		worker.execute(() -> {
			if (destroyed) {
				return;
			}
			T prepared = prepare.get();
			main.execute(() -> {
				if (destroyed) {
					return;
				}
				result = prepared;
				ready = true;
				deliverIfResumed();
			});
		});
		return true;
	}

	void onResume() {
		resumed = true;
		deliverIfResumed();
	}

	void onPause() {
		resumed = false;
	}

	void onDestroy() {
		destroyed = true;
		result = null;
		route = null;
	}

	private void deliverIfResumed() {
		if (destroyed || !resumed || !ready) {
			return;
		}
		ready = false;
		T prepared = result;
		result = null;
		route.accept(prepared);
	}
}
