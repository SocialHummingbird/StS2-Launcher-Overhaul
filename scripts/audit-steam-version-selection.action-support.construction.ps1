function Add-SteamVersionSelectionActionSupportConstructionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.cs" `
        "orchestrates support toggle and tool button construction through focused helpers" `
        @(
            "private SupportControls BuildSupportControls",
            "BuildSupportToggle",
            "AddChild\(_supportGroup\)",
            "return new SupportControls",
            "BuildUpdateSupportButton",
            "BuildRefreshVersionsSupportButton",
            "BuildRedownloadSupportButton",
            "BuildClearCachedVersionsSupportButton",
            "BuildDiagnosticsSupportButton",
            "BuildShowLastErrorSupportButton",
            "BuildCopyRawLogSupportButton",
            "SupportToggleText\(\)",
            "ToggleSupportOptions",
            "SetCompactActionButtonText\(supportToggle, supportToggle\.Text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Tools.cs" `
        "keeps compact update version and cache tool labels in focused button builders" `
        @(
            "AddCompactSupportToolButton",
            "`"Check Files`"",
            "`"Game Versions`"",
            "`"Repair Files`"",
            "`"Free Space`"",
            "`"Updates`"",
            "`"Refresh list`"",
            "`"Rebuild game`"",
            "`"Old versions`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
        "keeps compact report problem and launcher-log support labels isolated" `
        @(
            "AddCompactSupportToolButton",
            "`"Help Report`"",
            "`"Last Problem`"",
            "`"Copy Log`"",
            "`"Share details`"",
            "`"Open details`"",
            "`"Review first`"",
            "`"Create Help Report`"",
            "`"Show Last Problem`"",
            "`"Copy Launcher Log \(Review First\)`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Primary.cs" `
        "keeps compact safe start as a support-grid action with Cloud-off detail" `
        @(
            "AddCompactSupportToolButton",
            "supportToolsParent",
            "`"Safe Start`"",
            "`"Cloud off`""
        )
}
