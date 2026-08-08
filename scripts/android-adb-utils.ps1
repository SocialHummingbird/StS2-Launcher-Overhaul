function Resolve-AndroidAdbPath {
    param(
        [string]$AdbPath = "adb"
    )

    if (-not [string]::IsNullOrWhiteSpace($AdbPath)) {
        if (Test-Path -LiteralPath $AdbPath) {
            return (Resolve-Path -LiteralPath $AdbPath).Path
        }

        $adbCommand = Get-Command $AdbPath -ErrorAction SilentlyContinue
        if ($adbCommand -and $adbCommand.Source) {
            return (Resolve-Path -LiteralPath $adbCommand.Source).Path
        }
    }

    $candidateRoots = @(
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk"),
        (Join-Path $env:USERPROFILE ".w40k-android-toolchain\android-sdk")
    ) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { (Resolve-Path -LiteralPath $_ -ErrorAction SilentlyContinue).Path } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique

    foreach ($root in $candidateRoots) {
        foreach ($name in @("adb.exe", "adb")) {
            $candidate = Join-Path $root (Join-Path "platform-tools" $name)
            if (Test-Path -LiteralPath $candidate) {
                return (Resolve-Path -LiteralPath $candidate).Path
            }
        }
    }

    throw "ADB not found. Pass -AdbPath with an adb executable or set ANDROID_HOME/ANDROID_SDK_ROOT to an Android SDK with platform-tools installed."
}

function Assert-AndroidAdbPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    if (-not (Test-Path -LiteralPath $AdbPath)) {
        throw "ADB not found: $AdbPath"
    }
}

function Invoke-AndroidAdb {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    if ($DeviceSerial) {
        & $AdbPath "-s" $DeviceSerial @Arguments
    } else {
        & $AdbPath @Arguments
    }
    if ($LASTEXITCODE -ne 0) {
        throw "adb $($Arguments -join ' ') failed"
    }
}

function Invoke-AndroidAdbCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if ($DeviceSerial) {
            return @(& $AdbPath "-s" $DeviceSerial @Arguments 2>&1)
        }

        return @(& $AdbPath @Arguments 2>&1)
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Find-AndroidTargetDevice {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = ""
    )

    if ($DeviceSerial) {
        $matchingLine = @(& $AdbPath devices | Select-Object -Skip 1 | Where-Object { $_ -match "^$([regex]::Escape($DeviceSerial))\s+device$" })
        if ($matchingLine.Count -eq 0) {
            return $null
        }
        return $DeviceSerial
    }

    $deviceLines = @(& $AdbPath devices | Select-Object -Skip 1 | Where-Object { $_ -match "\tdevice$" })
    if ($deviceLines.Count -eq 0) {
        return $null
    }
    if ($deviceLines.Count -gt 1) {
        $serials = $deviceLines | ForEach-Object { ($_ -split "\s+")[0] }
        throw "Multiple Android devices/emulators attached. Pass -DeviceSerial with one of: $($serials -join ', ')"
    }
    return ($deviceLines[0] -split "\s+")[0]
}

function Resolve-AndroidTargetDevice {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [int]$WaitForDeviceSeconds = 0
    )

    $deadline = (Get-Date).AddSeconds($WaitForDeviceSeconds)

    while ($true) {
        $device = Find-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial
        if ($device) {
            return $device
        }

        if ((Get-Date) -ge $deadline) {
            if ($DeviceSerial) {
                throw "Requested Android device/emulator is not attached or not in 'device' state: $DeviceSerial"
            }
            throw "No attached Android device/emulator."
        }

        Start-Sleep -Seconds 1
    }
}

function Get-AndroidLauncherComponent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageName
    )

    return "$PackageName/com.game.sts2launcher.LauncherActivity"
}

function Resolve-AndroidInstalledLauncherPackageName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [string]$PackageName = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($PackageName)) {
        return $PackageName
    }

    $packages = @(
        Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "pm", "list", "packages", "com.sts2launcher.overhaul.fork") |
            ForEach-Object {
                $line = ([string]$_).Trim()
                if ($line.StartsWith("package:")) {
                    $line.Substring("package:".Length)
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )

    if ($packages.Count -eq 1) {
        Write-Host "Resolved installed launcher package: $($packages[0])"
        return $packages[0]
    }

    if ($packages.Count -gt 1) {
        throw "Multiple StS2 launcher packages are installed: $($packages -join ', '). Pass -PackageName explicitly."
    }

    throw "No installed StS2 launcher package found. Pass -PackageName explicitly."
}
