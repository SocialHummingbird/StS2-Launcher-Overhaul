function Add-SteamVersionSelectionNativeRoutingChecks {
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
        "android\src\com\game\sts2launcher\SteamBranchInfo.java" `
        "centralizes native selected-branch guidance and branch cache directory naming" `
        @(
            "selectorHelpText",
            "installSlotKind",
            "installSlotDirectory",
            "gameDirectory",
            "stateDirectoryName",
            "stableBranchHash",
            "BETA_BRANCH",
            "Default/public Steam branch",
            "Private/password-protected branches may be inaccessible",
            "Failed downloads do not change Steam Cloud saves"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\LauncherActivity.java" `
        "logs selected branch guidance before native routing" `
        @(
            "logSelectedBranchBeforeRouting",
            "Selected Steam branch before routing",
            "Selected Steam branch note before routing",
            "SteamBranchInfo\.selectorHelpText",
            "Selected game version slot kind before routing",
            "Selected game version slot directory before routing",
            "SteamBranchInfo\.installSlotDirectory",
            "SteamBranchInfo\.gameDirectory",
            "hasInstallSlotProvenance",
            "Resolved game directory before routing",
            "Steam branch marker install slot kind before routing",
            "Steam branch marker expected install slot kind before routing",
            "Steam branch marker install slot directory before routing",
            "Steam branch marker expected install slot directory before routing",
            "Steam branch marker has matching install slot provenance before routing",
            "Steam branch marker depot manifest entries before routing",
            "Steam branch marker ready before routing"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
        "shows branch marker readiness in native fallback diagnostics" `
        @(
            "steam_branch\.txt",
            "Selected Steam branch note",
            "SteamBranchInfo\.selectorHelpText",
            "SteamBranchInfo\.gameDirectory",
            "branch marker",
            "hasInstallSlotProvenance",
            "Steam branch marker install slot kind",
            "Steam branch marker expected install slot kind",
            "Steam branch marker install slot directory",
            "Steam branch marker expected install slot directory",
            "Steam branch marker has matching install slot provenance",
            "depotManifestCount"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
        "keeps native fallback recovery controls visible before verbose diagnostics" `
        @(
            "boolean landscape",
            "boolean compactActionRows",
            "LinearLayout actions",
            "actions\.setOrientation\(compactActionRows \? LinearLayout\.VERTICAL : \(landscape \? LinearLayout\.HORIZONTAL : LinearLayout\.VERTICAL\)\)",
            "createFallbackActionRow",
            "addFallbackActionRow",
            "useCompactFallbackActionRows",
            "width < dp\(900\)",
            "Copy diagnostics",
            "Restart launcher",
            "Clear files",
            "Show diagnostics",
            "Hide diagnostics",
            "styleActionButton",
            "setMinHeight\(dp\(48\)\)",
            "GradientDrawable",
            "addActionButton",
            "root\.addView\(actions, actionsParams\)",
            "createDiagnosticsView",
            "diagnosticsView\.setVisibility\(View\.GONE\)",
            "diagnosticsView\.setVisibility\(show \? View\.VISIBLE : View\.GONE\)",
            "copyDiagnostics\(diagnosticsText\)",
            "root\.addView\(diagnosticsView"
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
