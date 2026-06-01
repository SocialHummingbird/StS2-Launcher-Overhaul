param(
    [Parameter(Mandatory = $true)]
    [string]$ApkPath,
    [string]$PreviousApkPath = "",
    [string]$ExpectedPackageName = "",
    [string]$ExpectedSignerSha256 = "",
    [int64]$ExpectedMinVersionCode = 0
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

$current = Get-AndroidApkIdentity -Path $ApkPath

if ($ExpectedPackageName -and $current.packageName -ne $ExpectedPackageName) {
    throw "APK package mismatch. Expected $ExpectedPackageName, got $($current.packageName). This APK cannot update the intended app."
}

if ($ExpectedMinVersionCode -gt 0 -and $current.versionCode -lt $ExpectedMinVersionCode) {
    throw "APK versionCode $($current.versionCode) is below required minimum $ExpectedMinVersionCode."
}

if ($ExpectedSignerSha256) {
    $expectedSigner = Normalize-Sha256 $ExpectedSignerSha256
    if ($current.signerSha256 -ne $expectedSigner) {
        throw "APK signer mismatch. Expected $expectedSigner, got $($current.signerSha256). This APK cannot update existing installs signed by the expected key."
    }
}

if ($PreviousApkPath) {
    $previous = Get-AndroidApkIdentity -Path $PreviousApkPath

    if ($current.packageName -ne $previous.packageName) {
        throw "APK cannot update previous release. Package changed from $($previous.packageName) to $($current.packageName)."
    }

    if ($current.signerSha256 -ne $previous.signerSha256) {
        throw "APK cannot update previous release. Signing certificate changed from $($previous.signerSha256) to $($current.signerSha256)."
    }

    if ($current.versionCode -le $previous.versionCode) {
        throw "APK cannot update previous release. versionCode must increase: previous $($previous.versionCode), current $($current.versionCode)."
    }

    Write-Host "Update compatibility OK:"
    Write-Host "  previous: $($previous.packageName) $($previous.versionName) ($($previous.versionCode)) signer=$($previous.signerSha256)"
    Write-Host "  current:  $($current.packageName) $($current.versionName) ($($current.versionCode)) signer=$($current.signerSha256)"
} else {
    Write-Host "APK identity OK:"
    Write-Host "  current: $($current.packageName) $($current.versionName) ($($current.versionCode)) signer=$($current.signerSha256)"
}
