# GitHub Release Inventory

Generated from `SocialHummingbird/StS2-Launcher-Overhaul` with `scripts/audit-github-release-inventory.ps1`.

## Current Download

- Recommended newest APK release: `v0.2.336-cleartext-cdn-debug`
- APK: `StS2Launcher-v0.2.336-cleartext-cdn-debug-arm64-v8a.apk`
- SHA-256: `a9dc26899726b64d70a25ad827e80374f46bdb79618c0e6a935a7b171938650a`
- GitHub `/releases/latest` non-prerelease target: `v0.2.188-branch-cache-hardening`
- Warning: GitHub `/releases/latest` does not point at the newest APK because newer APKs are prereleases.

## Release Classes

- `current-prerelease`: newest APK release and the current recommended tester download.
- `github-latest-non-prerelease`: what GitHub marks as Latest when prereleases are excluded; may be older than the recommended APK.
- `historical-test-prerelease`: older local/debug/evidence/audit build kept for traceability.
- `historical-prerelease`: older prerelease kept for traceability.
- `historical-release`: older non-prerelease APK kept for traceability or upgrade-baseline context.

## Inventory

| Published | Release | Class | APK | Checksum | Metadata | Body APK/SHA | Issues |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 2026-06-28 | `v0.2.336-cleartext-cdn-debug` | `current-prerelease` | `StS2Launcher-v0.2.336-cleartext-cdn-debug-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-25 | `v0.2.335-mod-selector-deps-cloud-marker-debug` | `historical-test-prerelease` | `StS2Launcher-v0.2.335-mod-selector-deps-cloud-marker-debug-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-22 | `v0.2.316-workshop-runtime-mod-evidence` | `historical-test-prerelease` | `StS2Launcher-v0.2.316-workshop-runtime-mod-evidence-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-21 | `v0.2.293-local-audit-module-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.293-local-audit-module-split-arm64-v8a.apk` | yes | no | yes/yes | missing metadata |
| 2026-06-21 | `v0.2.292-local-audit-docs-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.292-local-audit-docs-split-arm64-v8a.apk` | yes | json | no/no | body missing APK; body missing SHA |
| 2026-06-21 | `v0.2.291-local-portal-governance-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.291-local-portal-governance-split-arm64-v8a.apk` | yes | json | no/no | body missing APK; body missing SHA |
| 2026-06-21 | `v0.2.290-local-evidence-collector-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.290-local-evidence-collector-split-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-21 | `v0.2.289-local-audit-orchestrator-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.289-local-audit-orchestrator-split-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-21 | `v0.2.288-local-audit-ui-support-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.288-local-audit-ui-support-split-arm64-v8a.apk` | yes | no | yes/yes | missing metadata |
| 2026-06-21 | `v0.2.287-local-audit-orchestrator-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.287-local-audit-orchestrator-split-arm64-v8a.apk` | yes | no | no/yes | missing metadata; body missing APK |
| 2026-06-21 | `v0.2.286-local-audit-ui-module-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.286-local-audit-ui-module-split-arm64-v8a.apk` | yes | no | no/yes | missing metadata; body missing APK |
| 2026-06-21 | `v0.2.285-local-audit-module-split` | `historical-test-prerelease` | `StS2Launcher-v0.2.285-local-audit-module-split-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-21 | `v0.2.284-local-branch-availability-marker-helpers` | `historical-test-prerelease` | `StS2Launcher-v0.2.284-local-branch-availability-marker-helpers-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-21 | `v0.2.283-local-cache-cleanup-marker-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.283-local-cache-cleanup-marker-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-21 | `v0.2.282-local-evidence-marker-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.282-local-evidence-marker-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-21 | `v0.2.274-local-latest-smoke` | `historical-test-prerelease` | `StS2Launcher-v0.2.274-local-latest-smoke-arm64-v8a.apk` | yes | json | no/no | body missing APK; body missing SHA |
| 2026-06-21 | `v0.2.281-local-branch-marker-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.281-local-branch-marker-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-21 | `v0.2.280-local-evidence-redaction-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.280-local-evidence-redaction-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-20 | `v0.2.279-local-audit-helper-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.279-local-audit-helper-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-20 | `v0.2.278-local-compact-label-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.278-local-compact-label-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-20 | `v0.2.277-local-helper-refactor` | `historical-test-prerelease` | `StS2Launcher-v0.2.277-local-helper-refactor-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-20 | `v0.2.276-local-refactor-audits` | `historical-test-prerelease` | `StS2Launcher-v0.2.276-local-refactor-audits-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-20 | `v0.2.275-local-mobile-ui-ux-1b6ffd1` | `historical-test-prerelease` | `StS2Launcher-v0.2.275-local-mobile-ui-ux-1b6ffd1-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-20 | `v0.2.274-local-status-docs` | `historical-test-prerelease` | `StS2Launcher-v0.2.274-local-status-docs-arm64-v8a.apk` | yes | json | yes/yes | ok |
| 2026-06-20 | `v0.2.273-local-startup-overlay-fix` | `historical-test-prerelease` | `StS2Launcher-v0.2.273-local-startup-overlay-fix-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-19 | `v0.2.188-local-runtime-beta-fix38-cache-identity` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix38-cache-identity-arm64-v8a.apk` | yes | json | no/no | body missing APK; body missing SHA |
| 2026-06-19 | `v0.2.188-local-runtime-beta-fix30-public-after-beta` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix30-public-after-beta-arm64-v8a.apk` | yes | json | no/yes | body missing APK |
| 2026-06-18 | `v0.2.188-local-runtime-beta-fix27` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix27-arm64-v8a.apk` | yes | json | no/no | body missing APK; body missing SHA |
| 2026-06-18 | `v0.2.188-local-runtime-beta-fix23` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix23-arm64-v8a.apk` | yes | json | no/no | body missing APK; body missing SHA |
| 2026-06-18 | `v0.2.188-local-runtime-beta-fix21` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix21-arm64-v8a.apk` | no | no | no/yes | missing checksum; missing metadata; body missing APK |
| 2026-06-18 | `v0.2.188-local-runtime-beta-fix20` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix20-arm64-v8a.apk` | no | no | no/yes | missing checksum; missing metadata; body missing APK |
| 2026-06-18 | `v0.2.188-local-runtime-beta-fix16` | `historical-test-prerelease` | `StS2Launcher-v0.2.188-local-runtime-beta-fix16-arm64-v8a.apk` | yes | no | yes/yes | missing metadata |
| 2026-06-16 | `v0.2.188-branch-cache-hardening` | `github-latest-non-prerelease` | `StS2Launcher-v0.2.188-branch-cache-hardening-arm64-v8a.apk` | yes | build-info | yes/yes | GitHub latest is not newest APK |
| 2026-06-14 | `v0.2.187-beta-art-fallback` | `historical-release` | `StS2Launcher-v0.2.187-beta-art-fallback-arm64-v8a.apk` | yes | build-info | no/no | body missing APK; body missing SHA |
| 2026-06-13 | `v0.2.186-sts2-mobile-version-selection` | `historical-release` | `StS2Launcher-v0.2.186-sts2-mobile-version-selection-arm64-v8a.apk` | yes | build-info | no/no | body missing APK; body missing SHA |
| 2026-06-09 | `v0.2.185-responsive-ui` | `historical-release` | `StS2Launcher-v0.2.185-responsive-ui-arm64-v8a.apk` | yes | build-info | yes/yes | ok |
| 2026-06-09 | `v0.2.184-loading-scale` | `historical-release` | `StS2Launcher-v0.2.184-loading-scale-arm64-v8a.apk` | yes | build-info | no/no | body missing APK; body missing SHA |
| 2026-06-09 | `v0.2.183-login-hardening` | `historical-release` | `StS2Launcher-v0.2.183-login-hardening-arm64-v8a.apk` | yes | build-info | no/no | body missing APK; body missing SHA |
| 2026-06-09 | `v0.2.182-launcher-scroll-widthfix` | `historical-release` | `StS2Launcher-v0.2.182-launcher-scroll-widthfix-arm64-v8a.apk` | yes | build-info | no/no | body missing APK; body missing SHA |
| 2026-06-09 | `v0.2.181-launcher-layoutfix` | `historical-release` | `StS2Launcher-v0.2.181-launcher-layoutfix-arm64-v8a.apk` | yes | build-info | no/no | body missing APK; body missing SHA |

## Required Hygiene For New Releases

- Use explicit fork repo arguments: `--repo SocialHummingbird/StS2-Launcher-Overhaul`.
- Attach exactly named APK assets, for example `StS2Launcher-v<version>-arm64-v8a.apk`.
- Attach a sha256sum-compatible sidecar named `<apk>.sha256` containing the APK filename, not a runner absolute path.
- Attach metadata as `<apk>.json` for local builds or `<apk>.build-info.txt` for GitHub Actions builds.
- Include APK name, SHA-256, package name, versionName, versionCode, signing channel, validation, and limitations in the release body.
- Run `scripts/check-github-release-hygiene.ps1` before public announcements.
