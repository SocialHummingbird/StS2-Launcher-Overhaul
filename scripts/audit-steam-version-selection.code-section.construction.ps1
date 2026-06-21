function Add-SteamVersionSelectionCodeSectionConstructionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
        "orchestrates compact Steam Guard code entry layout and submission wiring" `
        @(
            "bool compactStackedActionRows = false",
            "_compactStackedActionRows = compact && compactStackedActionRows",
            "CreateCodePromptLabel\(scale, compact\)",
            "CreateCodeHelpLabel\(scale, compact\)",
            "CreateCodeField\(scale, compact\)",
            "CreateCodeSubmitButton\(scale, compact\)",
            "BuildCompactCodeActionRow",
            "GridContainer _compactCodeActionRow",
            "codeActionParent\.AddChild\(_codeField\)",
            "codeActionParent\.AddChild\(submitButton\)",
            "MoveChild\(_codeHelpLabel, compactCodeActionRow\.GetIndex\(\) \+ 1\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
        "passes compact width class into the Steam Guard code section for responsive verification controls" `
        @(
            "new CodeSection\(scale, profile\.Compact, profile\.CompactStackedActionRows\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherSectionMetrics.cs" `
        "defines compact Steam Guard touch target metrics" `
        @(
            "CodeInputFontSize\s*=\s*22",
            "CodeInputHeight\s*=\s*76",
            "CodeSubmitFontSize\s*=\s*19",
            "CompactDetailLabelFontSize\s*=\s*12",
            "CompactDrawerToggleHeight\s*=\s*54"
        )
}
