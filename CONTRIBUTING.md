# Contributing

Thank you for contributing to this overhaul project.

This repository is an independent continuation of the original launcher with a strong focus on reliability and architecture changes.

## Branching

- `main` is the stable base branch for the overhaul.
- Use short-lived feature branches:
  - `rewrite/<area>-<topic>`
  - `fix/<area>`
  - `chore/<topic>`
  - `refactor/<topic>`

## Workflow

1. Create or grab an issue for your work.
2. Implement one small, reviewable change per PR.
3. Add/update notes, migration rationale, and test notes in the PR body.
4. Open a PR marked as draft until you can verify expected behavior.
5. Move to Ready when validation is complete.

## What PRs should include

- Scope-limited changes (small surface area)
- Repro steps (when fixing bugs)
- Validation data (manual logs, device/test case, or build output)
- Any compatibility assumptions or known limitations
- Rollback plan for risky refactors

## Validation priorities

- Prefer local behavior checks on target devices.
- Include log snippets for startup/crash/lifecycle paths when relevant.
- If build/test setup is incomplete, explain exact blockers in PR notes.

## Communication

- Keep discussion explicit: if behavior changed, state why and what risk remains.
- If a fix is a workaround, label it clearly and include a follow-up task to remove it when feasible.
