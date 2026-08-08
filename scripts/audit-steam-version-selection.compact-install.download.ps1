function Add-SteamVersionSelectionCompactInstallDownloadChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.ActionButton.cs" `
        "renders compact install primary actions as structured title/detail labels" `
        @(
            "CompactDownloadActionLabels",
            "CompactButtonDetailLabelSpec",
            "CompactDownloadActionBodyName",
            "CompactDownloadActionTitleName",
            "CompactDownloadActionDetailName",
            "CompactDownloadActionTitleFontSize",
            "CompactDownloadActionDetailFontSize",
            "CompactDownloadActionHorizontalMargin",
            "CompactDownloadActionVerticalMargin",
            "SetCompactDownloadButtonText",
            "CompactButtonDetailLabels\.Apply"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
        "keeps compact install primary-action copy as structured title/detail text" `
        @(
            "CompactDownloadButtonTitleDetail",
            "CompactDownloadButtonText",
            "`"DOWNLOAD SELECTED VERSION`"",
            "`"Download Version`"",
            "`"Local files only`"",
            "`"REDOWNLOAD SELECTED VERSION`"",
            "`"Redownload Version`"",
            "`"Rebuild local files`"",
            "`"Retry Download`"",
            "`"Downloading\.\.\.`"",
            "`"Steam files`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.cs" `
        "promotes compact download progress controls directly under the active primary action" `
        @(
            "MoveCompactProgressControlsNearPrimaryAction",
            "MoveChild\(_progressLabel, _downloadButton\.GetIndex\(\) \+ 1\)",
            "MoveChild\(_progressBar, _progressLabel\.GetIndex\(\) \+ 1\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
        "keeps compact download progress copy concise and bounded" `
        @(
            "CompactDownloadProgressButtonText",
            "CompactDownloadProgressText",
            "CompactDownloadProgressDetail",
            "NormalizeCompactProgressText",
            "Downloading selected version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Download.cs" `
        "constructs compact download progress controls with readable mobile sizing" `
        @(
            "new StyledProgressBar\(scale, compact\)",
            "BuildProgressLabel",
            "label\.AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "label\.ClipText = compact",
            "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "label\.CustomMinimumSize = new Vector2",
            "compact\s*\?\s*LauncherSectionMetrics\.SecondaryButtonFontSize",
            "compact\s*\?\s*LauncherComponentTheme\.CyanAccent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Progress.cs" `
        "updates compact download progress state next to the disabled primary action" `
        @(
            "_progressLabel\.Text = _compact \? CompactDownloadProgressText\(text\) : text",
            "SetCompactDownloadButtonText\(_downloadButton, CompactDownloadProgressButtonText\(\)\)",
            "_compactSelectedVersionPanel\.Disabled = true",
            "_compactSelectedVersionPanel\.Disabled = false"
        )
}
