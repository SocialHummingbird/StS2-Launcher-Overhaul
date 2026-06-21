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

`audit-multi-version-runtime.ps1` is intentionally only an orchestrator. Its focused modules cover helper boundaries, runtime-slot identity/readiness, runtime-pack generation/validation, patch compatibility, native prepared-cache routing, startup patch contracts, save-origin/Steam Cloud safety, diagnostics, and evidence tooling/docs.

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
  -PackageName <installed.package.name> `
  -RunLabel public `
  -AdbPath "C:\path\to\platform-tools\adb.exe"

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel public-beta `
  -AdbPath "C:\path\to\platform-tools\adb.exe"

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel branch-switch `
  -AdbPath "C:\path\to\platform-tools\adb.exe"
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
- installed source PCK SHA-256 matches `current_runtime_slot.json` and the runtime-pack source manifest
- Android-patched selected PCK SHA-256 matches runtime validation and the native runtime-cache marker
- selected source `sts2.dll` SHA-256 matches `current_runtime_slot.json`
- `current_runtime_slot.json` has `filesReady=true`
- `current_runtime_slot.json` has `playable=true`
- `current_runtime_slot.json` has `runtimeCompatible=true`
- `current_runtime_slot.json` has `patchCompatible=true`
- runtime pack status is usable
- runtime pack source runtime slot ID matches the installed selected runtime slot ID
- runtime pack selected branch/source PCK/source assembly hashes match `current_runtime_slot.json`
- mounted selected PCK hash matches runtime validation and native runtime-cache evidence
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

## Fix16 prerelease evidence note

The `v0.2.188-local-runtime-beta-fix16` GitHub prerelease is a local ARM64 validation build for `com.sts2launcher.overhaul.fork.local`. It proved that public-beta no longer routes to `NativeFallbackActivity` when a usable branch runtime pack exists, and the launch evidence showed matched beta PCK/runtime hashes plus save-backed main-menu state after Pull from Cloud. It must not be described as full public/beta coexistence release signoff until fresh public, public-beta, and public-after-beta artifacts all pass the wrapper with branch-specific evidence directories and resolved classification.

## Fix20 prerelease evidence note

The `v0.2.188-local-runtime-beta-fix20` local ARM64 validation build adds Android-patched PCK source-hash acceptance and writes `current_runtime_slot.json` immediately after selected-runtime readiness inspection. Device evidence captured on 2026-06-18 showed:

