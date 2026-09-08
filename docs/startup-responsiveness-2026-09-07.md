# Startup responsiveness investigation — September 7, 2026

Implemented changes for slow Play, Android “wait or close” prompts, and large resource-loading bursts after the title screen appears. This is a local candidate, not a published release.

## Findings and changes

| Problem | Evidence | Change |
| --- | --- | --- |
| Android startup can block input before the game window exists | `LauncherActivity` called runtime preparation from `postOnAnimation` on the UI thread. `GodotApp.getCommandLine`, called from the Godot fragment's `onCreate`, then hashed the selected PCK three times and inspected its managed entries twice. Retained `artifacts/android/public-beta-fix16/window.txt` records an input-dispatch timeout with no focused window. | A process-wide serial worker performs bootstrap and launch-file preparation. Godot uses the fresh in-process hash/inspection snapshot and checks file stamps; missing/changed evidence returns to the launcher. A paused activity defers routing; a destroyed activity cannot route. |
| Play repeatedly rereads a multi-gigabyte archive | Launcher repair/routing checks call read-only identity inspection, which previously disabled both cache reads and writes. Counting-hasher regressions reproduce unnecessary full-PCK hashing despite a valid generation-bound digest. | Read-only inspection reuses the existing validated PCK digest without writing cache files. Generation, file snapshot and PCK structure checks remain; the source assembly is still freshly hashed by each managed identity inspection. |
| Cloud setup freezes the game thread while connecting | The first `SyncAsync` lock acquisition can finish synchronously, leading through `SteamCloudTransport` to `EnsureConnected`'s blocking connection wait before any incomplete await. Retained Stage 9 logs show a failed connection taking about 31 seconds before game startup. | Run synchronization on a worker and await it before the first settings/save read. Capture game status/settings before dispatch, then resume save loading on the original context. Timeouts, conflict behavior, automatic-upload policy and save ordering are unchanged. |
| Title-screen background preloading can create a large render-thread burst | The supplied game's `NGame` queues Common/menu assets after showing the menu. Its `NAssetLoader` calls `AssetLoadingSession.Process` on the render thread; that session requests up to 128 resources and drains the entire finalization queue. The unpatched real-class Godot probe exceeds the new outstanding-work bound. The historical title-screen freeze log stops between the 3-second and 10-second managed heartbeats while the Android activity remains alive. | Android Harmony prefixes cap ordinary outstanding loads, including ready-to-finalize resources, at eight; finalize at most four per frame; and yield between operations after a 4 ms phase budget. Keep remaining work queued, original failed-load handling, serial VFX loading and task completion. Cached paths can advance up to 64 candidates per frame. |

The last row is a mitigation for an observed unbounded workload, **not proof that every intermittent title-screen freeze has the same cause**. A single resource operation cannot be preempted; the existing synchronous fallback for a failed threaded resource can still stall. The 4 ms limit is checked between operations in each patched phase, not a hard bound on the whole frame. Actual Android graphics-driver stalls still need ARM64 gameplay testing.

Android's [ANR diagnosis guidance](https://developer.android.com/topic/performance/anrs/diagnose-and-fix-anrs) identifies slow main-thread startup, blocking I/O and lock contention as input-timeout causes. Godot's [thread-safety guidance](https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html) also distinguishes background loading from scene-tree operations. The fixes keep scene operations on their existing thread; they do not suppress Android ANRs or change battery settings.

## Verification

- Managed suite: 140 passed, no failures. Full output is `artifacts/startup-responsiveness-managed-tests.log`.
- Android JVM suite: 53 passed, no failures/errors/skips; includes slow preparation, pause/resume, destroyed-owner, once-only routing and changed-file invalidation.
- Save/gameplay safety probe: 12/12 passed. It uses a simulated remote and does not prove live Steam Cloud transport.
- Real game `AssetLoadingSession` in desktop Godot: baseline fails the backlog bound; patched run loads all 161 resources, including a cached resource and serial VFX, with at most four ordinary finalizations per frame and eight outstanding loads. A fully cached second session completes in three frames. Exact elapsed time varies with host load and is not a phone startup benchmark.
- Two deterministic budget tests cover FIFO/eventual completion and yielding after a costly item.
- Managed identity and cloud-worker regressions cover cache invalidation, no cache writes from read-only checks, synchronous connection work off the caller thread, and save loading back on the original context.

Reproduce the real-resource check with `scripts/test-asset-preload.ps1 -VerifyBaseline`. Logs are `artifacts/asset-preload-verification.log`, `artifacts/asset-preload-godot-baseline.log` and `artifacts/asset-preload-godot-patched.log`. Save probe output is `artifacts/startup-responsiveness-save-safety.log`.

## Emulator scope

The available AVD is API 36/x86_64. This application deliberately routes that architecture to its native fallback because the existing Godot/Mono runtime is unsupported there. Native startup/lifecycle/input tests therefore do not establish Android title-screen or gameplay stability. The separate `tools/AndroidStartupHarness` compiles the exact production preparation helper and exercises blocked preparation and activity lifecycle through a real Android main Looper. It uses its own package and preserves the launcher package's data.

The local candidate is `0.2.432-responsiveness-local` (version code `432001`),
package `com.sts2launcher.overhaul.fork.local`, with ARM64 and x86_64 libraries.
Build output is `artifacts/startup-responsiveness-apk-build.log`. The existing
nullable/deprecation warnings remain; no warning-policy changes are included.

APK build, content/ABI verification and Android crypto checks passed. Artifact:
`artifacts/android/StS2Launcher-v0.2.432-responsiveness-local-universal.apk`
(76,171,406 bytes). SHA-256:
`a1b901f806b6a1b3196bee45ab463f50ca66c7a372161825da53d17db2d7edef`.

The APK installed in place successfully on `emulator-5554`. The isolated Android
harness passed all four instrumentation tests: real touch delivery while its
worker remained blocked; completion deferred until resume and delivered once;
destroyed-activity completion discarded; and failure-result delivery on the main
Looper with duplicate starts rejected. The run returned `INSTRUMENTATION_CODE:
-1`; evidence is
`artifacts/emulator-validation-20260907/harness-instrumentation-after-system-dialog.txt`.

Earlier emulator attempts were invalidated by Android System UI/Pixel Launcher
ANR dialogs owning input focus before our test began. Those failures are retained
in the same evidence directory. Dismissing the system-launcher dialog allowed the
instrumentation to pass; no launcher application data was cleared. Host load and
these interruptions make before/after wall-clock startup comparisons unreliable.

The final production APK passed two cold launches, real diagnostics-toggle input,
Home/resume and an explicit game-launch request through its expected native x86
fallback. The installed package reports version `432001` and primary ABI
`x86_64`. The bounded log capture contains no app ANR/crash matches and shows
preparation on a worker followed by routing on the main thread. Results,
package metadata, logs and screenshots are in
`artifacts/emulator-validation-20260907/final/`; `result.json` records
`passed: true` and explicitly limits the scope to native x86 fallback.

There were no game files, saves or runtime cache in this emulator installation;
this test does not establish preservation of a populated phone installation.
The existing package was updated with `adb install -r`, without uninstalling,
clearing data or changing Android ANR/battery settings. Changes remain local;
no release was published.
