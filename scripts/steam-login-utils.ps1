$SteamGuardCodePattern = '^[A-Z0-9]{5}$'
$SteamLoginCrashPatterns = @(
    "FATAL EXCEPTION",
    "Fatal signal",
    "BUG: Unreferenced static string",
    "CryptoNative",
    "Interop+Crypto",
    "AndroidCryptoNative_",
    "SafeEvpCipherCtxHandle",
    "SafeSslHandle",
    "Android Java SHA-1 TryHashData bridge failed",
    "MethodAccessException",
    "MissingMethodException",
    "EntryPointNotFoundException",
    "Android Java random byte bridge returned an empty response",
    "HTTP bridge request failed: GET wss://",
    "Android Java HTTP bridge cannot handle WebSocket CM requests",
    "Steam CM WebSocket using managed .NET transport",
    "unknown protocol: wss"
)
$SteamLoginSuccessPatterns = @(
    "[Auth] Authentication successful",
    "[Launcher] Ownership verified"
)
$SteamLoginUnsupportedTargetPatterns = @(
    "Routing to native x86 fallback",
    "Showing native x86 fallback",
    "This Android x86 emulator cannot safely run the Godot/.NET runtime"
)
$SteamLoginFailurePatterns = @(
    "[Auth] Login failed",
    "Login failed:",
    "Could not establish a Steam auth connection",
    "The SteamClient instance must be connected",
    "InvalidPassword",
    "AccountLoginDenied",
    "AccountLogonDenied",
    "TwoFactorCodeMismatch",
    "ServiceUnavailable"
)
$SteamLoginInteractionRequiredPatterns = @(
    "Steam Guard",
    "two-factor",
    "TwoFactor",
    "EmailCode"
)

function Read-HiddenText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prompt
    )

    $secure = Read-Host $Prompt -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Normalize-SteamGuardCode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Code
    )

    $normalized = $Code.Trim().ToUpperInvariant()
    if ($normalized -notmatch $SteamGuardCodePattern) {
        throw "Steam Guard code must be exactly 5 letters or digits."
    }

    return $normalized
}

function ConvertFrom-SteamBase64 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $normalized = $Value.Trim()
    switch ($normalized.Length % 4) {
        2 { $normalized += "==" }
        3 { $normalized += "=" }
    }
    return [Convert]::FromBase64String($normalized)
}

function Get-SteamGuardCodeFromSharedSecret {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SharedSecret
    )

    $alphabet = "23456789BCDFGHJKMNPQRTVWXY"
    $secret = ConvertFrom-SteamBase64 $SharedSecret
    $unixTime = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $timeSlice = [Math]::Floor($unixTime / 30)
    $timeBytes = [BitConverter]::GetBytes([Int64]$timeSlice)
    if ([BitConverter]::IsLittleEndian) {
        [Array]::Reverse($timeBytes)
    }

    $hmac = [System.Security.Cryptography.HMACSHA1]::new($secret)
    try {
        $hash = $hmac.ComputeHash($timeBytes)
    } finally {
        $hmac.Dispose()
    }

    $offset = $hash[$hash.Length - 1] -band 0x0F
    $codePoint =
        (($hash[$offset] -band 0x7F) -shl 24) -bor
        (($hash[$offset + 1] -band 0xFF) -shl 16) -bor
        (($hash[$offset + 2] -band 0xFF) -shl 8) -bor
        ($hash[$offset + 3] -band 0xFF)

    $code = ""
    for ($i = 0; $i -lt 5; $i++) {
        $code += $alphabet[$codePoint % $alphabet.Length]
        $codePoint = [Math]::Floor($codePoint / $alphabet.Length)
    }

    return $code
}

function Resolve-SteamGuardCode {
    param(
        [string]$GuardCode = "",
        [switch]$PromptForGuardCode,
        $Credentials = $null
    )

    if (-not [string]::IsNullOrWhiteSpace($GuardCode)) {
        return Normalize-SteamGuardCode $GuardCode
    }

    if ($PromptForGuardCode) {
        return Normalize-SteamGuardCode (Read-HiddenText "Enter current Steam Guard code")
    }

    if ($Credentials -and $Credentials.shared_secret) {
        return Normalize-SteamGuardCode (Get-SteamGuardCodeFromSharedSecret $Credentials.shared_secret)
    }

    if ($Credentials -and $Credentials.guard_code) {
        return Normalize-SteamGuardCode ([string]$Credentials.guard_code)
    }

    return $null
}

function Resolve-SteamLauncherPackageName {
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
        Invoke-SteamLoginAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "pm", "list", "packages", "com.sts2launcher.overhaul.fork") |
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

function Invoke-SteamLoginAdb {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        & $AdbPath -s $DeviceSerial @Arguments
    } else {
        & $AdbPath @Arguments
    }

    if (-not $AllowFailure -and $LASTEXITCODE -ne 0) {
        throw "adb $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Invoke-SteamLoginAdbCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $output = @(& $AdbPath -s $DeviceSerial @Arguments 2>&1)
    } else {
        $output = @(& $AdbPath @Arguments 2>&1)
    }

    if (-not $AllowFailure -and $LASTEXITCODE -ne 0) {
        throw "adb $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return $output
}

function Clear-SteamLauncherForcedX86GodotFlag {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = ""
    )

    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "settings", "delete", "global", "sts2_force_godot_x86") -AllowFailure | Out-Null
}

function Start-SteamLauncherApp {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string]$PackageName
    )

    Clear-SteamLauncherForcedX86GodotFlag -AdbPath $AdbPath -DeviceSerial $DeviceSerial

    $launcherActivity = "$PackageName/com.game.sts2launcher.LauncherActivity"
    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "start", "-n", $launcherActivity) | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start launcher activity: $launcherActivity"
    }
}

