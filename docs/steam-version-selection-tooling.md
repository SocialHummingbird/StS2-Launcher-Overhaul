# Steam Version Selection Tooling

This page lists the local helper scripts used by the Steam version-selection validation path.

These tools do not replace ARM64 runtime validation. They only make static guardrails and evidence capture repeatable.

The static audit entrypoint in `scripts/audit-steam-version-selection.ps1` is intentionally thin: module loading lives in `scripts/audit-steam-version-selection.modules.ps1`, and check registration is grouped by runtime/download, auth/cloud, compact support/docs, and portal/action concerns in `scripts/audit-steam-version-selection.registration.runtime.ps1`, `scripts/audit-steam-version-selection.registration.auth-cloud.ps1`, `scripts/audit-steam-version-selection.registration.support-docs.ps1`, and `scripts/audit-steam-version-selection.registration.portal-action.ps1`.

The PowerShell helpers normalize local path separators so they can be run from Windows PowerShell or PowerShell Core on CI/Linux. Android `adb shell` paths intentionally remain Android-style paths. Device evidence helpers resolve `adb` from an explicit `-AdbPath`, PATH, `ANDROID_HOME`, `ANDROID_SDK_ROOT`, the standard local Android SDK path, or the repo local `.w40k-android-toolchain` SDK path. If none is available, they fail before capture with an actionable SDK/platform-tools message.

Shared helper boundaries are intentional release guardrails. Static audits use `scripts/static-audit-utils.ps1` for common file/pattern checks. Device evidence collectors use `scripts/android-shell-utils.ps1` for Android `run-as sh -c` quoting, `scripts/evidence-path-utils.ps1` for repo-relative and evidence-relative artifact paths, `scripts/evidence-marker-utils.ps1` for marker-file parsing, `scripts/evidence-report-utils.ps1` for Markdown evidence rows, and `scripts/evidence-redaction-utils.ps1` for public-evidence redaction, focused-log redaction, local-only artifact policy, and sensitive-content review checks. Managed launcher/runtime checks use `LauncherBranchMarkerFields` for branch marker labels, `LauncherBranchMarkerIntegrityProvenance` for selected/public depot comparison completeness, `SteamBranchAvailabilityMarkerFields`, `SteamBranchAvailabilityMarkerRow`, and `SteamBranchAvailabilityMarkerFile` for Steam app-info branch availability marker labels, row metadata keys, visible-branch row parsing, and marker file reads, and `LauncherAndroidAppPrivatePath` for `/data/user/0/<package>` versus `/data/data/<package>` alias-safe comparisons. Keep those helpers small and covered by the static audits when adding new branch/runtime evidence capture.

## Static guardrail audit

Purpose:

