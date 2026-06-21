function Add-SteamVersionSelectionActionCoreLayoutChecks {
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
