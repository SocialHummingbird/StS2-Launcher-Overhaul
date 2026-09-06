# Steam version selection architecture

## Purpose

Version selection lets an authenticated Steam owner discover, download, update, cache, repair, and launch an account-visible game branch without overwriting another downloaded branch.

## One identity authority

`GameIdentity` contains four equality fields:

- Normalized branch.
- Completed install-generation fingerprint.
- SHA-256 of the final installed, Android-ready `SlayTheSpire2.pck`.
- SHA-256 of the final installed source `data_sts2_windows_x86_64/sts2.dll`.

The PCK and DLL hashes come from the actual installed files. Compatibility manifests, validation reports, runtime packs, release metadata, branch markers, and active-cache markers are derived evidence and cannot supply current-file identity.

## Per-branch transaction

Public/default uses the primary slot; other branches use stable side-by-side slots. Every slot uses the same lifecycle:

```text
ready → updating → ready
```

The downloader writes `installation_state.json` as `updating` before the first installed-file mutation. It then commits verified Steam files, prepares and commits the final PCK, calculates a fresh `GameIdentity`, builds the matching runtime pack, and publishes launch-ready state only after validation succeeds. Failure or interruption leaves the branch non-launchable and recoverable; it cannot silently reuse the previous identity.

## Runtime-pack promotion

Every branch launches through a runtime pack. Generation writes a unique staging directory and validates both metadata files, the complete `GameIdentity`, patch/validation versions, every declared assembly hash, and the absence of undeclared DLLs. Promotion uses same-filesystem rename with backup/restore and final-path revalidation.

Android independently hashes the selected installed files, validates the promoted pack, stages the active assembly cache, and promotes that cache only after validation. Missing, malformed, `updating`, stale, or mismatched evidence blocks launch.

## Selected-branch recovery

**Redownload Selected Version** uses the same selected-branch cleanup contract as native recovery. It may remove only that branch's game directory, download state, runtime pack/owned staging paths, installation state, and matching derived active cache. It must preserve:

- Other branch slots and runtime packs.
- Gameplay saves and local save backups.
- Steam Cloud data and Steam credentials.
- Workshop content and launcher preferences unrelated to the pending launch.

There is no bulk **Remove old versions** behavior in the current launcher.

## Diagnostics

Diagnostics report the selected branch/slot, installation state and transaction, complete `GameIdentity`, runtime-pack ID/hashes/validation, active-cache result, and the first readiness failure. Legacy branch-marker or global runtime-slot evidence must not be presented as authority.

## Validation boundary

The Issue #38 suites cover N → N+1 updates, interrupted transactions, stale evidence, atomic runtime-pack promotion, selected-branch isolation, save/credential preservation, and attempt-bound handoff ordering. RC4 additionally passed 10/10 counted launches on Samsung `SM-F971B`, Android 17 / API 37. Other devices, branches, GPUs, mods, and live-service paths remain separate validation scopes.

See [Issue #38 runtime identity design](issue-38-runtime-identity-design.md) for schemas, failure states, migration details, and the full test matrix.
