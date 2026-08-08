function Add-SteamVersionSelectionCompactInstallMetricChecks {

    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherComponentTheme.cs" `
        "defines compact readable download progress bar metrics and colors" `
        @(
            "ProgressBarHeight = 24",
            "CompactProgressBarHeight = 34",
            "ProgressBarFontSize = 12",
            "CompactProgressBarFontSize = 14",
            "ProgressBarRadius = 6",
            "ProgressBackground",
            "ProgressFill",
            "ProgressFillCompact"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherComponentTheme.cs" `
        "rounds shared component scaling instead of flooring compact Android metrics" `
        @(
            "using System;",
            "MathF\.Round\(value \* scale, MidpointRounding\.AwayFromZero\)",
            "Math\.Max\(0,"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherViewLayoutMetrics.cs" `
        "rounds shared layout scaling instead of flooring compact Android metrics" `
        @(
            "using System;",
            "MathF\.Round\(value \* scale, MidpointRounding\.AwayFromZero\)",
            "Math\.Max\(0,"
        )
}
