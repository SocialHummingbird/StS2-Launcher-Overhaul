function Add-SteamVersionSelectionActionSupportChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Foundation.cs" `
        "packs compact recovery and support tools into a responsive grid that becomes full-width on narrow compact viewports" `
        @(
            "BuildSupportFoundation",
            "BuildCompactSupportToolsGrid\(scale, compact, compactStackedActionRows\)",
            "if \(compact\)",
            "supportGroup\.AddChild\(supportToolsGrid\)",
            "supportToolsParent = compact",
            "new SupportFoundation\(supportGroup, supportToolsGrid, supportToolsParent\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Types.cs" `
        "uses typed support construction return values instead of long tuple signatures" `
        @(
            "private readonly struct SupportFoundation",
            "internal VBoxContainer Group",
            "internal GridContainer ToolsGrid",
            "internal Container ToolsParent",
            "private readonly struct SupportControls",
            "internal Button SupportToggle",
            "internal Button UpdateButton",
            "internal Button RefreshVersionsButton",
            "internal Button RedownloadButton",
            "internal Button ClearCachedVersionsButton",
            "internal Button DiagnosticsButton",
            "internal Button ShowLastErrorButton",
            "internal Button CopyRawLogButton"
        )

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
        "src\STS2Mobile\Launcher\Sections\ActionSection.Support.cs" `
        "keeps compact support drawer grid and support toggle copy responsive" `
        @(
            "GridContainer",
            "Columns = compactStackedActionRows \? 1 : 2",
            "`"Fixes & Help`"",
            "`"Hide Fixes`"",
            "`"Repair tools`"",
            "`"Back to play`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Buttons.cs" `
        "keeps base ActionSection hidden button factories and push/pull buttons isolated" `
        @(
            "AddPrimaryHiddenButton",
            "AddSecondaryHiddenButton",
            "AddHiddenButton",
            "new StyledButton",
            "button\.Visible = false",
            "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "AddPushPullButton",
            "LauncherSectionMetrics\.SecondaryButtonHeight"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CompactActionButton.cs" `
        "uses readable fill-width buttons inside the compact support grid" `
        @(
            "AddCompactSupportToolButton",
            "SetCompactActionButtonText",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactActionButtonLabels",
            "CompactActionButtonBodyName",
            "CompactActionButtonTitleName",
            "CompactActionButtonDetailName",
            "CompactButtonDetailLabelSpec\.Default",
            "_compact",
            "CompactSupportToolHeight",
            "CompactSupportToolFontSize",
            "CompactSupportToolText"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
        "keeps dynamic compact version and safety drawer labels synced with structured title/detail button labels" `
        @(
            "SetCompactActionButtonText\(_branchDetailsToggle",
            "SetCompactActionButtonText\(_cloudSafetyToggle"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
        "keeps dynamic compact save-settings drawer label synced with structured title/detail button labels" `
        @(
            "SetCompactActionButtonText\(_cloudOptionsToggle",
            "Backup \{OnOff\(_localBackupEnabled\)\} / Cloud \{OnOff\(_cloudSyncEnabled\)\}"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
        "keeps update button text routed through structured compact button labels" `
        @(
            "SetCompactActionButtonText\(_updateButton, text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
        "keeps compact Save Backup and Cloud Sync option toggles synced with structured title/detail button labels" `
        @(
            "SetCompactActionButtonText\(button, text\)",
            "CompactCloudOptionText",
            "Local safety",
            "Steam saves"
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

function Add-SteamVersionSelectionActionSupportDiagnosticsChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
        "labels launcher support log copy as review-before-sharing" `
        @(
            "Copy Launcher Log \(Review First\)",
            "Create Help Report",
            "Show Last Problem"
        )
}
