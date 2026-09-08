# Branch audit — 2026-09-08

All non-main local and origin branches were reviewed before release v0.2.432. No missing product fix required a cherry-pick. Each original tip is preserved by the archive tag below before its branch is pruned. Upstream refs are untouched. The two clean external worktree directories are retained at detached original commits.

Rationale for branches with unique history:

- Stage 5 has a byte-identical tree to merged main ancestor d8039b5 (#39). Bootstrap differs only in ten older audit/evidence scripts; its runtime changes are integrated.
- P0 locale and ModelDb fallback fixes already exist in PlatformPatches.Locale.cs and ModelDbInitPatch.Access.cs.
- PR 49 font handling is registered in the current orchestrator; its older mod reflection path is superseded by explicit runtime Loaded-state validation.
- Phase 8 governance changes affect a workflow deliberately removed by reduction commit 62ccbc1.
- Cloud RPC idle suspension is covered by SteamCloudTransport.RunOperationAsync: every operation calls BeginOperation before its RPCs and EndOperation in finally. Both delegate to the connection idle-suspension methods.

| Original ref | Original tip | Archive tag | Decision |
| --- | --- | --- | --- |
| refs/heads/codex/android-release-bootstrap | 1f240ab697a46a0247ade3c8b9ebf7a130c5a835 | archive/2026-09-08/local/codex/android-release-bootstrap | Reviewed: integrated or superseded (see rationale) |
| refs/heads/codex/android-release-pipeline | 2fb19b3033e19bc17de9373a99860af20f419eec | archive/2026-09-08/local/codex/android-release-pipeline | Patch-equivalent work integrated |
| refs/heads/codex/bind-unsigned-provenance | 6d49d302e698ee1f40e29fdc20775d516e304666 | archive/2026-09-08/local/codex/bind-unsigned-provenance | Patch-equivalent work integrated |
| refs/heads/codex/complete-gradle-verification | 19c0f953e7fdd64d6081c4af0028fc84b5e3f94e | archive/2026-09-08/local/codex/complete-gradle-verification | Already merged |
| refs/heads/codex/docs-loading-scale-main | 2a365353e5d8363420fea46c56fc3604fa22c886 | archive/2026-09-08/local/codex/docs-loading-scale-main | Patch-equivalent work integrated |
| refs/heads/codex/fix-candidate-shell | 6eb0a0ee38c0541f29e7442b97a7f362fd85ed93 | archive/2026-09-08/local/codex/fix-candidate-shell | Patch-equivalent work integrated |
| refs/heads/codex/fix-steam-cloud-job-timeout | 71bf02cfcc0ac15757d42f5b0ca3186886b069e0 | archive/2026-09-08/local/codex/fix-steam-cloud-job-timeout | Patch-equivalent work integrated |
| refs/heads/codex/issue-37-fmod-update-loop | 890f9d9fe43713713c6b32c547591a8818d8ba09 | archive/2026-09-08/local/codex/issue-37-fmod-update-loop | Already merged |
| refs/heads/codex/issue-38-runtime-identity | 97914975d987307c6081c6aceace1e577a9696f1 | archive/2026-09-08/local/codex/issue-38-runtime-identity | Already merged |
| refs/heads/codex/migration-baseline-implementation | 89ee9a650f375418b51f733888144831d67fe255 | archive/2026-09-08/local/codex/migration-baseline-implementation | Already merged |
| refs/heads/codex/overhaul-phase1-reliability | c0f57b76f4b4853523e591069ddf2da0c8b2be2b | archive/2026-09-08/local/codex/overhaul-phase1-reliability | Already merged |
| refs/heads/codex/overhaul-phase3-runbooks | 7d8e446db3a79a88aec141cab1330b7f051b9235 | archive/2026-09-08/local/codex/overhaul-phase3-runbooks | Already merged |
| refs/heads/codex/p0-startup-crash-hardening | 69dfe04d898c94c1303269316b4d12fc0f3e3feb | archive/2026-09-08/local/codex/p0-startup-crash-hardening | Reviewed: integrated or superseded (see rationale) |
| refs/heads/codex/p3-lan-stability | e264bb61df7e73e7cba9652c849f276486fa20be | archive/2026-09-08/local/codex/p3-lan-stability | Patch-equivalent work integrated |
| refs/heads/codex/phase6-ci-smoke-build | 903b464b10f025507856bec3e4fb83dffb6b3c3b | archive/2026-09-08/local/codex/phase6-ci-smoke-build | Already merged |
| refs/heads/codex/phase7-closure | 279aa5efc923ec7fd1330b515317b197434ba978 | archive/2026-09-08/local/codex/phase7-closure | Patch-equivalent work integrated |
| refs/heads/codex/phase8-ci-artifact-coverage | a15da3a32cd5dcf2a971a9490de50480b17ec3f2 | archive/2026-09-08/local/codex/phase8-ci-artifact-coverage | Reviewed: integrated or superseded (see rationale) |
| refs/heads/codex/phase8-handoff-closeout | 0ccefe07f971689e04106c37837fada4859389e9 | archive/2026-09-08/local/codex/phase8-handoff-closeout | Patch-equivalent work integrated |
| refs/heads/codex/reduction-baseline | 38ddcaa388a8838b0bffd2357f4d0feb5eec20ef | archive/2026-09-08/local/codex/reduction-baseline | Already merged |
| refs/heads/codex/reliability-p2-downloader-race | 5753d88c3b53f01c2e970c3cf783bcd86f58b2fd | archive/2026-09-08/local/codex/reliability-p2-downloader-race | Already merged |
| refs/heads/codex/resolve-release-gfi | 8f17b62264bd3d4175024ea465389d921f72323d | archive/2026-09-08/local/codex/resolve-release-gfi | Patch-equivalent work integrated |
| refs/heads/codex/stage-next-reliability | 0b329aba98db8b1708731ff5d6e9df9eddfe034b | archive/2026-09-08/local/codex/stage-next-reliability | Already merged |
| refs/heads/codex/stage5-release-main | edaeabb78dc8363ee95d9cd0c701f98ef9f45193 | archive/2026-09-08/local/codex/stage5-release-main | Reviewed: integrated or superseded (see rationale) |
| refs/heads/codex/suspend-idle-during-cloud-rpc | 40c60c44cc39b97d52cde4401b4863ecac99dd0d | archive/2026-09-08/local/codex/suspend-idle-during-cloud-rpc | Reviewed: integrated or superseded (see rationale) |
| refs/heads/codex/tester-onboarding-main | 8f528aa2c5d40536bef7cf2075d56dd63fe2577b | archive/2026-09-08/local/codex/tester-onboarding-main | Patch-equivalent work integrated |
| refs/heads/compat/legacy | 173fa103d157645f93bb7054f2acb6c8d80cdda8 | archive/2026-09-08/local/compat/legacy | Already merged |
| refs/heads/pr-38 | 4018da6a6e8d88f341b6ede3bdce4892b0ec4a36 | archive/2026-09-08/local/pr-38 | Already merged |
| refs/heads/pr-49 | e7eb6e6f7b9dee5cb19e1e77a88f4884837ae629 | archive/2026-09-08/local/pr-49 | Reviewed: integrated or superseded (see rationale) |
| refs/heads/rewrite/phase4-governance | ba063df1ee3a7bbc4aeb0957f2326e4a941f78e5 | archive/2026-09-08/local/rewrite/phase4-governance | Already merged |
| refs/remotes/origin/codex/android-release-bootstrap | 1f240ab697a46a0247ade3c8b9ebf7a130c5a835 | archive/2026-09-08/origin/codex/android-release-bootstrap | Reviewed: integrated or superseded (see rationale) |
| refs/remotes/origin/codex/android-release-pipeline | 2fb19b3033e19bc17de9373a99860af20f419eec | archive/2026-09-08/origin/codex/android-release-pipeline | Patch-equivalent work integrated |
| refs/remotes/origin/codex/complete-gradle-verification | 19c0f953e7fdd64d6081c4af0028fc84b5e3f94e | archive/2026-09-08/origin/codex/complete-gradle-verification | Already merged |
| refs/remotes/origin/codex/issue-37-fmod-update-loop | 890f9d9fe43713713c6b32c547591a8818d8ba09 | archive/2026-09-08/origin/codex/issue-37-fmod-update-loop | Already merged |
| refs/remotes/origin/codex/issue-38-runtime-identity | 559251309f68659102bcabbf12e669f9512e7776 | archive/2026-09-08/origin/codex/issue-38-runtime-identity | Already merged |
| refs/remotes/origin/codex/migration-baseline-implementation | 89ee9a650f375418b51f733888144831d67fe255 | archive/2026-09-08/origin/codex/migration-baseline-implementation | Already merged |
| refs/remotes/origin/codex/overhaul-phase1-reliability | c0f57b76f4b4853523e591069ddf2da0c8b2be2b | archive/2026-09-08/origin/codex/overhaul-phase1-reliability | Already merged |
| refs/remotes/origin/codex/overhaul-phase3-runbooks | 7d8e446db3a79a88aec141cab1330b7f051b9235 | archive/2026-09-08/origin/codex/overhaul-phase3-runbooks | Already merged |
| refs/remotes/origin/codex/phase5-ci-gates | 991a7a0338cb4f0ad31e38c5d3ba539035575d67 | archive/2026-09-08/origin/codex/phase5-ci-gates | Already merged |
| refs/remotes/origin/codex/phase6-ci-smoke-build | 903b464b10f025507856bec3e4fb83dffb6b3c3b | archive/2026-09-08/origin/codex/phase6-ci-smoke-build | Already merged |
| refs/remotes/origin/codex/phase7-closure | 279aa5efc923ec7fd1330b515317b197434ba978 | archive/2026-09-08/origin/codex/phase7-closure | Patch-equivalent work integrated |
| refs/remotes/origin/codex/reduction-baseline | 38ddcaa388a8838b0bffd2357f4d0feb5eec20ef | archive/2026-09-08/origin/codex/reduction-baseline | Already merged |
| refs/remotes/origin/codex/reliability-p2-downloader-race | 5753d88c3b53f01c2e970c3cf783bcd86f58b2fd | archive/2026-09-08/origin/codex/reliability-p2-downloader-race | Already merged |
| refs/remotes/origin/codex/resolve-release-gfi | 8f17b62264bd3d4175024ea465389d921f72323d | archive/2026-09-08/origin/codex/resolve-release-gfi | Patch-equivalent work integrated |
| refs/remotes/origin/codex/stage-next-reliability | 0b329aba98db8b1708731ff5d6e9df9eddfe034b | archive/2026-09-08/origin/codex/stage-next-reliability | Already merged |
| refs/remotes/origin/codex/suspend-idle-during-cloud-rpc | 40c60c44cc39b97d52cde4401b4863ecac99dd0d | archive/2026-09-08/origin/codex/suspend-idle-during-cloud-rpc | Reviewed: integrated or superseded (see rationale) |
| refs/remotes/origin/compat/legacy | 173fa103d157645f93bb7054f2acb6c8d80cdda8 | archive/2026-09-08/origin/compat/legacy | Already merged |
| refs/remotes/origin/pr-38 | 4018da6a6e8d88f341b6ede3bdce4892b0ec4a36 | archive/2026-09-08/origin/pr-38 | Already merged |
| refs/remotes/origin/pr-49 | e7eb6e6f7b9dee5cb19e1e77a88f4884837ae629 | archive/2026-09-08/origin/pr-49 | Reviewed: integrated or superseded (see rationale) |
| refs/remotes/origin/rewrite/phase4-governance | ba063df1ee3a7bbc4aeb0957f2326e4a941f78e5 | archive/2026-09-08/origin/rewrite/phase4-governance | Already merged |
