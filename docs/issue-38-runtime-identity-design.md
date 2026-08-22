# Issue 38: runtime identity and update transaction design

Status: authoritative design. The `GameIdentity`, current-file reader, identity-selection replacement, per-branch installation state, Steam downloader transaction wiring, and self-validating runtime-pack candidate generation are implemented. Runtime-pack promotion remains staged for a later prompt.

## Decision

The launcher will have one current-game identity, one per-branch installation state, and one runtime-pack generation and promotion path.

The content hashes in the current identity of a branch are computed only from the final installed `SlayTheSpire2.pck` and final installed source `sts2.dll`. The completed install-generation fingerprint comes only from the selected branch's completed Steam install marker. A compatibility manifest, validation report, runtime pack, runtime-cache marker, or release metadata may describe or validate an identity, but none may supply current file hashes.

Every downloaded branch, including `public`, will use the same runtime-pack path for game startup. The public-branch raw-assembly/legacy baseline exception is removed. Launcher-only bootstrap remains separate because it does not load game code.

The existing final runtime-pack layout is retained:

```text
<data>/runtime_packs/<branch-state-directory>/
```

Generation uses sibling staging and backup directories, but the final path does not change.

## Why the current architecture fails

The issue 38 failure is enabled by the following cycle:

1. `DepotDownloader` replaces Steam files at their existing paths.
2. Prior launcher-owned manifests and global cache markers survive the replacement.
3. `GameRuntimeSlot.Hashes.cs` asks those prior outputs for the selected PCK and source-assembly hashes before hashing the files.
4. `GameRuntimeSlot.HashCache.cs` does not bind the cached source-assembly hash to the current file at all.
5. `GameRuntimeSlot.HashValidation.cs` treats hashes from `.android_patch_validation.json` and `compatibility.json` as current hashes.
6. `PatchCompatibilityValidator` can therefore skip regeneration or generate a pack from current bytes while declaring old input hashes.
7. `LauncherRuntimeSlotEvidence` copies the old hashes into `current_runtime_slot.json`.
8. `AndroidAssemblyBootstrapper` hashes the actual files, correctly rejects the mismatch, and cannot run the managed generator because Godot has not started.

The important introducing history is:

- `e6672a9` introduced the beta runtime-pack lifecycle and selected-source hash cache without a source-file identity guard.
- `ff8a054` added the fallback from selected files to passed game-directory and runtime-pack hashes.
- `d101361` split that implementation into the current partial files without changing the model.
- `2253d04` added the native bootstrap validator that compares pack declarations with actual installed files. That check exposed the stale managed identity instead of silently loading mismatched code.

The native validation is correct and remains fail-closed.

Current implementation anchors inspected for this design:

- `GameRuntimeSlot.Hashes.cs:9-50`, `GameRuntimeSlot.HashCache.cs:9-112`, and `GameRuntimeSlot.HashValidation.cs:10-108` contain the competing identity sources.
- `DepotDownloader.cs:42-68`, `DepotDownloader.DepotDownload.cs:38-43`, `DepotDownloader.FileTarget.cs:55-83`, and `DepotDownloader.FileSystem.Operations.cs:8-18` define the current per-file download and commit order.
- `LauncherDownloadCoordinator.Execution.cs:34-67` performs readiness work only after the downloader reports completion, but has no durable `updating` state.
- `PatchCompatibilityValidator.Validation.cs:15-81` owns the current skip/generate decision; `RuntimePackWriter.Assemblies.cs:15-24` deletes the final pack before direct generation.
- `LauncherRuntimeSlotEvidence.Write.cs:25-99` copies the inspected identities into a global marker.
- `LauncherGameFiles.Redownload.cs:12-55` has selected-branch managed cleanup, while `NativeFallbackActivity.java:272-280` deletes all branch installations.
- `AndroidAssemblyBootstrapper.java:1921-2210` independently validates actual selected files and pack outputs; `prepareOnce` at lines 545-806 promotes only a validated assembly cache.

## 1. `GameIdentity`

### Value-object fields

`GameIdentity` has exactly four equality fields:

```csharp
internal sealed record GameIdentity(
    string Branch,
    string InstallGeneration,
    string PckSha256,
    string SourceAssemblySha256
);
```

Serialization adds `schemaVersion: 1`, but the schema version is not an equality field. It selects the parser and canonicalization rules.

Field rules:

| Field | Rule |
|---|---|
| `Branch` | `SteamGameBranch.StorageIdentity` result. It must be non-empty. |
| `InstallGeneration` | Lowercase SHA-256 fingerprint of the exact completed selected-branch Steam marker, which must declare the normalized branch and at least one depot-manifest row. It prevents reuse across completed installs even when paths and metadata happen to collide. |
| `PckSha256` | Lowercase 64-character SHA-256 of the final installed, Android-prepared `SlayTheSpire2.pck`. |
| `SourceAssemblySha256` | Lowercase 64-character SHA-256 of the final installed source `sts2.dll`. This is not the publicized/patched runtime-pack assembly. |

Paths, file sizes, modification times, release strings, individual depot manifest IDs, the PCK preparation version, patch-set version, runtime-pack ID, Android assembly hash, and active-cache hash are deliberately not `GameIdentity` fields. Path, size, and time may only qualify a PCK cache entry together with `InstallGeneration`; they never enter equality.

### Construction

`GameIdentityReader.ReadInstalled(dataDir, branch)`:

1. Normalizes the branch.
2. Resolves the branch's existing game directory and the single supported PCK path.
3. Resolves the source assembly path. If more than one `data_*` directory contains `sts2.dll`, construction fails as ambiguous instead of taking the first directory.
4. Reads the completed install-generation marker and requires its normalized branch and depot provenance to be valid.
5. Requires both files to be regular files, no newer than the completion marker, and the PCK to pass the existing structural checks.
6. Hashes the source DLL directly. It streams the PCK unless the one allowed cache has an exact generation/path/length/mtime match.
7. Rechecks all file and marker snapshots so a concurrent or partial replacement fails closed.
8. Returns a complete `GameIdentity` or a typed failure. It never returns placeholder strings such as `<missing>` as identity fields.