- Checks that branch/version implementation guardrails, docs, release blockers, tester guidance, and CI coverage are still present.
- Keeps shared helper-boundary orchestration in `scripts/audit-steam-version-selection.helper-boundaries.ps1`, with audit-module, shared-utility, and marker-helper boundaries split into `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.shared-utils.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.markers.ps1`; audit-module boundary inventory is further split into `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.ps1`; auth/cloud boundary inventory is further split into session/auth, cloud-safety, and login-panel checks in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.session.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.login-panel.ps1`; runtime boundary inventory is further split into shell/selector, branch/native, and download-workflow checks in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.download-workflow.ps1`; support/docs boundary inventory is further split into `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.ps1`; portal/action boundary inventory is further split into `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.ps1`; chrome/workflow boundary inventory is further split into chrome/status, workflow, and compact-install checks in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.chrome-status.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.compact-install.ps1`; launcher shell checks in `scripts/audit-steam-version-selection.launcher-shell.ps1`; branch-selector orchestration in `scripts/audit-steam-version-selection.branch-selector.ps1` with focused catalog, storage/preference, download-section, and action-section selector checks in `scripts/audit-steam-version-selection.branch-selector.catalog.ps1`, `scripts/audit-steam-version-selection.branch-selector.storage.ps1`, `scripts/audit-steam-version-selection.branch-selector.download-section.ps1`, and `scripts/audit-steam-version-selection.branch-selector.action-section.ps1`; branch catalog checks are further split into capability/refresh, option metadata/status, marker read/merge, and dropdown formatting/blocker checks in `scripts/audit-steam-version-selection.branch-selector.catalog.capability.ps1`, `scripts/audit-steam-version-selection.branch-selector.catalog.options.ps1`, `scripts/audit-steam-version-selection.branch-selector.catalog.read.ps1`, and `scripts/audit-steam-version-selection.branch-selector.catalog.dropdown.ps1`; branch runtime/cache orchestration in `scripts/audit-steam-version-selection.branch-runtime.ps1` with focused depot provenance, branch-marker integrity, and cache-safety checks in `scripts/audit-steam-version-selection.branch-runtime.depots.ps1`, `scripts/audit-steam-version-selection.branch-runtime.markers.ps1`, and `scripts/audit-steam-version-selection.branch-runtime.cache-safety.ps1`; native Android routing/fallback checks in `scripts/audit-steam-version-selection.native-routing.ps1`; branch-availability marker contract orchestration in `scripts/audit-steam-version-selection.branch-availability.ps1` with focused downloader, marker, and launcher-status checks in `scripts/audit-steam-version-selection.branch-availability.downloader.ps1`, `scripts/audit-steam-version-selection.branch-availability.marker.ps1`, and `scripts/audit-steam-version-selection.branch-availability.launcher-status.ps1`; download/update workflow orchestration in `scripts/audit-steam-version-selection.download-workflows.ps1` with focused model/depot-connection, controller action/cache, and update-check workflow checks in `scripts/audit-steam-version-selection.download-workflows.model.ps1`, `scripts/audit-steam-version-selection.download-workflows.actions.ps1`, and `scripts/audit-steam-version-selection.download-workflows.update-checks.ps1`; Steam session authentication checks in `scripts/audit-steam-version-selection.session-auth.ps1`; launcher automation checks in `scripts/audit-steam-version-selection.automation.ps1`; local Steam credential handoff checks in `scripts/audit-steam-version-selection.local-login.ps1`; contextual confirmation checks in `scripts/audit-steam-version-selection.confirmations.ps1`; branch-switch/manual cloud Push safety orchestration in `scripts/audit-steam-version-selection.cloud-safety.ps1` with focused branch-switch, push-request, evidence-marker, local-backup, and startup-context checks in `scripts/audit-steam-version-selection.cloud-safety.branch-switch.ps1`, `scripts/audit-steam-version-selection.cloud-safety.push-requests.ps1`, `scripts/audit-steam-version-selection.cloud-safety.evidence-markers.ps1`, `scripts/audit-steam-version-selection.cloud-safety.local-backups.ps1`, and `scripts/audit-steam-version-selection.cloud-safety.startup-context.ps1`; manual cloud evidence-marker checks are further split into shared core/parser, Pull, completed Push, and blocked Push checks in `scripts/audit-steam-version-selection.cloud-safety.evidence-markers.core.ps1`, `scripts/audit-steam-version-selection.cloud-safety.evidence-markers.pull.ps1`, `scripts/audit-steam-version-selection.cloud-safety.evidence-markers.push.ps1`, and `scripts/audit-steam-version-selection.cloud-safety.evidence-markers.blocked-push.ps1`; native credential/login-panel orchestration in `scripts/audit-steam-version-selection.login-panel.ps1` with focused capability/support, managed login-section, and native Android panel checks in `scripts/audit-steam-version-selection.login-panel.support.ps1`, `scripts/audit-steam-version-selection.login-panel.managed-section.ps1`, and `scripts/audit-steam-version-selection.login-panel.native-android.ps1`; reusable compact two-line label checks in `scripts/audit-steam-version-selection.compact-labels.ps1`; compact section setup checks in `scripts/audit-steam-version-selection.section-setup.ps1`; quick-start safe-flow guide checks in `scripts/audit-steam-version-selection.safe-flow-guide.ps1`; Help & Reports diagnostics drawer checks in `scripts/audit-steam-version-selection.diagnostics-drawer.ps1`; diagnostics report/branch-switch evidence orchestration in `scripts/audit-steam-version-selection.diagnostics-reporting.ps1` with focused public-sharing shell, launcher-state/cache, and branch-switch safety diagnostics checks in `scripts/audit-steam-version-selection.diagnostics-reporting.shell.ps1`, `scripts/audit-steam-version-selection.diagnostics-reporting.launcher-state.ps1`, and `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.ps1`; evidence capture/tooling orchestration in `scripts/audit-steam-version-selection.evidence-tooling.ps1` with focused capture, scaffold/parity, redaction, and tooling-doc checks in `scripts/audit-steam-version-selection.evidence-tooling.capture.ps1`, `scripts/audit-steam-version-selection.evidence-tooling.scaffold.ps1`, `scripts/audit-steam-version-selection.evidence-tooling.redaction.ps1`, and `scripts/audit-steam-version-selection.evidence-tooling.docs.ps1`; branch evidence-template documentation checks in `scripts/audit-steam-version-selection.branch-evidence-docs.ps1`; Android login validation documentation orchestration in `scripts/audit-steam-version-selection.login-validation-docs.ps1` with focused native-proof, portal-workflow, compact-action, and validation-boundary checks in `scripts/audit-steam-version-selection.login-validation-docs.native-proof.ps1`, `scripts/audit-steam-version-selection.login-validation-docs.portal-workflow.ps1`, `scripts/audit-steam-version-selection.login-validation-docs.compact-actions.ps1`, and `scripts/audit-steam-version-selection.login-validation-docs.validation-boundary.ps1`; login portal evidence-template documentation orchestration in `scripts/audit-steam-version-selection.login-portal-evidence-docs.ps1` with focused auth/status, compact-workflow/layout, install/cloud, and validation-matrix checks in `scripts/audit-steam-version-selection.login-portal-evidence-docs.auth-status.ps1`, `scripts/audit-steam-version-selection.login-portal-evidence-docs.compact-workflow.ps1`, `scripts/audit-steam-version-selection.login-portal-evidence-docs.install-cloud.ps1`, and `scripts/audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.ps1`; release/status documentation orchestration in `scripts/audit-steam-version-selection.release-status-docs.ps1` with focused release-note UX, release policy/loading, release limitations, and Android status/README checks in `scripts/audit-steam-version-selection.release-status-docs.release-note-ux.ps1`, `scripts/audit-steam-version-selection.release-status-docs.release-policy.ps1`, `scripts/audit-steam-version-selection.release-status-docs.release-limitations.ps1`, and `scripts/audit-steam-version-selection.release-status-docs.android-status.ps1`; release/readiness documentation orchestration in `scripts/audit-steam-version-selection.release-docs.ps1` with focused overview, completion-audit, runbook/user-guide, and signoff checks in `scripts/audit-steam-version-selection.release-docs.overview.ps1`, `scripts/audit-steam-version-selection.release-docs.completion.ps1`, `scripts/audit-steam-version-selection.release-docs.runbook-user.ps1`, and `scripts/audit-steam-version-selection.release-docs.signoff.ps1`; beta branch integrity evidence orchestration in `scripts/audit-steam-version-selection.beta-integrity.ps1` with focused release-readiness, capture/review, evidence-doc, and issue-template checks in `scripts/audit-steam-version-selection.beta-integrity.release-readiness.ps1`, `scripts/audit-steam-version-selection.beta-integrity.capture-review.ps1`, `scripts/audit-steam-version-selection.beta-integrity.evidence-docs.ps1`, and `scripts/audit-steam-version-selection.beta-integrity.issue-template.ps1`; GitHub governance checks in `scripts/audit-steam-version-selection.governance.ps1`; launcher portal chrome checks in `scripts/audit-steam-version-selection.portal-chrome.ps1`; portal behavior checks in `scripts/audit-steam-version-selection.portal-behavior.ps1`; status capsule checks in `scripts/audit-steam-version-selection.status-capsule.ps1`; compact workflow/current-task orchestration in `scripts/audit-steam-version-selection.compact-workflow.ps1` with focused strip, sticky-header, state/navigation, and section-transition checks in `scripts/audit-steam-version-selection.compact-workflow.strip.ps1`, `scripts/audit-steam-version-selection.compact-workflow.sticky-header.ps1`, `scripts/audit-steam-version-selection.compact-workflow.state.ps1`, and `scripts/audit-steam-version-selection.compact-workflow.sections.ps1`; compact Steam Guard code-section checks in `scripts/audit-steam-version-selection.code-section.ps1`; compact section flow checks in `scripts/audit-steam-version-selection.compact-section-flow.ps1`; compact install/version/download orchestration in `scripts/audit-steam-version-selection.compact-install.ps1` with focused version-layout, download/progress, and metric-scaling checks in `scripts/audit-steam-version-selection.compact-install.version.ps1`, `scripts/audit-steam-version-selection.compact-install.download.ps1`, and `scripts/audit-steam-version-selection.compact-install.metrics.ps1`; compact install version checks are further split into shell/drawer, selected-version summary, action-label, and responsive ordering checks in `scripts/audit-steam-version-selection.compact-install.version.shell.ps1`, `scripts/audit-steam-version-selection.compact-install.version.summary.ps1`, `scripts/audit-steam-version-selection.compact-install.version.actions.ps1`, and `scripts/audit-steam-version-selection.compact-install.version.layout.ps1`; startup/warmup/status orchestration in `scripts/audit-steam-version-selection.startup-warmup.ps1` with focused startup-mode, shader-warmup, and startup-status checks in `scripts/audit-steam-version-selection.startup-warmup.startup-mode.ps1`, `scripts/audit-steam-version-selection.startup-warmup.shader.ps1`, and `scripts/audit-steam-version-selection.startup-warmup.status.ps1`; startup recovery panel/report checks in `scripts/audit-steam-version-selection.startup-recovery.ps1`; ready-state action-section orchestration checks in `scripts/audit-steam-version-selection.action-section.ps1` with focused core, support, cloud, and visibility checks in `scripts/audit-steam-version-selection.action-core.ps1`, `scripts/audit-steam-version-selection.action-support.ps1`, `scripts/audit-steam-version-selection.action-cloud.ps1`, and `scripts/audit-steam-version-selection.action-visibility.ps1`; ready-state action core checks are further split into construction/dropdown, button-style, ready-summary, and layout/drawer-copy checks in `scripts/audit-steam-version-selection.action-core.construction.ps1`, `scripts/audit-steam-version-selection.action-core.button-styles.ps1`, `scripts/audit-steam-version-selection.action-core.ready-summary.ps1`, and `scripts/audit-steam-version-selection.action-core.layout.ps1`; ready-state cloud checks are further split into cloud-control construction/copy and Steam Cloud safety/visibility checks in `scripts/audit-steam-version-selection.action-cloud.controls.ps1` and `scripts/audit-steam-version-selection.action-cloud.safety.ps1`; and portal status/UX support orchestration in `scripts/audit-steam-version-selection.portal-ux-support.ps1` with focused status-formatter, support-flag, narrative, and feature-registry checks in `scripts/audit-steam-version-selection.portal-ux-status.ps1`, `scripts/audit-steam-version-selection.portal-ux-flags.ps1`, `scripts/audit-steam-version-selection.portal-ux-narrative.ps1`, and `scripts/audit-steam-version-selection.portal-ux-features.ps1`; portal UX support flags are further split into status/layout, workflow, auth/chrome, install/cloud, and diagnostics/recovery checks in `scripts/audit-steam-version-selection.portal-ux-flags.status.ps1`, `scripts/audit-steam-version-selection.portal-ux-flags.workflow.ps1`, `scripts/audit-steam-version-selection.portal-ux-flags.auth-chrome.ps1`, `scripts/audit-steam-version-selection.portal-ux-flags.install-cloud.ps1`, and `scripts/audit-steam-version-selection.portal-ux-flags.diagnostics.ps1` so focused guardrails can evolve without expanding the top-level audit orchestrator.
- Login portal evidence-template validation-matrix checks are further split into native credential, portal workflow, compact action/cloud, and signoff/sanitization checks in `scripts/audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.native-credential.ps1`, `scripts/audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.portal-workflow.ps1`, `scripts/audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.compact-actions.ps1`, and `scripts/audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.signoff.ps1`.
- Ready-state support action checks are further split into support foundation, support construction, compact label synchronization, and diagnostics-sharing checks in `scripts/audit-steam-version-selection.action-support.foundation.ps1`, `scripts/audit-steam-version-selection.action-support.construction.ps1`, `scripts/audit-steam-version-selection.action-support.labels.ps1`, and `scripts/audit-steam-version-selection.action-support.diagnostics.ps1`.
- Branch-switch diagnostics-reporting checks are further split into branch-switch shell, marker, Manual Pull, save-origin, Manual Push/blocked-Push, and backup evidence checks in `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.shell.ps1`, `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.marker.ps1`, `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.pull.ps1`, `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.save-origin.ps1`, `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.push.ps1`, and `scripts/audit-steam-version-selection.diagnostics-reporting.branch-switch.backup.ps1`.
- Help & Reports diagnostics drawer checks are further split into drawer shell/log, primary-column host, viewport sizing, and reveal/export action checks in `scripts/audit-steam-version-selection.diagnostics-drawer.shell.ps1`, `scripts/audit-steam-version-selection.diagnostics-drawer.primary-column.ps1`, `scripts/audit-steam-version-selection.diagnostics-drawer.sizing.ps1`, and `scripts/audit-steam-version-selection.diagnostics-drawer.actions.ps1`.
- Diagnostics launcher-state checks are further split into preference/portal UX, branch-availability marker, and cached-version cleanup/redownload diagnostics modules in `scripts/audit-steam-version-selection.diagnostics-reporting.launcher-state.preferences.ps1`, `scripts/audit-steam-version-selection.diagnostics-reporting.launcher-state.branch-availability.ps1`, and `scripts/audit-steam-version-selection.diagnostics-reporting.launcher-state.cached-versions.ps1`.
- Diagnostics support/docs boundary checks are further split into drawer, reporting, branch-switch, and evidence-tooling boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.drawer.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.reporting.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.branch-switch.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.diagnostics.evidence-tooling.ps1`.
- Runtime branch/native boundary checks are further split into branch-runtime and native-routing boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.branch-runtime.ps1` and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.native-routing.ps1`.
- Runtime shell/selector boundary checks are further split into launcher-shell, branch-selector, and branch-catalog boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.shell.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.branch-selector.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.branch-catalog.ps1`.
- Branch-selector storage checks are further split into install-path/runtime-cache, selected-branch preference, and local-backup/cloud preference modules in `scripts/audit-steam-version-selection.branch-selector.storage.paths.ps1`, `scripts/audit-steam-version-selection.branch-selector.storage.preferences.ps1`, and `scripts/audit-steam-version-selection.branch-selector.storage.cloud.ps1`.
- Ready-state cloud-control checks are further split into construction orchestration, primary action/confirmation controls, compact cloud copy, and responsive layout/order modules in `scripts/audit-steam-version-selection.action-cloud.controls.construction.ps1`, `scripts/audit-steam-version-selection.action-cloud.controls.primary-actions.ps1`, `scripts/audit-steam-version-selection.action-cloud.controls.copy.ps1`, and `scripts/audit-steam-version-selection.action-cloud.controls.layout.ps1`.
- Shader warmup checks are further split into lifecycle/run-state, execution/timing, and Android-readable UI/progress modules in `scripts/audit-steam-version-selection.startup-warmup.shader.lifecycle.ps1`, `scripts/audit-steam-version-selection.startup-warmup.shader.execution.ps1`, and `scripts/audit-steam-version-selection.startup-warmup.shader.ui.ps1`.
- Startup status checks are further split into shell/legacy routing, Android status-card composition, and startup-observed cleanup modules in `scripts/audit-steam-version-selection.startup-warmup.status.shell.ps1`, `scripts/audit-steam-version-selection.startup-warmup.status.android.ps1`, and `scripts/audit-steam-version-selection.startup-warmup.status.cleanup.ps1`.
- Portal/action startup boundary checks are further split into startup shell, shader, status, and recovery boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.shell.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.shader.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.status.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.recovery.ps1`.
- Compact Steam Guard code-section checks are further split into construction, controls, responsive layout, and submission-normalization modules in `scripts/audit-steam-version-selection.code-section.construction.ps1`, `scripts/audit-steam-version-selection.code-section.controls.ps1`, `scripts/audit-steam-version-selection.code-section.responsive.ps1`, and `scripts/audit-steam-version-selection.code-section.submission.ps1`.
- Branch runtime cache-safety checks are further split into selected redownload cleanup, inactive cache cleanup marker, and runtime-pack cleanup/preservation modules in `scripts/audit-steam-version-selection.branch-runtime.cache-safety.redownload.ps1`, `scripts/audit-steam-version-selection.branch-runtime.cache-safety.cleanup-markers.ps1`, and `scripts/audit-steam-version-selection.branch-runtime.cache-safety.runtime-packs.ps1`.
- Native Android routing checks are further split into startup provenance, branch-info/pre-routing guidance, and fallback diagnostics/recovery modules in `scripts/audit-steam-version-selection.native-routing.startup-provenance.ps1`, `scripts/audit-steam-version-selection.native-routing.branch-info.ps1`, and `scripts/audit-steam-version-selection.native-routing.fallback.ps1`.
- Manual Steam Cloud Push request checks are further split into safety gates, request construction/confirmation, and execution/lifecycle modules in `scripts/audit-steam-version-selection.cloud-safety.push-requests.gates.ps1`, `scripts/audit-steam-version-selection.cloud-safety.push-requests.request.ps1`, and `scripts/audit-steam-version-selection.cloud-safety.push-requests.execution.ps1`.
- Branch-switch cloud-safety checks are further split into cached-version enumeration, marker identity/read parsing, and Push gate/write modules in `scripts/audit-steam-version-selection.cloud-safety.branch-switch.cache.ps1`, `scripts/audit-steam-version-selection.cloud-safety.branch-switch.marker.ps1`, and `scripts/audit-steam-version-selection.cloud-safety.branch-switch.gates.ps1`.
- Branch-switch local backup safety checks are further split into local save evidence, backup evidence, and Push enforcement modules in `scripts/audit-steam-version-selection.cloud-safety.local-backups.local-saves.ps1`, `scripts/audit-steam-version-selection.cloud-safety.local-backups.backup-evidence.ps1`, and `scripts/audit-steam-version-selection.cloud-safety.local-backups.push-enforcement.ps1`.
- Auth/cloud cloud-safety boundary checks are further split into shell/Push orchestration, manual cloud evidence markers, and local backup/startup boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.shell.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.markers.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.backups.ps1`.
- Native Android login-panel checks are further split into credential field/autofill, responsive layout/input routing, and short-lived credential handoff/cleanup modules in `scripts/audit-steam-version-selection.login-panel.native-android.fields.ps1`, `scripts/audit-steam-version-selection.login-panel.native-android.layout.ps1`, and `scripts/audit-steam-version-selection.login-panel.native-android.security.ps1`.
- Steam session authentication checks are further split into launcher model attempt/result handling and Steam-session connection reuse/saved-credential adoption modules in `scripts/audit-steam-version-selection.session-auth.model.ps1` and `scripts/audit-steam-version-selection.session-auth.connection.ps1`.
- Android login validation native-proof checks are further split into core proof-contract, compact status/quick-start, compact action/cloud, task-header/section, and evidence-matrix modules in `scripts/audit-steam-version-selection.login-validation-docs.native-proof.core.ps1`, `scripts/audit-steam-version-selection.login-validation-docs.native-proof.compact-status.ps1`, `scripts/audit-steam-version-selection.login-validation-docs.native-proof.compact-actions.ps1`, `scripts/audit-steam-version-selection.login-validation-docs.native-proof.task-header.ps1`, and `scripts/audit-steam-version-selection.login-validation-docs.native-proof.evidence-matrix.ps1`.
- Login validation support/docs boundary checks are further split into shell, native-proof, and workflow/action boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.shell.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.native-proof.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.workflow-actions.ps1`.
- Release docs runbook/user-guide checks are further split into runbook, user support/setup, branch/cloud marker, beta-integrity, and artifact-hygiene modules in `scripts/audit-steam-version-selection.release-docs.runbook-user.runbook.ps1`, `scripts/audit-steam-version-selection.release-docs.runbook-user.support.ps1`, `scripts/audit-steam-version-selection.release-docs.runbook-user.branch-cloud.ps1`, `scripts/audit-steam-version-selection.release-docs.runbook-user.beta-integrity.ps1`, and `scripts/audit-steam-version-selection.release-docs.runbook-user.artifact-hygiene.ps1`.
- Support/docs release-beta boundary checks are further split into release docs, beta core tooling, and beta documentation boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.beta-core.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.beta-docs.ps1`.
- Support/docs release documentation boundary checks are further split into release core/signoff and runbook/user-guide boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.core.ps1` and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.release-beta.release-docs.runbook-user.ps1`.
- Branch runtime marker checks are further split into diagnostics, typed field/path, and readiness/integrity modules in `scripts/audit-steam-version-selection.branch-runtime.markers.diagnostics.ps1`, `scripts/audit-steam-version-selection.branch-runtime.markers.fields.ps1`, and `scripts/audit-steam-version-selection.branch-runtime.markers.readiness.ps1`.
- Compact workflow strip checks are further split into shell/result, cell composition, and style/navigation modules in `scripts/audit-steam-version-selection.compact-workflow.strip.shell.ps1`, `scripts/audit-steam-version-selection.compact-workflow.strip.cells.ps1`, and `scripts/audit-steam-version-selection.compact-workflow.strip.style-navigation.ps1`.
- Portal/action chrome workflow boundary checks are further split into compact workflow, code section, and compact section-flow boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.code.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.section-flow.ps1`.
- Portal behavior checks are further split into SteamKit debug-log, compact guidance, keyboard/viewport, and portal chrome modules in `scripts/audit-steam-version-selection.portal-behavior.debug-logs.ps1`, `scripts/audit-steam-version-selection.portal-behavior.compact-guidance.ps1`, `scripts/audit-steam-version-selection.portal-behavior.keyboard.ps1`, and `scripts/audit-steam-version-selection.portal-behavior.chrome.ps1`.
- Compact workflow sticky-header checks are further split into placement, current-task button, layout/reflow, and style modules in `scripts/audit-steam-version-selection.compact-workflow.sticky-header.placement.ps1`, `scripts/audit-steam-version-selection.compact-workflow.sticky-header.button.ps1`, `scripts/audit-steam-version-selection.compact-workflow.sticky-header.layout.ps1`, and `scripts/audit-steam-version-selection.compact-workflow.sticky-header.style.ps1`.
- Portal/action compact workflow boundary checks are further split into compact shell, strip, sticky-header, and state/section boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.shell.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.strip.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.sticky.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.compact.state-sections.ps1`.
- Quick-start safe-flow guide checks are further split into panel, step-card, and toggle modules in `scripts/audit-steam-version-selection.safe-flow-guide.panel.ps1`, `scripts/audit-steam-version-selection.safe-flow-guide.steps.ps1`, and `scripts/audit-steam-version-selection.safe-flow-guide.toggle.ps1`.
- Public beta-integrity issue-template checks are further split into artifact hygiene, branch/cache provenance, and manual cloud marker modules in `scripts/audit-steam-version-selection.beta-integrity.issue-template.hygiene.ps1`, `scripts/audit-steam-version-selection.beta-integrity.issue-template.branch-cache.ps1`, and `scripts/audit-steam-version-selection.beta-integrity.issue-template.cloud-markers.ps1`.
- Portal chrome checks are further split into shell/brand and compact layout modules in `scripts/audit-steam-version-selection.portal-chrome.shell-brand.ps1` and `scripts/audit-steam-version-selection.portal-chrome.compact-layout.ps1`.
- Branch evidence-template checks are further split into selector/branch-switch, cloud Push/cache, and artifact hygiene/redaction modules in `scripts/audit-steam-version-selection.branch-evidence-docs.selector.ps1`, `scripts/audit-steam-version-selection.branch-evidence-docs.cloud-push.ps1`, and `scripts/audit-steam-version-selection.branch-evidence-docs.artifact-hygiene.ps1`.
- Compact section-flow checks are further split into visibility, active-section scrolling, and viewport re-anchor modules in `scripts/audit-steam-version-selection.compact-section-flow.visibility.ps1`, `scripts/audit-steam-version-selection.compact-section-flow.scrolling.ps1`, and `scripts/audit-steam-version-selection.compact-section-flow.reanchor.ps1`.
- Ready-state cloud safety checks are further split into Push flow, compact option drawer, and safety cue modules in `scripts/audit-steam-version-selection.action-cloud.safety.push-flow.ps1`, `scripts/audit-steam-version-selection.action-cloud.safety.compact-options.ps1`, and `scripts/audit-steam-version-selection.action-cloud.safety.cue.ps1`.
- Portal/action portal-UX boundary checks are further split into portal UX support, support flags, narrative/features, and branch-availability boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.support.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.flags.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.features.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.branch-availability.ps1`.
- Portal/action ready-state action boundary checks are further split into action core, support, cloud, and visibility boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.core.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.support.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.visibility.ps1`.
- Portal/action ready-state cloud boundary checks are further split into cloud-control and Steam Cloud safety boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.controls.ps1` and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.safety.ps1`.
- Compact support/docs boundary checks are further split into compact label, section setup, and safe-flow guide boundary modules in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.labels.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.section-setup.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.compact.safe-flow.ps1`.
- Audit-module boundary checks keep the execution orchestrator small by moving top-level inventory, category, and subcategory guardrails into `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.inventory.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.categories.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.ps1`.
- Audit-module category boundary checks are further split into runtime, auth/cloud, portal/action, and support/docs boundary inventories in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.categories.runtime.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.categories.auth-cloud.ps1`, `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.categories.portal-action.ps1`, and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.categories.support-docs.ps1`.
- Audit-module subcategory boundary checks are further split into portal/action and support/docs boundary inventories in `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.portal-action.ps1` and `scripts/audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.support-docs.ps1`.
- Status capsule checks are further split into shell/style constants, compact headline/reflow, and touch-expandable detail row checks in `scripts/audit-steam-version-selection.status-capsule.shell.ps1`, `scripts/audit-steam-version-selection.status-capsule.compact.ps1`, and `scripts/audit-steam-version-selection.status-capsule.detail.ps1`.
- Compact section setup checks are further split into section shell/framing, compact/desktop header styling, and per-section cue text modules in `scripts/audit-steam-version-selection.section-setup.shell.ps1`, `scripts/audit-steam-version-selection.section-setup.headers.ps1`, and `scripts/audit-steam-version-selection.section-setup.cues.ps1`.
- Does not prove Steam login, depot download, beta branch behavior, game launch, save compatibility, or Push safety.

