# Issue #38: runtime identity and update lifecycle

This describes the current implementation at [source commit `33abc177`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/commit/33abc177097fe55bd2c630756f55ecd5e7a60ef8), used for v0.2.436. For user actions, see [Steam version selection](steam-version-selection-user-guide.md). For a shorter overview, see [the architecture summary](steam-version-selection-architecture.md).

## Decision

Installed game files define one `GameIdentity` per branch. `installation_state.json` records whether those files are being changed or have completed preparation. Runtime-pack and Android assembly-cache preparation follow installed-file completion. Launch authorization is a separate final step.

Every branch, including `public`, requires a matching runtime pack. Public keeps its existing primary storage slot; other branches use separate slots.

## Identity and artifact roles

`GameIdentity` has four equality fields:

| Field | Source |
|---|---|
| `Branch` | Normalized Steam branch storage identity. |
| `InstallGeneration` | SHA-256 of the completed selected game's `steam_branch.txt` bytes. The reader requires matching branch and complete depot-row provenance. |
| `PckSha256` | SHA-256 of the final installed, Android-prepared `SlayTheSpire2.pck`. |
| `SourceAssemblySha256` | SHA-256 of the installed source `sts2.dll`. |

[GameIdentityReader](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Launcher/GameIdentityReader.cs) resolves the installed files, checks the PCK header and completion timestamps, and rejects ambiguous source DLL locations. It rechecks file/marker snapshots around hashing. The source DLL is always hashed directly. A persistent PCK digest can be reused only for the same completed generation, normalized path, length, and modification time.

Hashes are validated and normalized to lowercase. The derived `GameIdentity.Id` hashes this UTF-8 text with `\n` separators and no trailing newline:

```text
game-identity-v1
branch=<normalized branch>
installGeneration=<generation hash>
pckSha256=<PCK hash>
sourceAssemblySha256=<source DLL hash>
```

The persisted files serve different purposes:

| Artifact | Current role |
|---|---|
| `installation_state.json` in the branch slot | Schema 1, `updating` or `ready`, with transaction and depot information. `ready` carries the installed `gameIdentity` and PCK preparation version; it is insufficient to authorize launch. |
| `<game>/steam_branch.txt` | Retained completed-install provenance and the input to `InstallGeneration`. It cannot supply PCK or DLL content hashes. |
| `<data>/runtime_packs/<branch-state-directory>/` | Derived pack containing `compatibility.json`, `patch_validation.json`, and an unchanged copy of source `sts2.dll`. Presence alone is insufficient. |
| `<data>/current_runtime_slot.json` | Retained launch-authorization marker for an exact `GameIdentity` and runtime-pack ID. Written after pack/cache checks and rechecked before launch. |
| `<data>/current_runtime_cache.txt` | Output describing the active cache for diagnostics; it does not define installed-file identity. |
| `.godot/mono/publish/<abi>` | Derived Android assembly cache, validated against the selected runtime pack. |
| Old `.android_patch_validation.json` and `.android_pck_patch_v*` markers | Not accepted as current-file identity or a substitute for current validation; selected-branch cleanup removes them. |

State paths remain `<data>/installation_state.json` for public and `<data>/game_versions/<branch-state-directory>/installation_state.json` for other branches.

## Installed-file update

[DepotDownloader](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Steam/DepotDownloader.cs) performs this sequence:

1. Fetch the target depot list, obtain the branch lock, and publish `updating` before installed-file mutations.
2. Download and verify Steam files through temporary `.downloading` files, then commit them to their installed paths.
3. Prepare the installed PCK **in place**, while the branch remains `updating`.
4. Publish the completed Steam marker and calculate `GameIdentity` from the final files.
5. Invalidate the selected branch's previous derived artifacts, retaining the active Android assembly cache.
6. Commit installed-file `ready` and return that exact identity to runtime readiness.

Raw PCK commit precedes Android preparation. The implementation does not preserve an old installed PCK throughout preparation: interruption can leave installed bytes needing recovery, and `updating` blocks their launch. [DepotDownloader.PckPatch.cs](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Steam/DepotDownloader.PckPatch.cs) implements this preparation.

[BranchInstallUpdate.CompleteInstalledFiles](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Launcher/BranchInstallUpdate.cs) publishes `ready` before runtime-pack work. Its ready record has no runtime-pack descriptor; that schema field is optional derived information. A completed install can therefore still need runtime preparation before it launches.

Recognized ARM64 V2 PCK repairs use the same `updating`/completion machinery and in-place preparer. [LocalPckRepairOperation](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Launcher/LocalPckRepairOperation.cs) resumes only repairs that pass its eligibility checks; other incomplete installations require the launcher's download/recovery path.

The managed atomic JSON writer uses a unique same-directory temporary file, flushes it to disk, then replaces the target. It does not sync the containing directory. The update uses per-file commits and a readiness gate rather than a second copy of the whole branch; power-loss durability across Android filesystems has not been established by the launch smoke tests.

## Runtime pack and launch authorization

Runtime preparation starts with an installed-file `ready` state and the expected identity:

