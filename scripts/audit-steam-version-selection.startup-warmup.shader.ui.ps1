function Add-SteamVersionSelectionStartupWarmupShaderUiChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Components\StyledProgressBar.cs" `
        "uses a taller styled compact percentage progress bar" `
        @(
            "internal StyledProgressBar\(float scale, bool compact = false\)",
            "compact\s*\?\s*LauncherComponentTheme\.CompactProgressBarHeight",
            "ShowPercentage = true",
            "CompactProgressBarFontSize",
            "BackgroundStyle",
            "FillStyle",
            "ProgressFillCompact",
            "BuildProgressStyle"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.UI.cs" `
        "uses Android-readable compact sizing and styled progress for shader warmup" `
        @(
            "OperatingSystem\.IsAndroid\(\)",
            "AndroidMinimumScale = 1\.06f",
            "AndroidMinimumPanelWidth = 320f",
            "AndroidPanelWidthRatio = 0\.94f",
            "CalculateAdaptiveScale\(vpSize, androidCompact\)",
            "widthRatio: CalculatePanelWidthRatio\(vpSize, androidCompact\)",
            "compact: androidCompact",
            "CalculateWarmupPanelSize\(vpSize, androidCompact\)",
            "new StyledProgressBar\(scale, androidCompact\)",
            "androidCompact \? AndroidMinimumScale : MinimumScale",
            "return AndroidPanelWidthRatio"
        )
}
