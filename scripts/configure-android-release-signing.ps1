param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [Parameter(Mandatory = $true)]
    [string]$KeystorePath,
    [Parameter(Mandatory = $true)]
    [string]$KeystorePassword,
    [Parameter(Mandatory = $true)]
    [string]$KeyAlias,
    [string]$KeytoolPath = "",
    [switch]$OfflineBackupConfirmed
)

$ErrorActionPreference = "Stop"

if (-not $OfflineBackupConfirmed) {
    throw "Back up the v0.2.416 signing keystore to controlled offline storage and verify that backup first, then rerun with -OfflineBackupConfirmed. No GitHub secret or variable was changed."
}

. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

$ghCommand = Get-Command gh -ErrorAction SilentlyContinue
if (-not $ghCommand) {
    throw "GitHub CLI not found: gh"
}

if ($Repo -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    throw "Repository must use the owner/name form without whitespace."
}

$signingEnvironment = "android-local-signing"
$requiredReleaseBranch = "main"

function Get-GitHubApiJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Endpoint
    )

    $apiOutput = @(& $ghCommand.Source api --method GET $Endpoint 2>&1)
    $apiExitCode = $LASTEXITCODE
    if ($apiExitCode -ne 0) {
        throw "Could not verify the protected $signingEnvironment environment. Configure it manually first; no GitHub secret or variable was changed."
    }

    try {
        return (($apiOutput -join "`n") | ConvertFrom-Json)
    } catch {
        throw "GitHub returned invalid environment-protection metadata. No GitHub secret or variable was changed."
    }
}

$environmentMetadata = Get-GitHubApiJson `
    -Endpoint "repos/$Repo/environments/$signingEnvironment"
$deploymentPolicy = $environmentMetadata.deployment_branch_policy
if (-not $deploymentPolicy -or
    $deploymentPolicy.protected_branches -or
    -not $deploymentPolicy.custom_branch_policies) {
    throw "Environment $signingEnvironment must use a custom deployment-branch policy restricted to $requiredReleaseBranch. No GitHub secret or variable was changed."
}

$branchPolicyMetadata = Get-GitHubApiJson `
    -Endpoint "repos/$Repo/environments/$signingEnvironment/deployment-branch-policies?per_page=100"
$branchPolicies = @($branchPolicyMetadata.branch_policies)
if ($branchPolicyMetadata.total_count -ne 1 -or
    $branchPolicies.Count -ne 1 -or
    $branchPolicies[0].name -cne $requiredReleaseBranch -or
    $branchPolicies[0].type -cne "branch") {
    throw "Environment $signingEnvironment must allow exactly the $requiredReleaseBranch branch and no tag or wildcard policy. No GitHub secret or variable was changed."
}

if (-not (Test-Path -LiteralPath $KeystorePath)) {
    throw "Keystore not found: $KeystorePath"
}

$resolvedKeystore = (Resolve-Path -LiteralPath $KeystorePath).Path
$signerSha256 = Get-KeystoreSignerSha256 -KeystorePath $resolvedKeystore -KeystorePassword $KeystorePassword -KeyAlias $KeyAlias -KeytoolPath $KeytoolPath
$expectedSigner = "FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A"
if ($signerSha256 -ne $expectedSigner) {
    throw "Keystore signer $signerSha256 does not match the published v0.2.416 signer $expectedSigner. No GitHub secret or variable was changed."
}
$keystoreBase64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($resolvedKeystore))

Invoke-ProcessWithStandardInput `
    -Executable $ghCommand.Source `
    -Arguments @('secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64', '--env', $signingEnvironment, '--repo', $Repo) `
    -StandardInput $keystoreBase64 `
    -FailureMessage "Failed to set ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64" `
    -SensitiveValues @($keystoreBase64)
Invoke-ProcessWithStandardInput `
    -Executable $ghCommand.Source `
    -Arguments @('secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD', '--env', $signingEnvironment, '--repo', $Repo) `
    -StandardInput $KeystorePassword `
    -FailureMessage "Failed to set ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD" `
    -SensitiveValues @($KeystorePassword)
Invoke-ProcessWithStandardInput `
    -Executable $ghCommand.Source `
    -Arguments @('secret', 'set', 'ANDROID_LOCAL_UPDATE_KEY_ALIAS', '--env', $signingEnvironment, '--repo', $Repo) `
    -StandardInput $KeyAlias `
    -FailureMessage "Failed to set ANDROID_LOCAL_UPDATE_KEY_ALIAS" `
    -SensitiveValues @($KeyAlias)
Invoke-ProcessWithStandardInput `
    -Executable $ghCommand.Source `
    -Arguments @('variable', 'set', 'ANDROID_LOCAL_UPDATE_SIGNER_SHA256', '--env', $signingEnvironment, '--repo', $Repo) `
    -StandardInput $signerSha256 `
    -FailureMessage "Failed to set ANDROID_LOCAL_UPDATE_SIGNER_SHA256"

Write-Host "Configured v0.2.416 .local update signing for $Repo environment $signingEnvironment"
Write-Host "Keystore: $resolvedKeystore"
Write-Host "ANDROID_LOCAL_UPDATE_SIGNER_SHA256=$signerSha256"
