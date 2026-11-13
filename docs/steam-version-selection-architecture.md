# Steam version selection architecture

## Purpose

Version selection lets an authenticated Steam owner discover, download, update, cache, repair, and launch an account-visible game branch without overwriting another downloaded branch.

## One identity authority

`GameIdentity` contains four equality fields:

- Normalized branch.
- Completed install-generation fingerprint.
- SHA-256 of the final installed, Android-ready `SlayTheSpire2.pck`.
- SHA-256 of the final installed source `data_sts2_windows_x86_64/sts2.dll`.

The PCK and DLL hashes come from the installed files. Managed reads may reuse a PCK digest only when the completed generation, path, length, and modification time match; the source DLL is always hashed. Android independently hashes both files before accepting the runtime pack.

`steam_branch.txt` remains the completed Steam-install provenance: its bytes supply the install-generation fingerprint. Compatibility manifests, validation reports, release metadata, and cache/authorization markers cannot supply current PCK or DLL hashes.

## Per-branch transaction

Public/default uses the primary slot; other branches use stable side-by-side slots. Every slot uses the same lifecycle:

```text
ready → updating → ready
```

The downloader writes `installation_state.json` as `updating` before changing installed files. It verifies and commits downloaded files, then prepares the installed PCK in place while the branch remains `updating`. It publishes the completed Steam marker, calculates `GameIdentity`, invalidates previous derived artifacts, and commits installed-file `ready`.

Runtime-pack work follows that commit. A `ready` installation can still be blocked by missing or invalid runtime outputs. Pack/cache preparation must succeed before `current_runtime_slot.json` is written as launch authorization for the exact identity and pack. Failures before installed-file completion leave `updating`; failures afterward block launch without necessarily changing installed-file `ready`.

## Chunk assembly and resume

This follows SteamPipe's reuse of unchanged chunks and replacement-file assembly, described in [Valve's content guidance](https://partner.steamgames.com/doc/sdk/uploading#Game_Content_Structure_-_Best_Practices). [SteamRE's downloader](https://github.com/SteamRE/DepotDownloader/blob/master/DepotDownloader/ContentDownloader.cs) is the implementation reference; the launcher's branch transaction and Android preparation remain its own responsibilities.

Game downloads use one bounded chunk pool per active operation: three workers on Android and eight on desktop, with depots processed in order. Requests are deduplicated by depot ID, chunk SHA-1 and uncompressed length. A large archive can use the whole pool. Per-file gates protect offset writes without holding a gate during a network request. Workshop retains its existing transfer path.

The selected branch's download-state directory holds pending generations under `staging-v1/<depotId>/<manifestId>/`: an atomically saved `manifest.bin` and replacement files under `files/`. Completed `<depotId>.manifest` files remain separate. Pending metadata selects Resume; it never authorizes launch.

The assembler verifies staged ranges, then tries the selected installation, superseded partial generations and other completed installations. Manifest content identities find chunks even when their offsets change. Every source range passes managed SHA-1 verification on a worker before reuse; file length and preallocation are not completion evidence. Legacy `.downloading` files can supply verified target ranges. There is no permanent chunk cache.

Only missing chunks reach the existing SteamKit/CDN authentication, decryption, verification and retry path. Each payload is written to all target offsets that need it. Complete staged files must pass Steam's full file SHA-1 before any replacement is promoted. Old files remain readable until promotion, and obsolete files are deleted afterward. A partially committed batch stays `updating` and must finish before launch.

Progress separates logical reused/resumed bytes from the manifest's compressed lengths for successfully fetched, unique chunks. That payload counter excludes failed attempts and protocol overhead; it is not a measurement of total network traffic. A request timeout reports failure; only cancellation of the owning operation reports Pause.

An unchanged Android-prepared PCK is retained only with valid authoritative ready identity, supported preparation version and unchanged original Steam file identity. Otherwise its ranges are untrusted donors: reconstruct and verify raw Steam content before Android preparation.

Pause cancels scheduling and further retries, drains active requests and flushes staging before releasing ownership. Retry, reset and disposal wait for this drain. Restart rechecks actual staged bytes. Superseded partials are retired only after matching content is durably copied; useful current staging survives errors, pause and process death. Successful installation or explicit selected-version reset removes it. Storage checks allow complete replacement files alongside old files and retain the Android free-space reserve.

## Runtime-pack promotion

Every branch launches through a runtime pack. Generation copies `sts2.dll` unchanged into a unique staging directory and validates both metadata files, the complete `GameIdentity`, patch/validation versions, declared assembly hashes, and the absence of undeclared DLLs. Promotion uses sibling directories, rename with backup/restore, and final-path revalidation. Runtime patches use shared Harmony when the game loads.

Android independently hashes the selected installed files, validates the promoted pack, stages the active assembly cache, and promotes that cache only after validation. Missing, malformed, `updating`, stale, or mismatched evidence blocks launch.

## Selected-branch recovery

**Redownload Selected Version** and native recovery use selected-branch cleanup. They remove that branch's game directory, download state, runtime pack/owned temporary paths, and installation state. Global derived evidence is removed when it belongs to that branch or has no trustworthy owner. The active assembly cache is retained and must pass later launch validation. Cleanup preserves:

- Other branch slots and runtime packs.
- Gameplay saves and local save backups.
- Steam Cloud data and Steam credentials.
- Workshop content and launcher preferences unrelated to the pending launch.

There is no bulk **Remove old versions** behavior in the current launcher.

## Diagnostics

Diagnostics report the selected branch/slot, installation state and transaction, complete `GameIdentity`, runtime-pack ID/hashes/validation, active-cache result, launch authorization, and the first readiness failure. They distinguish completed-generation provenance, installed-file readiness, and launch authorization from current-file hashes.

## Validation boundary

The Issue #38 suites cover N → N+1 updates, interrupted transactions, stale evidence, atomic runtime-pack promotion, selected-branch isolation, save/credential preservation, and attempt-bound handoff ordering. RC4 additionally passed 10/10 counted launches on Samsung `SM-F971B`, Android 17 / API 37. Other devices, branches, GPUs, mods, and live-service paths remain separate validation scopes.

See [Issue #38 runtime identity design](issue-38-runtime-identity-design.md) for artifact roles, failure/recovery boundaries, implementation links, and pre-fix history.
