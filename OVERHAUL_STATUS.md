# OVERHAUL_STATUS.md

This file tracks what we are actively working on in the overhaul branch.

## Current Focus (Phase 5 CI Bootstrapping)
- Add a minimal required status check workflow for PR merge safety
- Align branch protection on `main` with the new CI context
- Keep compatibility backlog visible while CI matures beyond governance checks
- Keep monthly status checks visible and action-oriented

## High-Impact Reliability Backlog

| Priority | Area | Issue | Category | Target |
| --- | --- | --- | --- | --- |
| P0 | Startup crash paths | Locale parsing + patch compatibility | Reliability | Must-have |
| P1 | Cloud sync path | Timeout handling for slow or stalled reads/writes | Reliability | High |
| P2 | Downloader | Duplicate download/write race conditions under resume/retry | Reliability | Medium |
| P3 | Multiplayer | LAN beacon persistence and discovery stability | Reliability | Low |

## Open Follow-up Tasks
- Keep status issue cadence alive and close issue #2 on phase transition
- Expand CI checks from governance-only to include build smoke targets when references are available
- Complete remaining compatibility hardening and close remaining phase-2 backlog items

## Active Status Issue
- https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/2

## Rollback Strategy
- Keep each PR scoped to one logical change so labels and patches can be reverted independently.
- For platform/game-API fixes, preserve previous behavior behind compatibility guards where practical.
