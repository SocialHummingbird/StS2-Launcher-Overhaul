# Steam Version Selection Save Compatibility Matrix

This matrix tracks save behavior when switching between Steam game versions. Until these rows have current ARM64 evidence, save compatibility across branches remains unknown. Steam Cloud Push must be treated as destructive unless the selected version has fresh Pull-before-Push evidence and Android local save evidence; after a branch switch, the extra branch-switch backup gates also apply.

## Rule

Do not assume public/default and beta saves are compatible. Prove compatibility with device evidence, or document the risk as unsupported.

## Required evidence per row

Each completed row should include:

- App version and APK asset.
- Device model and Android version.
- Starting selected game version.
- Target selected game version.
- Pull-from-Cloud status before switching.
- Local save/profile state before switching.
- Local save/profile state after switching.
- Whether the game launched.
- Whether the selected profile appeared in-game.
- Whether any save migration, reset, crash, or profile loss occurred.
- Whether local backup was enabled before switching.
- Whether Push was avoided or, if intentionally tested, backup evidence existed first.
- Linked diagnostics, logcat, screenshots, and branch marker files.

## Compatibility matrix

| From | To | Expected risk | Required result before release signoff | Current state |
| --- | --- | --- | --- | --- |
| Public/default | Public/default | Baseline regression | Existing public save/profile survives update/download/launch | Missing current version-selection evidence |
| Public/default | Beta | Save format may differ | Beta launches and either reads public save safely or clearly isolates/rejects it without data loss | Missing ARM64 evidence |
| Beta | Public/default | Save format may differ | Public launches after beta use and either reads save safely or clearly isolates/rejects it without data loss | Missing ARM64 evidence |
| Beta | Beta | Branch cache regression | Beta save/profile survives restart/update/download/launch within the selected beta cache | Missing ARM64 evidence |
| Public/default after beta cache exists | Public/default | Cache selection regression | Public launch uses `files/game/SlayTheSpire2.pck`, not beta PCK | Missing ARM64 evidence |
| Beta after public cache exists | Beta | Cache selection regression | Beta launch uses `files/game_versions/beta/game/SlayTheSpire2.pck`, not public PCK | Missing ARM64 evidence |

## Push safety matrix

| Scenario | Minimum gate before Push | Current state |
| --- | --- | --- |
| No branch switch in current session | Pull first for the selected version, verify Android local saves exist, confirm overwrite-risk prompt intentionally, and confirm diagnostics/markers show `Manual Pull completed before Push`, current important Android local save evidence, and `Baseline manual Push prerequisites satisfied` | Needs newest-public evidence |
| Public/default to beta switch | Pull first for the selected version, local saves exist, local backup enabled, backup storage permission available, local pre-Push backup exists, cloud pre-Push backup exists, `last_manual_cloud_push.txt` records selected branch, baseline prerequisites, local-save evidence, and backup evidence | Missing ARM64 evidence |
| Beta to public/default switch | Pull first for the selected version, local saves exist, local backup enabled, backup storage permission available, local pre-Push backup exists, cloud pre-Push backup exists, `last_manual_cloud_push.txt` records selected branch, baseline prerequisites, local-save evidence, and backup evidence | Missing ARM64 evidence |
| Missing backup storage permission after branch switch | Push must remain blocked | Missing ARM64 evidence |

## Release decision language

Use this wording until the matrix is complete:

```text
Save compatibility between public and beta Steam branches is not yet proven. Pull from Cloud for the selected version and verify Android local saves exist before any Push. After switching branches, do not Push unless Pull from Cloud after the branch switch, Pull-after-switch evidence, backup storage permission, local/cloud pre-Push backup evidence, and successful Push marker evidence are present.
```

Only soften this wording after ARM64 evidence proves the relevant rows.
