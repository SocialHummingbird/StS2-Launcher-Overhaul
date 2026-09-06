# Steam version selection user guide

Steam version selection lets an authenticated owner download and launch an account-visible Slay the Spire 2 branch from a separate local runtime slot.

## Basic use

1. Sign in with a Steam account that owns Slay the Spire 2.
2. Open **Versions** and refresh the branch list.
3. Select an available public or beta branch.
4. Choose **Download Selected Version** or update the installed selection.
5. Return to **Home** and start the game.

Unavailable and private/password branches are blocked when Steam metadata says they cannot be downloaded. Beta password entry is not implemented.

## What an update changes

Each branch has its own launcher-managed installation slot. An update changes only the selected slot; public/default and other downloaded branches are not overwritten.

Updates are transactional. Before any installed file is replaced, the selected branch is marked `updating` and cannot launch. The launcher commits Steam files, prepares the final PCK, hashes the actual PCK and source `sts2.dll`, validates and promotes the matching runtime pack, and returns the branch to `ready`. An interrupted update remains blocked for resume or repair rather than mixing old and new files.

Changing or repairing a downloaded runtime does not move gameplay saves.

## Repair one branch

Use **Redownload Selected Version** only when the selected branch is incomplete or repeatedly fails identity/runtime validation. It deletes and rebuilds that branch's downloaded game/runtime state while preserving:

- Vanilla and modded gameplay saves.
- Steam login/session credentials.
- Workshop content.
- Every other downloaded branch.

The former **Remove old versions** bulk action has been removed. Do not uninstall the app, clear application data, or clear every downloaded branch as routine recovery.

## Runtime identity

The authority is the final installed `SlayTheSpire2.pck` and source `data_sts2_windows_x86_64/sts2.dll`, plus the normalized branch and completed install generation. Runtime-pack metadata and active-cache markers are derived evidence; they cannot replace hashes of the current files.

See [Steam version selection architecture](steam-version-selection-architecture.md) for the concise technical contract.

## Mods and saves

Modded launch uses the selected runtime and selected Workshop/manual mod files. SavesMerger and UnifiedSavePath are deprecated; current source uses the game's native vanilla and modded save namespaces. Branch changes do not merge, move, or reinterpret those saves.

## Diagnostics

For a branch problem, create a support report from **Help → Diagnostics** and record:

- Exact APK release tag, version code, and selected branch.
- Installation-state status, phase, and transaction ID.
- Selected slot directory and completed install generation.
- `GameIdentity` ID, PCK path/hash, and source `sts2.dll` path/hash.
- Runtime-pack path/ID, patched `sts2.dll` hash, and validation result.
- The first readiness or update error.
- Whether a redownload was attempted and whether other branches/saves remained present.

Review the report before sharing. Remove account identifiers and private paths or save data, and never post a Steam password, Steam Guard code, refresh/session token, or full unsanitized log.

## Evidence boundary

RC4 passed 10/10 counted launches on one Samsung ARM64 device, including `public-beta` runtime identity and handoff checks. That validates the exact release and device matrix, not every branch or device. Report only the artifact, branch, and path actually tested.
