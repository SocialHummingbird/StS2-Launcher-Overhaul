. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.local-backups.local-saves.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.local-backups.backup-evidence.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.local-backups.push-enforcement.ps1")

function Add-SteamVersionSelectionCloudSafetyLocalBackupChecks {
    Add-SteamVersionSelectionCloudSafetyLocalSaveEvidenceChecks
    Add-SteamVersionSelectionCloudSafetyBackupEvidenceChecks
    Add-SteamVersionSelectionCloudSafetyBackupPushEnforcementChecks
}
