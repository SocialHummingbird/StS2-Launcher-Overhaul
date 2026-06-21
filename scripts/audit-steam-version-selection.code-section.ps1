function Add-SteamVersionSelectionCodeSectionChecks {
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
        "src\STS2Mobile\Launcher\Sections\CodeSection.Labels.cs" `
        "isolates compact Steam Guard prompt and help label construction" `
        @(
            "CreateCodePromptLabel\(float scale, bool compact\)",
            "CreateCodeHelpLabel\(float scale, bool compact\)",
            "CompactCodePromptHeight",
            "CompactCodeHelpHeight",
            "AutowrapMode = TextServer\.AutowrapMode\.Off",
            "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "ConfigureCompactCodeLabel",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.Input.cs" `
        "isolates Steam Guard code input sizing and keyboard configuration" `
        @(
            "CreateCodeField\(float scale, bool compact\)",
            "compact \? `"ABC123`" : `"Steam Guard code`"",
            "VirtualKeyboardType\.Default",
            "CodeInputHeight\(bool compact\)",
            "CodeInputFontSize\(bool compact\)",
            "CodeInputHeight",
            "CodeInputFontSize",
            "field\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.SubmitButton.cs" `
        "isolates Steam Guard verification button construction and compact labels" `
        @(
            "CreateCodeSubmitButton\(float scale, bool compact\)",
            "CodeSubmitFontSize",
            "CompactCodeSubmitText",
            "SetCompactCodeSubmitButtonText",
            "height: CodeInputHeight\(compact\)",
            "Verify Code",
            "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.SubmitButton.cs" `
        "wires compact Steam Guard submit to shared two-line labels" `
        @(
            "CompactCodeSubmitLabels",
            "CompactButtonDetailLabelSpec",
            "CompactCodeSubmitText",
            "Verify Code",
            "Submit once",
            "SetCompactCodeSubmitButtonText",
            "CompactButtonDetailLabels\.Apply"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.Layout.cs" `
        "reflows compact Steam Guard code controls after viewport changes" `
        @(
            "UpdateViewportProfile\(LauncherLayoutProfile profile\)",
            "GodotObject\.IsInstanceValid\(_compactCodeActionRow\)",
            "_compactStackedActionRows = profile\.Compact && profile\.CompactStackedActionRows",
            "ApplyCompactCodeActionRowLayout\(_compactCodeActionRow, profile\.Scale, _compactStackedActionRows\)",
            "row\.Columns = compactStackedActionRows \? 1 : 2",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactCodeActionRowSeparation, scale\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
        "updates compact section responsive rows after viewport changes" `
        @(
            "private void UpdateCompactSectionResponsiveRows\(Vector2 viewportSize\)",
            "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
            "Code\.UpdateViewportProfile\(profile\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.Prompt.cs" `
        "keeps compact Steam Guard retry guidance short and readable" `
        @(
            "CompactIncorrectPrompt",
            "Code rejected",
            "CompactIncorrectHelp",
            "Use newest Steam Guard code",
            "Old codes can expire; spaces removed",
            "One-shot submit; code is not stored",
            "CodePromptText\(_compact, wasIncorrect\)",
            "CodeHelpText\(_compact, wasIncorrect\)"
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

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
        "wires Steam Guard code input to alphanumeric normalization and one-shot submit" `
        @(
            "TextChanged \+= NormalizeCodeText",
            "TextSubmitted \+= _ => OnSubmit\(\)",
            "submitButton\.Pressed \+= OnSubmit",
            "CodeSubmitted"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.Submission.cs" `
        "normalizes Steam Guard codes to uppercase alphanumeric text before submitting once" `
        @(
            "NormalizeCodeText",
            "NormalizeCode\(string text\)",
            "char\.IsLetterOrDigit",
            "char\.ToUpperInvariant",
            "CodeSubmitted\?\.Invoke\(code\)"
        )
}
