# Startup responsiveness implementation plan

**Goal:** reduce title-screen render stalls, remove blocking Android startup work, and avoid redundant game-archive reads on Play.

**Architecture:** preserve game asset queues and completion semantics while limiting their per-frame work; prepare Android files on a serial worker and route only a live foreground activity; reuse only the existing validated installation-generation PCK digest.

**Constraints:** preserve saves, credentials, package identity, integrity checks, and save-sync ordering. Test on the local emulator; distinguish its native fallback from ARM64 gameplay.

- [x] Add behavioral preload-budget regression tests, then bound threaded requests and resource finalization. Validate patch targets against the actual game assembly and exercise real resource loading in desktop Godot.
- [x] Reproduce synchronous Android preparation in unit tests, move preparation to a serial worker, and cover activity pause/destruction races.
- [x] Demonstrate repeated archive hashing with a counting hasher, reuse valid read-only cache entries, and check invalidation/no-write behavior.
- [x] Run managed and Android regression suites, the save-safety probe, build APKs, and validate native lifecycle/responsiveness on the emulator.
- [x] Review changes and record measured results and remaining title-screen/device limitations in `docs/startup-responsiveness-2026-09-07.md`.
