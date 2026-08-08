function Add-SteamVersionSelectionCompactLabelChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabelSpec.cs" `
        "keeps compact two-line button label configuration typed" `
        @(
            "internal readonly struct CompactButtonDetailLabelSpec",
            "BodyName",
            "TitleName",
            "DetailName",
            "TitleFontSize",
            "DetailFontSize",
            "HorizontalMargin",
            "VerticalMargin",
            "Default\(",
            "LauncherSectionMetrics\.CompactDetailButtonFontSize",
            "LauncherSectionMetrics\.CompactDetailLabelFontSize",
            "horizontalMargin: 6",
            "verticalMargin: 4"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabels.cs" `
        "centralizes reusable compact two-line button label application" `
        @(
            "CompactButtonDetailLabels",
            "Apply",
            "TrySplitText",
            "Hide\(Button button, CompactButtonDetailLabelSpec spec\)",
            "button\.Text = text",
            "labels\.Title\.Text = title",
            "labels\.Detail\.Text = detail"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabels.Text.cs" `
        "keeps compact two-line button label parsing isolated from Godot controls" `
        @(
            "TrySplitText",
            "IndexOf\('\\n'\)",
            "Trim\(\)",
            "title\.Length > 0 && detail\.Length > 0"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabels.Controls.cs" `
        "keeps compact two-line button label Godot control construction isolated" `
        @(
            "Ensure\(",
            "BuildBody",
            "BuildLabel",
            "LauncherComponentTheme\.TextPrimary",
            "LauncherComponentTheme\.TextSecondary",
            "Control\.LayoutPreset\.FullRect",
            "LauncherViewLayoutMetrics\.ScaleInt",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.CompactNativeButton.cs" `
        "wires compact Android Steam sign-in to shared two-line CTA labels" `
        @(
            "CompactNativeLoginButtonHeight = LauncherSectionMetrics\.CodeInputHeight",
            "CompactNativeLoginLabels",
            "CompactButtonDetailLabelSpec",
            "CompactNativeLoginText",
            "SetCompactNativeLoginButtonText",
            "Sign in with Steam",
            "Android login",
            "CompactButtonDetailLabels\.Apply"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.Help.cs" `
        "uses a readable two-line compact Android credential helper" `
        @(
            "LauncherSectionMetrics\.CompactCredentialHelpHeight",
            "TextServer\.AutowrapMode\.WordSmart",
            "ClipText = false",
            "VerticalAlignment\.Center",
            "ScaleInt\(LauncherSectionMetrics\.CompactCredentialHelpHeight, scale\)",
            "Password manager can appear\.",
            "Steam password is not stored\."
        )

}
