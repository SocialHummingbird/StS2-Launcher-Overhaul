function Add-SteamVersionSelectionActionSupportLabelChecks {
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
}
