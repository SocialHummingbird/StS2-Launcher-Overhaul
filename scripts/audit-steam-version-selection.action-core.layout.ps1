function Add-SteamVersionSelectionActionCoreLayoutChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Destinations.cs" `
        "assigns each existing workflow to one stable destination at construction time" `
        @(
            "BuildDestinationLayout",
            'BuildDestination\(\s*"Home"',
            'BuildDestination\(\s*"Saves"',
            'BuildDestination\(\s*"Versions"',
            'BuildDestination\(\s*"Mods"',
            'BuildDestination\(\s*"Help"',
            "MoveTo\(_homeDestination, _launchButton\)",
            "MoveTo\(_savesDestination, _cloudGroup\)",
            "MoveTo\(_versionsDestination, _branchDropdown\)",
            "MoveTo\(_modsDestination, _modsGroup\)",
            "MoveTo\(_helpDestination, _diagnosticsButton\)",
            "SetDestination\(LauncherDestination\.Home\)"
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
