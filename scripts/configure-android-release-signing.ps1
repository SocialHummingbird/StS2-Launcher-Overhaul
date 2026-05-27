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

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI not found: gh"
}

if (-not (Test-Path -LiteralPath $KeystorePath)) {
    throw "Keystore not found: $KeystorePath"
}

$resolvedKeystore = (Resolve-Path -LiteralPath $KeystorePath).Path
$keytool = Get-Command $KeytoolPath -ErrorAction SilentlyContinue
if (-not $keytool) {
    throw "keytool not found. Pass -KeytoolPath or install a JDK."
}

$certOutput = & $keytool.Source `
    -list `
    -v `
    -keystore $resolvedKeystore `
    -storepass $KeystorePassword `
    -alias $KeyAlias 2>&1

if ($LASTEXITCODE -ne 0) {
    throw "keytool failed for alias '$KeyAlias'. Output:`n$($certOutput -join "`n")"
}

$shaMatch = [regex]::Match(($certOutput -join "`n"), "SHA256:\s*([A-Fa-f0-9:]+)")
if (-not $shaMatch.Success) {
    throw "Could not read SHA256 certificate fingerprint from keytool output."
}

$signerSha256 = ($shaMatch.Groups[1].Value -replace ":", "").ToUpperInvariant()
$keystoreBase64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($resolvedKeystore))

$keystoreBase64 | gh secret set ANDROID_RELEASE_KEYSTORE_BASE64 --repo $Repo
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_RELEASE_KEYSTORE_BASE64"
}

$KeystorePassword | gh secret set ANDROID_RELEASE_KEYSTORE_PASSWORD --repo $Repo
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set ANDROID_RELEASE_KEYSTORE_PASSWORD"
}

$KeyAlias | gh secret set ANDROID_RELEASE_KEY_ALIAS --repo $Repo
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
