function Add-SteamVersionSelectionCompactInstallVersionActionLabelChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.ActionButton.cs" `
        "renders compact version action labels as structured title/detail controls" `
        @(
            "SetCompactVersionActionButtonText",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactVersionActionLabels",
            "CompactVersionActionBodyName",
            "CompactVersionActionTitleName",
            "CompactVersionActionDetailName",
            "CompactButtonDetailLabelSpec\.Default",
            "enabled: false",
            "enabled: true",
            '\$"\{title\}\\n\{detail\}"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
        "maps compact download action copy to local-file-only title/detail labels" `
        @(
            "CompactDownloadButtonText",
            "CompactDownloadButtonTitleDetail",
            "`"Download Version`"",
            "`"Redownload Version`"",
            "`"Retry Download`"",
            "`"Downloading\.\.\.`"",
            "Local files only",
            "Rebuild local files",
            "Steam files"
        )
}
