param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.helpers.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.runtime-slot.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.runtime-pack.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.patch-compatibility.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.native-cache.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.startup-patches.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.save-safety.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.diagnostics.ps1")
. (Join-Path $PSScriptRoot "audit-multi-version-runtime.evidence-tooling.ps1")
Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-MultiVersionRuntimeHelperChecks

Add-MultiVersionRuntimeSlotChecks

Add-MultiVersionRuntimePackChecks

Add-MultiVersionRuntimePatchCompatibilityChecks

Add-MultiVersionRuntimeNativeCacheChecks

Add-MultiVersionRuntimeStartupPatchChecks

Add-MultiVersionRuntimeSaveSafetyChecks

Add-MultiVersionRuntimeDiagnosticsChecks

Add-MultiVersionRuntimeEvidenceToolingChecks

Complete-StaticAudit `
    -FailureHeading "Multi-version runtime audit failed:" `
    -SuccessMessage "Multi-version runtime audit passed ({0} checks)." `
    -ThrowOnFailure
