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
