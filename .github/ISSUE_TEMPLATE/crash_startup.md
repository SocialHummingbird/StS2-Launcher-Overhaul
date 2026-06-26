---
name: Crash or startup problem
about: Report an app crash, hang, black screen, NativeFallback route, or very slow startup
title: "[CRASH] "
labels: ["bug", "needs-triage", "category: reliability"]
assignees: []
---

## What happened

Describe the crash, hang, black screen, fallback screen, or slow startup.

## When it happened

- [ ] Immediately after opening the app
- [ ] During Steam login
- [ ] During game download/update
- [ ] After pressing Start Game
- [ ] After selecting a branch
- [ ] After enabling mods
- [ ] After returning from the game to the launcher

## Environment

- Device model:
- Android version:
- Vendor skin/version, for example One UI:
- Device ABI:
- APK release tag:
- APK filename:
- Package name shown by Android, if known:
- Clean install or update install:

## Timing

- App icon tap to launcher visible:
- Start Game tap to game main menu:
- Last visible screen before failure:

## Evidence

- Screenshot or screen recording:
- Focused logcat attached:
- Any generated diagnostics file attached:

Useful logcat search terms:

```text
AndroidRuntime
FATAL EXCEPTION
Godot
STS2Mobile
PatchHelper
NativeFallback
SteamKit
Workshop
Mods
```

## Safety check

- [ ] I removed Steam credentials, Steam Guard codes, tokens, and private account details from logs.