No overload accepts a compatibility manifest, runtime-pack manifest, validation report, runtime-slot marker, or caller-supplied identity/hash. The completed Steam marker and narrowly bound PCK cache are read only by this entry point.

### Equality and IDs

Two identities are equal only when all four fields match:

- branches compare after normalization with ordinal equality;
- hashes compare as decoded 32-byte values, not with culture-sensitive string comparison;
- missing, malformed, or unreadable fields cannot equal a valid identity;
- no canonicalization from an Android-patched PCK hash to an earlier source-PCK hash is allowed.

For logs and artifact keys, `GameIdentityId` is the lowercase SHA-256 of this exact UTF-8 text, with `\n` line endings and no trailing line:

```text
game-identity-v1
branch=<normalized branch>
installGeneration=<lowercase generation fingerprint>
pckSha256=<lowercase hash>
sourceAssemblySha256=<lowercase hash>
```

`GameIdentityId` is derived convenience, not a second identity model. Full hashes remain in every persisted artifact that needs to declare the identity.

## 2. Hashing the large PCK

Correctness takes precedence over a metadata-only hash shortcut.

- The final installed PCK is streamed through SHA-256 with a large sequential buffer; it is never loaded into memory as one byte array.
- One persistent PCK digest cache is allowed. A hit requires schema version 1 and exact equality of normalized branch, exact completed `InstallGeneration`, normalized absolute PCK path, file length, and UTC modification ticks. Missing or legacy fields, malformed JSON, or any mismatch is a cache miss and causes a direct PCK hash.
- The source `sts2.dll` is always hashed directly. Its small size does not justify a digest cache, even when size and modification time appear unchanged.
- The PCK cache is written only after the DLL, PCK, and completed marker have all been rechecked. Persistence uses a unique same-directory temporary file, flush-to-disk, and atomic replace; a failed write is an optimization failure and cannot authorize launch.
- Managed finalization hashes the actual installed PCK once after all Steam writes and PCK preparation have closed their handles.
- Native game startup hashes the actual installed PCK again before accepting the ready state and runtime pack. This preserves, and simplifies, `AndroidAssemblyBootstrapper`'s current fail-closed behavior.
- The OS page cache should make a managed-hash-then-immediate-native-hash sequence cheaper than two cold reads. Device measurements may motivate a later optimization, but such an optimization is outside this design and may not reintroduce manifest or marker fallbacks.

File length and modification time alone are not proof of content. They may authorize PCK digest reuse only as part of the complete generation-bound cache key above. They never authorize DLL reuse. Tests replace the DLL while preserving its length and timestamp to enforce this rule.

### One PCK preparation path

The PCK whose hash enters `GameIdentity` is the final Android-ready PCK. It must not be mutated after the identity is calculated.

`AndroidPckPreparer` becomes the only PCK mutation path:

1. A newly downloaded raw PCK is fully verified against Steam SHA-1 while still at its `.downloading` path.
2. `AndroidPckPreparer` transforms that temporary file and validates the resulting PCK structure.
3. The prepared temporary file is atomically moved over the installed PCK.
4. If a new preparation recipe must be applied to an unchanged Steam PCK, the downloader creates a same-directory temporary copy, prepares and validates the copy, then atomically replaces the installed file. Insufficient space leaves the existing installed file untouched and the branch `updating`.

The current second PCK mutation in `GodotApp.patchGamePckForAndroid` is removed. FMOD inventory/extraction that is still required is moved into the single pre-ready preparation operation; it may produce diagnostics but may not later modify the ready PCK.

An unchanged Steam manifest is handled specially because the installed PCK intentionally differs from Steam's raw SHA-1. The updater hashes the actual installed PCK and compares it with the previous ready state's expected identity as an integrity check. If the manifest or PCK preparation recipe changed, or the actual hash does not match, the raw PCK is downloaded again. The previous state is an expected value here, never the source of the current hash.

## 3. Per-branch lifecycle: `ready -> updating -> ready`

### State file and location

The one update-state artifact is `installation_state.json` in the branch version-slot directory:

```text
public:       <data>/installation_state.json
other branch:<data>/game_versions/<branch-state-directory>/installation_state.json
```

Absence means “not installed.” Absence is not a third persisted lifecycle state. An unreadable or unknown-schema file is treated as not ready and must never authorize launch.

The state schema has two status values.

Updating form:

```json
{
  "schemaVersion": 1,
  "branch": "public-beta",
  "status": "updating",
  "transactionId": "c8a4fdbd-76de-4be2-b9f6-f180fc827b4f",
  "phase": "downloading",
  "startedUtc": "2026-08-22T12:00:00.0000000Z",
  "targetDepots": [
    { "depotId": 123, "manifestId": 456, "manifestSource": "selected" }
  ],
  "lastError": ""
}
```

Ready form:

```json
{
  "schemaVersion": 1,
  "branch": "public-beta",
  "status": "ready",
  "transactionId": "c8a4fdbd-76de-4be2-b9f6-f180fc827b4f",
  "completedUtc": "2026-08-22T12:03:00.0000000Z",
  "depots": [
    { "depotId": 123, "manifestId": 456, "manifestSource": "selected" }
  ],
  "pckPreparationVersion": "android-pck-v1",
  "gameIdentity": {
    "schemaVersion": 1,
    "branch": "public-beta",
    "installGeneration": "...",
    "pckSha256": "...",
    "sourceAssemblySha256": "..."
  },
  "runtimePack": {
    "packId": "...",
    "patchSetVersion": "...",
    "validationSurfaceVersion": "...",
    "androidAssemblySha256": "..."
  }
}
```

