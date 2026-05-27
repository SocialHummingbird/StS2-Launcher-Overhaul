param(
    [Parameter(Mandatory = $true)]
    [string]$ApkPath,
    [string]$PreviousApkPath = "",
    [string]$ExpectedPackageName = "",
    [string]$ExpectedSignerSha256 = "",
    [int64]$ExpectedMinVersionCode = 0
)

$ErrorActionPreference = "Stop"

function Resolve-AndroidTool([string]$Name) {
    $extensions = @("")
    if ($IsWindows) {
        $extensions = @(".bat", ".exe", "")
    }

    $sdkRoots = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT) |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } |
        Select-Object -Unique

    foreach ($sdkRoot in $sdkRoots) {
        $buildToolsRoot = Join-Path $sdkRoot "build-tools"
        if (-not (Test-Path -LiteralPath $buildToolsRoot)) {
            continue
        }

        $buildToolVersions = Get-ChildItem -LiteralPath $buildToolsRoot -Directory |
            Sort-Object Name -Descending

        foreach ($buildToolVersion in $buildToolVersions) {
            foreach ($extension in $extensions) {
                $candidate = Join-Path $buildToolVersion.FullName "$Name$extension"
                if (Test-Path -LiteralPath $candidate) {
                    return $candidate
                }
            }
        }
    }

    foreach ($extension in $extensions) {
        $command = Get-Command "$Name$extension" -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    throw "Android SDK tool not found: $Name. Set ANDROID_HOME or ANDROID_SDK_ROOT to an SDK with build-tools installed."
}

function Invoke-CheckedTool([string]$Tool, [string[]]$Arguments) {
    $output = & $Tool @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Tool $($Arguments -join ' ')`n$($output -join "`n")"
    }

    return ($output -join "`n")
}

function Normalize-Sha256([string]$Value) {
    return ($Value -replace ":", "" -replace "\s", "").ToUpperInvariant()
}

function Get-ApkIdentity([string]$Path, [string]$Aapt, [string]$ApkSigner) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "APK not found: $Path"
    }

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $badging = Invoke-CheckedTool $Aapt @("dump", "badging", $resolvedPath)
    $packageMatch = [regex]::Match($badging, "package:\s+name='([^']+)'\s+versionCode='([0-9]+)'\s+versionName='([^']*)'")
    if (-not $packageMatch.Success) {
        throw "Unable to read package identity from APK: $resolvedPath"
    }

    $certOutput = Invoke-CheckedTool $ApkSigner @("verify", "--print-certs", $resolvedPath)
    $certMatch = [regex]::Match($certOutput, "certificate SHA-256 digest:\s*([A-Fa-f0-9:\s]+)")
    if (-not $certMatch.Success) {
        throw "Unable to read APK signer SHA-256 digest from: $resolvedPath"
    }

    return [ordered]@{
        path = $resolvedPath
        packageName = $packageMatch.Groups[1].Value
        versionCode = [int64]$packageMatch.Groups[2].Value
        versionName = $packageMatch.Groups[3].Value
        signerSha256 = Normalize-Sha256 $certMatch.Groups[1].Value
    }
}

$aapt = Resolve-AndroidTool "aapt"
$apkSigner = Resolve-AndroidTool "apksigner"
$current = Get-ApkIdentity -Path $ApkPath -Aapt $aapt -ApkSigner $apkSigner

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
    $previous = Get-ApkIdentity -Path $PreviousApkPath -Aapt $aapt -ApkSigner $apkSigner

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
