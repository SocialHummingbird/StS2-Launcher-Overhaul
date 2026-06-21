function Add-SteamVersionSelectionCodeSectionControlChecks {
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
}
