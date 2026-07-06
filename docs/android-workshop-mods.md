# Android Steam Workshop Mods

_Last updated: 2026-07-05_

This document tracks the Android Workshop/mod path for StS2 Mobile. It covers subscribed Steam Workshop discovery, Android staging, runtime loading, Steam Cloud safety, current compatibility limits, and the current unsupported legacy UGC blocker.

## Current State

Workshop/mod support is in progress. The current latest APK is `v0.2.369-public-beta-stability`; the latest strict modded device evidence is still the July 3 public-beta validation noted below. The current Android Workshop path can discover, stage, present, and launch the tested public-beta mod set:

- `Sync Workshop Mods` discovers subscribed Workshop items from Steam.
- Usable Workshop items are downloaded from Steam depot manifests or direct UGC URLs.
- Downloads are staged under app-private storage at `files/workshop_mods/staged`.
- The launcher exposes a first-class Mods section on the main Play screen, showing active staged-mod count, unsupported Workshop item count, manual-import guidance, Cloud upload lock state, and primary `Sync Workshop` / `Clear Staged` actions before repair/help diagnostics.
- Public-beta `v0.108.0` now launches with matched beta PCK plus matched beta managed runtime after the adaptive ModelDb fix. Public-after-beta branch switching and core-release side-by-side launch were retested on July 3.
- Core-release can be selected and launched through a side-by-side slot. Current Steam metadata still records it as inheriting the public depot manifest rather than a distinct core-release payload.
- BaseLib and Quick Restart are staged from Workshop. Vanilla and Modded Saves Merger is available through manual import, but direct Workshop acquisition for that item remains unsupported.
- Public-beta modded launch now passes connected-device validation with BaseLib, Quick Restart, and manual Saves Merger enabled. The launcher bypasses the Android-incompatible upstream directory scanner for selected manifest-backed mods, loads BaseLib through the Android-safe path, loads Quick Restart through a synthetic mod object, and applies Saves Merger's save-path behavior through a built-in Android compatibility patch.
- Steam Cloud Push is not run by Workshop sync, Workshop clear, or Workshop evidence capture.

This is not finished mod-manager UX yet:

- BaseLib compatibility is currently handled by an Android direct-load path and PatchAll filter that skips known Android-incompatible BaseLib patch classes and the BaseLib extended-save registration path. This keeps the staged `BaseLib`/`Quick Restart` path launchable, but those skipped BaseLib features are not proven usable on Android.
- Core-release is not currently proven as a distinct Steam branch payload because Steam metadata exposes no separate branch manifest in the latest capture.
- Unsupported legacy UGC-only Workshop items are visible but not downloadable through the currently implemented Steam content routes. The launcher keeps them classified as unsupported and now points users to the supported manual import folder instead of treating the sync as complete.
- Saves Merger direct Workshop download remains unsupported, but the manual-imported mod's functional save-path behavior is now supplied by the launcher when that mod is selected.

Latest device evidence:

```text
latestStrictModdedRuntimeBuild=0.2.352-savemerger-compat-local
package=com.sts2launcher.overhaul.fork.local
device=RFCY70XQE7F
publicEvidence=artifacts/android/workshop-mods-public-public-after-beta-modeldb-adaptive-20260703-20260703-084514
publicBetaEvidence=artifacts/android/workshop-mods-public-beta-public-beta-playable-modeldb-adaptive-20260703-20260703-084139
coreReleaseEvidence=artifacts/android/workshop-mods-core-release-core-release-modeldb-adaptive-20260703-20260703-085147
moddedPublicBetaEvidence=artifacts/android/workshop-mods-public-beta-connected-public-beta-modded-savemerger-20260703-10-20260703-103201
moddedWrapper=artifacts/android/public-beta-modded-validation-connected-public-beta-modded-savemerger-20260703-10
result=Public-beta v0.108.0 launches with matched beta PCK/runtime. Public-after-beta switching and core-release side-by-side launch pass current runtime evidence. Public-beta modded validation passes with BaseLib, Quick Restart, and manual Saves Merger selected; fresh marker records playMode=modded, scannedRoots=3, enabledMods=3.
cloudSafety=No Steam Cloud Push was run.
```

## Connected-Device Validation

Use the latest build containing selected-root diagnostics and run this sequence:

