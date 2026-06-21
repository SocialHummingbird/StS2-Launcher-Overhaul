function Add-SteamVersionSelectionPortalUxDiagnosticsFlagChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Diagnostics.cs" `
        "declares diagnostics, fallback recovery, startup recovery, and validation-boundary support flags" `
        @(
            "NativeFallbackRecoveryActionsStyledSupported\s*=\s*true",
            "NativeFallbackDiagnosticsCollapsedSupported\s*=\s*true",
            "NativeFallbackResponsiveRecoveryRowsSupported\s*=\s*true",
            "StartupRecoveryScrollSafeControlsSupported\s*=\s*true",
            "StartupRecoveryStructuredCompactActionsSupported\s*=\s*true",
            "VersionInstallCloudSeparationGuidanceSupported\s*=\s*true",
            "DiagnosticsConsoleHiddenByDefault\s*=\s*true",
            "DiagnosticsConsoleAutoOpensForDiagnosticsActionsSupported\s*=\s*true",
            "CompactLowProfileDiagnosticsToggleSupported\s*=\s*true",
            "CompactDiagnosticsToggleDetailLabelsSupported\s*=\s*true",
            "CompactStructuredDiagnosticsToggleLabelsSupported\s*=\s*true",
            "PlainLanguageHelpReportCopySupported\s*=\s*true",
            "CompactDiagnosticsScrollHostedSupported\s*=\s*true",
            "CompactReadableDiagnosticsLogSupported\s*=\s*true",
            "CompactBoundedDiagnosticsLogViewportSupported\s*=\s*true",
            "ViewportAwareDiagnosticsLogResizeSupported\s*=\s*true",
            "StartupFallbackRawBannerSuppressed\s*=\s*true",
            "PortalUxDeviceValidated\s*=\s*false"
        )
}
