# Launcher loading screen staging

## Current cold-start path

The launch/loading path is split across two deliberately separate surfaces:

- Android cold-start transition: `GodotAppSplashTheme` displays the official Godot Engine mark while Android and Godot initialize. Once the managed launcher has rendered two process frames and one post-draw frame, native Android transitions to the StS2 Launcher identity and then reveals the launcher.
- Godot post-launch warmup/status: `ShaderWarmupScreen` displays shader warmup progress, and `LauncherStartupStatus` displays game-startup phase text after the launcher closes.

The cold-start transition is silent and uses vector assets. It borrows the structure of a deliberate two-stage boot confirmation, but it does not reproduce PlayStation branding, timing, audio, or assets.

## Native transition policy

- The full transition runs only for the first ordinary launcher start in a process.
- Pending Start Game, Safe Start, return-to-launcher restarts, and deliberate game restarts skip it.
- The Godot mark remains visible for real initialization time; the exact four-second sequence starts only after the launcher-first-frame bridge fires.
- Reduced-motion devices receive a direct 200 ms fade with no icon hold.
- The overlay blocks touch only while visible and is removed from the view hierarchy on completion.
- A 30-second watchdog removes the overlay if launcher construction or readiness signalling fails.
- Repeated splash-exit callbacks are ignored after completion or timeout, and IME suppression is posted twice during overlay attachment to cover Samsung window-focus timing.
- `last_startup_timeline.txt` records installed, skipped, launcher-ready, completed, reduced-motion, and timeout phases.
- The generated bootstrap project disables Godot's separate boot image. Downloaded game PCKs and shader-warmup behavior are unchanged.

## Visual sequence

`AndroidBootSequence` owns one contiguous 4,000 ms clock. Every visual channel and future sound cue is derived from these phase boundaries:

| Phase | Timeline | Presentation |
| --- | ---: | --- |
| `registration` | 0-450 ms | The Godot mark fades and scales from 100% to 84% while offset cyan and orange identity layers establish the next mark. |
| `separation` | 450-700 ms | Both marks clear for a deliberate 250 ms near-black separation. |
| `identity-reveal` | 700-1,500 ms | Cyan and orange layers converge, the identity scales from 82% through 103.5%, and the resolved full-colour mark appears. |
| `final-settle` | 1,500-1,850 ms | The resolved mark compresses through 99.2%, returns once to 100%, and then reveals the live `StS2 LAUNCHER` wordmark. |
| `confirmation-hold` | 1,850-3,050 ms | The completed identity remains stable for 1,200 ms. |
| `identity-pulse` | 3,050-3,300 ms | One 1.4% pulse gives the confirmed identity a restrained tactile response. |
| `launcher-handoff` | 3,300-4,000 ms | The overlay fades out over the already-rendered launcher and is then removed from the hierarchy. |

This timing starts only after native overlay attachment and the managed first-frame readiness signal are both present. Runtime initialization before readiness is not forced into a fixed duration.

## Silent sound extension point

- `AndroidBootTransitionSound` defines `play(cue)`, `pause()`, `resume()`, and `close()` without selecting an Android playback API.
- `AndroidBootTransitionCue` maps one cue to the start of each of the seven visual phases. The animation clock remains authoritative; sound cannot delay, pause, or retime it.
- `AndroidBootTransitionSoundSession` emits each cue at most once, suppresses all staged cues for skipped and reduced-motion paths, discards phases reached while paused instead of replaying stale sound, and closes on completion, cancellation, timeout, activity destruction, or overlay-install failure.
- `SilentBootTransitionSound` is the active implementation. It contains no audio asset, playback call, audio-focus request, permission, service, dependency, or preference.
- Android hardware inspection still sees Godot's existing OpenSL/FMOD runtime stream. That stream is part of normal Godot initialization and is not a boot-transition cue or an implementation of this extension point.

A future sound implementation must fit the existing cue lifecycle. It must not move phase boundaries, make launcher readiness wait for audio, replay missed cues after resume, or make sound failure block overlay cleanup.

## Branding restrictions

The permitted identity is the attributed official Godot Engine mark followed by project-owned StS2 Launcher artwork and the live `StS2 LAUNCHER` wordmark. The launcher name identifies the tool and must not imply ownership of Slay the Spire 2 or an official mobile port. The transition may use the general idea of a deliberate two-stage boot confirmation, but contributors must not:

