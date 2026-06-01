param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [Parameter(Mandatory = $true)]
    [string]$KeystorePath,
    [Parameter(Mandatory = $true)]
    [string]$KeystorePassword,
    [Parameter(Mandatory = $true)]
    [string]$KeyAlias,
    [string]$KeytoolPath = "keytool"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI not found: gh"
}

if (-not (Test-Path -LiteralPath $KeystorePath)) {
    throw "Keystore not found: $KeystorePath"
}

$resolvedKeystore = (Resolve-Path -LiteralPath $KeystorePath).Path
$signerSha256 = Get-KeystoreSignerSha256 -KeystorePath $resolvedKeystore -KeystorePassword $KeystorePassword -KeyAlias $KeyAlias -KeytoolPath $KeytoolPath
$keystoreBase64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($resolvedKeystore))

gh secret set ANDROID_RELEASE_KEYSTORE_BASE64 --repo $Repo --body $keystoreBase64
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_RELEASE_KEYSTORE_BASE64"
}

gh secret set ANDROID_RELEASE_KEYSTORE_PASSWORD --repo $Repo --body $KeystorePassword
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_RELEASE_KEYSTORE_PASSWORD"
}

gh secret set ANDROID_RELEASE_KEY_ALIAS --repo $Repo --body $KeyAlias
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_RELEASE_KEY_ALIAS"
}

gh variable set ANDROID_RELEASE_SIGNER_SHA256 --repo $Repo --body $signerSha256
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_RELEASE_SIGNER_SHA256"
}

Write-Host "Configured Android release signing for $Repo"
Write-Host "Keystore: $resolvedKeystore"
Write-Host "Alias: $KeyAlias"
Write-Host "ANDROID_RELEASE_SIGNER_SHA256=$signerSha256"
