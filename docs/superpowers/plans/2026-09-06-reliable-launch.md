# Reliable launch implementation plan

**Goal:** smooth, single-owner startup with explicit failure recovery and no overlapping game initialization.
**Architecture:** preserve the identity authorization and visibility owner; add an attempt-scoped operation for owned tasks, lifecycle-aware stage deadlines, and scene provenance. Replace restart booleans as the managed source of truth with a versioned durable request and acknowledged native acceptance.
**Tech stack:** Godot C#/.NET 9, Harmony, Android Java, existing console/JUnit tests.
**Spec:** `docs/launch-cycle-review-2026-09-06.md` (approved by the user).

Work in the current working tree to preserve the user's integrated UI/updater changes. Do not commit unrelated edits. Never uninstall or clear device data.

- [x] Restart request contract: add versioned request storage and validation; explicit native acceptance/failure; migrate legacy pending flags; consume request once; retain failure evidence. Test persistence failure, stale/malformed requests, branch/runtime identity, duplicate consumption and missing restart target. Files: Android pending state/GodotApp, Android bridge, managed restart handoff and restored attempt reconstruction.
- [x] Launch operation: test late completion/fault, cancellation, stage timeout and paused time; implement a single owned startup task and restart-required recovery. Remove watchdog force-loading. Keep the Android wrapper lifetime anchor consistently after successful startup.
- [x] Game adapter: bind main-menu scene identity to its originating attempt at scene entry; remove readiness from the layout patch; wait for current-scene/render/visibility evidence; reject late callbacks. Test actual binding semantics rather than just explicit stale IDs.
- [x] Preparation: move file readiness work off the UI thread with generation revalidation and main-thread result delivery; bound frame waits by node/attempt lifetime; surface save-sync cleanup state without bypassing save ordering.
- [x] Cleanup: remove unused workshop helpers, unused previous-stall method and launch-sequence forwarding, identical text branches and cosmetic warmup finish delay. Consolidate obsolete recovery code.
- [ ] Verification: managed tests, Android unit tests, Release build/APK verification, independent code review, then install a higher-version APK on the Samsung and exercise available startup paths. Record any game-data prerequisites separately from launcher success.

## Progress

Plan accepted through the user's explicit request to implement the review. Execution uses the subagent-driven development skill for the independent native restart contract while the primary agent implements operation ownership and integration.

Implementation is complete. Verification: 132 managed tests, 34 Android tests, 12 save-safety scenarios, both launcher preview profiles, and Release APK verification passed. Build 430042 was installed on the Samsung and opened. Full game-launch device checks remain blocked by its existing PCK FMOD preparation rejection; see `docs/reliable-launch-implementation-2026-09-06.md` for evidence and exact scope.

