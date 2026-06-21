function Add-SteamVersionSelectionPortalUxStatusFormatterChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.cs" `
        "declares status formatter UX support flags" `
        @(
            "internal static partial class LauncherPortalStatusFormatter",
            "PhaseLabelStatusSupported\s*=\s*true",
            "StructuredStatusChipSupported\s*=\s*true",
            "GuidedNextActionStatusSupported\s*=\s*true",
            "ErrorFirstGuidedStatusSupported\s*=\s*true",
            "CompactPlainLanguageStatusCopySupported\s*=\s*true",
            "CompactShortStatusDetailsSupported\s*=\s*true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Message.cs" `
        "formats compact launcher status text as plain-language user copy" `
        @(
            "MessageFor",
            "CompactMessageFor",
            "CompactMessageMaxChars = 86",
            "ShortenCompactMessage",
            "Waiting for launcher state",
            "Sign in with Steam to continue",
            "Signing in to Steam",
            "Checking game ownership",
            "Download this game version to play",
            "Ready to play this version",
            "Signed in\. Checking game files",
            "Get Steam saves before uploading",
            "Upload blocked\. Check save safety first",
            "Runtime files need repair\. Redownload this version",
            "Last launch failed\. Open details or try Safe Start"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Action.cs" `
        "formats launcher status next-action labels" `
        @(
            "ActionFor",
            "Fix Required",
            "Verify Code",
            "Install Game",
            "Start Game",
            "Choose Version",
            "Sync Saves",
            "Review Details",
            "Next Step"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Phase.cs" `
        "classifies launcher status into clear phase labels" `
        @(
            "PhaseFor",
            "Attention",
            "Steam",
            "Version",
            "Install",
            "Cloud",
            "Ready",
            "Details",
            "Status"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Color.cs" `
        "maps launcher status phases to portal colors" `
        @(
            "ColorFor",
            "LauncherComponentTheme\.OrangeHot",
            "LauncherComponentTheme\.CyanAccent",
            "LauncherComponentTheme\.OrangeAccent",
            "new Color\(0\.36f, 0\.9f, 0\.42f\)",
            "LauncherComponentTheme\.TextSecondary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Predicates.cs" `
        "centralizes launcher status text predicates" `
        @(
            "ContainsAny",
            "StringComparison\.OrdinalIgnoreCase",
            "ContainsFailure",
            "Could not",
            "IsDownloadRequiredStatus",
            "IsReadyStatus",
            "Runtime pairing is verified",
            "Active install slot"
        )
}
