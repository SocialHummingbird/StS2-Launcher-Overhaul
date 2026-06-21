function Add-SteamVersionSelectionRuntimeDownloadWorkflowBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.ps1" `
        "keeps download, update, and branch-refresh workflow audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowChecks",
            "audit-steam-version-selection.download-workflows.model.ps1",
            "audit-steam-version-selection.download-workflows.actions.ps1",
            "audit-steam-version-selection.download-workflows.update-checks.ps1",
            "Add-SteamVersionSelectionDownloadWorkflowModelChecks",
            "Add-SteamVersionSelectionDownloadWorkflowActionChecks",
            "Add-SteamVersionSelectionDownloadWorkflowUpdateCheckChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.model.ps1" `
        "keeps launcher model download and depot connection workflow checks focused" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowModelChecks",
            "LauncherModel.Downloads.cs",
            "LauncherModel.Downloads.Action.cs",
            "LauncherModel.Downloads.RunGuard.cs",
            "LauncherModel.Downloads.Start.cs",
            "LauncherModel.Downloads.Catalog.cs",
            "LauncherModel.Downloads.Connection.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.actions.ps1" `
        "keeps download controller action and cache workflow checks focused" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowActionChecks",
            "LauncherController.Downloads.Actions.cs",
            "LauncherController.Downloads.Execution.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.update-checks.ps1" `
        "keeps selected-version update-check workflow checks focused" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowUpdateCheckChecks",
            "LauncherController.UpdateChecks.cs",
            "LauncherController.UpdateChecks.ViewUpdate.cs",
            "LauncherController.UpdateChecks.Run.cs",
            "LauncherController.UpdateChecks.Workflow.cs",
            "LauncherController.UpdateChecks.Results.cs"
        )
}