Command:

```powershell
.\scripts\audit-steam-version-selection.ps1
```

Quiet mode:

```powershell
.\scripts\audit-steam-version-selection.ps1 -Quiet
```

## Managed/native selector guidance parity audit

Purpose:

- Checks that the managed launcher selector note and native Android selector note still share the same important safety phrases.
- Catches drift between `SteamGameBranch.SelectorHelpText` and `SteamBranchInfo.selectorHelpText`.
- Does not prove UI rendering, startup routing, or device behavior.

Command:

```powershell
.\scripts\audit-steam-branch-guidance-parity.ps1
```

Quiet mode:

```powershell
.\scripts\audit-steam-branch-guidance-parity.ps1 -Quiet
```

## Create an evidence folder

Purpose:

- Creates a timestamped validation folder under `artifacts/android/`.
- Copies the evidence template into `evidence.md`.
- Points the generated README at `docs/steam-version-selection-release-readiness.md` so each evidence run starts from the current release gate.
- Creates `ARTIFACT_HYGIENE.txt` with local-only/raw-log and public-sharing guidance.
- Creates `PUBLIC_SHARE_MANIFEST.txt` with preferred public artifacts and local-only/manual-review artifacts.
- Creates `PUBLIC_EVIDENCE_REDACTION_REVIEW.txt` for the manual review fields required before public sharing.
- Creates subfolders for logs, diagnostics, branch markers, screenshots, and backup evidence.

