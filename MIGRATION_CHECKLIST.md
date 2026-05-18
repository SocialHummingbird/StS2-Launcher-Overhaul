# Migration Checklist (Implemented Baseline)

This repo started as an independent copy of [Ekyso/StS2-Launcher](https://github.com/Ekyso/StS2-Launcher) with the goal of an architectural overhaul.

## 1) Repository Setup (Done)

- [x] Created independent repository: [SocialHummingbird/StS2-Launcher-Overhaul](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul)
- [x] Kept full code history in a clean copy
- [x] Added explicit project note in [README](README.md) describing the independent copy + overhaul objective
- [x] Set remotes:
  - `origin` => `https://github.com/SocialHummingbird/StS2-Launcher-Overhaul.git`
  - `upstream` => `https://github.com/Ekyso/StS2-Launcher.git`

## 2) Branching Strategy (To Do)

- [x] Define branch naming conventions (suggested: `rewrite/<area>-<topic>`, `fix/<area>`, `chore/<topic>`)
- [ ] Protect `main` in GitHub settings (direct pushes disabled, PR-first workflow)
- [ ] Add branch protection rules for required status checks once CI exists
- [ ] Keep a `compat/legacy` branch for rollback if needed

## 3) Contributor & PR Workflow (To Do)

- [x] Add contributor process docs (see [CONTRIBUTING.md](CONTRIBUTING.md))
- [x] Add `PULL_REQUEST_TEMPLATE.md` with:
  - scope + validation checklist
  - test plan + reproducibility notes
  - explicit risk/rollback note
- [x] Add issue templates for:
  - bug reports (device/log evidence required)
  - feature proposals (scope + acceptance criteria)
- [x] Add issue labels for severity and priority tracking

## 4) Overhaul Tracking (To Do)

- [x] Add a dedicated roadmap file for active overhaul initiatives
- [x] Track high-impact reliability fixes from prior review separately from feature work
- [x] Publish a recurring "overhaul status" issue with current focus and blockers
- [ ] Create and pin a recurring status issue template/guidance in the repo for monthly updates

## 5) Validation Flow (To Do)

- [x] Add explicit smoke-test matrix for:
  - locale startup path
  - cloud sync push/pull
  - pause/resume and quit lifecycle
  - depot download + update flow
- [x] Add device log checklist for crash-path validation
- [ ] Require manual verification step in each PR where full test automation is not possible

## 6) Tagging & Discovery (Done)

- [x] Added repository topics:
  - `android`
  - `csharp`
  - `game`
  - `godot`
  - `harmony`
  - `mod-loader`
  - `mobile`
  - `overhaul`
  - `slay-the-spire`
  - `slay-the-spire-2`
  - `steam`
  - `sts2-launcher`

## 6a) Long-Term Maintenance (To Do)

- [ ] Add release notes / changelog strategy (milestones + tag-based release notes)
- [ ] Define backport policy from upstream fixes:
  - cherry-pick when compatible
  - rewrite/replace when architecture changed
