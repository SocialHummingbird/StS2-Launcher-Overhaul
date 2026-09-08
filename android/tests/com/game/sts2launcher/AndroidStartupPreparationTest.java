package com.game.sts2launcher;

import org.junit.Test;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Executor;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public final class AndroidStartupPreparationTest {
	@Test
	public void slowPreparationDoesNotBlockTheCallerOrDeliverOnTheWorker() throws Exception {
		ExecutorService worker = Executors.newSingleThreadExecutor();
		QueueExecutor main = new QueueExecutor();
		CountDownLatch preparing = new CountDownLatch(1);
		CountDownLatch release = new CountDownLatch(1);
		CountDownLatch returned = new CountDownLatch(1);
		List<String> routes = new ArrayList<>();
		AndroidStartupPreparation<String> task = new AndroidStartupPreparation<>(
			worker, main, routes::add);
		task.onResume();
		try {
			Thread caller = new Thread(() -> {
				task.start(() -> {
					preparing.countDown();
					try {
						release.await();
					} catch (InterruptedException error) {
						Thread.currentThread().interrupt();
					}
					return "ready";
				});
				returned.countDown();
			});
			caller.start();
			assertTrue("preparation never started", preparing.await(2, TimeUnit.SECONDS));
			assertTrue("startup blocked its caller", returned.await(1, TimeUnit.SECONDS));
			assertTrue(routes.isEmpty());
			release.countDown();
			worker.shutdown();
			assertTrue(worker.awaitTermination(2, TimeUnit.SECONDS));
			assertTrue("worker routed without main-thread delivery", routes.isEmpty());
			main.runNext();
			assertEquals(List.of("ready"), routes);
		} finally {
			release.countDown();
			worker.shutdownNow();
		}
	}

	@Test
	public void completionWhilePausedWaitsForResumeAndRoutesOnlyOnce() {
		QueueExecutor worker = new QueueExecutor();
		QueueExecutor main = new QueueExecutor();
		List<String> routes = new ArrayList<>();
		AndroidStartupPreparation<String> task = new AndroidStartupPreparation<>(worker, main, routes::add);
		task.onResume();
		assertTrue(task.start(() -> "ready"));
		assertFalse(task.start(() -> "duplicate"));
		task.onPause();
		worker.runNext();
		main.runNext();
		assertTrue(routes.isEmpty());
		task.onResume();
		task.onResume();
		assertEquals(List.of("ready"), routes);
	}

	@Test
	public void destroyedActivityCannotRouteFromQueuedCompletion() {
		QueueExecutor worker = new QueueExecutor();
		QueueExecutor main = new QueueExecutor();
		List<String> routes = new ArrayList<>();
		AndroidStartupPreparation<String> task = new AndroidStartupPreparation<>(worker, main, routes::add);
		task.onResume();
		task.start(() -> "ready");
		worker.runNext();
		task.onDestroy();
		main.runNext();
		task.onResume();
		assertTrue(routes.isEmpty());
		assertFalse(task.start(() -> "late"));
	}

	@Test
	public void destroyedActivitySkipsPreparationThatHasNotStarted() {
		QueueExecutor worker = new QueueExecutor();
		QueueExecutor main = new QueueExecutor();
		AndroidStartupPreparation<String> task = new AndroidStartupPreparation<>(worker, main,
			result -> { throw new AssertionError("destroyed activity routed"); });
		task.start(() -> { throw new AssertionError("destroyed activity prepared files"); });
		task.onDestroy();
		worker.runNext();
		assertTrue(main.isEmpty());
	}

	private static final class QueueExecutor implements Executor {
		private final ArrayDeque<Runnable> queue = new ArrayDeque<>();

		@Override
		public synchronized void execute(Runnable action) { queue.add(action); }
		synchronized boolean isEmpty() { return queue.isEmpty(); }
		void runNext() {
			Runnable action;
			synchronized (this) { action = queue.remove(); }
			action.run();
		}
	}
}