- use Sony, PlayStation, PS1, or Sony Computer Entertainment logos, wordmarks, symbols, artwork, or trade dress;
- copy or derive the PlayStation startup sound, add a sound-alike recording, or market a future cue as PlayStation audio;
- reproduce the console sequence's exact timing, easing, geometry, composition, or animation assets; or
- describe the result as PlayStation branding, an official PlayStation-style boot screen, or an affiliation or endorsement.

The Godot mark remains subject to its Creative Commons Attribution 4.0 terms and Godot press-kit guidance. Its use identifies the runtime and does not imply Godot endorsement of StS2 Launcher.

## Shader and game boundary

- Shader warmup remains a separate Godot progress screen shown later when Start Game requires real first-run work. Its progress, watchdog, recovery behavior, and potentially 45-90 second runtime are not hidden by the native boot identity.
- The transition changes only the launcher/bootstrap presentation. Disabling the bootstrap project's duplicate Godot boot image does not alter a downloaded game PCK.
- No downloaded Slay the Spire 2 asset, assembly, PCK, shader cache, save, mod, or other core-game file is patched for this feature.
- Normal launch and Safe Start retain their existing startup, renderer, shader-warmup, and recovery routes after the native transition policy skips the ceremony.

## Stage 2 adaptive scaling patch

Implemented in this stage:

- The earlier native splash replaced Android's default system app icon with the scalable launcher vector; Stage 3 below supersedes that treatment with the readiness-driven Godot-to-StS2 Launcher sequence.
- Shader warmup panel scales from the short viewport edge, not only the long edge, so short/wide Samsung-style landscape screens do not inflate text and controls beyond the available height.
- Shader warmup panel width is clamped with safe side margins and keeps a bounded aspect-friendly layout.
- Warmup status/detail labels use word wrapping.
- Android shader warmup uses the launcher compact touch-scale floor, a mobile-width compact panel, compact panel padding, and the styled percentage progress bar.
- Game startup status is anchored top-wide with viewport-derived safe margins instead of a fixed `(24, 24)` position.
- Android game startup status now uses a framed mobile-width status card after the launcher closes, instead of floating unframed text.
- Successful startup cleanup now frees the whole Android startup status root container, not only the message label, so the `Starting Game` card does not remain over the main menu after `NGame.GameStartup` reaches `NMainMenu`.

## Stage 3 branded boot transition

Implemented in this stage:

- Android splash-safe Godot Engine vector mark on the existing `#0E141D` background.
- Readiness-driven Godot-to-StS2 Launcher transition with matching centered bounds.
- Transparent cyan, orange, and full-colour StS2 Launcher boot vectors replace the square adaptive icon in the controlled reveal.
- The boot identity scales from 52% of the viewport short edge within a 144-232 dp clamp, with `StS2 LAUNCHER` rendered as responsive live Android text rather than image content.
- `AndroidBootIdentityView` owns the full native presentation surface: official Godot mark, independent cyan/orange registration layers, resolved mark, reveal clipping, pulse overlay, and wordmark.
- Its pure visual-state model exposes independently testable opacity, scale, X/Y registration offsets, reveal progress, and pulse strength while the controller retains startup policy and lifecycle ownership.
- `AndroidBootSequence` defines the seven named, contiguous timeline phases and samples every visual channel from one authoritative 4,000 ms clock. The controller logs phase entry, which provides a synchronization point for later sound work without loading or playing audio now.
- `AndroidBootTransitionSound` and the phase-aligned cue definitions provide a replaceable sound boundary. `SilentBootTransitionSound` is the active implementation; staged cues are suppressed for skipped and reduced-motion transitions, and the session closes on completion, cancellation, timeout, activity destruction, or overlay-install failure.
- Activity pause/resume is forwarded only to the sound boundary. Calls are idempotent, phases reached while paused are discarded instead of replayed out of sync, and the visual sequence is never paused or retimed.
- The sound boundary contains no audio assets, playback API, audio-focus request, permission, service, dependency, or user-facing setting. A future audible implementation can replace the silent implementation without changing animation timing or sequencing.
- The staged reveal brings offset cyan and orange layers into registration, resolves them into the full-colour mark, reveals the live wordmark during the settle, holds the confirmed identity, emits one restrained pulse, and then fades the overlay into the ready launcher.
- Managed `NotifyLauncherFirstFrameReady()` bridge after two process frames and `FramePostDraw`.
- Cold-start, pending-game, Safe Start, explicit-restart, and process-consumption policy.
- Reduced-motion handling, early-readiness caching, duplicate-signal protection, lifecycle cleanup, and a 30-second fail-open watchdog.
- Pure Java policy/state tests and Android release resource/Java compilation coverage.
- Godot Engine logo attribution and explicit non-PlayStation branding guidance.