function Write-SteamGuardCodeFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string]$PackageName,
        [Parameter(Mandatory = $true)]
        [string]$Code
    )

    $normalizedCode = Normalize-SteamGuardCode $Code
    $deviceDir = "/sdcard/Android/data/$PackageName/files"
    $devicePath = "$deviceDir/steam_guard_code.txt"
    $tempPath = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -LiteralPath $tempPath -Value $normalizedCode -NoNewline -Encoding ASCII
        Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "mkdir", "-p", $deviceDir) | Out-Null
        Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("push", $tempPath, $devicePath) | Out-Null
    } finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function Clear-SteamLoginHandoffFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string]$PackageName
    )

    $deviceDir = "/sdcard/Android/data/$PackageName/files"
    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "rm", "-f", "$deviceDir/steam_login_credentials.txt", "$deviceDir/steam_guard_code.txt") | Out-Null
}

function Write-SteamLoginCredentialFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string]$PackageName,
        [Parameter(Mandatory = $true)]
        [string]$Username,
        [Parameter(Mandatory = $true)]
        [string]$Password
    )

    $deviceDir = "/sdcard/Android/data/$PackageName/files"
    $devicePath = "$deviceDir/steam_login_credentials.txt"
    $tempPath = [System.IO.Path]::GetTempFileName()
    try {
        $usernameBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Username))
        $passwordBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Password))
        Set-Content -LiteralPath $tempPath -Value @($usernameBase64, $passwordBase64) -Encoding ASCII
        Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "mkdir", "-p", $deviceDir) | Out-Null
        Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("push", $tempPath, $devicePath) | Out-Null
    } finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function Send-AndroidInputText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $builder = [System.Text.StringBuilder]::new()
    foreach ($char in $Text.ToCharArray()) {
        $value = [string]$char
        if ($value -eq " ") {
            [void]$builder.Append("%s")
        } elseif ($value -match '^[a-zA-Z0-9._@%+=:,/-]$') {
            [void]$builder.Append($value)
        } else {
            [void]$builder.Append("\")
            [void]$builder.Append($value)
        }
    }

    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "input", "text", $builder.ToString()) | Out-Null
}

function Find-FirstSteamLoginLogPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Log,
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ($Log.IndexOf($pattern, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $pattern
        }
    }

    return $null
}

function Wait-SteamLoginPostGuardResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [int]$TimeoutSeconds = 180,
        [int]$PollSeconds = 3
    )

    $result = Wait-SteamLoginSignal `
        -AdbPath $AdbPath `
        -DeviceSerial $DeviceSerial `
        -TimeoutSeconds $TimeoutSeconds `
        -PollSeconds $PollSeconds `
        -AllowInteractionRequired:$false `
        -SuccessLabel "post-2FA success"

    if ($result -ne "success") {
        Write-Host "Post-2FA login result wait completed: $result"
    }

    return $result
}

function Wait-SteamLoginResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [int]$TimeoutSeconds = 180,
        [int]$PollSeconds = 3
    )

    return Wait-SteamLoginSignal `
        -AdbPath $AdbPath `
        -DeviceSerial $DeviceSerial `
        -TimeoutSeconds $TimeoutSeconds `
        -PollSeconds $PollSeconds `
        -AllowInteractionRequired:$true `
        -SuccessLabel "auth success"
}

function Wait-SteamLoginSignal {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$DeviceSerial = "",
        [int]$TimeoutSeconds = 180,
        [int]$PollSeconds = 3,
        [bool]$AllowInteractionRequired = $true,
        [string]$SuccessLabel = "auth success"
    )

    if ($TimeoutSeconds -lt 1) {
        $TimeoutSeconds = 1
    }

    if ($PollSeconds -lt 1) {
        $PollSeconds = 1
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Seconds $PollSeconds
        $currentLog = (
            Invoke-SteamLoginAdbCapture `
                -AdbPath $AdbPath `
                -DeviceSerial $DeviceSerial `
                -Arguments @("logcat", "-d", "-v", "time") |
                Out-String
        )

        $matchedSuccess = Find-FirstSteamLoginLogPattern -Log $currentLog -Patterns $SteamLoginSuccessPatterns
        if ($matchedSuccess) {
            Write-Host "Detected ${SuccessLabel} signal: $matchedSuccess"
            return "success"
        }

        $matchedUnsupportedTarget = Find-FirstSteamLoginLogPattern -Log $currentLog -Patterns $SteamLoginUnsupportedTargetPatterns
        if ($matchedUnsupportedTarget) {
            Write-Host "Detected unsupported validation target: $matchedUnsupportedTarget"
            return "unsupported-target"
        }

        $matchedCrash = Find-FirstSteamLoginLogPattern -Log $currentLog -Patterns $SteamLoginCrashPatterns
        if ($matchedCrash) {
            Write-Host "Detected crash signature: $matchedCrash"
            return "crash"
        }

        $matchedFailure = Find-FirstSteamLoginLogPattern -Log $currentLog -Patterns $SteamLoginFailurePatterns
        if ($matchedFailure) {
            Write-Host "Detected auth failure signal: $matchedFailure"
            return "failure"
        }

        if ($AllowInteractionRequired) {
            $matchedInteractionRequired = Find-FirstSteamLoginLogPattern -Log $currentLog -Patterns $SteamLoginInteractionRequiredPatterns
            if ($matchedInteractionRequired) {
                Write-Host "Detected interaction-required signal: $matchedInteractionRequired"
                return "interaction-required"
            }
        }
    } while ((Get-Date) -lt $deadline)

    Write-Host "Timed out waiting for auth result after $TimeoutSeconds seconds."
    return "timeout"
}
