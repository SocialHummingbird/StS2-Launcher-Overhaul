function Add-SteamVersionSelectionNativeRoutingBranchInfoChecks {
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
}
