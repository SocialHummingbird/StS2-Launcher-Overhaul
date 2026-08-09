# Changelog

## 2026-08-09 - Save-sync restoration and reduction (unreleased)

- Replaced the former coordinator/cache/recovery/evidence stack with one Steam Cloud transport, one manifest-based synchronization service, and one atomic four-field sync-state document.
- Added automatic Pull before the first settings/profile read and automatic verified Push after committed gameplay save changes, while keeping local writes authoritative and launch fail-open.
- Restored the Saves page actions `Sync now`, `Pull from Steam`, and `Push to Steam`; automatic and manual operations use the same service and conflict policy.
- Retained Steam authentication, owned-game and Workshop download, unconditional launch, the existing package identity, and the app-private save root.
- Removed recovery bundles, backup/export/snapshot/quarantine surfaces, evidence collectors, architecture matrices, static-audit families, and release-governance scaffolding.
- Redesigned the launcher around Home, Saves, Versions, Mods, and Help, with one focused navigation/action test.
- Built and structurally inspected the ARM64 `0.2.421-auto-sync-unverified` candidate. Offline tests pass, but no Android device was available and real Steam enumerate/download/upload/commit/read-back remains unproven.
- Kept the downloader's existing idle-timeout suspension without restoring the abandoned Cloud-specific timer workaround.

Older implementation history remains available in Git commits and published GitHub releases.
