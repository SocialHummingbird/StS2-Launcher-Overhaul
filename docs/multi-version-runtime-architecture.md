# Multi-version runtime architecture

## Objective

Keep downloaded Steam branches in separate local runtime slots and start the selected slot without mixing game assets.

## Runtime slot

A slot contains:

- Downloaded game files.
- `installation_state.json` with transactional `updating`/`ready` state.
- Authoritative `GameIdentity`: normalized branch, completed install generation, and hashes of the actual final PCK and source `sts2.dll`.
- Runtime-pack metadata and an unchanged copy of the source game assembly. Harmony applies runtime patches when the game loads.
- Patch-validation result.

Runtime-pack metadata and active-cache markers are derived from `GameIdentity`; they cannot define the current installed-file identity.

The slot identifier is stable, filesystem-safe, and derived from the selected branch. Public/default keeps the existing primary slot for compatibility.

## Activation

`ready` means the installed files have completed preparation; it is published before runtime-pack work. Before launch, the launcher reads their `GameIdentity` and validates the matching runtime pack. Managed reads may reuse a PCK digest bound to the completed generation and exact file snapshot; the source DLL is always hashed.

Android independently hashes the installed PCK and source DLL and validates the pack before preparing the active assembly cache. After pack/cache checks succeed, the launcher writes `current_runtime_slot.json` as authorization for that exact identity and pack. It rechecks authorization before launch. Markers cannot replace current-file hashing.

Activation must not move gameplay saves or block launch on save state.

## Cleanup

Recovery targets only the selected slot. **Redownload Selected Version** removes that branch's game files, download state, runtime pack and owned temporary paths, plus derived evidence owned by that branch or too invalid to identify an owner. It retains the active assembly cache, which must be validated or replaced during a later launch. Sibling branches, application package data, credentials, Workshop content, and gameplay saves are preserved. The former bulk **Remove old versions** behavior is not present.

## Validation boundary

Focused suites validate slot isolation, interruption recovery, stale-evidence rejection, and promotion rules. The RC4 APK also passed 10/10 counted launches on one ARM64 Samsung device. That does not establish every device, branch, or GPU.
