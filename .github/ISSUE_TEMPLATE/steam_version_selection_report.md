---
name: Steam version selection test report
about: Report validation results for public/beta Steam branch selection, branch caches, or save compatibility
title: "[STEAM VERSION] "
labels: ["needs-triage", "category: reliability"]
assignees: []
---

## Summary

Describe what you tested and the result.

## Artifact hygiene

Do not attach Steam credentials, refresh tokens, shared preferences, or unsanitized logs that expose account names, device identifiers, or local user paths. Redact identifying data before sharing publicly.

## Environment

- Device model:
- Android version:
- Device ABI:
- App version:
- Release tag / APK asset:
- Package name:
- Clean install or update:
- Steam account owns Slay the Spire 2:

## Selected game version

- [ ] Default/public
- [ ] Beta
- [ ] Switched from default/public to beta during this test
- [ ] Switched from beta back to default/public during this test

If beta was selected:

- Did the beta branch appear downloadable?
- Did Steam report the branch as missing, private, inaccessible, or password-protected?
- Was a beta password expected for this branch?

## Download and startup result

- [ ] Download/update check completed
- [ ] Game files downloaded
- [ ] Game launched
- [ ] Game did not launch
- [ ] Launcher showed native fallback diagnostics
- [ ] Launcher requested redownload/rebuild-cache for the selected version

Observed selected game directory if shown in diagnostics:

```text

```

Observed selected PCK path if shown in diagnostics:

```text

```

## Branch marker evidence

Attach launcher diagnostics if possible. Paste the relevant `steam_branch.txt` or diagnostics lines here.

```text
Selected game branch:
Selected game version:
Selected game version note:
Selected game version slot kind:
Selected game version slot directory:
Selected game directory:
Selected game PCK path:
Selected game files ready:
Selected readiness problem:
Selected game branch marker path:
Selected game branch marker present:
Selected game branch marker branch:
Selected game branch marker install slot kind:
Selected game branch marker install slot directory:
Selected game branch marker expected install slot kind:
Selected game branch marker expected install slot directory:
Selected game branch marker has matching install slot provenance:
Selected game branch marker has depot manifests:
Selected game branch marker depot manifest entries:
Selected game branch marker ready:
Current selected branch for version marker comparison:
Game version redownload marker filename:
Game version redownload marker path:
Game version redownload marker present:
Game version redownload marker UTC:
Game version redownload marker UTC parseable:
Game version redownload marker selected branch:
Game version redownload marker matches selected branch:
Game version redownload marker selected version:
Game version redownload marker selected version slot kind:
Game version redownload marker selected version slot directory:
Game version redownload marker game directory:
Game version redownload marker game directory existed before delete:
Game version redownload marker game directory exists after delete:
Game version redownload marker download state directory:
Game version redownload marker download state directory existed before delete:
Game version redownload marker download state directory exists after delete:
Game version redownload marker selected directories cleared:
Branch switch marker filename:
Branch switch marker path:
Branch switch marker UTC:
Branch switch marker UTC parseable:
Branch switch previous branch:
Branch switch selected branch:
Branch switch selected version:
Branch switch selected version slot kind:
Branch switch selected version slot directory:
Branch switch selected branch matches current selected branch:
Branch switch selected branch note:
Branch switch local backup forced:
Branch switch manual Push requires backup storage:
Branch switch warning acknowledged:
Branch switch non-public warning acknowledged:
Branch switch marker has required safety evidence:
Branch switch marker has required safety evidence for selected branch:
Manual Pull evidence marker filename:
Manual Pull evidence marker path:
Manual Pull evidence marker present:
Manual Pull evidence UTC:
Manual Pull evidence UTC parseable:
Manual Pull evidence selected branch:
Manual Pull evidence selected version:
Manual Pull evidence selected version slot kind:
Manual Pull evidence selected version slot directory:
Manual Pull completion flag recorded:
Manual Pull evidence is after branch switch:
Manual Pull evidence matches selected branch:
Manual Pull completed after branch switch for selected version:
Manual Push evidence marker filename:
Manual Push evidence marker path:
Manual Push evidence marker present:
Latest manual Push evidence outcome:
Latest manual Push evidence UTC:
Latest manual Push evidence selected branch:
Latest manual Push evidence selected version:
Latest manual Push evidence selected version slot kind:
Latest manual Push evidence selected version slot directory:
Latest manual Push evidence reason:
Manual Push evidence UTC:
Manual Push evidence UTC parseable:
Manual Push evidence selected branch:
Manual Push evidence selected version:
Manual Push evidence selected version slot kind:
Manual Push evidence selected version slot directory:
Manual Push evidence recorded local backup count:
Manual Push evidence recorded cloud backup count:
Manual Push evidence recorded latest local backup UTC:
Manual Push evidence recorded latest cloud backup UTC:
Manual Push completion flag recorded:
Manual Push evidence is after branch switch:
Manual Push evidence matches selected branch:
Manual Push evidence recorded pre-Push backup evidence satisfied:
Manual Push completed after branch switch for selected version with backup evidence:
Manual Push blocked evidence marker filename:
Manual Push blocked evidence marker path:
Manual Push blocked evidence marker present:
Manual Push blocked evidence UTC:
Manual Push blocked evidence UTC parseable:
Manual Push blocked evidence selected branch:
Manual Push blocked evidence selected version:
Manual Push blocked evidence selected version slot kind:
Manual Push blocked evidence selected version slot directory:
Manual Push blocked evidence matches selected branch:
Manual Push blocked evidence recorded prerequisites satisfied:
Manual Push blocked evidence recorded local backup count:
Manual Push blocked evidence recorded cloud backup count:
Manual Push blocked evidence recorded latest local backup UTC:
Manual Push blocked evidence recorded latest cloud backup UTC:
Manual Push blocked evidence recorded pre-Push backup evidence satisfied:
Manual Push blocked evidence reason:
Manual Push blocked before upload evidence recorded:
Important Android local save evidence count in bounded scan:
Important Android local save evidence present:
Branch-switch manual Push prerequisites satisfied:
Pre-Push local backup evidence count:
Pre-Push cloud backup evidence count:
Latest pre-Push local backup UTC:
Latest pre-Push cloud backup UTC:
Pre-Push local backup evidence after branch switch:
Pre-Push cloud backup evidence after branch switch:
Branch-switch pre-Push backup evidence satisfied:
```

