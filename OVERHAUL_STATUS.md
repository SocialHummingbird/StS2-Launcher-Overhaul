# OVERHAUL_STATUS.md

This file tracks what we are actively working on in the overhaul branch.

## Current Focus (Phase 3 Validation & Maintainability)
- Add reproducible device validation runbooks for representative hardware classes
- Run monthly stability check-ins via recurring status issue workflow
- Keep PR validation consistent when automation cannot provide full coverage
- Track blockers and acceptance criteria for unresolved high-impact issues

## High-Impact Reliability Backlog

| Priority | Area | Issue | Category | Target |
| --- | --- | --- | --- | --- |
| P0 | Startup crash paths | Locale parsing + patch compatibility | Reliability | Must-have |
| P1 | Cloud sync path | Timeout handling for slow or stalled reads/writes | Reliability | High |
| P2 | Downloader | Duplicate download/write race conditions under resume/retry | Reliability | Medium |
| P3 | Multiplayer | LAN beacon persistence and discovery stability | Reliability | Low |

## Open Follow-up Tasks
- Publish/run recurring status issue check-ins using `overhaul_status` template
- Add branch protection and release gating once CI is in place
- Complete remaining compatibility hardening and close remaining phase-2 backlog items

## Active Status Issue
- https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/2

## Rollback Strategy
- Keep each PR scoped to one logical change so labels and patches can be reverted independently.
- For platform/game-API fixes, preserve previous behavior behind compatibility guards where practical.
