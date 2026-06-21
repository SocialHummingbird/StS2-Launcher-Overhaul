function Add-SteamVersionSelectionAutomationChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Automation.cs" `
        "keeps launcher automation entry point thin around request consumption and execution" `
        @(
            "AutomationFileName = ""launcher_automation_action\.txt""",
            "AutomationMarkerFileName = ""last_launcher_automation\.txt""",
            "TryStartAutomation",
            "LauncherAutomationRequest\.TryConsume",
            "RunAutomationAsync"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Automation.Request.cs" `
        "parses and consumes launcher automation requests from data-directory files" `
        @(
            "LauncherAutomationRequest",
            "Path\.Combine\(dataDir, AutomationFileName\)",
            "File\.ReadAllLines",
            "File\.Delete",
            "ReadValue\(lines, ""action""\)",
            "ReadValue\(lines, ""branch""\)",
            "SteamGameBranch\.Public",
            "RefreshCatalog",
            "CheckUpdates",
            "Redownload",
            "Download",
            "LaunchSafe"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Automation.Run.cs" `
        "runs launcher automation through normal branch, catalog, download, and safe-launch paths" `
        @(
            "RunAutomationAsync",
            "WriteAutomationMarker\(request, ""started""\)",
            "LauncherPreferences\.SaveGameBranch",
            "LauncherBranchAvailabilityStatus\.Clear",
            "RefreshGameBranchOptions",
            "RefreshBranchCatalogAsync",
            "CheckForUpdatesAsync",
            "ResetGameFilesForRedownload",
            "StartDownloadAsync",
            "RefreshSelectedRuntimeSlotEvidence",
            "LaunchSafe",
            "WriteAutomationMarker\(request, ""completed""\)",
            "WriteAutomationMarker\(request, ""failed"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Automation.Marker.cs" `
        "records launcher automation status without driving cloud push side effects" `
        @(
            "WriteAutomationMarker",
            "Path\.Combine\(_model\.DataDir, AutomationMarkerFileName\)",
            "UTC:",
            "Status:",
            "Action:",
            "Selected branch:",
            "Requested branch:",
            "Message:",
            "SteamGameBranch\.Normalize\(LauncherPreferences\.ReadGameBranch\(\)\)"
        )
}
