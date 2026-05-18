# Overhaul Roadmap

This roadmap tracks the ongoing rewrite and stabilization effort.

## Phase 0 — Baseline and migration

- [x] Create independent project with clear provenance
- [x] Baseline all branches from upstream lineage
- [ ] Finish contributor process files and templates
- [ ] Set branch protections / PR validation

## Phase 1 — Reliability hardening

- [x] Stabilize background/cloud async flow with timeouts and structured cancellation
- [x] Fix lifecycle flushing correctness for cloud writes
- [x] Remove thread-safety issues in parallel download/token caches
- [x] Improve reflection patch hardening for version drift

## Phase 2 — Architecture cleanup

- [ ] Split optional patches from required startup path
- [ ] Introduce clearer service boundaries in launcher orchestration
- [ ] Reduce global/static mutable state where feasible
- [ ] Improve observability for recurring failure paths

## Phase 3 — Validation and maintainability

- [ ] Add runbooks for representative device matrix
- [x] Add issue templates and issue triage labels
- [ ] Add recurring issue triage and status issue workflow
- [x] Publish changelog for each tagged release
