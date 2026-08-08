param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [Parameter(Mandatory = $true)]
    [string]$KeystorePath,
    [Parameter(Mandatory = $true)]
    [string]$KeystorePassword,
    [Parameter(Mandatory = $true)]
    [string]$KeyAlias,
    [string]$KeytoolPath = "keytool",
    [switch]$OfflineBackupConfirmed
)

$ErrorActionPreference = "Stop"

if (-not $OfflineBackupConfirmed) {
    throw "Back up the v0.2.416 signing keystore to controlled offline storage and verify that backup first, then rerun with -OfflineBackupConfirmed. No GitHub secret or variable was changed."
}

. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI not found: gh"
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

gh secret set ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64 --repo $Repo --body $keystoreBase64
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64"
}

gh secret set ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD --repo $Repo --body $KeystorePassword
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD"
}

gh secret set ANDROID_LOCAL_UPDATE_KEY_ALIAS --repo $Repo --body $KeyAlias
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_LOCAL_UPDATE_KEY_ALIAS"
}

gh variable set ANDROID_LOCAL_UPDATE_SIGNER_SHA256 --repo $Repo --body $signerSha256
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_LOCAL_UPDATE_SIGNER_SHA256"
}

Write-Host "Configured v0.2.416 .local update signing for $Repo"
Write-Host "Keystore: $resolvedKeystore"
Write-Host "Alias: $KeyAlias"
Write-Host "ANDROID_LOCAL_UPDATE_SIGNER_SHA256=$signerSha256"
