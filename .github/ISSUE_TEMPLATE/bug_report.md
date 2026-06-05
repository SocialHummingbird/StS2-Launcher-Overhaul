---
name: Bug report
about: Report a reproducible problem with startup, Steam login, cloud sync, downloads, or gameplay flow
title: "[BUG] "
labels: ["bug", "needs-triage", "category: reliability"]
assignees: []
---

## What happened

Describe the issue clearly.

## Repro steps

1.
2.
3.

## Expected behavior

Describe what should happen.

## Actual behavior

Describe what happened instead.

## Environment

- Device model:
- Android version:
- Device ABI:
- Locale/Region settings:
- App version:
- Release tag / APK asset:
- Branch/commit:
- Clean install or update:

## Area

- [ ] Startup
- [ ] Steam login/authentication
- [ ] Game download
- [ ] Cloud sync
- [ ] Game launch/runtime
- [ ] Android x86 fallback/diagnostics

## Priority / severity (pick one)

- [ ] severity: low
- [ ] severity: high
- [ ] severity: critical
- [ ] priority: p0
- [ ] priority: p1
- [ ] priority: p2
- [ ] priority: p3

## Logs / evidence

- Attach output from `adb logcat` (ideally with the crash window)
- Include lines for `PatchHelper`, `Steam`, `SteamKit`, `Cloud`, and key exceptions where relevant
- If relevant, include screenshot of black screen / launcher overlay
- Link to any repro save / device profile

## Attachments required

- [ ] Device log checklist completed (see `docs/device-log-checklist.md`)
- [ ] Expected workaround attempted
