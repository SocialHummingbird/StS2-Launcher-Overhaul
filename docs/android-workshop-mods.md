# Android Steam Workshop Mods

_Last updated: 2026-06-22_

This document tracks the Android Workshop mod path for StS2 Mobile. It covers subscribed Steam Workshop discovery, Android staging, runtime loading, Steam Cloud safety, and the current unsupported legacy UGC blocker.

## Current State

The first proven Android Workshop path is working on ARM64 hardware for public-branch Workshop PCK mods:

- `Sync Workshop Mods` discovers subscribed Workshop items from Steam.
- Usable Workshop items are downloaded from Steam depot manifests or direct UGC URLs.
- Downloads are staged under app-private storage at `files/workshop_mods/staged`.
- Staged mods are loaded by the runtime mod-loader patch during game startup.
- The tested public path loaded `BaseLib` and `Quick Restart` and reached the main menu.
- Steam Cloud Push is not run by Workshop sync, Workshop clear, or Workshop evidence capture.

Latest device evidence:

```text
build=0.2.314-workshop-load-order
package=com.sts2launcher.overhaul.fork.local
device=RFCY70XQE7F
evidence=artifacts/android/workshop-mods-public-0.2.314-load-order-20260622-205238
result=BaseLib loaded, Quick Restart loaded, total loaded=2, main menu reached
cloudSafety=No Steam Cloud Push was run.
```

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

This is not a subscription-discovery failure. The launcher can see the subscribed item, but Steam does not currently expose a usable Android download source for it through the paths above.

Required evidence before changing this classification:

- Steam response proving a non-zero `ManifestId`, or
- `SteamCloud.RequestUGCDetails` resolving the legacy handle to a URL, or
- a newly implemented authenticated Steam content route that can legally fetch the item payload and preserve the same staging/hash/safety evidence.

Until then, this item must remain `unsupported` rather than being treated as synced or loaded.

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

- Verify the Workshop path across public, public-beta, and core-release branch switching with labeled evidence.
- Decide whether a legitimate additional Steam content route exists for legacy UGC-only items.
- Keep launcher UX copy focused on user actions: sync subscribed mods, clear staged mods, and understand that Push to Cloud remains locked while modded content is active.