## Cached version behavior

- [ ] Default/public cache stayed usable
- [ ] Beta cache stayed usable
- [ ] Inactive cache was preserved after switching versions
- [ ] `REDOWNLOAD SELECTED VERSION` only cleared the selected version
- [ ] `CLEAR CACHED VERSIONS` only cleared inactive non-public caches

Paste cached-version diagnostics if available:

```text
Game version cache cleanup marker filename:
Game version cache cleanup marker path:
Game version cache cleanup marker present:
Game version cache cleanup marker UTC:
Game version cache cleanup marker UTC parseable:
Game version cache cleanup marker selected branch:
Game version cache cleanup marker matches selected branch:
Game version cache cleanup marker selected version:
Game version cache cleanup marker selected version slot kind:
Game version cache cleanup marker selected version slot directory:
Game version cache cleanup marker game_versions present:
Game version cache cleanup marker removed count:
Game version cache cleanup marker selected cache preserved where applicable:
```

If a branch switch occurred, paste relevant `last_game_branch_switch.txt` lines:

```text
Previous branch:
Selected branch:
Selected version:
Selected version slot kind:
Selected version slot directory:
Selected branch note:
Local backup forced on:
Manual Push requires backup storage:
```

If Pull from Cloud was run after a branch switch, paste relevant `last_manual_cloud_pull.txt` lines:

```text
UTC:
Selected branch:
Selected version:
Selected version slot kind:
Selected version slot directory:
Selected branch note:
Manual Pull completed before branch-switch Push:
```

## Save compatibility and cloud safety

Do not Push to Steam Cloud unless you intentionally tested overwrite behavior.

- [ ] Pull from Cloud was run before any Push attempt
- [ ] Pull from Cloud was run after the branch switch for the selected version
- [ ] Android local saves existed before any Push attempt
- [ ] Local backup was enabled before switching versions
- [ ] Backup storage permission was available before Push
- [ ] Local pre-Push backup evidence exists
- [ ] Cloud pre-Push backup evidence exists
- [ ] Manual Push was not tested
- [ ] Manual Push was tested intentionally

Save compatibility observations between selected versions:

```text

```

## Logs / attachments

- [ ] Launcher diagnostics attached
- [ ] `adb logcat` attached
- [ ] Screenshot attached
- [ ] Branch marker file attached or pasted
- [ ] Backup evidence attached if Push was tested

If native startup or fallback diagnostics appeared, paste relevant lines:

```text
Selected Steam branch:
Selected Steam branch note before routing:
Selected Steam branch note:
Resolved startup game directory:
Steam branch marker ready:
Steam branch marker depot manifest entries:
```

## Additional notes

Anything else that may affect branch availability, Steam login/session state, save state, or startup behavior.
