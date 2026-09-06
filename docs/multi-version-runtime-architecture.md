# Multi-version runtime architecture

## Objective

Keep downloaded Steam branches in separate local runtime slots and start the selected slot without mixing game assets.

## Runtime slot

A slot contains:

- Downloaded game files.
- `installation_state.json` with transactional `updating`/`ready` state.
- Authoritative `GameIdentity`: normalized branch, completed install generation, and hashes of the actual final PCK and source `sts2.dll`.
- Runtime-pack metadata and patched managed assemblies.
- Patch-validation result.

Runtime-pack metadata and active-cache markers are derived from `GameIdentity`; they cannot define the current installed-file identity.

The slot identifier is stable, filesystem-safe, and derived from the selected branch. Public/default keeps the existing primary slot for compatibility.

## Activation

Before launch, the launcher requires the selected installation state to be `ready`, rehashes the actual PCK and source DLL, and requires the promoted runtime pack to declare and validate that exact identity. Android then validates and atomically promotes the active assembly cache. It either activates that slot or reports one actionable readiness failure.

Activation must not move gameplay saves or block launch on save state.

## Cleanup

Recovery targets only the explicitly selected slot. **Redownload Selected Version** may delete that branch's game/runtime/download evidence and its matching derived cache; it must not remove sibling branches, application package data, credentials, Workshop content, or gameplay saves. The former bulk **Remove old versions** behavior is not present.

## Validation boundary

Focused suites validate slot isolation, interruption recovery, stale-evidence rejection, and promotion rules. The RC4 APK also passed 10/10 counted launches on one ARM64 Samsung device. That does not establish every device, branch, or GPU.