- public-after-beta launched with selected branch `public`, PCK `files/game/SlayTheSpire2.pck`, PCK SHA-256 `8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68`, source/active `sts2.dll` SHA-256 `81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49`, and runtime patch validation `passed`
- public-beta launched with selected branch `public-beta`, PCK `files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, Android-patched PCK SHA-256 `957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`, runtime source PCK SHA-256 `a263c68cfdeb6e94af9029088e1bab0c4c72a1641bc1c1ff72c180396a7b134c`, runtime-pack/source/active `sts2.dll` SHA-256 `4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3`, and runtime patch validation `passed`
- stale public or public-beta `current_runtime_slot.json` evidence blocked direct launch and returned to launcher until managed readiness inspection wrote fresh selected-branch evidence; this is expected and must not be counted as launch success

The beta Compendium route rendered from the synced-save main menu. The follow-up Bestiary tap evidence was inconclusive because the device returned to Android home without package-side missing-resource or fatal logs; do not use that tap as proof of a remaining game asset crash without a focused reproduction capture.

## Fix21 prerelease evidence note

The `v0.2.188-local-runtime-beta-fix21` GitHub prerelease is a fresh ARM64 APK build from commit `d1fedc1` on `codex/android-release-bootstrap`. It contains the same runtime-pack evidence fixes as fix20 and was published as:

```text
Asset: StS2Launcher-v0.2.188-local-runtime-beta-fix21-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.local
VersionName: 0.2.188-local-runtime-beta-fix21
VersionCode: 218847
SHA-256: 69df0581cf2a8cb3843317ddf0a34e789ffce54ba596cfe1a0a26be7f8e8dc3b
```

Before publishing the APK, `scripts/audit-multi-version-runtime.ps1` passed all 29 checks. No additional device runtime evidence was captured specifically for fix21; use the fix20 ARM64 public/public-beta/public-after-beta launch evidence as the matching code-level device validation for this commit.

## Fix23 prerelease evidence note

The `v0.2.188-local-runtime-beta-fix23` ARM64 hardening build fixes the crash observed immediately after confirming a switch to the locally installed `public-beta` slot. The root cause was the non-public runtime-slot inspector falling back to direct selected-PCK hashing when stale public runtime-cache evidence did not match the selected beta branch. On the connected ARM64 device that path died in native JNI before managed exception handling could catch it. Fix23 uses branch-local `.android_patch_validation.json` and `runtime_packs/<branch>/compatibility.json` hash evidence for non-public branches before any direct hash fallback; if that evidence is missing or mismatched, the branch remains not playable instead of being treated as success.

Device evidence captured on 2026-06-18:

- `artifacts/android/fix23-public-beta-startup-crash-retest-20260618`: launcher-only startup with selected `public-beta` stayed foreground, wrote fresh `current_runtime_slot.json`, and had no `JNI DETECTED`, `SIGSEGV`, `NativeFallback`, or fatal package log lines.
- `artifacts/android/fix23-public-beta-game-launch-20260618`: auto-launch mounted `/files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, reached `NGame.GameStartup completed`, and showed the main menu. Runtime slot evidence recorded source PCK SHA-256 `a263c68cfdeb6e94af9029088e1bab0c4c72a1641bc1c1ff72c180396a7b134c`; runtime cache/patch validation recorded Android-patched PCK SHA-256 `957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`; source, runtime-pack, and active Android `sts2.dll` SHA-256 all matched `4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3`.
- `artifacts/android/fix23-public-beta-compendium-route-20260618`: route retest attempts were not deterministic enough for signoff. Several raw-touch coordinate attempts hit the wrong menu region or Quit; D-pad relaunch evidence only proved main-menu startup and Bestiary resource preload, not a confirmed Compendium/Bestiary route. Do not count this as asset-route pass or asset-route failure.
- `artifacts/android/fix23-public-beta-compendium-route-retry-20260618`: corrected display capture and tap coordinates proved the synced-save public-beta route. The run launched selected `public-beta`, captured the main menu on display `4630946449689556883`, tapped Compendium at `853,1497`, then tapped Bestiary at `1815,760`. `after-bestiary-tap.png` shows the Bestiary screen with `Assassin Raider`, enemy list, and rendered model. Runtime markers show selected beta PCK/runtime and active Android `sts2.dll` matched the beta runtime hash `4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3`; focused logs had no `NativeFallback`, `SIGSEGV`, `JNI DETECTED`, or package fatal, old doormaker/no-loader failure count was `0`, and `unknown_monster` fallback resources loaded instead.
- `artifacts/android/multi-version-runtime-branch-switch-20260618-211533`: read-only branch-switch capture passed `review-multi-version-runtime-evidence.ps1 -RequireBranchSwitch -RequireSaveSafety` with 34 checks. It accepts the expected non-public Android split where `current_runtime_slot.json` and `runtime_packs/public-beta-8128824d/compatibility.json` record source PCK SHA-256 `a263c68cfdeb6e94af9029088e1bab0c4c72a1641bc1c1ff72c180396a7b134c`, while runtime validation and native cache record mounted Android-patched PCK SHA-256 `957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`. It rules out stale downloader cache, wrong launch path, and shared assembly/runtime cache for that snapshot. Save-origin evidence remains `branch switch pending Pull`; the report classifies Steam Cloud Push as `do-not-push`, and no Push-to-Cloud mutation was attempted for this evidence.
- `artifacts/android/multi-version-runtime-public-beta-20260618-224239`: fresh read-only public-beta capture after a clean package restart passed `review-multi-version-runtime-evidence.ps1 -RequirePublicBeta -RequireSaveSafety` with 42 checks. It shows selected branch `public-beta`, selected beta PCK SHA-256 `957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`, active Android `sts2.dll` SHA-256 `4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3`, selected runtime pack `public-beta-8128824d`, closed runtime-pack DLL set, and patch validation `passed`. The wrapper command `scripts/run-multi-version-runtime-release-gates.ps1 -PublicBetaEvidenceDirs artifacts/android/multi-version-runtime-public-beta-20260618-224239 -BranchSwitchEvidenceDirs artifacts/android/multi-version-runtime-branch-switch-20260618-211533 -RequireSaveSafety` now passes. This is local debug ARM64 evidence only; current public/default release-candidate evidence and public-share redaction review remain required before signoff.