Command:

```powershell
.\scripts\new-steam-version-selection-evidence.ps1
```

With a label:

```powershell
.\scripts\new-steam-version-selection-evidence.ps1 -Label "public-baseline"
```

Output shape:

```text
artifacts/android/steam-version-selection-<timestamp>/
  evidence.md
  ARTIFACT_HYGIENE.txt
  PUBLIC_SHARE_MANIFEST.txt
  PUBLIC_EVIDENCE_REDACTION_REVIEW.txt
  README.md
  logs/
  diagnostics/
  branch-markers/
  screenshots/
  backup-evidence/
```

## Review public evidence redaction

Purpose:

- Fails a public-share candidate when `PUBLIC_EVIDENCE_REDACTION_REVIEW.txt` is missing or incomplete.
- Fails raw/local-only evidence such as `logs/logcat-full.txt`, `logs/logcat-steam-version-focused.txt`, ad hoc raw logcat captures like `logs/logcat-after-launch.txt`, focused raw startup extracts like `logs/startup-routing-focused.txt`, credential handoff files, token-like files, private save dumps, local user paths, device-private Android paths, known device serials, email addresses, and credential/token assignments.
- Requires screenshots/images to have completed manual review fields before they are treated as shareable.
- Does not mutate artifacts and does not replace manual review.