1. [RuntimePackWriter](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Launcher/RuntimePackWriter.cs) rechecks the installed identity and writes a unique sibling staging directory scoped to the install transaction and generation attempt. It copies `sts2.dll` unchanged and verifies the copy's hash.
2. Compatibility checks inspect the source assembly. Manifest/report validation checks identity, recipe versions, validation results, declared assembly hashes, and undeclared DLLs.
3. [RuntimePackPromoter](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Launcher/RuntimePackPromoter.cs) locks promotion, revalidates the candidate, renames the existing final pack to a backup, promotes staging, and validates the final path. It attempts backup restoration on failure and cleans obsolete attempts after success. A valid existing final pack can be reused.
4. [AndroidAssemblyBootstrapper](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/android/src/com/game/sts2launcher/AndroidAssemblyBootstrapper.java) independently hashes the selected PCK, source DLL, and completed marker; checks the ready state and pack; then prepares and validates the active assembly cache using its staging/backup recovery path.
5. [RuntimePackLaunchLifecycle](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/33abc177097fe55bd2c630756f55ecd5e7a60ef8/src/STS2Mobile/Launcher/RuntimePackLaunchLifecycle.cs) rechecks installed identity, final pack, and cache results, then writes and reads back `current_runtime_slot.json`. Authorization must match the full identity and pack ID, and is reconfirmed before launch.

The final authorization marker is derived evidence. Its recorded hashes never replace reading the installed files. A pending launch preference is only a request. Runtime patches are applied through shared Harmony when game code loads; pack generation does not publicize or rewrite the source game DLL.

## Failure and recovery boundaries

| Failure or interruption | Durable state and next step |
|---|---|
| Before publishing `updating` | Installed files have not been mutated by this transaction. Any previous launch still needs its normal identity, pack, and authorization checks. |
| During download, installed-file replacement, PCK preparation, or final identity calculation | State remains `updating`; launch is blocked. Resume/revalidate the download, use an eligible local PCK repair, or redownload the selected version. There is no whole-install rollback guarantee. |
| After installed-file `ready`, before pack preparation | Installed files remain `ready`, but a missing/mismatched pack blocks launch. Runtime readiness can retry from that identity. |
| During pack generation | Installed-file state remains `ready`; an incomplete candidate cannot authorize launch. Rejected staging is cleaned where possible, and a later attempt uses fresh staging. |
| During pack promotion | Installed-file state remains `ready`. Promotion recovery restores an interrupted backup or validates the final pack; an abandoned staging directory is not automatically adopted as a new candidate. |
| During Android cache staging/replacement | Cache preparation fails for that attempt. Native backup recovery and validation must complete before launch preparation succeeds. |
| After cache preparation, before authorization publication | Preparation has not completed. Retry the checks and publish exact-identity authorization. |
| Ready state, installed files, pack, or authorization disagree | Launch is blocked. The ready state is not necessarily demoted; recovery depends on the failed check. Stale markers cannot repair the mismatch. |
| During selected-branch redownload cleanup | The selected branch stays `updating` until owned deletions succeed; its state is removed last. Retry incomplete cleanup. |

## Selected-branch cleanup and older installs

Managed and native recovery resolve paths from the selected branch. Full redownload removes its game/download state, runtime pack and owned staging/backup paths, then its installation state. Global derived evidence is removed when owned by that branch or too invalid to identify an owner. The active assembly cache is retained; future launches must validate or replace it.

Cleanup preserves sibling branches, Workshop content, credentials, preferences unrelated to launch, gameplay saves, and save backups. It does not delete all `game_versions` or `.godot` directories.

Missing, malformed, unknown-schema, or `updating` installation state cannot authorize a game launch. Older marker files do not substitute for ready state or actual-file hashes. There is no general offline migration that can infer readiness from those markers; use the recovery action shown by the launcher. `steam_branch.txt` remains part of the current completed-generation contract.

## Verification and device evidence

Use the commands and prerequisites in [Building and testing](development.md#tests). The managed suite covers identity reads, stale evidence, branch updates and selected interruption points, pack validation/promotion, branch isolation, save/credential preservation, and attempt-bound handoff. Android JVM tests cover native validation/recovery and handoff behavior. These are focused checks, not a claim that every physical-storage interruption point has been exercised.

The exact [v0.2.429 RC4 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4) passed 10/10 counted launches on Samsung `SM-F971B`, Android 17 / API 37: four cold, four warm, one background/resume, and one lock/unlock. The recorded audit found saves, Steam Cloud inventory, credentials, and unrelated branch/runtime data unchanged. [The runbook](runbook-android-validation.md#v02429-rc4-runtime-and-handoff-result) retains the exact artifact and result.

That older APK's result does not validate v0.2.436, every device/branch/GPU/mod, live Steam save transfers, or a full N → N+1 interruption campaign. The original Pixel 9 and Xiaomi 17 Ultra RC4 reporter flows remain unconfirmed in the published record. Current build-specific evidence belongs in [Android testing status](current-android-status.md).

## Pre-fix history

Issue #38 exposed an earlier identity cycle: Steam replaced game files, managed code reused hashes from prior cache/manifests, and native bootstrap rejected the resulting mismatch against the actual files. The native rejection was the final check exposing stale managed evidence.

The removed implementation can be inspected at the pre-fix `d101361` revision: [hash selection](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/d101361290e58482c5f7795ed8d315ffdc8ffdd1/src/STS2Mobile/Launcher/GameRuntimeSlot.Hashes.cs), [hash cache](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/d101361290e58482c5f7795ed8d315ffdc8ffdd1/src/STS2Mobile/Launcher/GameRuntimeSlot.HashCache.cs), and [manifest/marker fallbacks](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/d101361290e58482c5f7795ed8d315ffdc8ffdd1/src/STS2Mobile/Launcher/GameRuntimeSlot.HashValidation.cs). These links describe the old failure, not current APIs.

The lifecycle fix landed in [`6180d361`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/commit/6180d361e13807aecdb6b7ec828756f236e9119b). Handoff work followed in [`809cdf82`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/commit/809cdf827c69680df34177566433bd691aaff5ae) and [`55925130`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/commit/559251309f68659102bcabbf12e669f9512e7776), released as RC4. The current source links above also include later changes, including the restored original mod framework.
