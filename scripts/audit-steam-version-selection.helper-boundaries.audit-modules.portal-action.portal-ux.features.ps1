function Add-SteamVersionSelectionPortalActionPortalUxFeatureBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-narrative.ps1" `
        "keeps portal UX narrative audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxNarrativeChecks",
            "LauncherPortalUxSupport.cs",
            "Task-led launcher with five stable destinations",
            "display safe-area insets",
            "Steam Cloud Push gates remain unchanged",
            "unlocked ARM64 device"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-features.ps1" `
        "keeps portal UX diagnostics feature audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxFeatureChecks",
            "LauncherPortalUxSupport.Features.cs",
            "LauncherPortalUxSupport.Features.Status.cs",
            "LauncherPortalUxSupport.Features.Workflow.cs",
            "LauncherPortalUxSupport.Features.AuthChrome.cs",
            "LauncherPortalUxSupport.Features.InstallCloud.cs",
            "LauncherPortalUxSupport.Features.Diagnostics.cs",
            "PortalUxDeviceValidated"
        )
}
