function Add-SteamVersionSelectionActionCoreChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
        "keeps compact action construction ordered as branch details before ready/cloud/support controls" `
        @(
            "BuildBranchControls\(scale, compact\)",
            "_branchDetailsToggle = branchControls\.DetailsToggle",
            "_branchDropdown = branchControls\.Dropdown",
            "BuildReadyVersionSummaryControls\(scale, compact\)",
            "SetGameBranch\(_gameBranch\)",
            "BuildCloudControls\(scale, compact\)",
            "_cloudSafetyToggle = cloudControls\.CloudSafetyToggle",
            "_cloudOptionsToggle = cloudControls\.CloudOptionsToggle",
            "BuildSupportControls\(scale, compact, supportToolsParent\)",
            "_supportToggle = supportControls\.SupportToggle",
            "(?s)BuildBranchControls\(scale, compact\).*BuildReadyVersionSummaryControls\(scale, compact\).*SetGameBranch\(_gameBranch\).*BuildCloudControls\(scale, compact\).*BuildSupportControls\(scale, compact, supportToolsParent\).*ArrangeCompactReadyStatePriority\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Branch.cs" `
        "uses primary compact touch-target sizing for the ready-state version dropdown" `
        @(
            "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
            "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
            "ApplyDropdownAction",
            "(?s)ApplyDropdownAction\(\s*branchDropdown,\s*scale,.*?,\s*compact\s*\)"
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Ready.cs" `
        "configures compact ready-version summary as a readable responsive summary card" `
        @(
            "new Button",
            "TooltipText = `"Open save safety check`"",
            "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
            "ApplyReadyVersionSummaryButtonStyle",
            "readyVersionSummaryPanel\.Pressed \+= OpenCompactCloudSafetyFromReadySummary",
            "ApplyReadyVersionSummaryButtonStyle\(readyVersionSummaryPanel, scale, compact\)",
            "CompactVersionSummaryFontSize",
            "VerticalAlignment\.Center",
            "_compactStackedActionRows\s*\?\s*TextServer\.AutowrapMode\.WordSmart",
            "readyVersionSummaryLabel\.ClipText = compact && !_compactStackedActionRows",
            "readyVersionSummaryLabel\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
            "readyVersionSummaryLabel\.OffsetLeft",
            "readyVersionSummaryLabel\.OffsetRight",
            "TextServer\.OverrunBehavior\.TrimEllipsis",
            "CompactStackedVersionSummaryHeight",
            "CompactVersionSummaryHeight"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "uses responsive compact ready-version copy with Save Check and Upload-locked state" `
        @(
            "CompactReadySummaryBranchLimit",
            "CompactReadyStackedSummaryBranchLimit",
            "CompactReadyVersionSummary\(\)",
            "CompactReadyVersionHelpText\(\)",
            "SelectedOptionCompactStatus",
            "Play version:",
            "Saves: Get/Upload",
            "CompactReadyFileScope",
            "Default files",
            "Separate files",
            "_compactStackedActionRows",
            "Ready:",
            "Save Check \| Upload locked",
            "no auto cloud upload"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.Style.cs" `
        "keeps ready-version summary card skinning isolated from copy generation" `
        @(
            "ApplyReadyVersionSummaryButtonStyle",
            "LauncherComponentTheme\.StateNormal",
            "LauncherComponentTheme\.StateHover",
            "LauncherComponentTheme\.StatePressed",
            "LauncherComponentTheme\.StateDisabled",
            "CompactVersionSummaryRadius",
            "CompactVersionSummaryHorizontalMargin",
            "CompactVersionSummaryVerticalMargin",
            "Color body,",
            "Color border",
            "BuildReadyVersionSummaryStyle\(float scale, bool compact\)",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryRadius : 8",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryHorizontalMargin : 12",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryVerticalMargin : 9"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "opens compact cloud safety details from the ready-version summary" `
        @(
            "OpenCompactCloudSafetyFromReadySummary",
            "_cloudSafetyExpanded = true",
            "UpdateBranchHelpText\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Layout.cs" `
        "prioritizes compact ready state as summary, cloud safety actions, launch, then version management" `
        @(
            "ArrangeCompactCloudGroupPriority",
            "launchParent\?\.RemoveChild\(_launchButton\)",
            "_cloudGroup\.AddChild\(_launchButton\)",
            "MoveChildAfter\(_cloudGroup, _launchButton, _pushPullRow\)",
            "MoveChildAfter\(_cloudGroup, _cloudOptionsToggle, _launchButton\)",
            "MoveChildAfter\(_cloudGroup, _compactCloudOptionsRow, _cloudOptionsToggle\)",
            "ArrangeCompactReadyStatePriority",
            "var readyPrimaryPath = _launchButton\.GetParent\(\) == _cloudGroup",
            "MoveChild\(_readyVersionSummaryPanel, _branchDetailsToggle\.GetIndex\(\)\)",
            "MoveAfter\(_branchDetailsToggle, readyPrimaryPath\)",
            "MoveAfter\(_branchDropdown, _branchDetailsToggle\)",
            "MoveAfter\(_branchHelpLabel, _branchDropdown\)",
            "MoveCompactCloudSafetyCueBeforeCloudActions",
            "private static void MoveChildAfter\(Node parent, Node child, Node previous\)",
            "var previousIndex = previous\.GetIndex\(\)",
            "child\.GetIndex\(\) < previousIndex",
            "previousIndex \+ 1"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
        "uses compact Play and Sync drawer detail labels for version controls" `
        @(
            "Version target",
            "Hide Save Check",
            "CompactCloudSafetyDetailText",
            "Keep active"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "uses compact Play and Sync drawer detail labels for cloud-save safety" `
        @(
            "CompactPlaySyncDrawerText",
            "Save Check",
            "Get saves first",
            "CompactCloudSafetyDetailText",
            "Saves for:",
            "Get Steam saves before upload\. Upload can overwrite Steam\."
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
        "uses compact Play and Sync drawer detail labels for save settings" `
        @(
            "CompactPlaySyncDrawerText",
            "Save settings",
            "Backup and cloud"
        )
}
