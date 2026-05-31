function Invoke-CheckedTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tool,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & $Tool @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Tool $($Arguments -join ' ')`n$($output -join "`n")"
    }

    return ($output -join "`n")
}

function Normalize-Sha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return ($Value -replace ":", "" -replace "\s", "").ToUpperInvariant()
}

function Resolve-AndroidBuildTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

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

function Get-AndroidApkIdentity {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [string]$Aapt = "",
        [string]$ApkSigner = ""
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "APK not found: $Path"
    }

    if (-not $Aapt) {
        $Aapt = Resolve-AndroidBuildTool "aapt"
    }
    if (-not $ApkSigner) {
        $ApkSigner = Resolve-AndroidBuildTool "apksigner"
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

function Get-KeystoreSignerSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$KeystorePath,
        [Parameter(Mandatory = $true)]
        [string]$KeystorePassword,
        [Parameter(Mandatory = $true)]
        [string]$KeyAlias,
        [string]$KeytoolPath = "keytool"
    )

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

    return Normalize-Sha256 $shaMatch.Groups[1].Value
}