`runtimePack` is nullable at installed-file ready publication. It is derived readiness evidence, not part of `GameIdentity` or the branch lifecycle discriminator. A `ready` installation with no matching validated runtime pack is installed but not yet launchable; runtime-pack readiness starts from the exact identity carried by the completion result.

Depot rows are sorted by numeric depot ID before serialization. Individual rows remain provenance and resume information; the completed marker's aggregate fingerprint is the `InstallGeneration` equality field.

### Transition rules

1. The downloader obtains the target depot list without touching installed files.
2. It obtains the per-branch process lock.
3. It atomically writes `status: updating` before the first delete, file replacement, PCK mutation, runtime-pack deletion, or runtime-pack generation.
4. It performs and verifies all Steam changes. Per-file `.downloading` replacement remains in use.
5. It prepares the PCK through the single PCK path.
6. It constructs one `GameIdentity` from the final installed files.
7. It invalidates selected-branch derived artifacts for the previous identity while preserving the active Android assembly cache.
8. It atomically writes `status: ready` for the completed installed files.
9. Runtime-pack readiness receives that exact `GameIdentity`; it does not rediscover identity from derived evidence. Full launch remains blocked until the independently validated runtime pack agrees with the ready identity.

Failure and cancellation leave the state `updating`; they never restore `ready`. This is intentional because the directory may contain a mixture of generations. `lastError` and `phase` may be updated for diagnosis, but neither affects readiness.

On restart, an `updating` branch routes to the managed launcher. Recovery uses the target Steam manifests and existing download state to resume or revalidate the download, then repeats finalization from the actual files. It does not reuse a pre-interruption `GameIdentity` object or old runtime pack declaration.

All writers of branch game files must use this transaction. There is no separate “download complete” marker, “branch ready” marker, or patch-validation state machine.

## 4. Atomic writes and interruption guarantees

### Atomic JSON writer

`installation_state.json` and final runtime-pack JSON files use one `AtomicFileWriter`:

1. Create a uniquely named temporary file in the target directory.
2. Write UTF-8 without BOM.
3. Flush the file through to storage (`Flush(true)` or the platform equivalent).
4. Rename over the target within the same filesystem.
5. Best-effort sync the containing directory where the platform supports it.

Readers accept only the target filename. They ignore temporary files. A missing, malformed, truncated, unknown-schema, or non-`ready` state blocks game launch.

### Logical branch atomicity

The complete multi-gigabyte branch is not copied to a second directory. Existing per-file atomic moves are retained to avoid doubling storage. Logical atomicity comes from writing `updating` before any mutation and writing `ready` only after all outputs are promoted.

The state transition therefore gives this invariant:

> No combination of partially updated files, an old runtime pack, a new runtime pack, or stale evidence can authorize launch while `installation_state.json` is absent, invalid, or `updating`.

### Concurrent access

A per-branch OS file lock prevents two updater/finalizer operations in the same app data directory. The lock is a synchronization primitive, not persisted state. A dead process releases it. Native startup does not wait indefinitely for the lock; it sees `updating` and routes back to the launcher.

## 5. Runtime-pack generation, validation, and promotion

### One path for every branch

All downloaded game launches use:

```text
installed files -> GameIdentity -> staged runtime pack -> validated final runtime pack
-> staged Android assembly cache -> validated active Android assembly cache
```

There is no public-branch selected-game assembly path and no non-public special fallback.

### Staging layout

For branch state directory `<slot>` and transaction `<tx>`:

```text
final:   <data>/runtime_packs/<slot>/
staging: <data>/runtime_packs/<slot>.staging.<tx>.<attempt>/
backup:  <data>/runtime_packs/<slot>.backup.<tx>/
```

Only `RuntimePackPromoter` may rename a staging directory to the final path. `RuntimePackWriter` writes only to a newly created staging directory and never deletes or mutates the final directory. The ready install transaction scopes the candidate and a fresh attempt ID prevents a later generation from ever adopting an incomplete directory left by process interruption.

### Generation and managed validation order

1. Require branch state `ready` with the exact immutable `GameIdentity` supplied by completed-download finalization.
2. Receive the immutable `GameIdentity` object created from installed files. The writer cannot inspect prior pack identity to create it.
3. Create an empty, transaction-specific staging directory. Never reuse a staging directory.
4. Copy the actual source `sts2.dll` into staging and apply the Android publicizer/patches there.
5. Copy required support assemblies and hash every staged assembly.
6. Run symbol/compatibility checks against the actual source assembly and record their results.
7. Write `patch_validation.json` into staging.
8. Write `compatibility.json` into staging. Its schema contains the complete `gameIdentity`, `gameIdentityId`, pack recipe versions, staged assembly hashes, and validation result. Legacy `sourceRuntimeSlotIdentity` composition is removed.
9. Run the same pure runtime-pack validation rules used by native bootstrap against the staging directory and the immutable actual `GameIdentity`:
   - both manifest and report parse;
   - both declare the same complete `GameIdentity`;
   - the identity equals the actual installed identity;
   - branch matches;
   - patch and validation-surface versions match the launcher;
   - validation passed;
   - every declared assembly exists and hashes correctly;
   - no undeclared DLL is present.
10. Recheck that branch state is still `ready` with the same full `GameIdentity`.
11. Promote through `RuntimePackPromoter`:
    - remove stale staging/backup directories belonging to older transactions for this selected branch only;
    - rename existing final to this transaction's backup, if present;
    - rename validated staging to final;
    - validate the final directory again by path;
    - restore the backup if the rename or final-path validation fails;
    - retain the backup until final-path validation succeeds.
12. Return the validated pack descriptor to runtime readiness; installation identity remains unchanged.
13. Delete the selected branch's backup. Failure to delete the backup is diagnostic only because the final pack and ready identity already agree.

