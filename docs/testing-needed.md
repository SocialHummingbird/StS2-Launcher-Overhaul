# Testing needed

Updated: 2026-08-09

No Android device is available for the current reduction. The immediate test plan is therefore limited to evidence that can run locally and deterministically.

## Required now

- Build `src/STS2Mobile/STS2Mobile.csproj` in Release configuration.
- Run `scripts/test-local-gameplay-save-safety.ps1`.
- Run `scripts/test-launcher-ui-preview.ps1`.

The save suite is deliberately limited to:

- Local path containment and atomic writes.
- No change, Pull, Push, and conflict decisions.
- An interrupted Pull leaving local saves intact.
- A failed Push remaining dirty and retryable.
- Pull completing before saves are loaded.
- A local gameplay save queuing Push without reopening the launcher.
- Manual Push and Pull using the same synchronization service.

The launcher check covers basic destination navigation and action wiring.

## Not provable now

- Steam Cloud RPC or HTTP transport.
- Android filesystem or lifecycle behavior.
- Android launcher rendering and input.
- Real Steam authentication and depot download.
- Game startup or gameplay.

These are limitations, not reasons to rebuild the removed feature machinery.

The synchronization suite uses one small fake remote so policy and failure behavior stay deterministic. That fake does not prove Steam transport or Android transport. Do not report desktop or fake-backed results as device or live-service proof.