Command:

```powershell
.\scripts\review-public-evidence-redaction.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>"
```

## Export a sanitized public evidence candidate

Purpose:

- Copies a raw/local evidence folder into a separate public-share candidate folder without mutating the source evidence.
- Sanitizes text artifacts for local Windows/user paths, Android app-private package paths, known device serials, email addresses, and common credential/token assignment patterns.
- Skips local-only raw-log, ad hoc raw logcat/focused startup extracts, and credential/token-like artifact paths. The generated `logs/logcat-steam-version-focused-redacted.txt` remains the default public log excerpt.
- Omits images by default; use `-IncludeImages` only after direct visual review.
- Writes `PUBLIC_EVIDENCE_REDACTION_REVIEW.txt` and `PUBLIC_SHARE_MANIFEST.txt` into the export folder, then the export must still pass `review-public-evidence-redaction.ps1`.

Command:

```powershell
.\scripts\export-public-evidence-redaction.ps1 `
  -SourceEvidenceDir "artifacts\android\<raw-evidence-folder>" `
  -OutputDir "artifacts\android\<raw-evidence-folder>-public-redacted" `
  -Force

.\scripts\review-public-evidence-redaction.ps1 `
  -EvidenceDir "artifacts\android\<raw-evidence-folder>-public-redacted"
```