`packId` is derived from `GameIdentityId`, patch-set version, validation-surface version, and hashes of generated assemblies. Release/depot metadata may be copied into the manifest for support but does not participate in `GameIdentity` equality.

### Native validation and active-cache promotion

`AndroidAssemblyBootstrapper` remains the final independent gate:

1. Require the selected branch's `installation_state.json` to be `ready`.
2. Construct the actual `GameIdentity` by hashing the actual installed PCK and source DLL.
3. Require it to equal the ready state's declared identity.
4. Validate the final runtime pack against that actual identity, the validation report, and every actual staged assembly hash.
5. Build `.godot/mono/publish/<abi>.staging` from packaged launcher assemblies plus the validated runtime pack.
6. Validate the staged active cache.
7. Promote it using the existing active/backup rename recovery.
8. Store active-cache state keyed by assembly-cache schema, `GameIdentityId`, and `packId`.

`current_runtime_slot.json`, `current_runtime_cache.txt`, and `.android_pck_patch_v*` are not consulted. Native validation becomes stricter because the PCK comparison is direct rather than accepting a legacy source-hash translation.

## 6. Installed-file readiness is written last

The selected branch's `installation_state.json` with `status: ready` is the only file whose successful atomic replacement authorizes the completed installed files to enter runtime-pack readiness. It does not authorize game launch without a matching validated runtime pack.

It is written after:

- all Steam files are committed;
- the final PCK transformation is committed;
- actual files have produced `GameIdentity`;
- selected-branch derived artifacts for the previous identity have been invalidated without deleting the active Android assembly cache.

Runtime-pack readiness then consumes the same identity object and validates/promotes derived outputs. The pending-launch preference or intent is only a request. Native code must still require the ready state, recompute actual identity, and validate the pack. `current_runtime_slot.json` is removed rather than retained as a second launch authorization.

## 7. Selected-branch cleanup

“Clear downloaded files” and managed redownload use one shared selected-branch cleanup plan. The native UI may implement the same plan in Java, but the target computation and artifact list must be covered by cross-language contract tests.

Cleanup order:

1. Normalize the selected branch and resolve its exact version-slot paths.
2. Acquire the selected branch lock.
3. If an installation state exists, atomically write `updating` with phase `cleanup` before deleting anything.
4. Clear the pending game-launch request.
5. Delete only the selected branch's:
   - game directory;
   - download-state directory;
   - temporary state files, while retaining the durable `updating` state until cleanup is complete;
   - final runtime-pack directory;
   - runtime-pack staging and backup directories whose normalized slot key matches the selected branch.
6. If active Android cache state says the cache was built from the selected branch, delete only the derived Mono publish cache and its active-cache state. If it belongs to another branch, preserve it.
7. Delete obsolete global issue-38 markers listed in the migration section; they do not contain user data.
8. Remove the selected branch's installation state last, leaving the branch absent/not installed.

The operation must not delete:

- `<data>/game_versions` as a whole;
- any sibling branch directory;
- an unrelated branch runtime pack;
- launcher preferences other than the pending launch request;
- workshop content;
- local saves, Steam cloud files, local save backups, or anything under the user-save root;
- broad application data.

The current `NativeFallbackActivity` deletion of `<data>/game`, all `<data>/game_versions`, and all `<data>/.godot` is replaced by these selected-branch semantics.

## 8. Legacy migration and invalidation

Legacy artifacts never authorize launch under schema 1.

On first selection of a branch without a valid `installation_state.json`:

1. Discover the installation from its known version-slot path. `steam_branch.txt` may help display provenance during migration but cannot establish current identity or readiness.
2. Write `installation_state.json` as `updating`, phase `migrating`, before changing files.
3. Ignore and remove the selected branch's old runtime pack and `.android_patch_validation.json`.
4. Validate the actual PCK's final Android form without consulting `.android_pck_patch_v*`. If the single preparer can prove the current bytes are already in final form, retain them. Otherwise download a raw PCK from Steam and prepare it through the normal temporary-file path.
5. Compute `GameIdentity` from actual installed files.
6. Generate and promote a new runtime pack through the only promotion path.
7. Write ready state last.
8. Delete the selected branch's `steam_branch.txt` after its depot provenance has been transferred to ready state.

Global `current_runtime_slot.json` and `current_runtime_cache.txt` are deleted during app migration because their selected-file identities are unsafe. `last_runtime_patch_validation.json` may be retained only as historical support output; its identity fields are not read, and the next successful game startup overwrites it with the new diagnostic schema.

Unselected branch installations are not deleted or rewritten eagerly. They migrate independently when selected. User saves require no migration.

If the device is offline and the existing PCK cannot be proven to be in the new preparer's final form, migration stays `updating` and explains that Steam access is required. It does not delete saves or unrelated branches and does not fall back to old identity evidence.

## 9. Failure-state table

Every row assumes process death, I/O failure, cancellation, or power loss at the named point.

