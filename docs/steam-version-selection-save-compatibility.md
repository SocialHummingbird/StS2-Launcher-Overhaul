# Steam Version Selection Save Compatibility Matrix

This matrix tracks save behavior when switching between Steam game versions. Until these rows have current ARM64 evidence, save compatibility across branches remains unknown. Steam Cloud Upload remains destructive, but Pull and branch history are not prerequisites; safety comes from exact transfer context, destination backup, failure propagation, and verified read-back.

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

| Scenario | Transfer requirement | Current state |
| --- | --- | --- |
| No branch switch in current session | Selected namespace has transferable allowlisted local saves, no interrupted Pull marker exists, and the overwrite prompt is confirmed intentionally | Needs newest-public evidence |
| Public/default to beta switch | Runtime/public-beta context matches exactly; destination backup and verified read-back succeed | Missing ARM64 evidence |
| Beta to public/default switch | Runtime/public-beta context matches exactly; destination backup and verified read-back succeed | Missing ARM64 evidence |
| Vanilla to modded or changed mod set | Namespace and exact mod-set fingerprint must match; cross-context transfer fails | Missing ARM64 evidence |
| Different Steam account | Authenticated SteamID64 mismatch fails without reporting success | Missing ARM64 evidence |
| Interrupted Pull marker present | Upload remains blocked until the interrupted Pull is retried successfully | Missing ARM64 evidence |

## Release decision language

Use this wording until the matrix is complete:

```text
Save compatibility between public and beta Steam branches is not yet proven. Pull and Upload are independent operations; Pull is not an Upload prerequisite. Upload only the intended local namespace, verify the exact account/runtime/mod-set context, and keep the overwrite warning explicit until controlled ARM64 evidence proves destination backup and remote read-back verification.
```

Only soften this wording after ARM64 evidence proves the relevant rows.
