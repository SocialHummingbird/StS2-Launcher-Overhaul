function Add-SteamVersionSelectionReleaseDocsChecks {
    Add-Check `
        "docs\steam-version-selection-validation.md" `
        "keeps release blockers explicit" `
        @(
            "Release-readiness blockers",
            "Current release decision: not release-ready",
            "active install slot",
            "selected-cache-preserved aggregate",
            "Manual Pull completed before Push",
            "Current important Android local save evidence count",
            "Baseline manual Push prerequisites satisfied",
            "beta password",
            "save compatibility",
            "steam-version-selection-release-readiness\.md",
            "steam-version-selection-runbook\.md"
        )

    Add-Check `
        "docs\steam-version-selection-release-readiness.md" `
        "tracks implementation status versus release evidence requirements" `
        @(
            "Current status",
            "Evidence required before release-candidate signoff",
            "Known release blockers",
            "Release rule",
            "Public/default branch",
            "Branch selector",
            "No silent public fallback",
            "Side-by-side storage",
            "Native startup routing",
            "Steam Cloud safety",
            "Autofill",
            "Artifact hygiene",
            "ARM64",
            "Pull-before-Push",
            "not release-candidate signed off"
        )

    Add-Check `
        "docs\steam-version-selection-architecture.md" `
        "documents branch storage, readiness, cache, startup, and Push safety invariants" `
        @(
            "Steam branch dropdown",
            "SelectorInstallSlotHelpText",
            "active install slot",
            "Ready and download-required launcher status",
            "SteamBranchInfo\.selectorHelpText",
            "non-interactive helper text",
            "game_versions",
            "steam_branch\.txt",
            "Readiness rules",
            "Startup routing rules",
            "Selected game version note",
            "Selected game version slot kind",
            "Selected game version slot directory",
            "selected branch note",
            "Branch switch marker filename",
            "Cache cleanup rules",
            "Steam Cloud Push safety",
            "Release blockers"
        )

    Add-Check `
        "docs\steam-version-selection-completion-audit.md" `
        "maps goal requirements to static and runtime evidence" `
        @(
            "Completion rule",
            "steam-version-selection-release-readiness\.md",
            "Requirement audit",
            "selected-version note",
            "account-visible Steam branch dropdown",
            "public/default always available",
            "Manual Pull completed before Push",
            "Current important Android local save evidence count",
            "Baseline manual Push prerequisites satisfied",
            "Manual Push evidence marker filename",
            "last_manual_cloud_push_blocked\.txt",
            "Manual Push completed after branch switch for selected version with backup evidence",
            "full local pre-Push coverage",
            "full cloud pre-Push coverage",
            "marker backup counts/timestamps",
            "fail-before-upload",
            "Runtime evidence still required",
            "Do not mark Steam beta/version selection release-ready yet"
        )

    Add-ForbiddenCheck `
        "docs\steam-version-selection-completion-audit.md" `
        "does not describe the old manual selector model" `
        @(
            "manual Steam branch entry",
            "no arbitrary discovery"
        )

    Add-Check `
        "docs\steam-version-selection-runbook.md" `
        "orders destructive cloud validation behind Pull and backup gates" `
        @(
            "steam-version-selection-release-readiness\.md",
            "Cloud Pull gate",
            "Backup permission gate",
            "Pre-Push backup evidence",
            "Manual Push smoke test",
            "Optional auth diagnostics",
            "sts2_steamkit_debug_logs",
            "SteamKit debug logs sanitized for credentials/tokens"
        )

    Add-Check `
        "docs\steam-version-selection-user-guide.md" `
        "keeps tester-facing support boundaries and cloud safety rules visible" `
        @(
            "implemented for validation",
            "steam-version-selection-release-readiness\.md",
            "What is not supported yet",
            "Refresh Game Versions",
            "Steam login credential entry",
            "Android credential provider model",
            "Godot login field credential metadata configured",
            "Godot fields are native Android Autofill targets",
            "Password-manager suggestions device validated",
            "Native credential handoff result TTL seconds",
            "Android credential provider capability boundary",
            "blocked states",
            "Steam beta password entry",
            "Selected game version note",
            "Selected game version slot kind",
            "Selected game version slot directory",
            "wrapped helper text",
            "active install slot",
            "Selected Steam branch note before routing",
            "Selected branch note",
            "Branch switch marker filename",
            "Branch switch marker path",
            "Branch switch marker UTC",
            "Branch switch marker UTC parseable",
            "Branch switch previous branch",
            "Branch switch selected branch",
            "Branch switch selected version",
            "Branch switch selected version slot kind",
            "Branch switch selected version slot directory",
            "Branch switch selected branch matches current selected branch",
            "Branch switch selected branch note",
            "Branch switch local backup forced",
            "Branch switch manual Push requires backup storage",
            "Branch switch warning acknowledged",
            "Branch switch non-public warning acknowledged",
            "Branch switch marker has required safety evidence",
            "Branch switch marker has required safety evidence for selected branch",
            "Branch-switch manual Push prerequisites satisfied",
            "Pre-Push local backup evidence count",
            "Pre-Push cloud backup evidence count",
            "Latest pre-Push local backup UTC",
            "Latest pre-Push cloud backup UTC",
            "Pre-Push local backup evidence after branch switch",
            "Pre-Push cloud backup evidence after branch switch",
            "Branch-switch pre-Push backup evidence satisfied",
            "Manual Pull evidence marker filename",
            "last_manual_cloud_push\.txt",
            "last_manual_cloud_push_blocked\.txt",
            "Manual Pull evidence marker filename",
            "Manual Pull evidence marker path",
            "Manual Pull evidence UTC",
            "Manual Pull evidence UTC parseable",
            "Manual Pull evidence selected branch",
            "Manual Pull evidence selected version",
            "Manual Pull evidence selected version slot kind",
            "Manual Pull evidence selected version slot directory",
            "Manual Pull completion flag recorded",
            "Manual Pull evidence is after branch switch",
            "Manual Pull evidence matches selected branch",
            "Manual Pull completed after branch switch",
            "Manual Push evidence marker filename",
            "Manual Push evidence marker path",
            "Manual Push evidence UTC",
            "Latest manual Push evidence outcome",
            "Latest manual Push evidence UTC",
            "Latest manual Push evidence selected branch",
            "Latest manual Push evidence selected version",
            "Latest manual Push evidence selected version slot kind",
            "Latest manual Push evidence selected version slot directory",
            "Latest manual Push evidence reason",
            "Manual Push evidence UTC parseable",
            "Manual Push evidence selected branch",
            "Manual Push evidence selected version",
            "Manual Push evidence selected version slot kind",
            "Manual Push evidence selected version slot directory",
            "Manual Push evidence recorded local backup count",
            "Manual Push evidence recorded cloud backup count",
            "Manual Push evidence recorded latest local backup UTC",
            "Manual Push evidence recorded latest cloud backup UTC",
            "Manual Push completion flag recorded",
            "Manual Push evidence is after branch switch",
            "Manual Push evidence matches selected branch",
            "Manual Push evidence recorded pre-Push backup evidence satisfied",
            "Manual Push completed after branch switch for selected version with backup evidence",
            "Manual Push blocked evidence marker filename",
            "Manual Push blocked evidence marker path",
            "Manual Push blocked evidence UTC",
            "Manual Push blocked evidence UTC parseable",
            "Manual Push blocked evidence selected branch",
            "Manual Push blocked evidence selected version",
            "Manual Push blocked evidence selected version slot kind",
            "Manual Push blocked evidence selected version slot directory",
            "Manual Push blocked evidence matches selected branch",
            "Manual Push blocked evidence reason",
            "Manual Push blocked before upload evidence recorded",
            "Game version cache cleanup marker selected cache preserved where applicable",
            "Pull from Cloud first",
            "steam_branch\.txt",
            "selectedBranchManifest",
            "publicManifest",
            "public-inherited",
            "manifestRequestBranch=public",
            "branch-integrity provenance",
            "Branch marker depots inherited from public",
            "Branch marker depots missing selected branch manifest",
            "Branch marker depot manifest rows",
            "Classification:",
            "Evidence readiness: not ready for final classification",
            "Clean redownload matches investigated branch: true",
            "Clean redownload selected directories cleared: true",
            "Public-vs-beta key asset comparison captured",
            "Steam credentials",
            "refresh tokens",
            "shared preferences",
            "device identifiers",
            "Release readiness"
        )

    Add-Check `
        "docs\steam-version-selection-save-compatibility.md" `
        "tracks public/beta save compatibility and Push safety evidence" `
        @(
            "Compatibility matrix",
            "Push safety matrix",
            "Public/default",
            "Beta",
            "Pull from Cloud after the branch switch",
            "last_manual_cloud_push\.txt",
            "successful Push marker evidence"
        )

    Add-Check `
        "OVERHAUL_ROADMAP.md" `
        "tracks Steam version selection as an active hardening phase" `
        @(
            "Steam version selection and branch cache hardening",
            "Refresh Game Versions",
            "Login credential providers",
            "SteamKit debug logs disabled by default",
            "sts2_steamkit_debug_logs=1",
            "branch marker/provenance",
            "wrapped selector guidance",
            "managed/native guidance parity",
            "missing/private/password",
            "Pull-after-switch",
            "last_manual_cloud_push\.txt",
            "aggregate successful post-switch Push evidence"
        )

    Add-Check `
        "docs\android-release-validation.md" `
        "keeps release signoff gated on branch/version evidence" `
        @(
            "Steam version-selection validation",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "steam_branch\.txt",
            "selected-PCK startup routing",
            "backup storage permission"
        )

}
