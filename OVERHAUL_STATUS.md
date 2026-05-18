# OVERHAUL_STATUS.md

This file tracks what we are actively working on in the overhaul branch.

## Current Focus (Phase 2 Architecture Cleanup)
- Isolate required startup patches from optional ones in one-shot orchestration
- Reduce startup-time fragility by preventing one noncritical patch from disabling launcher fallback
- Improve startup observability with grouped patch outcome summaries
- Continue reducing global mutable state in startup control flow

## High-Impact Reliability Backlog

| Priority | Area | Issue | Category | Target |
| --- | --- | --- | --- | --- |
| P0 | Startup crash paths | Locale parsing + patch compatibility | Reliability | Must-have |
| P1 | Cloud sync path | Timeout handling for slow or stalled reads/writes | Reliability | High |
| P2 | Downloader | Duplicate download/write race conditions under resume/retry | Reliability | Medium |
| P3 | Multiplayer | LAN beacon persistence and discovery stability | Reliability | Low |

## Open Follow-up Tasks
- Add per-device reproducibility logs for pending issue paths
- Publish a lightweight recurring status issue (monthly) with blockers and acceptance criteria
- Add branch protection and release gating once CI is in place
- Complete Phase 2 backlog items and publish next runbook in phase-3 planning

## Active Status Issue
- https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/2

## Rollback Strategy
- Keep each PR scoped to one logical change so labels and patches can be reverted independently.
- For platform/game-API fixes, preserve previous behavior behind compatibility guards where practical.
