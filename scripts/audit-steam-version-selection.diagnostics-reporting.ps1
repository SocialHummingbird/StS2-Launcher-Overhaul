function Add-SteamVersionSelectionDiagnosticsReportingChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Diagnostics.cs" `
        "warns before launcher logs are copied for sharing" `
        @(
            "Public sharing warning",
            "review and redact this launcher log before posting publicly",
            "Review/redact before public posting",
            "Launcher log copied"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Reports.cs" `
        "warns after startup recovery launcher logs are copied" `
        @(
            "Launcher log copied to clipboard",
            "Review/redact before public posting"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
        "keeps the diagnostics report shell and public-sharing warning" `
        @(
            "Public sharing warning",
            "review and redact this diagnostics report before posting publicly",
            "AppendLauncherPreferences",
            "AppendFullReportDiagnostics"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportLauncherPreferences.cs" `
        "reports selected branch, credential, portal UX, and selected branch marker state" `
        @(
            "Selected game branch",
            "Selected game branch preference key",
            "Selected game branch source",
            "Selected game branch selection kind",
            "Selected game version note",
            "Steam branch selector mode",
            "Steam beta password entry supported",
            "Android credential provider model",
            "Native credential panel inline status configured",
            "Native credential panel keyboard-safe layout configured",
            "Native credential panel touch-target layout configured",
            "Native credential panel large field targets supported",
            "Native credential panel requests both Autofill fields",
            "Native credential panel focus Autofill requests supported",
            "Native credential panel task-led buttons supported",
            "Native credential panel responsive action rows supported",
            "Native credential panel orientation reflow supported",
            "Native credential panel short-height copy supported",
            "Native credential panel short-height reflow supported",
            "Native credential panel IME height reflow supported",
            "Native credential panel password visibility toggle supported",
            "Steam Guard one-shot code guidance supported",
            "Steam Guard alphanumeric keyboard supported",
            "Failed-login retry guidance supported",
            "Context-specific login recovery guidance supported",
            "Godot login field credential metadata configured",
            "Android keyboard credential hints configured",
            "Godot fields are native Android Autofill targets",
            "Password-manager suggestions device validated",
            "Native credential handoff popup supported",
            "Launcher stores Steam password for credential providers",
            "Native credential handoff result TTL seconds",
            "Android credential provider implementation note",
            "Android credential provider capability boundary",
            "Launcher portal UX model",
            "AppendLauncherPortalUxFeatureReports",
            "Launcher portal UX implementation note",
            "Launcher portal UX validation boundary",
            "SteamKit debug logs opt-in enabled",
            "SteamKit debug logs sanitized for credentials/tokens",
            "Selected game version slot kind",
            "Selected game version slot directory",
            "Selected game branch marker install slot kind",
            "Selected game branch marker install slot directory",
            "Selected game branch marker expected install slot kind",
            "Selected game branch marker expected install slot directory",
            "Selected game branch marker has matching install slot provenance",
            "Selected game branch marker has depot manifests",
            "Selected game branch marker depot manifest entries",
            "Selected game branch marker ready",
            "AppendBranchAvailability",
            "AppendBranchSwitchSafety",
            "AppendCachedGameVersions"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.cs" `
        "reports selected Steam branch availability state" `
        @(
            "Steam branch availability marker filename",
            "Steam branch availability marker path",
            "Steam branch availability marker present",
            "Steam branch availability UTC",
            "Steam branch availability selected branch",
            "Steam branch availability matches current selected branch",
            "Steam branch availability selected branch visibility",
            "Steam branch availability selected branch Windows depot manifests",
            "Steam branch availability selected branch downloadable",
            "Steam branch availability selected branch problem",
            "Steam branch availability visible branch count",
            "Steam branch availability visible branches"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.Marker.cs" `
        "parses selected Steam branch availability marker state" `
        @(
            "ReadBranchAvailabilityMarkerValue",
            "ReadBranchAvailabilityMarkerValues",
            "BranchAvailabilityMarkerMatchesSelectedBranch",
            "BranchAvailabilitySelectedBranchDownloadable",
            "BranchAvailabilitySelectedBranchProblem",
            "BranchAvailabilitySelectedBranchManifestCount",
            "BranchAvailabilitySelectedBranchPasswordProtected",
            "SteamBranchAvailabilityMarkerFile\.ReadValue",
            "SteamBranchAvailabilityMarkerFile\.ReadValues",
            "SteamBranchAvailabilityMarkerFile\.ReadVisibleRows",
            "SteamBranchAvailabilityMarkerFile\.Exists",
            "LauncherMarkerFile\.ReadFailedValue",
            "BranchMatches",
            "PasswordProtected",
            "selected branch is password-protected"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.Marker.cs" `
        "does not duplicate Steam branch availability visible-row parsing" `
        @(
            'IndexOf\(" \["',
            "BranchAvailabilityMarkerValueMatchesBranch",
            "PasswordRequiredTrueToken",
            "LauncherMarkerFile\.ReadJoinedValues",
            "LauncherMarkerFile\.ReadValues"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportCachedGameVersions.cs" `
        "reports cached game-version cleanup and redownload evidence" `
        @(
            "Current selected branch for version marker comparison",
            "Game version cache cleanup marker filename",
            "Game version cache cleanup marker path",
            "Game version cache cleanup marker present",
            "Game version cache cleanup marker UTC",
            "Game version cache cleanup marker UTC parseable",
            "Game version cache cleanup marker selected branch",
            "Game version cache cleanup marker matches selected branch",
            "Game version cache cleanup marker selected version",
            "Game version cache cleanup marker selected version slot kind",
            "Game version cache cleanup marker selected version slot directory",
            "Game version cache cleanup marker game_versions present",
            "Game version cache cleanup marker runtime_packs present",
            "Game version cache cleanup marker selected runtime pack directory",
            "Game version cache cleanup marker selected runtime pack present before cleanup",
            "Game version cache cleanup marker removed count",
            "Game version cache cleanup marker removed runtime pack count",
            "Game version cache cleanup marker selected cache preserved where applicable",
            "Game version cache cleanup marker selected runtime pack preserved where applicable",
            "Game version redownload marker filename",
            "Game version redownload marker path",
            "Game version redownload marker present",
            "Game version redownload marker UTC parseable",
            "Game version redownload marker selected branch",
            "Game version redownload marker matches selected branch",
            "Game version redownload marker selected version",
            "Game version redownload marker selected version slot kind",
            "Game version redownload marker selected version slot directory",
            "Game version redownload marker game directory existed before delete",
            "Game version redownload marker game directory exists after delete",
            "Game version redownload marker download state directory existed before delete",
            "Game version redownload marker download state directory exists after delete",
            "Game version redownload marker selected directories cleared",
            "branchMarkerExpectedInstallSlotKind",
            "branchMarkerExpectedInstallSlotDirectory",
            "branchMarkerMatchingInstallSlotProvenance",
            "branchMarkerDepotsMatchingPublic",
            "branchMarkerDepotsDifferingFromPublic",
            "branchMarkerDepotsInheritedFromPublic",
            "branchMarkerDepotsMissingSelectedManifest",
            "branchMarkerReady",
            "Cached non-public game versions"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.cs" `
        "orchestrates branch-switch cloud-sync and backup safety diagnostics" `
        @(
            "AppendBranchSwitchSafety",
            "selectedBranch = LauncherPreferences\.ReadGameBranch\(\)",
            "importantSaveEvidenceCount",
            "AppendBranchSwitchMarkerEvidence",
            "AppendManualPullEvidence",
            "AppendCurrentLocalSaveEvidence",
            "AppendSaveOriginEvidence",
            "AppendManualPushEvidence",
            "AppendManualPushBlockedEvidence",
            "AppendBranchSwitchBackupEvidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Marker.cs" `
        "reports branch-switch marker safety state" `
        @(
            "AppendBranchSwitchMarkerEvidence",
            "Branch switch marker filename",
            "Branch switch marker path",
            "Branch switch marker present",
            "Branch switch marker UTC",
            "Branch switch marker UTC parseable",
            "Branch switch previous branch",
            "Branch switch selected branch",
            "Branch switch selected branch selection kind",
            "Branch switch selector mode",
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
            "Push requires backup storage after branch switch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Pull.cs" `
        "reports Manual Pull evidence for branch-switch Push safety" `
        @(
            "AppendManualPullEvidence",
            "Manual Pull evidence marker filename",
            "Manual Pull evidence marker path",
            "Manual Pull evidence marker present",
            "Manual Pull evidence UTC",
            "Manual Pull evidence UTC parseable",
            "Manual Pull evidence selected branch",
            "Manual Pull evidence selected branch selection kind",
            "Manual Pull evidence selector mode",
            "Manual Pull evidence selected version",
            "Manual Pull evidence selected version slot kind",
            "Manual Pull evidence selected version slot directory",
            "Manual Pull completion flag recorded",
            "Manual Pull completed before Push",
            "Manual Pull evidence is after branch switch",
            "Manual Pull evidence matches selected branch",
            "Manual Pull completed after branch switch for selected version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.SaveOrigin.cs" `
        "reports Android save-origin and current local-save evidence for branch-switch Push safety" `
        @(
            "AppendCurrentLocalSaveEvidence",
            "Current important Android local save evidence count",
            "Current important Android local save evidence present",
            "AppendSaveOriginEvidence",
            "Android save-origin marker filename",
            "Android save-origin marker path",
            "Android save-origin marker present",
            "Android save-origin selected runtime slot ID",
            "Android save-origin selected PCK SHA256",
            "Android save-origin selected source sts2\.dll SHA256",
            "Android save-origin selected runtime playable at origin",
            "Android save-origin matches selected branch",
            "Android save-origin selected runtime slot ID matches current runtime",
            "Android save-origin current selected runtime is playable",
            "Android local saves verified for selected branch",
            "Android local saves verified for selected runtime",
            "Baseline manual Push prerequisites satisfied"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Push.cs" `
        "reports Manual Push evidence for branch-switch Push safety" `
        @(
            "AppendManualPushEvidence",
            "Manual Push evidence marker filename",
            "Manual Push evidence marker path",
            "Manual Push evidence marker present",
            "Manual Push evidence UTC",
            "Latest manual Push evidence outcome",
            "Latest manual Push evidence UTC",
            "Latest manual Push evidence selected branch",
            "Latest manual Push evidence selected branch selection kind",
            "Latest manual Push evidence selector mode",
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
            "Manual Push evidence recorded important local save evidence count",
            "Manual Push evidence recorded baseline prerequisites satisfied",
            "Manual Push completion flag recorded",
            "Manual Push evidence is after branch switch",
            "Manual Push evidence matches selected branch",
            "Manual Push evidence recorded pre-Push backup evidence satisfied",
            "Manual Push completed after branch switch for selected version with backup evidence",
            "LatestManualPushEvidenceOutcome"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.BlockedPush.cs" `
        "reports blocked Manual Push evidence for branch-switch Push safety" `
        @(
            "AppendManualPushBlockedEvidence",
            "Manual Push blocked evidence marker filename",
            "Manual Push blocked evidence marker path",
            "Manual Push blocked evidence marker present",
            "Manual Push blocked evidence UTC",
            "Manual Push blocked evidence UTC parseable",
            "Manual Push blocked evidence selected branch",
            "Manual Push blocked evidence selected version",
            "Manual Push blocked evidence selected version slot kind",
            "Manual Push blocked evidence selected version slot directory",
            "Manual Push blocked evidence matches selected branch",
            "Manual Push blocked evidence recorded prerequisites satisfied",
            "Manual Push blocked evidence recorded local backup count",
            "Manual Push blocked evidence recorded cloud backup count",
            "Manual Push blocked evidence recorded latest local backup UTC",
            "Manual Push blocked evidence recorded latest cloud backup UTC",
            "Manual Push blocked evidence recorded important local save evidence count",
            "Manual Push blocked evidence recorded baseline prerequisites satisfied",
            "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
            "Manual Push blocked evidence reason",
            "Manual Push blocked before upload evidence recorded"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Backup.cs" `
        "reports backup storage and pre-Push backup evidence for branch-switch safety" `
        @(
            "AppendBranchSwitchBackupEvidence",
            "Important Android local save evidence count in bounded scan",
            "Important Android local save evidence present",
            "Backup storage permission available",
            "Backup storage directory",
            "Backup storage directory exists",
            "Branch-switch manual Push prerequisites satisfied",
            "Pre-Push local backup evidence count",
            "Pre-Push cloud backup evidence count",
            "Latest pre-Push local backup UTC",
            "Latest pre-Push cloud backup UTC",
            "Pre-Push local backup evidence after branch switch",
            "Pre-Push cloud backup evidence after branch switch",
            "Branch-switch pre-Push backup evidence satisfied"
        )
}
