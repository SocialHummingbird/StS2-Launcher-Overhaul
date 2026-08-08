. (Join-Path $PSScriptRoot "audit-steam-version-selection.download-workflows.model.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.download-workflows.actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.download-workflows.update-checks.ps1")

function Add-SteamVersionSelectionDownloadWorkflowChecks {
    Add-SteamVersionSelectionDownloadWorkflowModelChecks

    Add-SteamVersionSelectionDownloadWorkflowActionChecks

    Add-SteamVersionSelectionDownloadWorkflowUpdateCheckChecks
}
