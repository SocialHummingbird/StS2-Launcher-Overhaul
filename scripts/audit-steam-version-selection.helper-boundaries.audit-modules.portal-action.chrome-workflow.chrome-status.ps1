function Add-SteamVersionSelectionPortalActionChromeStatusBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.portal-chrome.ps1" `
        "keeps launcher shell, brand, and compact panel chrome audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionPortalChromeChecks",
            "audit-steam-version-selection.portal-chrome.shell-brand.ps1",
            "audit-steam-version-selection.portal-chrome.compact-layout.ps1",
            "Add-SteamVersionSelectionPortalChromeShellBrandChecks",
            "Add-SteamVersionSelectionPortalChromeCompactLayoutChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-chrome.shell-brand.ps1" `
        "keeps launcher shell and brand chrome audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalChromeShellBrandChecks",
            "LauncherView.Layout.cs",
            "LauncherView.Layout.BrandHeader.cs",
            "LauncherView.Layout.BrandMark.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-chrome.compact-layout.ps1" `
        "keeps launcher compact viewport and panel chrome audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalChromeCompactLayoutChecks",
            "LauncherLayoutProfile.cs",
            "StyledPanel.cs",
            "LauncherView.Layout.PrimaryColumn.Support.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.status-capsule.ps1" `
        "keeps launcher status capsule audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionStatusCapsuleChecks",
            "audit-steam-version-selection.status-capsule.shell.ps1",
            "audit-steam-version-selection.status-capsule.compact.ps1",
            "audit-steam-version-selection.status-capsule.detail.ps1",
            "Add-SteamVersionSelectionStatusCapsuleShellChecks",
            "Add-SteamVersionSelectionStatusCapsuleCompactChecks",
            "Add-SteamVersionSelectionStatusCapsuleDetailChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.status-capsule.shell.ps1" `
        "keeps launcher status capsule shell, style, and sizing audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionStatusCapsuleShellChecks",
            "LauncherView.Layout.StatusCapsule.cs",
            "LauncherView.Layout.StatusCapsule.Styles.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.status-capsule.compact.ps1" `
        "keeps launcher compact status headline and responsive reflow audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionStatusCapsuleCompactChecks",
            "LauncherView.Layout.StatusCapsule.Compact.cs",
            "LauncherView.Behavior.Responsive.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.status-capsule.detail.ps1" `
        "keeps launcher compact status detail and touch-expand audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionStatusCapsuleDetailChecks",
            "LauncherView.Layout.StatusCapsule.Detail.cs",
            "LauncherView.Status.cs"
        )
}