The raw evidence remains local. Treat the export script as a repeatable sanitizer for text artifacts, not a substitute for release-readiness review.

## Capture non-secret device evidence

Purpose:

- Captures `adb devices -l`.
- Writes `ARTIFACT_HYGIENE.txt` so the bundle labels raw logs as local-only and points public reports to the redacted focused log.
- Writes `PUBLIC_SHARE_MANIFEST.txt` so testers can see which generated artifacts are the safer public-sharing defaults.
- Captures `sts2_steamkit_debug_logs` global setting state so evidence records whether SteamKit debug logging was disabled, or explicitly enabled for sanitized auth diagnostics.
- Captures focused logcat snapshots and a redacted focused logcat. Raw full logcat is omitted by default and only captured when `-IncludeRawLogcat` is passed for local diagnostics.
- Creates `logs/logcat-steam-version-focused-redacted.txt` as the safer default log excerpt for public GitHub issue sharing. The generated file includes a warning header because this is best-effort pattern-based redaction for common credentials, tokens, account/username fields, serial-like fields, email addresses, and local user paths, not a guarantee that every identifier is removed.
- Writes `diagnostics/logcat-redaction-summary.txt` with focused-line and changed-line counts so reviewers can see whether best-effort redaction changed the generated public-log artifact.
- Writes `diagnostics/launcher-diagnostics-index.txt` to list available launcher diagnostics reports without automatically copying full report contents.
- External diagnostics indexing must run through the quoted device-shell helper so `find` is scoped to `/storage/emulated/0/Android/data/<package>/files/diagnostics`; do not call `adb shell sh -c find ...` as split arguments, because that can run `find` from device root and collect a broad root listing.
- Captures coarse app game/cache directory listings through `run-as`.
- Captures app-private Steam branch markers, including the latest branch availability result from app info.
- Captures bounded non-public branch cache tree and cache-size snapshots.
- Captures non-secret local/cloud pre-Push backup filename listings and counts from external backup storage.
- Copies `steam_branch.txt` marker files from app storage when present.
- Copies `last_game_branch_switch.txt` branch-switch safety evidence when present.
- Copies `last_manual_cloud_pull.txt` Pull-after-switch safety evidence when present.
- Copies `last_manual_cloud_push.txt` successful Push evidence when present.
- Copies `last_manual_cloud_push_blocked.txt` blocked Push evidence when present.
- Copies `last_game_version_cache_cleanup.txt` cleanup evidence when present.
- Copies `last_game_version_redownload.txt` selected-version redownload evidence when present.
- Writes `branch-markers/marker-evidence-status.txt` so missing or empty optional marker files are explicit and cannot be mistaken for completed refresh, redownload, Pull, Push, or blocked-Push evidence.
- Avoids shared preferences and credential-bearing files.
- Normalizes local evidence-folder paths across Windows and PowerShell Core.