## Fix27 prerelease evidence note

The `v0.2.188-local-runtime-beta-fix27` ARM64 hardening build fixes the public-after-beta startup crash seen after switching the selected branch from `public-beta` back to `public`. The root causes were:

- launcher-only bootstrap could leave a previous branch game-code assembly in the Godot publish cache until the next game-launch request
- public runtime-slot inspection hashed the 1.8 GB PCK through Mono `SHA256.ComputeHash(FileStream)`, which crashed the Android process on the connected ARM64 device
- runtime patch validation wrote the native runtime cache identity into `runtimeSlotId`, making evidence reports show a false installed-slot mismatch

Fix27 refreshes stale launcher bootstrap game-code assemblies while allowing the packaged public `sts2.dll`, hashes large Android files through a Java SHA-256 bridge, and records canonical runtime-slot ID plus separate native runtime-cache ID.

Device evidence captured on 2026-06-18:

- `artifacts/android/public-after-beta-game-launch-20260618-230719`: public auto-launch from app data that previously held public-beta runtime evidence stayed foreground, completed startup patch orchestration, reached observed game startup, and wrote public runtime-cache evidence.
- `artifacts/android/multi-version-runtime-public-20260618-231242`: read-only public capture passed `review-multi-version-runtime-evidence.ps1 -RequirePublic -RequireSaveSafety` with 30 checks. It shows selected branch `public`, selected PCK SHA-256 `8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68`, selected source/cache `sts2.dll` SHA-256 `81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49`, runtime patch validation `passed`, installed slot `public-d8a7082fc63977cc`, and native runtime cache bound to that canonical slot.
- Combined gate passed:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1 `
  -PublicEvidenceDirs artifacts\android\multi-version-runtime-public-20260618-231242 `
  -PublicBetaEvidenceDirs artifacts\android\multi-version-runtime-public-beta-20260618-224239 `
  -BranchSwitchEvidenceDirs artifacts\android\multi-version-runtime-branch-switch-20260618-211533 `
  -RequireSaveSafety `
  -Quiet
```

This is still local debug ARM64 evidence. It does not replace release-candidate APK evidence, private/password/no-manifest negative-case validation, cross-branch save compatibility validation after Pull/restore, or public-share redaction review.

## Fix30 prerelease evidence note

The `v0.2.188-local-runtime-beta-fix30-public-after-beta` ARM64 prerelease is published on the fork and proves the public-after-beta native startup repair on the connected ARM64 device. It confirms that a direct `public` launch after a `public-beta` runtime-cache launch switches the active assembly cache back to public instead of routing to `NativeFallbackActivity`, mounts `files/game/SlayTheSpire2.pck`, applies 19/19 runtime patches, and reaches the main menu with active Android `sts2.dll` SHA-256 `81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49`.

Published asset:

```text
Release: v0.2.188-local-runtime-beta-fix30-public-after-beta
Asset: StS2Launcher-v0.2.188-local-runtime-beta-fix30-public-after-beta-arm64-v8a.apk
SHA-256: b3f0b645356dfd72e6bcddc735a07352a4b911720f84532b499e543358ce4515
VersionName: 0.2.188-local-runtime-beta-fix30-public-after-beta
VersionCode: 218857
```

Device evidence captured on 2026-06-19:

- `artifacts/android/fix30-public-after-beta-20260619-121225`: direct public startup after beta runtime-cache state reached the main menu without `NativeFallbackActivity`.
- `artifacts/android/multi-version-runtime-public-20260619-121256`: read-only public capture passed `review-multi-version-runtime-evidence.ps1 -RequirePublic -RequireSaveSafety` with 30 checks.
- `artifacts/android/fix30-public-beta-pull-cloud-complete-20260619-122423` and `artifacts/android/fix30-public-beta-synced-compendium-route-20260619-122605`: Pull completed first, the synced save opened on `Profile 1`, Compendium opened, Bestiary opened, and Assassin Raider rendered with matched beta PCK/runtime evidence. The earlier doormaker/no-loader hard-lock route did not reproduce.
- `artifacts/android/multi-version-runtime-public-beta-20260619-123054`: post-Pull public-beta runtime evidence passed 42 public-beta/save-safety checks with side-by-side PCK `files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, mounted PCK SHA-256 `957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`, and source/runtime-pack/active `sts2.dll` SHA-256 `4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3`.

Combined gate passed:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1 `
  -PublicEvidenceDirs artifacts\android\multi-version-runtime-public-20260619-121256 `
  -PublicBetaEvidenceDirs artifacts\android\multi-version-runtime-public-beta-20260619-123054 `
  -BranchSwitchEvidenceDirs artifacts\android\multi-version-runtime-branch-switch-20260618-211533 `
  -RequireSaveSafety `
  -Quiet
```

No Steam Cloud Push was performed for this evidence.

## Fix31 local validation evidence note

The `0.2.188-local-runtime-beta-fix31-save-origin-pck` ARM64 build is a local validation APK, not a published GitHub release. It fixes the false save-origin mismatch for non-public Android-patched PCKs: save-origin markers may record the source PCK hash while runtime patch validation and native cache markers record the mounted Android-patched PCK hash, and that pairing is accepted only through a usable runtime-pack source-PCK mapping plus the same runtime slot/source assembly.

Local artifact:

```text
Artifact: artifacts/android/StS2Launcher-v0.2.188-local-runtime-beta-fix31-save-origin-pck-arm64-v8a.apk
SHA-256: 33fa866b5d8b9462f2aa83cd34606b84b3f7f8b8a5a8da159d08cecc2ed04ae6
VersionName: 0.2.188-local-runtime-beta-fix31-save-origin-pck
VersionCode: 218858
```

Device evidence `artifacts/android/multi-version-runtime-public-beta-20260619-124816` passed `review-multi-version-runtime-evidence.ps1 -RequirePublicBeta -RequireSaveSafety` with 42 checks and reports `Steam Cloud Push save-origin safety` as `matched` with `pckDirect=False` and `pckRuntimePackSource=True`. The save/config asset-reference hypothesis is now `unknown` instead of falsely confirmed.

Combined gate passed:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1 `
  -PublicEvidenceDirs artifacts\android\multi-version-runtime-public-20260619-121256 `
  -PublicBetaEvidenceDirs artifacts\android\multi-version-runtime-public-beta-20260619-124816 `
  -BranchSwitchEvidenceDirs artifacts\android\multi-version-runtime-branch-switch-20260618-211533 `
  -RequireSaveSafety `
  -Quiet
