function Add-SteamVersionSelectionActionCloudSafetyCompactOptionChecks {
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
}