1. Confirm the installed package is the expected local debug package and version.
2. Keep Steam Cloud Push locked/off; do not press Push to Cloud.
3. Select public-beta and modded mode with BaseLib, Quick Restart, and manual Saves Merger enabled.
4. Run `scripts/run-public-beta-modded-validation.ps1` to park `files/modded`, select the known BaseLib/Quick Restart/Saves Merger set, launch public-beta with focused logs, capture `last_mod_launch.json`, `mod_selection.json`, selected root diagnostics, `diagnostics/selected-mod-root-hashes.txt`, `diagnostics/external-mods-tree.txt`, runtime cache marker, patch validation marker, PCK/runtime hashes, screenshots, and restore the prior local test state. The wrapper writes `diagnostics/validation-result.json` and exits non-zero if capture/review/save-validation fails, so a crash or fallback route cannot be mistaken for a playable modded beta pass.
5. Classify the result:
   - root not visible: launcher/path staging issue;
   - root visible but scan crashes before manifest load: upstream Godot/mod file-I/O scan issue;
   - manifests load but BaseLib/Quick Restart/Saves Merger fails: mod compatibility issue;
   - main menu reached: validate upstream first-modded-save copy and Saves Merger save usability.
6. Restore vanilla mode and any parked `files/modded` backup before ending the device session.

