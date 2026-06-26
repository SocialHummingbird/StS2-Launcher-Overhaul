---
name: Mods or save-merger test
about: Report Workshop/mod loading, mod selection, manual import, or Vanilla and Modded Saves Merger behavior
title: "[MODS] "
labels: ["bug", "needs-triage", "category: reliability"]
assignees: []
---

## Summary

Describe the mod loading or save-merger result.

## Test type

- [ ] Workshop sync/discovery
- [ ] Manual import
- [ ] Vanilla launch with mods disabled
- [ ] Modded launch with selected mods
- [ ] Vanilla and Modded Saves Merger
- [ ] Save compatibility

## Environment

- Device model:
- Android version:
- APK release tag:
- APK filename:
- Selected game branch:
- Clean install or update install:

## Mods

List every selected mod in launch order if known:

```text

```

For each important mod, note:

- Workshop item ID:
- Source: Workshop sync or manual import
- Files present, for example `.dll`, `.json`, `.pck`:
- Enabled or disabled:

## Save behavior

- Existing vanilla save present before test:
- Existing modded save present before test:
- Pull from Steam Cloud run before test:
- Push to Steam Cloud run during test:
- Save became visible in-game:
- Save loaded successfully:

## Result

- [ ] Game reached main menu
- [ ] Game loaded selected mods
- [ ] Game launched vanilla with zero mods
- [ ] Save merger made existing saves usable
- [ ] Save merger did not work
- [ ] App crashed or hung

## Evidence

Attach screenshots or focused logs. Useful logcat search terms:

```text
Workshop
Mods
SavesMerger
BaseLib
Quick Restart
Loaded
PatchHelper
AndroidRuntime
FATAL EXCEPTION
```

## Safety check

- [ ] I did not run Push to Cloud unless I intentionally wanted to upload the tested Android save state.
- [ ] I removed Steam credentials, tokens, and private account details from logs.
