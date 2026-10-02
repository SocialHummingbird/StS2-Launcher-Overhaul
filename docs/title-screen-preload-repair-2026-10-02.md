# Title-screen preload repair — October 2, 2026

The existing Android preload budget did not install on the prepared game assembly. `AndroidAssemblyPublicizer` makes game members public, while `AndroidAssetPreloadPatches.RequireField` and `RequireMethod` searched only nonpublic instance members. The released v434 phone log fails on `_loading changed`; the real-publicizer regression reproduces that failure. PR [#48](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/pull/48) is continued and brought up to current main, preserving the subsequent preload repairs.

## Repair and retained ownership

Both existing lookup helpers now use one class-private `Instance | Public | NonPublic` constant. All six fields (`_loading`, `_toLoad`, `_finalizing`, `_cache`, `_totalLoaded`, `_assetCache`) and all three methods (`ProcessLoadingQueue`, `FinalizeLoading`, `CheckLoadingStatus`) still require their original types, parameter counts and return types. Installation and post-mod inspection share those helpers.

The existing loader remains authoritative: eight outstanding requests, four finalizations per frame, one synchronous fallback per pass, four-millisecond cooperative budget, cache accounting, failure handling, VFX ordering and eventual completion. Harmony inspection reads declarations without executing factories. No production class, alternative loader, watchdog, renderer policy, preference, bridge signature or persisted schema was added. The Android startup-task lifetime anchor is unchanged.

The older PR's two partial test scaffolding files are superseded by private fixtures in the existing `Program.AssetPreloadBudgetTests.cs`. The fixture copies the real licensed reference assembly, runs the actual publicizer, loads both original/private and publicized/public variants in collectible contexts, and resolves dependencies from existing runtime inputs. Missing/incorrect field and method shapes remain rejected in both visibility forms. The original licensed assembly's hash remains unchanged.

The first candidate still froze. The existing Godot Mono bootstrap has a second
defect: creating its first managed delegate leaves the native engine thread
GC-unsafe. A loader worker can then wait for that thread to suspend while the
engine waits on the canvas shader mutex. The existing bootstrap now resolves
Mono's GC-safe transition and calls it immediately after first delegate creation.
This adds 14 native production lines, with no alternate bridge or state owner.
Later delegate creation and managed callbacks retain Mono's own transition logic.

Audit of this repair against current main: production changes add 20 lines and
remove two (six added/two removed in the preload class; 14 added in the existing
native bootstrap). Larger additions are behavioral fixtures and existing-script
plumbing, outside production. No old implementation is left running beside a
replacement. The two superseded test partials existed only on the older PR branch.

## Local validation

| Check | Result |
| --- | --- |
| Recorded baseline | 154 managed, 56 JVM, 12 save-safety scenarios passed |
| Before correction | Original 154 managed tests pass; both new regressions fail at the nonpublic-only lookup |
| After correction | 156 managed tests passed |
| Fresh Android/JVM suite | 56 tests, zero failures/errors |
| Save/lifetime probe | 12/12 passed, simulated remote |
| Existing real-resource probe | Original backlog reproduced; patched ownership/factory safety, fallback/failure/cache/VFX/completion passed; 161 resources, maximum eight outstanding/four finalized |
| Actual Android Mono regression | Original host blocks worker GC and times out; repaired transition completes 64 collections and subsequent callbacks on the same staged runtime |
| APK | ARM64 build, contents and crypto checks passed; unchanged package/certificate, increased code versus retained v434; in-place install succeeded |
| Independent code review | No Critical, Important or Minor findings; physical acceptance required before merge |

These desktop/JVM checks do not prove phone stability or a real Steam transfer.

## Device evidence and acceptance

Connected Samsung SM-F971B, Android 17/API 37, ARM64, Adreno 840, existing Vulkan mobile renderer. Branch `public-beta`, game identity `9eb628025c148dd818cb80ca514892303bd2f8aeb7d2602aac2a92efac850b57`, runtime pack `public-beta-acbabc37c121`, schema 26. The original BaseLib, Import Vanilla Saves and Quick Restart selection remains enabled. BaseLib's reported partial activation is the retained intentional Android compatibility path.

The retained v434 capture reaches handoff without Common completion. The first
v435 candidate installs all three budgets and passes three complete cold Modded
and four Vanilla launches, but its fifth cold Modded attempt freezes before Common
completion (attempt `fec3bfa8f18440b1a85abad0cc872c69`, PID 2181). A preliminary
Modded launch only tested title interaction. None count toward r2 acceptance.

Continuous logcat, unchanged screenshots, the current-process native dump,
bugreport and a 20-second Perfetto trace were captured before restarting. Matching
Mono 9.0.7 symbols (build ID `76c3c9dfcc2bbbcaa50b3dd0615704674e98019a`)
locate a loader worker in `sgen_stop_world`, waiting for thread suspension. Engine
build `b2cff92dbcbc26fc` waits in `RendererCanvasRenderRD::canvas_render_items`
on the canvas shader mutex. The mutex owner itself was not readable in this capture.

The retained engine symbol archive (`0e64c59967d75e56`) was rejected. A symbol
map recovered by relinking the retained objects has a different build ID, but all
23 allocated sections have matching addresses, sizes and bytes except the build-ID
note; this supports the engine function mapping without claiming matching IDs.
The actual-runtime regression independently reproduces the host-state defect:
Running/GC-unsafe after delegate creation, Blocking/GC-safe after the repair,
with the existing hybrid suspension policy unchanged. It uses the existing preload
script and private helpers in the existing test file.

Two alternating cold Modded and two Vanilla launches on r2, code `435002`, pass
Common completion, at least 60 seconds at the responsive title, Settings,
gameplay entry and Save and Quit. All four retain the native GC-safe marker and
three owned preload prefixes. A third Modded launch completes Common and retains
the same evidence, but the user removes the phone during title observation; it is
excluded from acceptance. No r2 freeze was observed during these tests.

| r2 attempt | Attempt ID | Full acceptance |
| --- | --- | --- |
| Cold Modded 01 | `1b28db7dafa543b099d98d55d2bd481b` | Passed |
| Cold Vanilla 01 | `c9824918e87e410fb1e7844d4a4a2e44` | Passed |
| Cold Modded 02 | `442909d983ff4a6995a3edc5d90d49c2` | Passed |
| Cold Vanilla 02 | `095f75a421c6437883ea5d154201e5e7` | Passed |
| Cold Modded 03 | `3a51416f29064d5e9c659f676d334f01` | Interrupted by device removal |

The phone is unavailable at the user's direction. Remaining acceptance is three
alternating comparisons and 18 Modded successes on this same APK: eight cold,
six warm, two background/resume and two lock/unlock. The early passes do not
establish that the intermittent freeze is resolved. PR #48 remains unmerged and
r2 remains unpublished. No existing run was abandoned; both save namespaces
remain available through Continue, credential loading remains successful and
the original three-mod selection remains activated. Exact mod semantic versions
were not exposed by the retained release logs; payloads were kept unchanged.

Raw original evidence is retained locally in `artifacts/investigations/title-freeze-20261002-122146/`; candidate test logs, screenshots and per-attempt records are in `artifacts/preload-repair-20261002/phone/`. Continuous logcat is retained. Operator force-stops and secure-lock pauses are distinguished from failures. Raw device captures are not published.

## Unpublished candidate

| Item | Value |
| --- | --- |
| APK | `artifacts/android/StS2Launcher-v0.2.435-preload-repair-local-r2-arm64-v8a.apk` |
| Version / code | `0.2.435-preload-repair-local-r2` / `435002` |
| Package | `com.sts2launcher.overhaul.fork.local` |
| Certificate SHA-256 | `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A` |
| APK SHA-256 | `e93342ae606ca28b396d939eeeb9ecae96319cff4a450712c2023482338f88aa` |

The failed code `435001` APK is also retained, SHA-256
`49da67f99619247ed564be1e72a1b5b0a0ae149c13c0c3b22dc8b4370cc9101f`.

Released v434 and its original freeze captures are preserved. The candidate remains unpublished until physical acceptance is complete. Android Steam transport, broader device/driver compatibility and individual mod features require separate validation.
