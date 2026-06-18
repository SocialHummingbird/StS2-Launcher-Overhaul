# Multi-version runtime release gates

Use this checklist before claiming that public and beta Steam branches can safely coexist on Android.

The release is not signed off for multi-version runtime support until every gate below has direct evidence from the exact APK being released. Do not treat a successful public launch as evidence for beta, and do not treat downloaded branch files as playable evidence unless the selected runtime slot is also playable.

## Required static gates

Run these before device validation:

```powershell
.\scripts\audit-multi-version-runtime.ps1
.\scripts\audit-steam-version-selection.ps1
.\scripts\audit-steam-branch-guidance-parity.ps1
```

Equivalent wrapper:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1
```

Required result:

- all audits pass
- no audit is skipped because a checked file or required marker is missing
- failures are fixed or carried as explicit release blockers

## Required build gates

The exact APK being released must prove:

- package name matches the current release/update path
- signing certificate matches the current release/update path
- versionCode increases over the previous GitHub APK
- ARM64 native payload is present
- packaged launcher assemblies and native Godot bridge are present
- APK structural verification passes

Use the existing Android release verification scripts for these checks:

```powershell
.\scripts\check-android-release-readiness.ps1
.\scripts\verify-android-release-apk.ps1 -ReleaseTag <tag> -AssetName <apk-name>
.\scripts\verify-android-update-compat.ps1 -PreviousApkPath <previous-apk> -CurrentApkPath <current-apk>
```

## Required device gates

Use an ARM64 physical Android device for runtime signoff. Emulator evidence is useful for install/routing checks, but it is not enough for Godot/.NET runtime signoff.

Minimum scenarios:

- fresh public download and launch
- fresh public-beta download and launch
- public downloaded first, then public-beta downloaded and launched
- public-beta downloaded first, then public downloaded and launched
- branch switch public -> public-beta -> public -> public-beta without clearing app data
- forced selected-version redownload for public-beta, then launch
- inactive cache cleanup, then selected public-beta launch
- public-beta launch with clean local saves
- public-beta launch after Pull from Cloud or restored local save/profile data

Each visual observation must be paired with runtime evidence from the same APK/session. Screenshots alone are not sufficient.

## Required runtime evidence

Run the read-only evidence collector after each meaningful branch/runtime change:

```powershell
.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName com.sts2launcher.overhaul.fork.dev `
  -RunLabel public

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName com.sts2launcher.overhaul.fork.dev `
  -RunLabel public-beta

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName com.sts2launcher.overhaul.fork.dev `
  -RunLabel branch-switch
```

Then review the collected artifact:

```powershell
.\scripts\review-multi-version-runtime-evidence.ps1 `
  -EvidenceDir artifacts\android\multi-version-runtime-public-beta-<timestamp> `
  -RequirePublicBeta `
  -RequireSaveSafety `
  -RequireResolvedClassification
```

Equivalent wrapper after evidence exists:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1 `
  -PublicEvidenceDirs artifacts\android\multi-version-runtime-public-<timestamp> `
  -PublicBetaEvidenceDirs artifacts\android\multi-version-runtime-public-beta-<timestamp> `
  -BranchSwitchEvidenceDirs artifacts\android\multi-version-runtime-branch-switch-<timestamp> `
  -RequireSaveSafety `
  -RequireResolvedClassification
```

For one-off evidence artifact review, use `-EvidenceDirs` with `-RequirePublic`, `-RequirePublicBeta`, or `-RequireBranchSwitch`. Prefer `-PublicEvidenceDirs`, `-PublicBetaEvidenceDirs`, and `-BranchSwitchEvidenceDirs` for release signoff so public, beta, and switching evidence are reviewed with branch-specific expectations in one command.

Each collected artifact must include `run-metadata.json` with the sanitized run label, package name, generated UTC time, collector name, artifact folder name, and read-only marker. The evidence reviewer checks this metadata as well as `summary.md`, so a public artifact cannot be reused as beta or branch-switch evidence by path name alone.

