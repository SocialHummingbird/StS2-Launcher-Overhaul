function Add-SteamVersionSelectionActionCloudControlsChecks {
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
}

function Add-SteamVersionSelectionActionCloudSafetyChecks {
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
}