| Interruption point | Durable state after restart | Launch result | Recovery |
|---|---|---|---|
| Before writing `updating` | Prior `ready`, and no installed file has been touched | Prior version may launch | Start a new transaction normally. |
| While writing `updating` temporary file | Prior valid state or no readable state; no installed file has been touched | Prior `ready` may launch only if the atomic rename did not occur | Ignore the temp file and retry. |
| After `updating`, before file mutation | `updating` | Blocked | Resume target transaction or restart update. |
| During obsolete-file deletion | `updating`, possibly mixed directory | Blocked | Re-evaluate target manifests and resume. |
| During a normal file download | `updating`, `.downloading` temp may exist | Blocked | Delete/overwrite stale temp and resume. |
| After a normal file's atomic commit | `updating`, some N+1 files installed | Blocked | Manifest diff/hash verification resumes remaining files. |
| During raw PCK download | `updating`; old installed PCK intact | Blocked | Resume/redownload PCK temp. |
| After raw PCK SHA-1 validation, before preparation | `updating`; old installed PCK intact | Blocked | Reuse only the verified transaction temp or redownload it. |
| During PCK preparation of temp | `updating`; old installed PCK intact | Blocked | Delete temp and repeat preparation. |
| After prepared PCK validation, before atomic commit | `updating`; old installed PCK intact | Blocked | Revalidate temp and commit, or regenerate it. |
| After prepared PCK commit | `updating`; final PCK is N+1 | Blocked | Continue finalization from actual files. |
| While hashing actual files | `updating`; no identity persisted as ready | Blocked | Discard partial digest and hash again. |
| After identity calculation, before pack staging | `updating`; identity exists only in memory | Blocked | Recompute identity and continue. |
| During runtime-pack assembly generation | `updating`; transaction staging only | Blocked | Delete selected staging and regenerate. Final old pack is ignored. |
| Between staged report and manifest writes | `updating`; incomplete staging | Blocked | Delete staging and regenerate. |
| During staged-pack validation | `updating`; staging untrusted | Blocked | Delete or diagnose staging; never promote it. |
| Before final-to-backup rename | `updating`; old final and valid staging may exist | Blocked | Validate staging and retry promotion. |
| After final-to-backup, before staging-to-final | `updating`; backup and staging exist, final absent | Blocked | Validate staging and promote it; restore backup only if promotion itself fails. |
| After staging-to-final, before final-path validation | `updating`; candidate final exists | Blocked | Validate final; regenerate if invalid. |
| After final-path validation, before writing `ready` | `updating`; valid N+1 final pack exists | Blocked | Recompute actual identity, revalidate final, then write ready. |
| While writing ready-state temp | `updating` target plus a temp file | Blocked | Ignore temp; revalidate and retry atomic ready write. |
| After ready rename, before backup cleanup | `ready`; valid final and obsolete backup | Allowed after native revalidation | Delete backup opportunistically. |
| During native actual-file hashing | `ready`; active cache unchanged | Blocked for this attempt | Retry hashing; do not use markers. |
| Native detects ready/actual/pack mismatch | `ready` is contradicted by actual files | Blocked and route to launcher | Atomically demote state to `updating`, record failure, regenerate from actual files or redownload. |
| During Android active-cache staging | `ready`; old active cache remains | Blocked for this attempt | Delete staging and retry. |
| After active cache renamed to backup, before staging promotion | `ready`; active backup exists | Blocked for this attempt | Existing `recoverInterruptedCacheReplacement` restores or completes promotion. |
| After staged active cache promotion, before active-cache state write | `ready`; new cache exists but is not marked current | Safe retry | Native validation detects missing/old cache state and rebuilds or revalidates. |
| After active-cache state write | `ready`; cache key matches identity and pack | Allowed | Normal launch. |
| During selected-branch cleanup | `updating`, phase `cleanup` | Blocked | Repeat idempotent selected-path deletion, then remove state. |

No recovery row reads a legacy marker to reconstruct current identity.

## 10. Deletion and demotion map

### Managed methods and fallback paths

