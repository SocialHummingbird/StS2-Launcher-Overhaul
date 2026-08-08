# Steam Beta Integrity Runtime Checklist

Use this checklist for the remaining ARM64/device pass before claiming the public-beta integrity goal is complete.

## Preconditions

- Latest local build installed on the target device.
- If the evidence script must read app-private files through `adb run-as`, install a debuggable local evidence build:

```powershell
.\scripts\build-android-local.ps1 `
  -Install `
  -EvidenceDebuggable `
  -VersionName "0.2.<n>-local-beta-evidence" `
  -VersionCode <higher-than-installed>
```

- Steam login works on the device.
- `REFRESH GAME VERSIONS` has been run after login.
- `public-beta` is visible/downloadable, or branch availability evidence proves why it is not.

## Runtime sequence

1. Select `public-beta` in the game version dropdown.
2. Use `REDOWNLOAD SELECTED VERSION` to clear the selected beta slot.
3. Download the selected version again.
4. Launch once if the launcher reports the selected version is ready.
5. Capture beta integrity evidence:

```powershell
.\scripts\capture-steam-beta-integrity-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-beta-integrity-<timestamp>" `
  -Branch "public-beta" `
  -ReviewSummary `
  -FailOnNotReady
```

## Pass criteria

- `beta-integrity-summary.txt` includes `Public sharing warning:`.
- `Evidence readiness:` is ready, or a branch-availability issue is explicitly classified.
- `Evidence missing/weak:` is `<none>` for ready classifications.
- `Clean redownload matches investigated branch: true`.
- `Clean redownload selected directories cleared: true`.
- `Selected branch marker:` is present unless classification is a branch-availability issue.
- `Public branch marker:` is present for public-vs-beta comparison.
- `Changed key asset rows (first 64):` is reviewed when art/assets look public.
- `Focused logcat:` exists and has been manually reviewed before public sharing.

## Classification outcomes

- `likely Steam partial branch`: Steam appears to serve mixed branch-specific and public-identical/inherited depot content.
- `likely Steam public-inherited branch content`: Steam exposes selected branch content that inherits public depots.
- `likely branch-specific installed content`: marker and inventory both show beta-specific installed files; investigate runtime remote/config if behavior still looks public.
- `likely Steam branch availability issue`: app-info says the branch is absent or has zero Windows depot manifests.
- `possible stale cache or runtime remote/config behavior`: evidence is not strong enough to claim Steam manifest behavior alone.
- `inconclusive`: do not close the goal; resolve `Evidence missing/weak:` first.

## Runtime follow-up after branch-specific content

When classification is `likely branch-specific installed content`, depot integrity is proven. Continue with runtime evidence instead of redownloading again:

- Launch the selected version from the launcher.
- Capture a screenshot after game startup.
- Capture focused logcat lines for selected branch, selected slot directory, resolved game directory, branch marker readiness, patch orchestration, selected PCK path/byte count/SHA-256, startup scene, and any art/config/remote errors.
- Pull `files/sts2_bootstrap_trace.log` from the debuggable evidence build.
- Treat mixed-looking beta/public behavior after this point as a runtime/game-content question: remote config, game-side runtime selection, bootstrap patch behavior, or asset mounting order.
- Expect the runtime PCK SHA-256 to differ from the raw Steam file-inventory hash after Android download completion. The launcher patches `SlayTheSpire2.pck` in place to remove Android-incompatible plugin startup references before launch. Use the focused logcat `Selected game PCK ... sha256=` line as the authoritative mounted-content fingerprint.

Known good evidence from 2026-06-14:

```text
evidence=artifacts/android/steam-beta-integrity-20260614-170912
runtime=artifacts/android/runtime-public-beta-20260614
runtimePckEvidence=artifacts/android/runtime-public-beta-pck-20260614
publicRuntimeEvidence=artifacts/android/runtime-public-game-auto-pck-20260614
classification=likely branch-specific installed content
startup=public-beta selected, side-by-side branch cache, marker ready, selected PCK path/hash logged, --main-pack points at selected beta PCK, 17/17 patches applied, main menu reached
publicPckSha256=8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68
selectedRuntimePckSha256=957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a
runtimeComparison=public and public-beta main-menu screenshots are visually equivalent at this level; public loads run-history doormaker_boss imported textures successfully, public-beta reports loader failures for the same resource names after selected beta PCK mount.
resourceChainFinding=public contains doormaker_boss run-history import/texture entries; public-beta does not and contains aeonglass_boss instead. public-beta still contains branch-local unknown_monster fallback art.
runtimeFix=ImageHelper run-history icon paths now fall back to branch-local unknown_monster art when the selected branch returns a missing icon path.
```

## Release gate

Do not mark beta branch integrity complete while `review-beta-integrity-summary.ps1 -FailOnNotReady` exits non-zero.
