function Add-SteamVersionSelectionStartupWarmupStatusAndroidChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.cs" `
        "composes an Android-readable framed startup status card after launcher close" `
        @(
            "internal static partial class LauncherStartupStatus",
            "AndroidMinimumScale = 1\.06f",
            "AndroidWidthRatio = 0\.94f",
            "AndroidMessageFontSize = 18",
            "AndroidPanelHeight = 98",
            "CreateAndroidStatusCard",
            "PanelContainer",
            "BuildAndroidPanelStyle",
            "CreateAndroidTitleLabel",
            "CreateAndroidMessageLabel",
            "CalculateAndroidPanelWidth",
            "MouseFilterEnum\.Ignore"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Metrics.cs" `
        "isolates Android startup status scale and width math" `
        @(
            "CalculateAndroidScale",
            "ReferenceShortEdge",
            "AndroidMinimumScale",
            "AndroidMaximumScale",
            "Math\.Clamp",
            "CalculateAndroidPanelWidth",
            "AndroidWidthRatio"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Labels.cs" `
        "isolates Android startup status title and message labels" `
        @(
            "CreateAndroidTitleLabel",
            "Starting Game",
            "AndroidTitleFontSize",
            "LauncherComponentTheme\.OrangeHot",
            "CreateAndroidMessageLabel",
            "MessageNodeName",
            "AndroidMessageFontSize",
            "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "LauncherComponentTheme\.TextPrimary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Style.cs" `
        "isolates Android startup status card frame styling" `
        @(
            "BuildAndroidPanelStyle",
            "LauncherStyleBoxes\.MakeFilled",
            "PanelBackground",
            "0\.92f",
            "AndroidPanelRadius",
            "LauncherComponentTheme\.CyanDim",
            "AndroidPanelHorizontalMargin",
            "AndroidPanelVerticalMargin"
        )
}
