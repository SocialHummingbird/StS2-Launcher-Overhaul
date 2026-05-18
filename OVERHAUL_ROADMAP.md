# Overhaul Roadmap

This roadmap tracks the ongoing rewrite and stabilization effort.

## Phase 0 — Baseline and migration

- [x] Create independent project with clear provenance
- [x] Baseline all branches from upstream lineage
- [x] Finish contributor process files and templates
- [x] Set branch protections / PR validation

## Phase 1 — Reliability hardening

- [x] Stabilize background/cloud async flow with timeouts and structured cancellation
- [x] Fix lifecycle flushing correctness for cloud writes
- [x] Remove thread-safety issues in parallel download/token caches
- [x] Improve reflection patch hardening for version drift

## Phase 2 — Architecture cleanup

- [x] Split optional patches from required startup path
- [x] Introduce clearer service boundaries in launcher orchestration
- [x] Reduce global/static mutable state where feasible
- [x] Improve observability for recurring failure paths

## Phase 3 — Validation and maintainability

- [x] Add runbooks for representative device matrix
- [x] Add issue templates and issue triage labels
- [x] Add recurring issue triage and status issue workflow
- [x] Publish changelog for each tagged release

## Phase 4 — Governance completion

- [x] Enforce branch protection for `main` with PR-first workflow
- [x] Establish rollback strategy branch (`compat/legacy`)
- [x] Publish release/changelog strategy and backport policy
- [x] Document branch protection expectations and deployment safety steps

## Phase 5 — CI bootstrap and merge safety

- [x] Add required-status-check workflow for governance/documentation safety
- [x] Wire branch protection to require a deterministic CI check context

## Phase 6 — CI smoke check expansion

- [x] Add optional artifact-aware build smoke job
- [x] Keep build smoke non-blocking until artifacts are standardized
- [x] Document required check expectations for both governance and build jobs

## Phase 7 — Reliability closure and workflow hardening

- [x] Close out P0–P3 backlog outcomes in the current status tracking
- [x] Keep both required check contexts active (`Governance Smoke Check`, `Build Smoke Check`) in branch protection
- [x] Normalize artifact coverage notes for environments without publish outputs
- [x] Complete phase handoff cleanup and prepare final tracking issue closeout
