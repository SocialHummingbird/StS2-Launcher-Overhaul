function Add-SteamVersionSelectionActionCoreButtonStyleChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.cs" `
        "keeps launcher button action presets as the public styling API" `
        @(
            "internal static partial class LauncherButtonStyles",
            "ApplyPrimaryAction",
            "ApplySafeAction",
            "ApplySupportAction",
            "ApplyCloudPullAction",
            "ApplyDangerAction",
            "LauncherComponentTheme\.OrangeAccent",
            "LauncherComponentTheme\.CyanAccent",
            "filled: false",
            "new Color\(0\.07f, 0\.18f, 0\.15f\)",
            "new Color\(0\.22f, 0\.07f, 0\.07f\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.Dropdown.cs" `
        "uses touch-safe compact dropdown popup row spacing and padding" `
        @(
            "ApplyDropdownAction",
            "bool compact = false",
            "PopupVerticalSeparation",
            "PopupHorizontalSeparation",
            "PopupItemStartPadding",
            "PopupItemEndPadding",
            "PopupHover",
            "CompactDropdownPopupVerticalSeparation = 16",
            "CompactDropdownPopupHorizontalSeparation = 12",
            "CompactDropdownPopupHorizontalPadding = 20",
            "compact\s*\?\s*CompactDropdownPopupVerticalSeparation",
            "compact\s*\?\s*CompactDropdownPopupHorizontalSeparation",
            "compact\s*\?\s*CompactDropdownPopupHorizontalPadding",
            "LauncherComponentTheme\.ButtonHover"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.State.cs" `
        "keeps launcher button state styleboxes and text colors isolated" `
        @(
            "private static void Apply",
            "BuildButtonStateStyle",
            "button\.ClipText = true",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "LauncherComponentTheme\.StateNormal",
            "LauncherComponentTheme\.StateHover",
            "LauncherComponentTheme\.StatePressed",
            "LauncherComponentTheme\.StateDisabled",
            "FontHoverColor",
            "FontPressedColor",
            "FontDisabledColor",
            "LauncherStyleBoxes\.MakeFilled",
            "LauncherStyleBoxes\.MakeOutline",
            "BorderWidthBottom = width",
            "LauncherComponentTheme\.TextMuted"
        )
}
