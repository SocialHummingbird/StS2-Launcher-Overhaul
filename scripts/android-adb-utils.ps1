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
