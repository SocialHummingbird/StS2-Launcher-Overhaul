function Add-SteamVersionSelectionNativeRoutingStartupProvenanceChecks {
    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "routes native startup through selected branch readiness checks" `
        @(
            "Selected Steam branch",
            "Selected Steam branch note",
            "SteamBranchInfo\.selectorHelpText",
            "SteamBranchInfo\.gameDirectory",
            "Resolved startup game directory",
            "steam_branch\.txt",
            "hasInstallSlotProvenance",
            "Steam branch marker install slot kind",
            "Steam branch marker expected install slot kind",
            "Steam branch marker install slot directory",
            "Steam branch marker expected install slot directory",
            "Steam branch marker has matching install slot provenance",
            "depotManifestCount"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "requires selected branch provenance before consuming native game launch requests" `
        @(
            "boolean branchMarkerReady = isBranchMarkerReady\(selectedBranch\)",
            "boolean gamePckReady = isGamePckReady\(\)",
            "branchMarkerReady && gamePckReady && consumeGameLaunchRequest\(\)",
            "Blocking selected game version startup because branch marker provenance is missing or mismatched",
            "returning to launcher instead of falling back to another branch"
        )
}