| Current code | Disposition |
|---|---|
| `GameRuntimeSlot.PckSha256OrMissing` fallback to `CachedSelectedPckSha256` | Delete. Use the PCK hash from `GameIdentityReader.ReadInstalled`. |
| `PckSha256OrMissing` fallback to `ValidatedGameDirectoryPckSha256` | Delete. A validation output cannot identify the current file. |
| `PckSha256OrMissing` fallback to `RuntimePackSourcePckSha256` | Delete. A runtime-pack output cannot identify the current file. |
| `GameRuntimeSlot.SourceAssemblySha256OrMissing` fallback to `CachedSelectedSourceAssemblySha256` | Delete. |
| `SourceAssemblySha256OrMissing` fallback to `ValidatedGameDirectorySourceAssemblySha256` | Delete. |
| `SourceAssemblySha256OrMissing` fallback to `RuntimePackSourceAssemblySha256` | Delete. |
| `GameRuntimeSlot.HashCache.cs`: `CachedSelectedPckSha256` | Delete. Replace it with the sole schema-versioned PCK cache bound to normalized branch, exact completed install generation, normalized path, length, and UTC mtime. |
| `GameRuntimeSlot.HashCache.cs`: `CachedSelectedSourceAssemblySha256` | Delete. |
| `GameRuntimeSlot.HashCache.cs`: `CachedActiveAndroidAssemblySha256` | Delete as an input to slot inspection. Hash the active file when diagnosing/validating it. |
| `GameRuntimeSlot.ActiveAndroidAssemblySha256OrMissing` cache-first behavior | Replace with an actual-file hash used only for derived active-cache validation/diagnostics. It is not part of `GameIdentity`. |
| `GameRuntimeSlot.HashIdentity.cs`: `FileIdentityMatches` and `FileIdentity` | Delete from identity selection. File metadata remains diagnostics only. |
| `GameRuntimeSlot.HashValidation.cs`: `ValidatedGameDirectoryPckSha256`, `ValidatedGameDirectorySourceAssemblySha256`, `ValidatedGameDirectoryHash` | Delete. |
| `GameRuntimeSlot.HashValidation.cs`: `RuntimePackSourcePckSha256`, `RuntimePackSourceAssemblySha256`, `RuntimePackSourceHash` | Delete. |
| `GameRuntimeSlot.Inspect.Canonicalize.cs`: `CanonicalizeRuntimePackSourcePckSha256` | Delete. The installed post-preparation PCK hash is canonical. |
| `RuntimePackManifest.Validation.SourcePck.cs`: `SourcePckMatches` marker translation | Delete. Manifest PCK hash must directly equal the actual installed PCK hash. |
| `GameRuntimeSlot.Identity.cs`: release/depot/runtime-source composition in `BuildRuntimeSlotIdentity` | Replace with `GameIdentity` plus separately derived `packId`; do not retain a second slot identity. |
| `RuntimePackSlotIdMatchesFor`, `BuildRuntimePackSlotIdentity`, `BuildRuntimePackSlotId` | Replace with direct full `GameIdentity` and pack-recipe validation. |
| `PatchCompatibilityEvidence.Inspect` fallback to game-directory marker | Delete. Only the validated final pack/report represents prepared compatibility. |
| `PatchCompatibilityEvidence.Inspect` `legacy public APK baseline` | Delete. Public uses the same runtime-pack path. |
| `PatchCompatibilityValidator.SelectedVersionSlotAlreadyValidated` | Replace with final-pack validation against freshly read actual `GameIdentity`; “playable” flags alone cannot skip generation. |
| `PatchCompatibilityValidator.WriteMarker` | Delete with `.android_patch_validation.json`. Validation report is written only inside staged pack. |
| `RuntimePackWriter.PreparePackDirectory` deleting the final pack | Delete. Writer receives a new staging directory. |
| `RuntimePackWriter.WriteValidatedRuntimePack` direct-final behavior | Split into generate-staging, validate-staging, and the sole `RuntimePackPromoter`. |
| `RuntimePackWriter.DeleteRuntimePack(GameRuntimeSlot, ...)` | Move selected-path cleanup out of the writer. Generation failure deletes staging, not an unrelated or previously final pack. |
| `LauncherRuntimeSlotEvidence.Write`, `.Read`, `.Match`, and `.Clear` | Delete. `installation_state.json` replaces the artifact and is per branch. |
| `LauncherRuntimeCacheEvidence.Match` readiness methods | Delete. Active-cache state cannot define selected-game identity. |
| `LauncherRuntimePatchValidationEvidence.RuntimeCacheValue` | Delete. Post-launch diagnostics receive explicit `GameIdentityId`/`packId` from the active launch context. |
| `LauncherLaunchReadinessCacheIdentities` dependencies on old marker timestamps | Delete those keys. A readiness object is scoped to one immutable `GameIdentity` read and one state snapshot. |
| `LauncherDownloadCoordinator.CompleteDownload` direct readiness refresh without durable update finalization | Replace with completion of the branch transaction; it cannot report ready until final state commit succeeds. |
| `DepotDownloader.WriteBranchMarker` | Delete. Depot provenance and readiness are committed together in `installation_state.json`. |
| `DepotDownloader.PatchGamePck` operating on the installed final file | Replace with the single `AndroidPckPreparer` operating on a verified temporary file before atomic commit. |
| `LauncherGameFiles.BranchMarkerReady` and branch-marker readiness readers | Replace with installation-state parsing plus actual identity/pack validation. |
| `RuntimeSlotMetadata` branch-marker identity inputs | Demote release/depot values to ready-state provenance and diagnostics; they do not participate in identity equality. |
| `LauncherGameFiles.DeleteDownloadedState` independently enumerating cleanup targets | Retain the user action but delegate it to the one selected-branch cleanup plan also used by native recovery. |

### Native methods and fallback paths

| Current code | Disposition |
|---|---|
| `AndroidAssemblyBootstrapper.selectedPckSha256ForRuntimePackValidation` marker shortcut | Delete. Stream-hash the actual selected PCK. |
| `AndroidAssemblyBootstrapper.pckMatchesRuntimeSource` `.android_pck_patch_v35` fallback | Delete. Compare the manifest's final installed PCK hash directly with the actual hash. |
| Acceptance of an empty/legacy PCK patch marker | Delete. |
| `CURRENT_RUNTIME_SLOT_MARKER` reads in `AndroidAssemblyBootstrapper` | Replace with per-branch `installation_state.json` ready-state validation. |
| `CURRENT_RUNTIME_CACHE_MARKER` as an input to managed identity | Delete that use. The native marker may remain diagnostic-only during transition. |
| `AndroidAssemblyBootstrapper.isBranchMarkerReady` / `isGamePckReady` branch-marker gate | Replace the branch-marker portion with selected installation-state `ready`; retain structural PCK validation. |
| `LauncherActivity` and `GodotApp` branch-marker readiness gates | Replace with the same installation-state parser/contract. No independent readiness interpretation. |
| `AndroidAssemblyBootstrapper.currentRuntimeCacheId` legacy runtime-slot composition | Replace with `(assemblyCacheSchema, GameIdentityId, packId, branch)`. |
| `AndroidAssemblyBootstrapper.writeRuntimeCacheMarker` selected-file fields | Reduce to output-only active-cache diagnostics; never read it for selected identity. |
| `GodotApp.patchGamePckForAndroid` | Delete after its required transformations/extraction are consolidated into `AndroidPckPreparer`. Ready files must be immutable. |
| `GodotApp.resolveAndroidPckPatchSourceSha256`, `findBestPckPatchMarkerCandidate`, and source-hash chain traversal | Delete. |
| `NativeFallbackActivity` deletion of all `game_versions` and all `.godot` | Replace with selected-branch cleanup. |
| Public raw selected-game assembly fallback in bootstrap | Delete. All game launches require the validated runtime pack. |

`AndroidAssemblyBootstrapper.isRuntimePackManifestUsable`, staged active-cache validation, undeclared-DLL rejection, assembly hashing, backup restoration, and fail-closed blocking are retained and updated to read the new manifest/state schema. They are not weakened.

### `current_runtime_cache.txt` field map

The old text file is never read for current-game identity or launch readiness. It may be replaced by an output-only active-cache diagnostic. Every existing field has this disposition:

