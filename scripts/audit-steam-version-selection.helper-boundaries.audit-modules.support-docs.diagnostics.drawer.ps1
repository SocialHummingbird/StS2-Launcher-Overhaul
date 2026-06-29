function Add-SteamVersionSelectionSupportDocsDiagnosticsDrawerBoundaryChecks {

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.ps1" `
        "keeps Help & Reports diagnostics drawer audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerChecks",
            "audit-steam-version-selection.diagnostics-drawer.shell.ps1",
            "audit-steam-version-selection.diagnostics-drawer.primary-column.ps1",
            "audit-steam-version-selection.diagnostics-drawer.sizing.ps1",
            "audit-steam-version-selection.diagnostics-drawer.actions.ps1",
            "Add-SteamVersionSelectionDiagnosticsDrawerShellChecks",
            "Add-SteamVersionSelectionDiagnosticsDrawerPrimaryColumnChecks",
            "Add-SteamVersionSelectionDiagnosticsDrawerSizingChecks",
            "Add-SteamVersionSelectionDiagnosticsDrawerActionChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.shell.ps1" `
        "keeps Help & Reports drawer shell and compact log audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerShellChecks",
            "LauncherView.Layout.LogColumn.cs",
            "LauncherView.Layout.LogColumn.Toggle.cs",
            "LauncherView.Log.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.primary-column.ps1" `
        "keeps compact diagnostics host layout-result audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerPrimaryColumnChecks",
            "LauncherView.Layout.PrimaryColumn.Result.cs",
            "LauncherView.Layout.PrimaryColumn.cs",
            "CompactDiagnosticsHost"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.sizing.ps1" `
        "keeps Help & Reports viewport sizing audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerSizingChecks",
            "LauncherView.Layout.LogColumn.Sizing.cs",
            "LauncherView.Behavior.cs",
            "UpdateDiagnosticsLogViewport"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.actions.ps1" `
        "keeps Help & Reports reveal/export action audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerActionChecks",
            "LauncherView.Diagnostics.cs",
            "LauncherDiagnosticsCoordinator.Export.cs",
            "ShowDiagnosticsConsole"
        )
}
