function Add-SteamVersionSelectionStartupWarmupStatusShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.cs" `
        "routes startup status creation between Android card and legacy label" `
        @(
            "internal static partial class LauncherStartupStatus",
            "OperatingSystem\.IsAndroid\(\)",
            "CreateAndroidStatusCard\(parent, viewportSize\)",
            "CreateLegacyLabel\(parent, viewportSize\)",
            "Startup status label creation failed",
            "CalculateSafeMargin"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Legacy.cs" `
        "keeps the desktop startup status label fallback isolated" `
        @(
            "CreateLegacyLabel",
            "CalculateFontSize",
            "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "Control\.LayoutPreset\.TopWide",
            "font_size",
            "new Color\(0\.55f, 0\.85f, 1f\)"
        )
}