Command:

```powershell
.\scripts\capture-steam-version-selection-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>"
```

To include raw full logcat for local-only diagnostics:

```powershell
.\scripts\capture-steam-version-selection-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>" `
  -IncludeRawLogcat
```

With explicit package and device serial:

```powershell
.\scripts\capture-steam-version-selection-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>" `
  -PackageName "com.sts2launcher.overhaul.fork.dev" `
  -DeviceSerial "<adb-serial>" `
  -AdbPath "C:\path\to\platform-tools\adb.exe"
```

Captured files:

```text
logs/adb-devices.txt
ARTIFACT_HYGIENE.txt
PUBLIC_SHARE_MANIFEST.txt
diagnostics/steamkit-debug-log-setting.txt
diagnostics/logcat-redaction-summary.txt
diagnostics/launcher-diagnostics-index.txt
logs/logcat-full.txt (placeholder by default; raw only with -IncludeRawLogcat)
logs/logcat-steam-version-focused.txt
logs/logcat-steam-version-focused-redacted.txt
diagnostics/app-game-cache-listing.txt
diagnostics/game-version-cache-tree.txt
diagnostics/game-version-cache-sizes.txt
backup-evidence/pre-push-backup-list.txt
backup-evidence/pre-push-backup-counts.txt
branch-markers/steam-branch-marker-list.txt
branch-markers/<copied-marker-files>
branch-markers/last_game_branch_switch.txt
branch-markers/last_manual_cloud_pull.txt
branch-markers/last_manual_cloud_push.txt
branch-markers/last_manual_cloud_push_blocked.txt
branch-markers/last_game_version_cache_cleanup.txt
branch-markers/last_game_version_redownload.txt
branch-markers/last_steam_branch_availability.txt
branch-markers/marker-evidence-status.txt
capture-summary.txt
```

## Capture beta branch integrity evidence

Purpose:

- Captures app-private `steam_branch.txt` markers for all installed branch caches.
- Names both public/default and selected branch marker paths in `beta-integrity-summary.txt` when present.
- Embeds bounded public/default and selected branch depot manifest rows in `beta-integrity-summary.txt`.
- Captures `last_steam_branch_availability.txt` and summarizes selected-branch visibility, Windows depot manifest count, visible-branch count, and whether the marker matches the investigated branch.
- Captures `last_game_version_redownload.txt` and summarizes whether the clean redownload marker matches the investigated branch and cleared the selected game/download-state directories.
- Captures best-effort focused logcat lines for selected branch routing, marker readiness, manifest provenance, fallback, and public-inherited evidence.
- Writes a public-sharing warning into `beta-integrity-summary.txt`; manually review the summary, focused logcat, branch markers, cache tree, and inventory paths before posting.
- Builds a public/default file inventory from `files/game`.
- Captures a bounded public/default cache tree for stale-cache and fallback comparison.
- Builds a selected beta file inventory from the selected branch cache.
- Captures a bounded selected-branch cache tree for stale-cache and fallback investigation.
- Records file size and SHA-256 for each installed file.
- Compares public/default files against the selected beta cache.
- Summarizes identical, different, public-only, selected-only, and art/bundle-like file differences.
- Writes `inventories/public-vs-<branch>-key-assets.tsv` for focused PCK, art, audio, data, font, and bundle hash comparison.
- Embeds bounded changed key-asset rows in `beta-integrity-summary.txt`.
- Writes a conservative `Classification:` line in `beta-integrity-summary.txt` using branch-availability evidence, clean-redownload proof, marker counters, and inventory differences.
- Writes `Evidence readiness:` and `Evidence missing/weak:` lines so the summary states whether the package is ready for branch-availability or manifest/cache/art classification.
- Gates manifest/cache/art classifications on `last_game_version_redownload.txt` proving the investigated branch was redownloaded and its selected game/download-state directories were cleared.
- Writes the classification input metrics into `beta-integrity-summary.txt` so the final cause label can be audited without reopening the full inventory files.
- Helps classify mixed beta/public behavior as Steam-served partial branch content, stale branch-cache files, launcher fallback, or possible runtime remote/config behavior.

Runtime checklist: `docs/steam-beta-integrity-runtime-checklist.md`

Review command:

```powershell
.\scripts\review-beta-integrity-summary.ps1 `
  -SummaryPath "artifacts\android\steam-beta-integrity-<timestamp>\beta-integrity-summary.txt"
```