| Existing field | Disposition |
|---|---|
| `UTC millis` | Retain as diagnostic generation time. |
| `Package` | Retain as diagnostic. |
| `Version name` | Retain as diagnostic. |
| `Version code` | Retain as diagnostic. |
| `Assembly cache schema` | Retain in native active-cache state and diagnostics. |
| `Selected branch` | Rename to `Active branch`; it describes the derived active cache, not selection authority. |
| `Selected branch requires runtime pack` | Delete; every game branch uses a pack. |
| `Runtime ID` | Replace with explicit `GameIdentityId` and `packId`. |
| `Runtime source` | Delete; game code source is always `runtime-pack`. |
| `Runtime pack directory` | Diagnostic only. Never use it to select a pack; derive final path from normalized branch. |
| `Runtime pack game assembly` | Diagnostic only. |
| `Game directory` | Delete from cache state. Derive from branch paths. |
| `Selected PCK path` | Delete. |
| `Selected PCK identity` | Delete. Size/mtime is not content identity. |
| `Selected PCK SHA256` | Delete from active-cache marker. Ready state and pack declare expected identity; actual current identity is rehashed. |
| `Selected source sts2.dll` | Delete. |
| `Selected source sts2.dll SHA256` | Delete from active-cache marker. |
| `Active source sts2.dll` | Delete or rename to output-only `Runtime pack sts2.dll path`. |
| `Active source sts2.dll SHA256` | Output-only runtime-pack assembly hash; never a selected-file hash. |
| `Publish cache directory` | Diagnostic only. |
| `Publish cache active sts2.dll SHA256` | Retain as output/active-cache integrity evidence, verified against the actual cache file. |

The authoritative native active-cache key is stored in the existing native cache-state mechanism as `(assemblyCacheSchema, GameIdentityId, packId, branch)`. The diagnostic text/JSON file is not consulted to determine that key.

### Artifact map

| Artifact | Disposition |
|---|---|
| `current_runtime_slot.json` | Delete. Replaced by per-branch `installation_state.json`. |
| `current_runtime_cache.txt` | Remove as input. Replace or retain only as output-only active-cache diagnostics with the reduced fields above. |
| `last_runtime_patch_validation.json` | Output-only post-launch patch diagnostics. Remove selected PCK/source identity fields copied from runtime-cache text. Never use for readiness. |
| `<game>/.android_patch_validation.json` | Delete. Its only safe role is duplicated by the pack's validation report. |
| `<game>/.android_pck_patch_v*` | Delete after migration. No source-hash translation or patch idempotence may depend on it. |
| `<game>/steam_branch.txt` | Replace with per-branch `installation_state.json`; use only for one-time provenance migration, then delete. |
| `<runtime-pack>/compatibility.json` | Retain as derived output, schema-updated to declare complete `GameIdentity`; never use it to discover current identity. |
| `<runtime-pack>/patch_validation.json` | Retain as derived output and validation evidence; never use it to discover current identity. |
| `<runtime-pack>/sts2.dll` and support DLLs | Retain as derived output, fully hash-validated. |
| `download_state/*.id` and `*.manifest` | Retain only for Steam diff/resume behavior. They do not authorize launch or define `GameIdentity`. |
| `release_info.json` | Retain as display/support metadata only. |
| `last_steam_branch_availability.txt` | Retain as discovery diagnostics only. |
| `last_game_version_redownload.txt` and cache-cleanup markers | Retain as diagnostics only; never readiness input. |
| `.godot/mono/publish/<abi>` | Retain as derived active cache. Its identity is the validated pack key, not a source of selected-file identity. |
| runtime-pack final directory | Retain existing layout as a derived output. Presence alone never authorizes launch. |

## 11. Test matrix

### Test structure

Add a focused managed test project, for example `tests/STS2Mobile.Tests`, with real temporary files and injected small test hash streams. Restore native JVM coverage under `android/src/test` or restore the historical `AndroidAssemblyBootstrapperTest.java` runner. Static audit scripts are not sufficient for lifecycle behavior.

The current reduced checkout has launcher preview/save-safety tests but no focused managed runtime-pack update test and no current `AndroidAssemblyBootstrapperTest.java`. Historical commit `2253d04` added the Java bootstrap suite, including stale-game-version and stale-PCK rejection cases. Those tests prove native fail-closed behavior, not the missing managed N -> N+1 regeneration path; their relevant assertions should be retained and extended rather than treated as complete coverage.

The managed and native suites share JSON fixtures for `GameIdentity`, `installation_state.json`, `compatibility.json`, and `patch_validation.json` so field names and equality rules cannot drift.

### Managed lifecycle matrix

| Scenario | Expected result |
|---|---|
| N files, N ready state, valid N pack | Identity reads from files; pack validates; no regeneration. |
| N files and valid pack, then PCK and DLL replaced by N+1 at same paths while all N markers/manifests remain | Actual N+1 identity is observed; every N artifact is ignored; N+1 pack is generated and ready state is written last. |
| PCK-only N -> N+1 update | Identity differs; pack regenerates. |
| DLL-only N -> N+1 update | Identity differs; pack regenerates. |
| Updated file keeps old length and modification time | Actual hash still changes and stale evidence cannot be reused. |
| N+1 bytes equal N under a distinct completed install generation | `GameIdentity` differs by `InstallGeneration`; no prior-generation cache entry or derived pack may silently authorize it. |
| Stale `.android_patch_validation.json` says passed | File is ignored/deleted; it cannot skip generation. |
| Stale `compatibility.json` and `patch_validation.json` agree with each other but not actual files | Final pack rejected and regenerated. |
| Stale `current_runtime_slot.json` and `current_runtime_cache.txt` contain matching old hashes | Both ignored/deleted; N+1 succeeds. This is the direct issue 38 regression. |
| Ambiguous multiple source `sts2.dll` locations | Identity construction fails explicitly; no first-directory fallback. |
| Missing or malformed PCK/source DLL | No identity, no ready state, no pack promotion. |
| Unknown state or manifest schema | Fail closed and migrate/regenerate; do not guess aliases. |
| Public branch | Uses the exact same identity, pack generation, promotion, and ready-state path. |
| Custom/non-public branch | Uses the same path with only normalized branch/path differences. |

