# Changelog

## 2026-08-22 - Issue #38 runtime identity and ARM64 RC4

- Made the final installed PCK and source `sts2.dll` authoritative, removing stale marker/manifest fallbacks from selected-game identity.
- Made Steam branch updates transactional and fail-closed with per-branch `updating`/`ready` state.
- Added unique staging, complete validation, atomic promotion/rollback, and final-path revalidation for runtime packs and the Android active assembly cache.
- Limited redownload/native recovery to the selected branch so saves, credentials, Workshop content, and unrelated downloaded branches are preserved.
- Removed the bulk **Remove old versions** behavior from the current launcher.
- Bound main-menu readiness and launcher-overlay dismissal to one launch-attempt ID, foreground activity, and window focus so late callbacks cannot complete a later launch.
- Published [`v0.2.429-issue38-arm64-rc4`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4) for `arm64-v8a`. The exact APK passed 10/10 Samsung `SM-F971B` / Android 17 launches: four cold, four warm, one background/resume, and one lock/unlock, with preservation checks passing.

## 2026-08-13 - Launcher simplification candidate prerelease

- Reduced the shared launcher shell to one useful status on Home and exceptional-state banners on secondary pages.
- Simplified Home, Saves, Versions, Mods, and Help around one decision and one dominant action per page.
- Kept save states, mod runtime results, version selection, and recovery actions explicit and truthful without repeating internal state as prose.
- Restored the existing Android main-menu preparation and startup-task lifetime guard with one focused behavioural test.
- Added an opt-in, same-APK deferred-preload experiment for a later one-device comparison; normal loading remains the default and no device result is claimed.
- Built `0.2.428-launcher-simplification-unverified` from the current source for offline inspection and prerelease handoff. No Android device was available for this build.

## 2026-08-13 - Mod-loading candidate prerelease

- Converged Workshop and manual mods into one validated, persisted launch plan and one loader path.
- Made missing or corrupt selection default to Vanilla and made Modded mode require an explicit selection.
- Added truthful persisted mod results and launcher states for installed, enabled, loaded last launch, partial, failed, stale, and not tested.
- Added early compiled-script registration for the representative importer, fail-closed activation evidence, current `ModManager` compatibility, and an Android-safe BaseLib path that omits the demonstrated hanging CustomPile compatibility patch.
- Reduced the retained mod tests to one representative fixture, fresh-process Vanilla/disabled/active checks, and one launcher interaction journey.
- Built and inspected the ARM64 `0.2.425-mod-chain-fix2-unverified` candidate. A limited device run showed BaseLib and ImportVanillaSaves loading and activating, the modded save namespace being used, and main-menu startup. The retained event then entered an intermittent live-process freeze after the main menu appeared; it does not prove a process crash. The importer control and relaunch result remain unverified, so this is an unverified prerelease rather than a phone-ready release.
- Reconnected the existing Android rendered-frame menu preparation and startup-task lifetime guard, added one behavioural lifetime test, and built the unpublished `0.2.426-main-menu-handoff-diagnostic` ARM64 artifact. This is diagnostic evidence only, not an Android stability claim.

## 2026-08-09 - Save-sync restoration and reduction (unreleased)

- Replaced the former coordinator/cache/recovery/evidence stack with one Steam Cloud transport, one manifest-based synchronization service, and one atomic four-field sync-state document.
- Added automatic Pull before the first settings/profile read and automatic verified Push after committed gameplay save changes, while keeping local writes authoritative and launch fail-open.
- Restored the Saves page actions `Sync now`, `Pull from Steam`, and `Push to Steam`; automatic and manual operations use the same service and conflict policy.
- Retained Steam authentication, owned-game and Workshop download, unconditional launch, the existing package identity, and the app-private save root.
- Removed recovery bundles, backup/export/snapshot/quarantine surfaces, evidence collectors, architecture matrices, static-audit families, and release-governance scaffolding.
- Redesigned the launcher around Home, Saves, Versions, Mods, and Help, with one focused navigation/action test.
- Built and structurally inspected the ARM64 `0.2.421-auto-sync-unverified` candidate. Offline tests pass, but no Android device was available and real Steam enumerate/download/upload/commit/read-back remains unproven.
- Kept the downloader's existing idle-timeout suspension without restoring the abandoned Cloud-specific timer workaround.

Older implementation history remains available in Git commits and published GitHub releases.
