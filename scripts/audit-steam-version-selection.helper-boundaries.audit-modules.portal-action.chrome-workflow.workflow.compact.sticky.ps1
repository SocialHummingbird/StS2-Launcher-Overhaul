function Add-SteamVersionSelectionPortalActionCompactWorkflowStickyBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.sticky-header.ps1" `
        "keeps compact sticky task header audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStickyHeaderChecks",
            "audit-steam-version-selection.compact-workflow.sticky-header.placement.ps1",
            "audit-steam-version-selection.compact-workflow.sticky-header.button.ps1",
            "audit-steam-version-selection.compact-workflow.sticky-header.layout.ps1",
            "audit-steam-version-selection.compact-workflow.sticky-header.style.ps1",
            "Add-SteamVersionSelectionCompactWorkflowStickyHeaderPlacementChecks",
            "Add-SteamVersionSelectionCompactWorkflowStickyHeaderButtonChecks",
            "Add-SteamVersionSelectionCompactWorkflowStickyHeaderLayoutChecks",
            "Add-SteamVersionSelectionCompactWorkflowStickyHeaderStyleChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.sticky-header.placement.ps1" `
        "keeps compact sticky header placement audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStickyHeaderPlacementChecks",
            "LauncherView.Layout.PrimaryColumn.cs",
            "LauncherView.Layout.PrimaryColumn.Body.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.sticky-header.button.ps1" `
        "keeps compact current-task button audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStickyHeaderButtonChecks",
            "LauncherView.Layout.CompactTaskHeader.Button.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.sticky-header.layout.ps1" `
        "keeps compact sticky header layout and viewport reflow audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStickyHeaderLayoutChecks",
            "LauncherView.Layout.CompactTaskHeader.cs",
            "LauncherView.Layout.CompactTaskHeader.Layout.cs",
            "LauncherView.Behavior.Responsive.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.sticky-header.style.ps1" `
        "keeps compact sticky header style audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStickyHeaderStyleChecks",
            "LauncherView.Layout.CompactTaskHeader.Style.cs"
        )
}