### Update-state and interruption matrix

Inject a process-stop exception after each numbered lifecycle checkpoint and assert the corresponding row in the failure table:

1. before `updating` rename;
2. after `updating` rename;
3. after an obsolete delete;
4. after each normal file commit;
5. during PCK temporary preparation;
6. after PCK commit;
7. during each actual-file hash;
8. during runtime-pack assembly generation;
9. after report write;
10. after manifest write;
11. after staged validation;
12. after final-to-backup rename;
13. after staging-to-final rename;
14. after final-path validation;
15. during ready-state temporary write;
16. after ready-state rename;
17. during selected-branch cleanup.

For every checkpoint:

- `updating`, absent, malformed, or mismatched state must block launch;
- recovery must be idempotent when run twice;
- no temp or backup path from another branch may be touched;
- ready state must never refer to a missing or invalid final pack.

### Runtime-pack promotion matrix

| Scenario | Expected result |
|---|---|
| Existing valid N final, valid N+1 staging | N becomes backup, N+1 becomes final, final revalidates, ready commits, backup is removed. |
| Staging manifest/report mismatch | Staging rejected; N final remains; branch remains `updating`. |
| Staging assembly hash mismatch or undeclared DLL | Staging rejected and never promoted. |
| Failure renaming final to backup | No ready write; recovery retries without deleting unrelated paths. |
| Failure renaming staging to final | N backup is restored where possible; state stays `updating`. |
| Process death with backup + staging and no final | Recovery validates/promotes staging; it does not authorize the old pack for N+1 files. |
| Process death with valid final + backup before ready | Recovery rehashes actual files, validates final, commits ready, then removes backup. |
| Patch-set or validation-surface version changes with identical game files | Pack regenerates; `GameIdentity` stays unchanged. |

### Native bootstrap matrix

| Scenario | Expected result |
|---|---|
| Ready identity, actual files, manifest/report, and pack payload all agree | Pack accepted; active cache staged and promoted. |
| Ready state's PCK differs from actual PCK | Block; demote selected branch to recovery. |
| Ready state's source DLL differs from actual DLL | Block; demote selected branch to recovery. |
| Pack differs from ready/actual identity | Block. No legacy PCK marker acceptance. |
| `.android_pck_patch_v35` claims a matching old source hash | Ignored; direct actual PCK mismatch still blocks. |
| `current_runtime_slot.json` is fresh and claims old hash | Ignored. |
| Active cache belongs to N and selected branch is N+1 | Rebuild from validated N+1 pack. |
| Active-cache promotion is interrupted at active-to-backup or staging-to-active | Existing backup recovery restores or completes a validated cache; no stale cache is marked current. |

### Branch isolation and saves

Use sentinel files with hashes before and after every update, recovery, migration, and clear test:

| Scenario | Required assertions |
|---|---|
| Update `public-beta` with `public` and another branch installed | Only `public-beta` game, state, download state, pack, staging, and backup paths change. |
| Clear `public-beta` | Public and other branch directories and packs are byte-for-byte unchanged. |
| Clear public | Side-by-side branch directories and packs are unchanged. |
| Active cache belongs to unrelated branch during selected-branch clear | Active cache is preserved. |
| Active cache belongs to selected branch during clear | Only derived publish cache/state is removed. |
| Any lifecycle failure | Local save root, Steam cloud cache, and local save backups are byte-for-byte unchanged. |
| Legacy migration | Saves are not enumerated, moved, rewritten, or deleted. |

At least one instrumented ARM64 device test must perform a real `public-beta` N -> N+1 update, kill the process at several transaction phases, and confirm that the first completed post-update Start Game succeeds. Pixel-class Android SDK 37 and Xiaomi Android 16 coverage remains required because those devices supplied issue 38 evidence, even though the state bug is architecture-independent.

## Implementation sequence

The production work should land in dependency order so no intermediate release has two authorities:

1. Add `GameIdentity`, shared schemas, atomic writer, and tests.
2. Add per-branch installation state and gate both managed and native launch on it.
3. Move PCK transformation to the downloader's temporary-file finalization and remove post-ready mutation.
4. Convert runtime-pack writer to staging-only generation and add the sole promoter.
5. Convert all branches to the same runtime-pack path.
6. Remove managed identity fallbacks and runtime-slot evidence.
7. Remove native marker/PCK-source fallbacks while retaining strict validation.
8. Replace native and managed clear behavior with selected-branch cleanup.
9. Add legacy migration, then delete obsolete artifact readers and diagnostics that imply authority.

Steps 2 through 7 should ship together or behind a one-way schema migration. A build must never write the new ready state while native startup still accepts old marker authority, or vice versa.

## Resolved trade-offs and remaining verification

Architectural decisions are resolved:

- actual installed bytes are authoritative;
- there is exactly one persistent PCK digest cache, and it is usable only with an exact completed-generation/path/length/mtime binding;
- every branch uses a runtime pack;
- `installation_state.json` ready is written last;
- runtime-pack layout remains branch-keyed at its existing final location;
- legacy evidence is invalidated, not synchronized;
- native validation remains fail-closed;
- saves and unrelated branch installations are outside every mutation set.

Implementation still requires measurement and platform verification, not further architecture choices:

- measure two sequential PCK hashes on affected ARM64 devices and report managed/native durations;
- verify same-directory rename and durable-flush behavior on every supported Android API level;
- inventory the non-mutating FMOD extraction behavior currently coupled to `GodotApp.patchGamePckForAndroid` so it is preserved in the single preparation operation;
- capture raw legacy artifact variants from a real v0.2.428 device to test migration diagnostics.

None of those checks permits reintroducing a manifest, marker, runtime pack, or active cache as a source of current `GameIdentity`.
