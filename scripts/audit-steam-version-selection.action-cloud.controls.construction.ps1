function Add-SteamVersionSelectionActionCloudControlConstructionChecks {
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
}
