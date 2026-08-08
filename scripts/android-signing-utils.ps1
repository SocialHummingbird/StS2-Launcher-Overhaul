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

function Resolve-Keytool {
    param(
        [string]$KeytoolPath = ""
    )

    if ($KeytoolPath) {
        if (Test-Path -LiteralPath $KeytoolPath -PathType Leaf) {
            return (Resolve-Path -LiteralPath $KeytoolPath).Path
        }

        $explicitCommand = Get-Command $KeytoolPath -ErrorAction SilentlyContinue
        if ($explicitCommand) {
            return $explicitCommand.Source
        }

        throw "keytool not found at the explicit path or command: $KeytoolPath"
    }

    $runningOnWindows = $env:OS -eq "Windows_NT"
    $keytoolExecutable = if ($runningOnWindows) { "keytool.exe" } else { "keytool" }
    $userHome = if ($env:USERPROFILE) { $env:USERPROFILE } else { $HOME }
    $javaHomes = @(
        $env:JAVA_HOME,
        (Join-Path $userHome ".w40k-android-toolchain\jdk-17"),
        (Join-Path $userHome ".w40k-android-toolchain\jdk-21")
    ) | Where-Object { $_ } | Select-Object -Unique

    foreach ($javaHome in $javaHomes) {
        $candidate = Join-Path $javaHome "bin\$keytoolExecutable"
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $pathCommand = Get-Command $keytoolExecutable -ErrorAction SilentlyContinue
    if ($pathCommand) {
        return $pathCommand.Source
    }

    throw "keytool not found. Pass -KeytoolPath, set JAVA_HOME, or install the repo JDK under .w40k-android-toolchain."
}

function Invoke-ProcessWithStandardInput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Executable,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$StandardInput,
        [Parameter(Mandatory = $true)]
        [string]$FailureMessage,
        [string[]]$SensitiveValues = @()
    )

    foreach ($argument in $Arguments) {
        if ([string]::IsNullOrWhiteSpace($argument) -or $argument -notmatch '^[A-Za-z0-9_./:\\=-]+$') {
            throw "Refusing an unsafe child-process argument."
        }
    }

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $Executable
    $startInfo.Arguments = $Arguments -join ' '
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $standardInputEncoding = New-Object Text.UTF8Encoding($false)
    if ($startInfo.PSObject.Properties.Name -contains 'StandardInputEncoding') {
        $startInfo.StandardInputEncoding = $standardInputEncoding
    }
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $previousConsoleInputEncoding = [Console]::InputEncoding
    try {
        [Console]::InputEncoding = $standardInputEncoding
        if (-not $process.Start()) {
            throw $FailureMessage
        }
        $process.StandardInput.Write($StandardInput)
        $process.StandardInput.Close()
        $standardOutput = $process.StandardOutput.ReadToEnd()
        $standardError = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            $safeOutput = "$standardOutput`n$standardError"
            foreach ($sensitiveValue in $SensitiveValues) {
                if (-not [string]::IsNullOrEmpty($sensitiveValue)) {
                    $safeOutput = $safeOutput.Replace($sensitiveValue, '<redacted>')
                }
            }
            throw "$FailureMessage`n$($safeOutput.Trim())"
        }
    } finally {
        [Console]::InputEncoding = $previousConsoleInputEncoding
        $process.Dispose()
    }
}

function Resolve-AndroidBuildTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $extensions = @("")
    $runningOnWindows = $env:OS -eq "Windows_NT"
    if ($runningOnWindows) {
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
        [string]$KeytoolPath = ""
    )

    if (-not (Test-Path -LiteralPath $KeystorePath)) {
        throw "Keystore not found: $KeystorePath"
    }

    if ([string]::IsNullOrEmpty($KeystorePassword)) {
        throw "Keystore password must not be empty."
    }

    $resolvedKeystore = (Resolve-Path -LiteralPath $KeystorePath).Path
    $resolvedKeytool = Resolve-Keytool -KeytoolPath $KeytoolPath
    $passwordEnvironmentName = "STS2_KEYTOOL_STOREPASS_$([Guid]::NewGuid().ToString('N').ToUpperInvariant())"
    $previousPasswordValue = [Environment]::GetEnvironmentVariable(
        $passwordEnvironmentName,
        [EnvironmentVariableTarget]::Process
    )
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        [Environment]::SetEnvironmentVariable(
            $passwordEnvironmentName,
            $KeystorePassword,
            [EnvironmentVariableTarget]::Process
        )
        # Native stderr must be captured and redacted before it can become a
        # terminating PowerShell error under a caller's Stop preference.
        $ErrorActionPreference = "Continue"
        $certOutput = & $resolvedKeytool `
            -list `
            -v `
            -keystore $resolvedKeystore `
            '-storepass:env' $passwordEnvironmentName `
            -alias $KeyAlias 2>&1
        $keytoolExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
        [Environment]::SetEnvironmentVariable(
            $passwordEnvironmentName,
            $previousPasswordValue,
            [EnvironmentVariableTarget]::Process
        )
    }

    if ($keytoolExitCode -ne 0) {
        $safeOutput = ($certOutput -join "`n").Replace($KeystorePassword, "<redacted>")
        if (-not [string]::IsNullOrEmpty($KeyAlias)) {
            $safeOutput = $safeOutput.Replace($KeyAlias, "<redacted>")
        }
        throw "keytool failed while inspecting the requested entry. Output:`n$safeOutput"
    }

    $shaMatch = [regex]::Match(($certOutput -join "`n"), "SHA256:\s*([A-Fa-f0-9:]+)")
    if (-not $shaMatch.Success) {
        throw "Could not read SHA256 certificate fingerprint from keytool output."
    }

    return Normalize-Sha256 $shaMatch.Groups[1].Value
}
