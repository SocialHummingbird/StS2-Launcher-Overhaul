function Add-SteamVersionSelectionDiagnosticsDrawerActionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Diagnostics.cs" `
        "can reveal the hidden Help & Reports drawer after explicit diagnostics actions" `
        @(
            "ShowDiagnosticsConsole",
            "DiagnosticsDrawer\.Visible = true",
            "SetDiagnosticsToggleText\(DiagnosticsToggle, _profile, visible: true\)",
            "ScrollCompactPrimaryTo\(DiagnosticsDrawer\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnosticsCoordinator.cs" `
        "opens Help & Reports when problem summary or launcher-log actions write output" `
        @(
            "ShowDiagnosticsSummary",
            "CopyRawLogToClipboard",
            "Last problem opened",
            "view\.ShowDiagnosticsConsole\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnosticsCoordinator.Export.cs" `
        "opens Help & Reports after manual help report export writes output" `
        @(
            "ShowDiagnosticsExportResult",
            "Help report ready",
            "Help report saved",
            "_view\.ShowDiagnosticsConsole\(\)"
        )
}
