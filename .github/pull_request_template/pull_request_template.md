## Summary

- What changed:
- Why now:
- Related issue(s):

## Validation

- [ ] Build/check command run:
- [ ] Steam version-selection static audit run when branch/version behavior, startup routing, diagnostics, cache cleanup, or cloud-save safety is touched:
- [ ] Steam branch guidance parity audit run when selector warning text or native branch diagnostics are touched:
- [ ] Release/APK asset checked when relevant:
- [ ] Manual verification:
  - Device(s):
  - Steps:
  - Result:
- [ ] Log evidence attached (if relevant):
- [ ] No unrelated files changed
- [ ] Device log checklist attached where relevant

## Risk

- [ ] Low: change is isolated and reversible
- [ ] Medium: change affects existing behavior in a narrow path
- [ ] High: change may impact startup/path-critical code
- [ ] Steam version-selection risk is called out when touched: selected branch persistence, branch-aware manifests/downloads, `steam_branch.txt` marker provenance, selected-version redownload, inactive-cache cleanup, startup routing, save compatibility, Pull-after-switch evidence, local-save evidence, and backup safety
- [ ] Rollback plan:

## Changelog

- [ ] CHANGELOG.md updated (for user-facing or reliability changes)

## Notes for reviewers

Mention any temporary workarounds, Steam startup/login risk, Steam version-selection risk, and planned follow-up cleanup.
