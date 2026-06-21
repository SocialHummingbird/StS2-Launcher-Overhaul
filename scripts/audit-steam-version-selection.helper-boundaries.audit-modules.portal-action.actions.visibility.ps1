function Add-SteamVersionSelectionPortalActionReadyActionVisibilityBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.action-visibility.ps1" `
        "keeps ready-state launch and visibility audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionVisibilityChecks",
            "ActionSection.Visibility.cs",
            "ActionSection.Visibility.SecondaryState.cs",
            "ActionSection.Visibility.Secondary.cs",
            "ActionSection.Visibility.Support.cs"
        )
}
