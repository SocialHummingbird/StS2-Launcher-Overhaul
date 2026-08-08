param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.modules.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.registration.ps1")

Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-SteamVersionSelectionStaticAuditChecks

Complete-StaticAudit `
    -FailureHeading "Steam version-selection static audit failed:" `
    -SuccessMessage "Steam version-selection static audit passed: {0} checks."
