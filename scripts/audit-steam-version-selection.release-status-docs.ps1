. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-status-docs.release-note-ux.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-status-docs.release-policy.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-status-docs.release-limitations.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-status-docs.android-status.ps1")

function Add-SteamVersionSelectionReleaseStatusDocsChecks {
    Add-SteamVersionSelectionReleaseStatusDocsReleaseNoteUxChecks

    Add-SteamVersionSelectionReleaseStatusDocsReleasePolicyChecks

    Add-SteamVersionSelectionReleaseStatusDocsReleaseLimitationChecks

    Add-SteamVersionSelectionReleaseStatusDocsAndroidStatusChecks
}
