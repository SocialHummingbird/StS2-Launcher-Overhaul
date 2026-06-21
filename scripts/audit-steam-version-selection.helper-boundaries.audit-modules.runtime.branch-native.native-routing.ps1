function Add-SteamVersionSelectionRuntimeNativeRoutingBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.native-routing.ps1" `
        "keeps native selected-branch routing and fallback audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionNativeRoutingChecks",
            "audit-steam-version-selection.native-routing.startup-provenance.ps1",
            "audit-steam-version-selection.native-routing.branch-info.ps1",
            "audit-steam-version-selection.native-routing.fallback.ps1",
            "Add-SteamVersionSelectionNativeRoutingStartupProvenanceChecks",
            "Add-SteamVersionSelectionNativeRoutingBranchInfoChecks",
            "Add-SteamVersionSelectionNativeRoutingFallbackChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.native-routing.startup-provenance.ps1" `
        "keeps native selected-branch readiness and launch-request provenance checks focused" `
        @(
            "function Add-SteamVersionSelectionNativeRoutingStartupProvenanceChecks",
            "GodotApp.java",
            "hasInstallSlotProvenance",
            "requires selected branch provenance before consuming native game launch requests",
            "Blocking selected game version startup because branch marker provenance is missing or mismatched",
            "returning to launcher instead of falling back to another branch"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.native-routing.branch-info.ps1" `
        "keeps native branch-info and launcher pre-routing guidance checks focused" `
        @(
            "function Add-SteamVersionSelectionNativeRoutingBranchInfoChecks",
            "SteamBranchInfo.java",
            "LauncherActivity.java",
            "selectorHelpText",
            "logSelectedBranchBeforeRouting",
            "Steam branch marker ready before routing"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.native-routing.fallback.ps1" `
        "keeps native fallback diagnostics and recovery-control checks focused" `
        @(
            "function Add-SteamVersionSelectionNativeRoutingFallbackChecks",
            "NativeFallbackActivity.java",
            "shows branch marker readiness in native fallback diagnostics",
            "keeps native fallback recovery controls visible before verbose diagnostics",
            "copyDiagnostics"
        )
}
