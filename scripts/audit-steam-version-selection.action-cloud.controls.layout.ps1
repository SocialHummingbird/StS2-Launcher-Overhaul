function Add-SteamVersionSelectionActionCloudControlLayoutChecks {
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
            "BuildCompactCloudPrimaryActionsRow",
            "pushPullRow",
            "_compactStackedActionRows",
            "CompactCloudPullText\(\)",
            "CompactCloudPushToggleText\(expanded: false\)",
            "CompactCloudPushDangerText\(\)"
        )
}
