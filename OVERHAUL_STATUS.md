# OVERHAUL_STATUS.md

This file tracks what we are actively working on in the overhaul branch.

## Current Focus (Reliability Backlog Continuation)
- Keep phase 6 CI checks enforced and documented
- Land `P0` startup crash-path hardening (`codex/p0-startup-crash-hardening`) in PR review
- Next after merge: `P3` multiplayer LAN discovery/broadcast stability (current open gap)
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
- Add actual publish artifacts to CI pipeline for full build validation coverage
- Complete remaining compatibility hardening and close remaining phase-2 backlog items

## Active Status Issue
- https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/2

## Rollback Strategy
- Keep each PR scoped to one logical change so labels and patches can be reverted independently.
- For platform/game-API fixes, preserve previous behavior behind compatibility guards where practical.