```

No Steam Cloud Push was performed. Successful branch-switch Push remains release-open until Pull-before-Push, local-save, and backup evidence are captured on the selected version.

## Audit orchestrator split prerelease evidence note

The `v0.2.289-local-audit-orchestrator-split` ARM64 prerelease packages a tooling-only refactor for the Steam version-selection static audit. It moves native routing/fallback, diagnostics reporting, evidence tooling, release/readiness documentation, and beta-integrity evidence guardrails into focused audit modules while keeping the top-level script as the orchestrator. It is published as a local-package build for `com.sts2launcher.overhaul.fork.local`:

```text
Asset: StS2Launcher-v0.2.289-local-audit-orchestrator-split-arm64-v8a.apk
SHA-256: cc581c603e3e28b1700e6944d0d309c8b4f4c6482810f2983745f95e361ac275
VersionName: 0.2.289-local-audit-orchestrator-split
VersionCode: 289000
```

Validation for this prerelease is build/static-gate only: `audit-steam-version-selection.ps1 -Quiet` passed 501 checks, `audit-multi-version-runtime.ps1 -Quiet` passed 156 checks, `audit-steam-branch-guidance-parity.ps1 -Quiet` passed, the managed Release build passed, the local ARM64 APK build passed structural verification, and Android crypto patch verification passed. No ARM64 device branch-switch/runtime route evidence was captured for this specific APK, and no Steam Cloud Push was performed. Continue to use the fix30/fix31 public/public-beta evidence notes above for runtime behavior until this APK has matching device evidence.

## Audit/evidence helper refactor prerelease evidence note

The `v0.2.279-local-audit-helper-refactor` ARM64 prerelease packages a tooling-only refactor for the branch/runtime validation scripts. It extracts common static-audit plumbing, Android `run-as` shell quoting, marker parsing, and Markdown report row formatting into shared PowerShell helpers guarded by both static audits. It is published as a local-package build for `com.sts2launcher.overhaul.fork.local`:

```text
Asset: StS2Launcher-v0.2.279-local-audit-helper-refactor-arm64-v8a.apk
SHA-256: 3e388d6f835468dd2ba88a193a1e5d08981bd38ffb0b2bfa2fe1d155dee12b9d
VersionName: 0.2.279-local-audit-helper-refactor
VersionCode: 279000
```

Validation for this prerelease is build/static-gate only: `audit-steam-version-selection.ps1 -Quiet` passed 456 checks, `audit-multi-version-runtime.ps1 -Quiet` passed 152 checks, the managed Release build passed, the local ARM64 APK build passed structural verification, and Android crypto patch verification passed. No ARM64 device branch-switch/runtime route evidence was captured for this specific APK, and no Steam Cloud Push was performed. Continue to use the fix30/fix31 public/public-beta evidence notes above for runtime behavior until this APK has matching device evidence.

## Refactor-audits prerelease evidence note

The `v0.2.278-local-compact-label-refactor` ARM64 prerelease packages the compact launcher helper consolidation for shared two-line button label application. It removes the remaining bespoke compact label implementations for standard compact CTAs while keeping intentionally custom sizing explicit. It is published as a local-package build for `com.sts2launcher.overhaul.fork.local`:

```text
Asset: StS2Launcher-v0.2.278-local-compact-label-refactor-arm64-v8a.apk
SHA-256: 9e85ce823c01fc0857ba6215dff9540362a4e38d82bf0297adb2aac1dedd4d76
VersionName: 0.2.278-local-compact-label-refactor
VersionCode: 278000
```

Validation for this prerelease is build/static-gate only: `audit-steam-version-selection.ps1 -Quiet` passed 453 checks, `audit-multi-version-runtime.ps1 -Quiet` passed 148 checks, the managed Release build passed, the local ARM64 APK build passed structural verification, and Android crypto patch verification passed. No ARM64 device branch-switch/runtime route evidence was captured for this specific APK, and no Steam Cloud Push was performed. Continue to use the fix30/fix31 public/public-beta evidence notes above for runtime behavior until this APK has matching device evidence.
