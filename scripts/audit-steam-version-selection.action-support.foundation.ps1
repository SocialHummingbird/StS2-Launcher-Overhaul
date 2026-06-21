function Add-SteamVersionSelectionActionSupportFoundationChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Foundation.cs" `
        "packs compact recovery and support tools into a responsive grid that becomes full-width on narrow compact viewports" `
        @(
            "BuildSupportFoundation",
            "BuildCompactSupportToolsGrid\(scale, compact, compactStackedActionRows\)",
            "if \(compact\)",
            "supportGroup\.AddChild\(supportToolsGrid\)",
            "supportToolsParent = compact",
            "new SupportFoundation\(supportGroup, supportToolsGrid, supportToolsParent\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Types.cs" `
        "uses typed support construction return values instead of long tuple signatures" `
        @(
            "private readonly struct SupportFoundation",
            "internal VBoxContainer Group",
            "internal GridContainer ToolsGrid",
            "internal Container ToolsParent",
            "private readonly struct SupportControls",
            "internal Button SupportToggle",
            "internal Button UpdateButton",
            "internal Button RefreshVersionsButton",
            "internal Button RedownloadButton",
            "internal Button ClearCachedVersionsButton",
            "internal Button DiagnosticsButton",
            "internal Button ShowLastErrorButton",
            "internal Button CopyRawLogButton"
        )
}
