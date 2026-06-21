function Add-SteamVersionSelectionStartupRecoveryChecks {

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.StartupRecoveryReports.cs" `
        "adds public-sharing warning before startup recovery diagnostics content" `
        @(
            "AppendStartupRecoveryDiagnostics",
            "AppendPublicSharingWarning",
            "Data dir:"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Text.cs" `
        "labels startup recovery launcher-log copy as review-before-sharing" `
        @(
            "If startup stalls, restart the app, try Safe Start, or create a help report",
            "Review logs before sharing"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Buttons.cs" `
        "labels startup recovery launcher-log button as review-before-sharing" `
        @(
            "Copy Launcher Log \(Review First\)",
            "Review first"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.CompactButton.cs" `
        "uses structured compact startup recovery button labels instead of raw debug labels" `
        @(
            "CompactRecoveryButtonBody",
            "CompactRecoveryButtonTitle",
            "CompactRecoveryButtonDetail",
            "AddCompactRecoveryButtonLabels",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactRecoveryButtonLabels",
            '\$"\{titleText\}\\n\{detailText\}"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Buttons.cs" `
        "uses compact startup recovery action copy instead of raw debug labels" `
        @(
            "Restart App",
            "Open launcher",
            "Safe Start",
            "Cloud off",
            "Help Report",
            "Share details",
            "Copy Log",
            "Review first",
            "Hide Help",
            "Keep waiting"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Layout.cs" `
        "keeps startup recovery controls reachable with a scroll-safe Android layout" `
        @(
            "CreateScrollContainer",
            "ScrollContainer",
            "FollowFocus = true",
            "SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
            "CreateFrame",
            "MarginContainer",
            "RecoveryMargin",
            "RecoveryTopMargin",
            "UseCompactRecoveryCopy",
            "OperatingSystem\.IsAndroid\(\)",
            "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Construction.cs" `
        "wires startup recovery scroll hierarchy in order" `
        @(
            "CreateScrollContainer",
            "CreateFrame",
            "CreateContainer",
            "scroll\.AddChild\(frame\)",
            "frame\.AddChild\(box\)"
        )
}
