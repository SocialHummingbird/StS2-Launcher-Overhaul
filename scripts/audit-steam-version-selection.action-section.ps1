function Add-SteamVersionSelectionActionSectionChecks {
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

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.cs" `
        "orchestrates support toggle and tool button construction through focused helpers" `
        @(
            "private SupportControls BuildSupportControls",
            "BuildSupportToggle",
            "AddChild\(_supportGroup\)",
            "return new SupportControls",
            "BuildUpdateSupportButton",
            "BuildRefreshVersionsSupportButton",
            "BuildRedownloadSupportButton",
            "BuildClearCachedVersionsSupportButton",
            "BuildDiagnosticsSupportButton",
            "BuildShowLastErrorSupportButton",
            "BuildCopyRawLogSupportButton",
            "SupportToggleText\(\)",
            "ToggleSupportOptions",
            "SetCompactActionButtonText\(supportToggle, supportToggle\.Text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Tools.cs" `
        "keeps compact update version and cache tool labels in focused button builders" `
        @(
            "AddCompactSupportToolButton",
            "`"Check Files`"",
            "`"Game Versions`"",
            "`"Repair Files`"",
            "`"Free Space`"",
            "`"Updates`"",
            "`"Refresh list`"",
            "`"Rebuild game`"",
            "`"Old versions`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
        "keeps compact report problem and launcher-log support labels isolated" `
        @(
            "AddCompactSupportToolButton",
            "`"Help Report`"",
            "`"Last Problem`"",
            "`"Copy Log`"",
            "`"Share details`"",
            "`"Open details`"",
            "`"Review first`"",
            "`"Create Help Report`"",
            "`"Show Last Problem`"",
            "`"Copy Launcher Log \(Review First\)`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Support.cs" `
        "keeps compact support drawer grid and support toggle copy responsive" `
        @(
            "GridContainer",
            "Columns = compactStackedActionRows \? 1 : 2",
            "`"Fixes & Help`"",
            "`"Hide Fixes`"",
            "`"Repair tools`"",
            "`"Back to play`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Buttons.cs" `
        "keeps base ActionSection hidden button factories and push/pull buttons isolated" `
        @(
            "AddPrimaryHiddenButton",
            "AddSecondaryHiddenButton",
            "AddHiddenButton",
            "new StyledButton",
            "button\.Visible = false",
            "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "AddPushPullButton",
            "LauncherSectionMetrics\.SecondaryButtonHeight"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CompactActionButton.cs" `
        "uses readable fill-width buttons inside the compact support grid" `
        @(
            "AddCompactSupportToolButton",
            "SetCompactActionButtonText",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactActionButtonLabels",
            "CompactActionButtonBodyName",
            "CompactActionButtonTitleName",
            "CompactActionButtonDetailName",
            "CompactButtonDetailLabelSpec\.Default",
            "_compact",
            "CompactSupportToolHeight",
            "CompactSupportToolFontSize",
            "CompactSupportToolText"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
        "keeps dynamic compact version and safety drawer labels synced with structured title/detail button labels" `
        @(
            "SetCompactActionButtonText\(_branchDetailsToggle",
            "SetCompactActionButtonText\(_cloudSafetyToggle"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
        "keeps dynamic compact save-settings drawer label synced with structured title/detail button labels" `
        @(
            "SetCompactActionButtonText\(_cloudOptionsToggle",
            "Backup \{OnOff\(_localBackupEnabled\)\} / Cloud \{OnOff\(_cloudSyncEnabled\)\}"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
        "keeps update button text routed through structured compact button labels" `
        @(
            "SetCompactActionButtonText\(_updateButton, text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
        "keeps compact Save Backup and Cloud Sync option toggles synced with structured title/detail button labels" `
        @(
            "SetCompactActionButtonText\(button, text\)",
            "CompactCloudOptionText",
            "Local safety",
            "Steam saves"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Primary.cs" `
        "keeps compact safe start as a support-grid action with Cloud-off detail" `
        @(
            "AddCompactSupportToolButton",
            "supportToolsParent",
            "`"Safe Start`"",
            "`"Cloud off`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.cs" `
        "keeps cloud construction as typed orchestration rather than one mixed UI method" `
        @(
            "private readonly record struct CloudControls",
            "private readonly record struct CloudPrimaryActionControls",
            "private readonly record struct CloudSafetyControls",
            "private readonly record struct CloudOptionControls",
            "BuildCloudPrimaryActionControls\(cloudGroup, scale, compact\)",
            "BuildCloudSafetyControls\(cloudGroup, scale, compact\)",
            "BuildCloudOptionControls\(cloudGroup, scale, compact\)",
            "return new CloudControls"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "uses explicit compact cloud direction labels while keeping upload locked" `
        @(
            "CompactCloudPullText\(\)",
            "CompactCloudPushToggleText\(expanded: false\)",
            "CompactCloudPushDangerText\(\)",
            "CompactCloudPushConfirmText\(\)",
            "SetCompactActionButtonText\(pullButton, pullButton\.Text\)",
            "SetCompactActionButtonText\(pushButton, pushButton\.Text\)",
            "SetCompactActionButtonText\(confirmPushButton, confirmPushButton\.Text\)",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PushConfirmation.cs" `
        "keeps compact cloud Push confirmation warning bounded and overwrite-explicit" `
        @(
            "BuildCloudPushConfirmationLabel",
            "CompactCloudPushWarningText\(\)",
            "Confirming Push uploads Android saves to Steam Cloud",
            "can overwrite remote Steam Cloud saves",
            "CompactCloudPushWarningFontSize",
            "CompactCloudPushWarningHeight",
            "pushConfirmationLabel\.ClipText = compact",
            "pushConfirmationLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "pushConfirmationLabel\.CustomMinimumSize = new Vector2",
            "LauncherComponentTheme\.OrangeHot",
            "pushConfirmationLabel\.Visible = false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "defines compact Pull label as an explicit title/detail Android download action" `
        @(
            "CompactCloudPullText",
            "Get Steam Saves",
            "Download to Android"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "defines compact dangerous Push labels as explicit title/detail actions" `
        @(
            "CompactCloudPushDangerText",
            "Upload to Steam",
            "Overwrite cloud",
            "CompactCloudPushConfirmText",
            "Confirm Upload",
            "Overwrite cloud",
            "CompactCloudPushWarningText",
            "Steam Cloud overwrite",
            "Confirm only after Pull/local saves are verified"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
        "keeps compact Push relock label direction-aware and structured after reset" `
        @(
            "CompactCloudPushToggleText",
            "SetCompactActionButtonText\(_cloudPushToggle, _compact"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ConstructionHelpers.cs" `
        "packs compact Get Steam Saves and locked Steam upload into responsive action rows" `
        @(
            "BuildCompactCloudPrimaryActionsRow",
            "compactStackedActionRows",
            "new VBoxContainer\(\) : new HBoxContainer\(\)",
            "CompactCloudPrimaryActionSeparation",
            "parent\.AddChild\(row\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "preserves get-saves-first order when wiring compact cloud actions" `
        @(
            "cloudPrimaryActionsParent = compact",
            "BuildCompactCloudPrimaryActionsRow\(pushPullRow, scale, _compactStackedActionRows\)",
            "CompactCloudPullText\(\)",
            "CompactCloudPushToggleText\(expanded: false\)",
            "CompactCloudPushDangerText\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Primary.cs" `
        "promotes compact retry recovery to a primary structured action" `
        @(
            'compact \? CompactRetryButtonText\(\) : "RETRY"',
            "LauncherButtonStyles\.ApplyPrimaryAction\(retryButton, scale\)",
            "SetCompactActionButtonText\(retryButton, retryButton\.Text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "labels compact retry recovery as TRY AGAIN with restart-task detail" `
        @(
            "CompactRetryButtonText",
            'CompactPlaySyncDrawerText\("Try Again", "Restart task"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "labels compact launch CTA with selected-version detail" `
        @(
            "CompactLaunchButtonText\(string text\)",
            "CompactLaunchButtonText",
            "Start Game",
            "Ready version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
        "applies compact launch CTA text to the launch button" `
        @(
            "SetCompactActionButtonText\(_launchButton",
            "CompactLaunchButtonText\(text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
        "reveals the framed play/sync action section when launch or retry actions are available" `
        @(
            "internal void ShowLaunch",
            "internal void ShowRetry",
            "internal void HideAll",
            "Visible = true",
            "Visible = false",
            "SetCloudControlsVisible",
            "ShowLaunchButtons",
            "ShowRetryButtons",
            "HideSecondaryButtons"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.SecondaryState.cs" `
        "models ready, retry, and hidden secondary button visibility presets" `
        @(
            "SecondaryButtonVisibility",
            "LaunchReady\(bool showUpdate\)",
            "Retry\(\)",
            "Hidden\(\)",
            "redownload: true",
            "support: true",
            "safeLaunch: true",
            "launch: true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Secondary.cs" `
        "applies secondary ready/retry/hidden button visibility to the action section" `
        @(
            "ShowLaunchButtons",
            "ShowRetryButtons",
            "HideSecondaryButtons",
            "SetSecondaryButtonsVisible",
            "ShowUpdateButton\(visibility\.Update\)",
            "_redownloadButton\.Visible = visibility\.Redownload",
            "_branchControlsAvailable = visibility\.Branch",
            "ApplyBranchControlVisibility",
            "SetSupportButtonsVisible\(visibility\.Support\)",
            "_readyVersionSummaryPanel\.Visible = _compact && visibility\.Launch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Support.cs" `
        "keeps compact update and support-tool visibility together" `
        @(
            "ShowUpdateButton",
            "CompactSupportToolText\(""Check Files"", ""Updates""\)",
            "Check for Updates",
            "SetSupportButtonsVisible",
            "_supportExpanded = false",
            "_supportGroup\.Visible = false",
            "SupportToggleText\(\)",
            "_diagnosticsButton\.Visible = visible",
            "_refreshVersionsButton\.Visible = visible",
            "_clearCachedVersionsButton\.Visible = visible",
            "_showLastErrorButton\.Visible = visible",
            "_copyRawLogButton\.Visible = visible"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "uses explicit Steam Cloud direction and overwrite-risk wording in the portal" `
        @(
            "pushPullRow",
            "pushButton",
            "confirmPushButton",
            "pushConfirmationLabel",
            "Push Locked",
            "CompactCloudPushDangerText\(\)",
            "CompactCloudPushConfirmText\(\)",
            "Pull Saves from Steam Cloud",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
        "keeps Steam Cloud Push behind an explicit arm and confirm flow" `
        @(
            "CloudPushArmRequested",
            "CloudPushArmRequested\?\.Invoke\(\) == false",
            "ArmCloudPush",
            "ConfirmCloudPush",
            "ResetCloudPushArm"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
        "uses explicit Steam Cloud direction wording in ready/action state" `
        @(
            "_readyVersionSummaryLabel",
            "Ready version:",
            "Start Game and Pull/Push use this version",
            "Push stays locked until explicitly opened",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Pull copies Steam Cloud saves to Android",
            "Push copies Android saves to Steam Cloud",
            "can overwrite remote saves",
            "Version/download actions affect local game files only",
            "Steam Cloud saves move only through Pull/Push"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Cloud.cs" `
        "collapses compact cloud drawers when cloud controls hide" `
        @(
            "SetCloudControlsVisible",
            "_cloudGroup\.Visible = visible",
            "ApplyCloudOptionVisibility\(visible\)",
            "_pushPullRow\.Visible = visible",
            "_cloudSafetyExpanded = false",
            "_cloudPushExpanded = false",
            "ResetCloudPushArm\(visible\)",
            "UpdateBranchHelpText",
            "SetPushPullDisabled",
            "ResetCloudPushArm\(_pushPullRow\.Visible\)",
            "_pushButton\.Disabled = disabled",
            "_cloudPushToggle\.Disabled = disabled",
            "_confirmPushButton\.Disabled = disabled",
            "_pullButton\.Disabled = disabled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
        "collapses compact cloud option drawer when cloud controls hide" `
        @(
            "_cloudOptionsExpanded = false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
        "uses compact cloud option labels that fit phone touch targets" `
        @(
            "_compact",
            "CompactCloudOptionText",
            "Save Backup",
            "Local safety",
            "Cloud Sync",
            "Steam saves",
            "Local Backup: \{OnOff\(value\)\}",
            "Game Cloud Sync: \{OnOff\(value\)\}"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ConstructionHelpers.cs" `
        "packs compact Save Backup and Cloud Sync options into responsive action rows" `
        @(
            "BuildCompactCloudOptionsRow",
            "compactStackedActionRows",
            "CompactCloudOptionToggleSeparation",
            "Visible = false",
            "new VBoxContainer\(\) : new HBoxContainer\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.Options.cs" `
        "wires compact Save Backup and Cloud Sync options into the cloud group" `
        @(
            "compactCloudOptionsRow = BuildCompactCloudOptionsRow",
            "_compactStackedActionRows",
            "AddCompactSupportToolButton\(cloudOptionsParent, ""Save Backup Off""",
            "AddCompactSupportToolButton\(cloudOptionsParent, ""Cloud Sync Off"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "keeps Pull and Push cloud actions beside the confirmation warning" `
        @(
            "cloudGroup.AddChild\(pushPullRow\)",
            "Pull Saves from Steam Cloud",
            "Push Locked",
            "CompactCloudPushConfirmText\(\)",
            "BuildCloudPushConfirmationLabel\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.Safety.cs" `
        "shows cloud-save safety guidance beside Pull/Push actions" `
        @(
            "cloudSafetyLabel",
            "OrangeHot",
            "CompactCloudSafetyDetailHeight",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "cloudGroup.AddChild\(cloudSafetyLabel\)",
            "CompactCloudSafetySummary\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Layout.cs" `
        "moves compact cloud-safety cue before Pull/Push controls" `
        @(
            "MoveCompactCloudSafetyCueBeforeCloudActions",
            "_cloudGroup\.MoveChild\(_cloudSafetyToggle, 0\)",
            "MoveChildAfter\(_cloudGroup, _cloudSafetyLabel, _cloudSafetyToggle\)",
            "MoveChildAfter\(_cloudGroup, _pushPullRow, _cloudSafetyLabel\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
        "labels launcher support log copy as review-before-sharing" `
        @(
            "Copy Launcher Log \(Review First\)",
            "Create Help Report",
            "Show Last Problem"
        )
}