Branch-switch artifacts must also include `last_game_branch_switch.txt` marker provenance in the collected diagnostics. A normal public or beta launch artifact is not enough to prove coexistence because it does not prove public -> public-beta -> public -> public-beta switching avoided stale runtime/cache reuse.

For every public-beta signoff observation, the artifact must show:

- selected branch is `public-beta`
- selected PCK path is under `files/game_versions/public-beta-*/game/SlayTheSpire2.pck`
- selected PCK SHA-256 matches `current_runtime_slot.json`
- selected source `sts2.dll` SHA-256 matches `current_runtime_slot.json`
- `current_runtime_slot.json` has `filesReady=true`
- `current_runtime_slot.json` has `playable=true`
- `current_runtime_slot.json` has `runtimeCompatible=true`
- `current_runtime_slot.json` has `patchCompatible=true`
- runtime pack status is usable
- runtime pack source runtime slot ID matches the selected runtime slot ID
- runtime pack selected branch/PCK/source assembly hashes match the selected on-device files
- runtime pack was generated from a clean directory
- runtime pack manifest and patch-validation report match
- runtime pack closed DLL set passes
- native runtime cache identity binds to the selected runtime slot
- active publish-cache `sts2.dll` hash matches the selected runtime pack Android `sts2.dll`
- final startup logs show the selected PCK path/hash, not a public fallback path

For public signoff, the artifact must show:

- selected branch is public/default
- selected PCK path is under `files/game/SlayTheSpire2.pck`
- selected PCK SHA-256 matches `current_runtime_slot.json`
- selected source `sts2.dll` SHA-256 matches `current_runtime_slot.json`
- selected runtime slot is playable
- native runtime cache identity binds to the selected public runtime
- startup logs show the public PCK path/hash

## Required save safety evidence

Do not claim save-safe branch switching until evidence shows:

- switching branch writes a pending/unverified save-origin state
- Pull from Cloud records selected branch, runtime slot ID, selected PCK hash, and selected source `sts2.dll` hash
- Push to Cloud is blocked when local saves do not match the selected playable runtime
- Push to Cloud is blocked when runtime slot evidence is stale or non-playable
- Push to Cloud remains guarded by Pull-first and backup evidence

Steam Cloud Push must not be used during branch-runtime investigation unless the test is explicitly a Push validation pass.

## Required classification output

The final evidence package for a release candidate must classify these hypotheses:

- Steam branch partial/shared content
- stale or incomplete downloader cache
- wrong launch path
- shared assembly/runtime cache
- in-process branch switch reuse
- Android PCK patch side effect
- Godot import/resource mismatch
- save/config asset reference mismatch

Allowed statuses:

- `confirmed`
- `ruled out`
- `likely`
- `unknown`
- `needs device-only validation`

If launcher routing/cache is ruled out, the report must cite the selected branch, selected PCK path, selected PCK hash, runtime pack ID, active Android `sts2.dll` hash, and final startup `Loading PCK from:` evidence used to rule it out.

## Release blocker rules

Block the release claim if any of these are true:

- public-beta can launch without a usable runtime pack
- public-beta can launch with stale `current_runtime_slot.json`
- public-beta can launch with a selected PCK/source assembly hash mismatch
- native startup falls back to public PCK or public game-code assembly for public-beta
- active publish-cache `sts2.dll` does not match the selected runtime pack
- runtime-pack manifest/report/hash evidence is missing or inconsistent
- runtime-pack directory contains undeclared DLLs
- branch switch reuses the previous branch runtime cache
- Push to Cloud can proceed after branch switch before selected-runtime Pull/save evidence is current
- mixed/split asset behavior is observed without selected PCK/hash/runtime evidence captured for that exact observation

## Current known limitation

The current architecture performs static patch compatibility validation before launch and records runtime Harmony patch validation after startup. Full runtime Harmony validation is still post-startup; it does not yet run full runtime Harmony patch application as a pre-launch gate. Treat that as a known hardening limitation unless and until the launcher can safely execute full runtime patch validation before marking a non-public slot playable.