Use `-FailOnNotReady` when CI or a release-gate checklist should fail if `Evidence readiness:` is not ready. Exit code `2` means the evidence package is not ready for final classification, not that the parser crashed. Exit code `3` means the evidence is otherwise ready but the public-sharing warning is missing. The review output also reports whether the beta-integrity public-sharing warning is present. The capture helper accepts `-ReviewSummary` and `-FailOnNotReady` to run the same review immediately after writing `beta-integrity-summary.txt`.

Command:

```powershell
.\scripts\capture-steam-beta-integrity-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-beta-integrity-<timestamp>" `
  -Branch "public-beta" `
  -ReviewSummary
```

With explicit package and device serial:

```powershell
.\scripts\capture-steam-beta-integrity-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-beta-integrity-<timestamp>" `
  -PackageName "com.sts2launcher.overhaul.fork.dev" `
  -Branch "public-beta" `
  -DeviceSerial "<adb-serial>" `
  -AdbPath "C:\path\to\platform-tools\adb.exe"
```

Captured files:

```text
branch-markers/steam-branch-marker-list.txt
branch-markers/last_steam_branch_availability.txt
branch-markers/<copied-marker-files>
logs/beta-integrity-logcat-focused.txt
inventories/public-files.tsv
inventories/public-cache-tree.txt
inventories/<branch>-files.tsv
inventories/<branch>-cache-tree.txt
inventories/public-vs-<branch>-comparison.txt
inventories/public-vs-<branch>-key-assets.tsv
beta-integrity-summary.txt
```

Interpretation:

- `manifestSource=selected` means the selected branch supplied that depot manifest.
- `manifestSource=public-inherited` means Steam exposed no explicit selected-branch manifest for that depot and the launcher intentionally used the public manifest as inherited branch content.
- `Changed key asset rows` in the beta-integrity summary highlights bounded public-vs-selected asset hash differences for review.
- Public-identical and branch-specific depots in the same marker are evidence of a partial Steam branch.
- File inventory differences show whether the installed beta cache actually differs from public for PCK, art, audio, JSON, font, or other bundle-like files.
- Treat an `inconclusive` summary classification as a requirement to clean-redownload, capture stronger marker evidence, or inspect runtime logs before claiming a cause. Strong manifest/cache/art classifications require clean-redownload proof for the investigated branch.
- Treat `Evidence readiness: not ready for final classification` as a release blocker until the missing/weak evidence line is resolved or explicitly accepted as a blocker note.
- Treat the public-sharing warning as part of the evidence contract: filtered logs and marker paths are still not automatically safe to publish.
- If a clean-redownloaded beta slot still has public-inherited depot markers and public-identical art hashes, the likely cause is Steam-served partial branch content.
- If marker evidence says selected manifests differ but files remain stale or public-only, investigate cache cleanup and download replacement.

## Safe validation sequence

Use the tools in this order:

1. Run the static audit only when you want a static guardrail check.
2. Create an evidence folder before touching device state.
3. Review `docs/steam-version-selection-release-readiness.md` and decide which gates this run can prove.
4. Follow `docs/steam-version-selection-runbook.md`.
5. Capture device evidence after each meaningful phase.
6. Run beta-integrity capture after a clean selected-branch redownload if public-beta appears mixed or art assets look wrong.
7. Fill `evidence.md` as results are observed.
8. Do not perform manual Push after a branch switch until Pull, local-save existence, backup permission, local pre-Push backup, and cloud pre-Push backup evidence are captured.

## Artifact hygiene

- Do not store Steam credentials.
- Do not store refresh tokens.
- Do not copy shared preferences.
- Keep SteamKit debug logging disabled by default; use `adb shell settings put global sts2_steamkit_debug_logs 1` only for focused sanitized auth diagnostics and reset it to `0` before routine evidence capture.
- Prefer scrubbed summaries when sharing evidence publicly.
- Prefer `logs/logcat-steam-version-focused-redacted.txt` over raw logcat when attaching evidence publicly, but review it manually before posting.
- Treat full launcher diagnostics and startup-recovery diagnostics reports as manual attachments; review/redact them before sharing because they can contain account names, local paths, device details, and log excerpts. These reports include a public-sharing warning, but that warning is not a substitute for manual review.
- Treat copied raw error logs, ad hoc `logcat-*` captures, and raw focused startup extracts as local-only unless they are manually redacted into a separate reviewed artifact.
- The launcher support UI labels raw-log copy as review-before-sharing.
- The startup recovery UI labels raw-log copy as review-before-sharing because raw logs can contain identifying data.
- Keep raw logs local if they contain account-identifying paths, usernames, or device identifiers.
- The static audit keeps login portal support/docs contracts split across login validation docs, portal evidence docs, and validation-matrix/signoff boundary modules so evidence wording changes stay scoped.

## Credential providers versus local credential handoff

Android/Samsung/password-manager support targets the integrated native Android credential panel. The launcher no longer exposes the native `USE ANDROID AUTOFILL` handoff dialog as the user-facing path, and it must not create a launcher-owned password store or inject Steam passwords.

Local credential handoff files are developer-only automation aids for repeatable test runs. Do not describe them as Autofill, do not include them in evidence bundles, and do not copy them into public artifacts.
