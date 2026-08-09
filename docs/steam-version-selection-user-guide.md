# Steam version selection user guide

Steam version selection lets an authenticated owner download and launch an account-visible Slay the Spire 2 branch from a separate local runtime slot.

## Basic use

1. Sign in with a Steam account that owns Slay the Spire 2.
2. Open Versions.
3. Refresh the branch list.
4. Select an available public or beta branch.
5. Download or update that branch.
6. Return Home and start the game.

Unavailable or private/password branches are blocked when Steam metadata says they cannot be downloaded. Beta password entry is not implemented.

## Storage

Each downloaded branch uses its own launcher-managed runtime slot so changing versions does not overwrite the public runtime cache. The selected branch marker and runtime-pack metadata are checked before launch.

Changing the downloaded runtime does not move gameplay saves.

## Mods

Modded launch uses the selected runtime and selected Workshop/mod files. SavesMerger and UnifiedSavePath are deprecated; current source uses the game's native vanilla and modded save namespaces.

## Diagnostics

For a branch problem, record:

- Selected branch and displayed availability.
- Runtime slot directory.
- Selected PCK path and SHA-256.
- Runtime-pack path and SHA-256.
- Branch marker and patch-validation result.
- Exact error shown before download or launch.

Use the Help diagnostics export only for troubleshooting and review it before sharing.

## Evidence boundary

Version selection remains experimental. Static and desktop tests do not prove a branch on Android hardware. Report only the exact artifact, branch, and path actually tested.
