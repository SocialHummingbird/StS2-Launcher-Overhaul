# Stage 1-4 retain/delete inventory

Baseline: public `main` at `0a4f65a`. The restored transfer primitive is derived from `e75d9aa`, the June revision whose manual Push and Pull completed on a real ARM64 phone. The old Stage 5 coordinator/recovery architecture is not being restored.

## Retain

- Package `com.sts2launcher.overhaul.fork.local` and the existing Android app-private data root. No save, credential, or downloaded-game path moves.
- `AndroidLocalSaveStore` as the only gameplay save store, with `CancellableAtomicFile` for atomic local writes.
- `SteamAuth`, `SteamCredentialStore`, Android Keystore encryption, and the existing Steam connection/HTTP bridge.
- `DepotDownloader`, version selection, Workshop downloading, and the unconditional launch path. Cloud failure has no launch dependency.

## Restore

- One `SteamCloudTransport`: CCloud enumerate, download, single-file upload/commit/read-back verification, and verified explicit delete using `SteamGameApp.AppId`, `ManagedSha1`, and the existing Android HTTP bridge. Each complete operation owns one serialized lease and one aggregate deadline; a failed operation poisons its connection.
- One `SaveSyncService`: automatic and manual callers share its single manifest-based `SyncAsync` policy. Each pass captures one remote enumeration and reuses it while Push or Pull mirrors the allowlisted save set, including deletions.
- Automatic reconciliation runs before the first settings/profile read. Successful gameplay commits durably queue one coalesced verified Push in the game process, with a short final Quit flush.
- The Saves page exposes only `Sync now`, `Pull from Steam`, and `Push to Steam`. A genuine conflict offers exactly `Use Steam saves` or `Use this device`; both choices call the same service again.
- Pull is accepted only when the caller declares gameplay save writes stopped; in-game synchronization may Push but cannot overwrite live Android saves.
- One active, account-scoped `sync-state.json` under `.sts2-launcher/<account-hash>/`, written atomically. It contains only the last successful local and remote path/size/SHA-1 manifests, a local-upload-waiting flag, and an interrupted-transfer-retry flag. The account hash keeps one account's baseline from being applied to another without storing the account name in the state.

## Delete / do not restore

- `CloudSyncCoordinator*`, `LauncherCloudSyncCoordinator*`, and any coordinator family.
- `SteamKit2CloudSaveStore*`, `CloudFileCache*`, cloud-backed gameplay stores, save-store factories, and parallel write queues.
- Separate baseline/pending documents, snapshots, save-context hashes, evidence events, recovery, quarantine, export, backups, Restore/Undo, source-history prompts, and Cloud launch gates.
- Fake Cloud stores, scenario matrices, evidence collectors, reviewers, static architecture audits, or a second automatic/manual implementation.

Stages 1-6 establish the small transfer boundary, deterministic policy, automatic lifecycle seams, and minimal manual UI. A successful build and fake-remote behavior suite are not evidence that Steam transfer works on Android; that requires a later real-device transfer test.

## Stage 4 mod fix boundary

Stage 3 demonstrated exactly two mod-loader defects:

| Demonstrated failure | Retained fix | Focused proof |
|---|---|---|
| A runtime mod with no state marker could be reported as loaded, and diagnostics could abort while instantiating unrelated custom-attribute dependencies. | Accept only the exact runtime enum state `Loaded`; inspect patch attributes as metadata; emit ordered `Discovery`, `PayloadLoading`, `Initialization`, `Activation`, and `UiRegistration` results instead of inferring success. | A markerless runtime object is rejected, metadata inspection completes without loading unrelated attribute dependencies, and unsupported or partial activation reports its first failing gate. |
| `ImportVanillaSaves` activated but produced no buttons because its compiled Godot scripts were registered after its PCK scene needed them; a later second registration then collided. | Register declared Godot script assemblies at the existing mod assembly-load boundary and make that registration idempotent. | The real importer DLL creates three labelled `Import Profile` buttons with no `_Ready` errors in the isolated Godot host. |

No other Stage 4 product fix is justified:

- `ImportVanillaSaves` is not rejected by the `SavesMerger` / `UnifiedSavePath` policy, so no importer allowlist is added.
- The existing validated manifest resolver selects the exact importer manifest and payload; no recursive first-JSON resolver or second manifest-selection path is added.
- `mod_selection.json` survives save, reload, and the simulated restart with the same immutable launch-plan fingerprint; no command-line mod arguments or coordinator are added.
- The exact `ModManager.Initialize` postfix is installed and ownership-checked; missing integration is reported rather than bypassed.
- `ImportVanillaSaves` declares no BaseLib dependency. The launcher's BaseLib path can report concrete exact-owner Harmony activation while remaining `Partial`; broader BaseLib behavior remains unverified on Android, and no additional PatchAll or custom-save surface is restored for this journey.
- Unknown or unsupported mods are not promoted from installed/selected to loaded or activated without the corresponding runtime evidence.
- No generic compatibility layer or second mod-loader path is added or restored.

The comment-only importer source-path shims live only in the desktop Godot host. They do not implement a mod, add a production loader path, or replace the types loaded from `ImportVanillaSaves.dll`.

## Stage 5 mod-loading boundary

- Workshop and manual candidates resolve through one validated immutable `LauncherModLaunchPlan`, then one source-independent planned-loader entry. No recursive scanner fallback remains.
- Missing, corrupt, wrong-version, or unknown selection state is Vanilla. Modded mode requires at least one explicitly enabled mod.
- `last_mod_launch.json` is atomically replaced for every launch attempt and contains only the mode, selection fingerprint, timestamp, five counts, and one short terminal result per selected mod.
- A malformed or incompatible selection overwrites stale activation with `Failed`; Vanilla overwrites it with an empty Vanilla result. Runtime state and concrete activation evidence are required before a mod can be `Active`.
- Native vanilla and modded save namespaces remain separate. This path never copies, merges, or reinterprets saves.