Latest result: `connected-public-beta-modded-savemerger-20260703-10` passed. It loaded the selected public-beta PCK from `files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, used runtime pack `files/runtime_packs/public-beta-8128824d`, matched active Android `sts2.dll` hash `51a671bfeb937271af3e643d017396b13432098ed2b9debceb110c74939bbba1`, loaded all three selected mods, wrote a fresh modded launch marker, and recorded `steamCloudPushPerformed=false`.

## BaseLib Android Compatibility

The current strict public-beta artifact (`artifacts/android/workshop-mods-public-beta-connected-public-beta-modded-savemerger-20260703-10-20260703-103201`) proves the launcher is no longer falling back to public or `NativeFallbackActivity`:

- selected branch: `public-beta`
- selected PCK: `files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`
- active Android `sts2.dll`: `51a671bfeb937271af3e643d017396b13432098ed2b9debceb110c74939bbba1`
- runtime pack: `files/runtime_packs/public-beta-8128824d`
- runtime pack support assemblies and hashes are declared in `compatibility.json`
- runtime pack patch validation passed

The previous `BaseLib 3.3.2` `System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<T>.set_IsProperty(bool)` failure is fixed by targeting the mod-loader/mod-compat layer instead of copying Steam Windows BCL assemblies into the runtime pack. The Android compatibility layer now:

- skips BaseLib's extended-save registration path on Android
- replaces BaseLib PatchAll with an Android filter that skips known incompatible BaseLib patch classes
- bypasses the upstream Android directory scanner for selected manifest-backed mods
- lets the remaining BaseLib patches and dependent `Quick Restart` mod load without forbidden initializer errors in strict public-beta evidence
- applies Saves Merger's `UserDataPathProvider` behavior through built-in Android compatibility patches when the manual Saves Merger mod is selected

The skipped BaseLib features remain compatibility limitations until they are ported or proven unnecessary for specific mods.

## Supported Sources

Workshop items are considered usable when Steam exposes at least one of these content sources:

- `direct-url`: `PublishedFileDetails.file_url` is present and downloaded through the Android HTTP client.
- `ugc-hcontent`: Steam exposes a UGC content handle and `SteamCloud.RequestUGCDetails` resolves it to a URL.
- `depot-manifest`: Steam exposes an item manifest id, and the launcher downloads the corresponding SteamPipe manifest through the depot downloader.

The manifest stores source classification without raw signed URLs:

- `DownloadSourceKind`
- `ManifestId`
- `HContentFile`
- `DownloadUrlPresent`
- `DownloadUrlHost`
- `ReusedCachedDownload`

## Unsupported Legacy UGC

Some Workshop items can expose only an old UGC content handle with no direct URL and no depot manifest id. The current known item is:

```text
publishedFileId=3747532120
title=Vanilla and Modded Saves Merger
status=unsupported
reason=Steam exposed a legacy Workshop UGC handle but no direct URL or depot manifest
```

The latest ARM64 evidence strengthens that classification:

```text
evidence=artifacts/android/save-merger-ugc-sync-20260624-075451
manifestStatus=unsupported
hcontent=4186905754413598255
expectedBytes=6443
manifestId=0
downloadUrlPresent=false
fileCount=0
hasPck=false
deviceLog=SteamCloud.RequestUGCDetails was canceled before completion; no depot manifest was exposed
```

This is not a subscription-discovery failure. The launcher can see the subscribed item, but Steam does not currently expose a usable Android download source for it through the paths above.

Required evidence before changing this classification:

- Steam response proving a non-zero `ManifestId`, or
- `SteamCloud.RequestUGCDetails` resolving the legacy handle to a URL, or
- a newly implemented authenticated Steam content route that can legally fetch the item payload and preserve the same staging/hash/safety evidence.

Until then, this item must remain `unsupported` rather than being treated as synced or loaded.

## Manual Import Fallback

The supported fallback for legacy UGC-only mods is manual import into shared Android storage:

```text
/storage/emulated/0/StS2Launcher/Mods
```

Place the mod folder or PCK/DLL/JSON payload copied from a trusted Steam install into that directory, then start the game. The Android runtime scans this folder in addition to app-private `files/workshop_mods/staged`. This is the only supported current route for `3747532120` / `Vanilla and Modded Saves Merger` unless Steam begins exposing a direct URL, UGC URL, or depot manifest for the item.

Latest ARM64 fallback proof:

```text
source=C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747532120
device=/sdcard/StS2Launcher/Mods/3747532120
SavesMerger.dll=08b7cbb36c7b48820e44c060d2dc6b6c49edbb945c7cb1e8f7a44866f7c650cc
SavesMerger.json=21e60996aeee64a1bd91f6ec83320fbc5550f1677259a0ce7b1868da793f6124
SavesMerger.pck=8a50a0b5704493ab11b39132d9adcced1eb335988a79dd5413fa1ee9109f18a3
log=Found mod manifest file /storage/emulated/0/StS2Launcher/Mods/3747532120/SavesMerger.json
log=Finished mod initialization for 'SavesMerger' (SavesMerger)
screen=Running Modded. Loaded 3 mods.
saveRead=profile1/saves/progress.save and profile1/saves/prefs.save
```

## Runtime Loading

The Android runtime patch extends the game's `ModManager` after the built-in mod scan:

- It scans `files/workshop_mods/staged` for synced Workshop mods.
- It scans `/storage/emulated/0/StS2Launcher/Mods` for manual sideloaded mods.
- It supports both older and current public `ModManager` method shapes.
- It uses the game's sort method before loading newly discovered mods so shared dependencies such as `BaseLib` load before dependent mods.
- Workshop staged mods require the launcher consent marker before the patch applies the in-game mod-loading consent setting.

Relevant files:

- `src/STS2Mobile/Patches/ModLoaderPatches.cs`
- `src/STS2Mobile/WorkshopModConsent.cs`
- `src/STS2Mobile/Steam/Workshop/`

## Steam Cloud Safety

Workshop mod support deliberately keeps save upload separate:

- `Sync Workshop Mods` does not press or call Push to Cloud.
- `Clear Workshop Mods` removes staged mod entries and clears the consent marker without uploading saves.
- Evidence capture scripts state the same boundary and derive a Workshop Cloud Push lock when staged PCK mods are active.
- Manual Push to Cloud is blocked when active staged Workshop PCK mods are present.

Do not use Workshop launch evidence as proof that modded saves are safe to upload to Steam Cloud.

## Evidence Requirements

A complete Workshop evidence bundle should include:

- selected branch and selected PCK path/hash
- active `sts2.dll` hash
- runtime cache marker and patch validation marker
- Android PCK patch marker when the mounted Android PCK hash differs from the source/unpatched Steam PCK hash
- Workshop manifest with source classification and item statuses
- staged Workshop PCK hashes
- derived Cloud Push lock state
- focused package logs showing Workshop scan and mod initialization
- screenshots or window-state capture proving launch route reached the expected game state

Run the reviewer against captured evidence:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/review-workshop-mod-evidence.ps1 -EvidenceDir <artifact-dir> -RequirePhase public
```

Use `-RequirePhase public`, `-RequirePhase public-beta`, or `-RequirePhase core-release` when reviewing branch-specific launch evidence. The capture script records Android window state before launch, before Start Game, and before screenshots; it aborts instead of producing a misleading artifact when the device reports the lockscreen or dreaming lockscreen as active.

## Remaining Work

- Expand the new first-class launcher Mods section into fuller mod-manager UX: mod list/detail, enable/disable staging, unsupported-item explanation, and clearer dependency state.
- Keep BaseLib Android PatchAll skip coverage current as BaseLib and subscribed Workshop mods update.
- Keep the strict public, public-beta, public-after-beta, and core-release branch-switch evidence current as the game and Workshop items update.
- Decide whether a legitimate additional Steam content route exists for legacy UGC-only items.
- Keep launcher UX copy focused on user actions: sync subscribed mods, clear staged mods, and understand that Push to Cloud remains locked while modded content is active.