## Connected hardware validation

The corrected local ARM64 build `0.2.407-boot-hardware-resume-local` (`407001`) was installed on a Samsung `SM-F966B` running Android 16 / API 36. The installed `base.apk` exactly matched the tested artifact SHA-256 `f4ed4266a2019669383d16b5a4108601f4fe41a9a5107969f85532f74387434a`.

The connected-device pass covered portrait and landscape cold starts, reduced motion, Home/resume during and after the sequence, rotation during startup, secure lock-screen interruption and manual unlock, post-removal touch input, and native skip policy for deliberate restart, normal game handoff, and Safe Start.

The first `0.2.406` pass found a Samsung lifecycle defect: a repeated splash-exit callback after terminal Home/resume could reattach the overlay and expose the keyboard beneath it. The `0.2.407` correction rejects overlay attachment after completion/timeout and posts a second IME suppression pass. Dense Home/resume and secure-lock retesting did not reproduce the defect.

Validation results:

- the then-current sequence logged the exact 2,480 ms phase timeline in portrait, landscape, rotation, and lifecycle-interruption scenarios;
- reduced motion logged the direct 200 ms path;
- 87 screenshots contained no accidental near-white app frame, adaptive-icon square background, launcher control above the overlay, or visible keyboard;
- the only three mostly black captures were Samsung secure-keyguard privacy frames from the automated unlock attempt, and the manual-unlock retry returned immediately to an intact launcher;
- Help and Home taps proved that the removed overlay no longer intercepted input;
- explicit restart, normal game handoff, and Safe Start logged `explicit-restart-handoff`, `pending-game-launch`, and `safe-start` skip reasons respectively;
- focused logs contained no `NativeFallback`, fatal Android exception, fatal signal, app ANR, unexpected app process death, or transition timeout; and
- managed Release compilation, all Java policy/identity/sequence/sound tests, ARM64 APK structure, crypto-patch verification, installed hash comparison, and diff checks passed.

The normal and Safe Start probes stopped after their native skip markers; this pass is not new downstream `NMainMenu` or shader-warmup evidence. The implementation itself does not alter those paths. Steam Cloud Push was not run, no release was published, and no downloaded/core-game file was modified. The local evidence bundle is `artifacts/android/boot-transition-hardware-0.2.407-20260717-115657/`.

That hardware pass validated the original transition policy and lifecycle safeguards. A later exact non-debuggable `v0.2.416` pass on the same Samsung ARM64 device validated the larger four-second choreography, `StS2 LAUNCHER` wordmark, launcher handoff with `mInputShown=false`, public Start Game through real `NMainMenu`, and heartbeats through 60 seconds. See [v0.2.416 release notes](release-notes/v0.2.416-startup-recovery-ime.md).

Current source also contains post-release native first-frame, splash, task-routing, and fallback-recovery changes. The API 36 x86_64 emulator validates those native paths only; production x86_64 cannot run the managed launcher or game. The exact current-source candidate therefore still requires ARM64 visual and startup validation before release.

## Remaining stages

Stage 4 should add richer launch progress copy:

- current phase such as `Checking files`, `Preparing Steam session`, `Starting game`, or `Recovering startup`;
- last meaningful launcher log line;
- a visible diagnostics shortcut if startup stalls.

Stage 5 should improve the longer-running warmup/loading surface without hiding real progress:

- scalable icon/logo treatment using the orange/cyan launcher identity;
- real Godot controls for all text and actions;
- responsive composition for phones, foldables, tablets, notches, and navigation bars;
- no text baked into images.

The connected Samsung foldable baseline is complete. Wider matrix coverage remains desirable for device-specific sizing and OEM splash behavior:

- short/wide Samsung-style landscape screen;
- normal phone landscape;
- foldable inner display;
- tablet landscape;
- high-DPI small-height viewport.

For each additional device, capture force-stop cold launch in portrait and landscape, reduced motion, Home/resume, rotation, locked-screen return, game restart, Safe Start, and return-to-launcher. Confirm there is no logo flash or accidental blank frame, launcher controls stay behind the overlay, touch works after removal, and game startup behavior is unchanged. Steam Cloud Push remains outside this validation.
