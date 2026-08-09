# Stage 1-3 retain/delete inventory

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
