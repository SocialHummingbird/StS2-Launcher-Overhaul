function Add-SteamVersionSelectionPortalActionPortalUxSupportBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-support.ps1" `
        "keeps portal status formatter and UX-support audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionPortalUxSupportChecks",
            "Add-SteamVersionSelectionPortalUxStatusFormatterChecks",
            "Add-SteamVersionSelectionPortalUxFlagChecks",
            "Add-SteamVersionSelectionPortalUxNarrativeChecks",
            "Add-SteamVersionSelectionPortalUxFeatureChecks",
            "audit-steam-version-selection.portal-ux-status.ps1",
            "audit-steam-version-selection.portal-ux-flags.ps1",
            "audit-steam-version-selection.portal-ux-narrative.ps1",
            "audit-steam-version-selection.portal-ux-features.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-status.ps1" `
        "keeps portal status formatter audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxStatusFormatterChecks",
            "LauncherPortalStatusFormatter.cs",
            "LauncherPortalStatusFormatter.Message.cs",
            "LauncherPortalStatusFormatter.Action.cs",
            "LauncherPortalStatusFormatter.Phase.cs",
            "LauncherPortalStatusFormatter.Color.cs",
            "LauncherPortalStatusFormatter.Predicates.cs"
        )
}
