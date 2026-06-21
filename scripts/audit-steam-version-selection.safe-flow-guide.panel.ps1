function Add-SteamVersionSelectionSafeFlowGuidePanelChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.cs" `
        "builds the quick-start safe-flow guide panel" `
        @(
            "BuildFirstRunGuide",
            "BuildFirstRunGuidePanel",
            "CompactSafeFlowGuideTitleHeight",
            "CompactSafeFlowGuideTitleFontSize",
            "BuildFirstRunGuideStyle\(scale, compact\)",
            "`"Quick start guide`"",
            "new PanelContainer",
            "AddCompactSafeFlowSteps\(body, scale\)",
            "choose a game version, get Steam saves, then start the game",
            "Upload stays locked until you deliberately open it after checking local saves"
        )
}
