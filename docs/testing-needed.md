# Testing needed

Updated: 2026-08-22

Use the smallest test that exercises the affected behavior. Do not turn desktop, fake-service, source inspection, or one-device evidence into a broader claim.

## Required local checks

```powershell
dotnet build src\STS2Mobile\STS2Mobile.csproj -c Release
.\scripts\test-local-gameplay-save-safety.ps1
dotnet run --project tests\STS2Mobile.GameIdentityTests\STS2Mobile.GameIdentityTests.csproj -c Release
.\android\gradlew.bat testReleaseUnitTest
```

Run `scripts/test-launcher-ui-preview.ps1` with the explicit importer, base-game PCK, and managed-runtime paths in [Focused development commands](steam-version-selection-tooling.md#launcher-navigation-and-mod-activation) when launcher or mod behavior changes.

The local suites cover:

- Local path containment, atomic saves, deterministic synchronization decisions, Pull/Push interruption behavior, and shared automatic/manual synchronization.
- Persisted mod/version selection and focused launcher interaction.
- Authoritative PCK/DLL identity and generation-bound PCK caching.
- Transactional branch N → N+1 updates, interruptions, stale-evidence rejection, and branch isolation.
- Runtime-pack candidate validation, atomic promotion/rollback, and active-cache gating.
- Selected-branch recovery preserving saves, credentials, and sibling branches.
- Attempt-bound launcher handoff under reordered, duplicated, background/resume, lock/unlock, timeout, and late-callback events.

The save suite uses one deterministic `FakeSaveRemote`; it does not prove Steam transport. Desktop mod fixtures do not prove Android activation or an in-game effect.

## Published RC4 device evidence

The exact [v0.2.429 RC4](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4) APK passed 10/10 launches on Samsung `SM-F971B`, Android 17 / API 37: four cold, four warm, one background/resume, and one lock/unlock. Each attempt validated identity/runtime-pack/patch compatibility and completed the active handoff once with no launcher overlay left visible. Saves, credentials, Steam Cloud inventory, and unrelated branch/runtime data were unchanged.

That result does not prove:

- The original Pixel 9 or Xiaomi 17 Ultra reporter flows; confirmation is still pending.
- Every Android version, device, GPU/driver, orientation, or accessibility configuration.
- Every Steam branch or interruption point on physical storage.
- Real Steam authentication or save enumerate/download/upload/commit/read-back on RC4.
- Android mod activation or a visible importer effect on RC4.

Record exact APK, commit, package/version/signer/hash, device, Android/API, ABI, selected branch, renderer, test actions, and failures for every future device result.
