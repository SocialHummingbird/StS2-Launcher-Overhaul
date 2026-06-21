function Add-SteamVersionSelectionPortalActionSectionFlowBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-section-flow.ps1" `
        "keeps compact section visibility and scroll-flow audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCompactSectionFlowChecks",
            "audit-steam-version-selection.compact-section-flow.visibility.ps1",
            "audit-steam-version-selection.compact-section-flow.scrolling.ps1",
            "audit-steam-version-selection.compact-section-flow.reanchor.ps1",
            "Add-SteamVersionSelectionCompactSectionFlowVisibilityChecks",
            "Add-SteamVersionSelectionCompactSectionFlowScrollingChecks",
            "Add-SteamVersionSelectionCompactSectionFlowReanchorChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-section-flow.visibility.ps1" `
        "keeps compact section visibility audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactSectionFlowVisibilityChecks",
            "SetCompactReadyInstallSectionVisible",
            "HideCompactCompletedAuthSections"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-section-flow.scrolling.ps1" `
        "keeps compact section scrolling audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactSectionFlowScrollingChecks",
            "ScrollCompactPrimaryTo",
            "CompactScrollAnchorTopPadding"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-section-flow.reanchor.ps1" `
        "keeps compact section viewport re-anchor audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactSectionFlowReanchorChecks",
            "ReanchorCompactScrollTargetAfterViewportChange",
            "ReadyScrollTarget"
        )
}
