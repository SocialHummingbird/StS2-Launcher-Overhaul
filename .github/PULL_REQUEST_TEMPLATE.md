## Summary

- What changed:
- Why:
- Related issue/PR:

## Validation

- [ ] Build/check command run:
- [ ] Steam version-selection static audit run when branch/version behavior, startup routing, diagnostics, cache cleanup, or cloud-save safety is touched:
- [ ] Steam branch guidance parity audit run when selector warning text or native branch diagnostics are touched:
- [ ] Manual test steps and device(s):
- [ ] Release/APK asset checked when relevant:
- [ ] Logs attached:
- [ ] No unrelated behavior changes

## Review checklist

- [ ] Code changes are scoped to one purpose
- [ ] Error paths return useful diagnostics
- [ ] Steam login/startup risk is called out when touched
- [ ] Steam version-selection risk is called out when touched: selected branch persistence, branch-aware manifests/downloads, `steam_branch.txt` marker provenance, selected-version redownload, inactive-cache cleanup, startup routing, save compatibility, Pull-after-switch evidence, local-save evidence, and backup safety
- [ ] Any workaround behavior is documented
