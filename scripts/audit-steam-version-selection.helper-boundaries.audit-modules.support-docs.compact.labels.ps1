function Add-SteamVersionSelectionSupportDocsCompactLabelBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-labels.ps1" `
        "keeps reusable compact two-line label audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactLabelChecks",
            "CompactButtonDetailLabelSpec.cs",
            "CompactButtonDetailLabels.cs",
            "CompactButtonDetailLabels.Text.cs",
            "CompactButtonDetailLabels.Controls.cs",
            "LoginSection.CompactNativeButton.cs"
        )
}
