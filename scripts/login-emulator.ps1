param(
    [string]$CredentialsPath = "tmp\steam-login.local.json",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$GuardCode = "",
    [switch]$PromptForGuardCode,
    [switch]$UseLocalCredentialFile,
    [switch]$Launch,
    [int]$StartupDelaySeconds = 15
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $CredentialsPath)) {
    throw "Credentials file not found: $CredentialsPath"
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

$creds = Get-Content -LiteralPath $CredentialsPath -Raw | ConvertFrom-Json
if (-not $creds.username -or -not $creds.password) {
    throw "Credentials file must contain username and password."
}

function Send-AdbText([string]$Text) {
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

    $escaped = $builder.ToString()
    & $AdbPath shell input text $escaped | Out-Null
}

function Send-AdbKeyCodeText([string]$Text) {
    foreach ($char in $Text.ToCharArray()) {
        if ($char -match '[A-Z]') {
            $keyCode = "KEYCODE_$char"
        } elseif ($char -match '[0-9]') {
            $keyCode = "KEYCODE_$char"
        } else {
            throw "Unsupported Steam Guard character: $char"
        }

        & $AdbPath shell input keyevent $keyCode | Out-Null
        Start-Sleep -Milliseconds 120
    }
}

function Write-GuardCodeFile([string]$Code) {
    $deviceDir = "/sdcard/Android/data/$PackageName/files"
    $devicePath = "$deviceDir/steam_guard_code.txt"
    $tempPath = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -LiteralPath $tempPath -Value $Code -NoNewline -Encoding ASCII
        & $AdbPath shell mkdir -p $deviceDir | Out-Null
        & $AdbPath push $tempPath $devicePath | Out-Null
    } finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function Write-LocalCredentialFile([string]$Username, [string]$Password) {
    $deviceDir = "/sdcard/Android/data/$PackageName/files"
    $devicePath = "$deviceDir/steam_login_credentials.txt"
    $tempPath = [System.IO.Path]::GetTempFileName()
    try {
        $usernameBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Username))
        $passwordBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Password))
        Set-Content -LiteralPath $tempPath -Value @($usernameBase64, $passwordBase64) -Encoding ASCII
        & $AdbPath shell mkdir -p $deviceDir | Out-Null
        & $AdbPath push $tempPath $devicePath | Out-Null
    } finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function ConvertFrom-SteamBase64([string]$Value) {
    $normalized = $Value.Trim()
    switch ($normalized.Length % 4) {
        2 { $normalized += "==" }
        3 { $normalized += "=" }
    }
    return [Convert]::FromBase64String($normalized)
}

function Get-SteamGuardCode([string]$SharedSecret) {
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

function Read-HiddenText([string]$Prompt) {
    $secure = Read-Host $Prompt -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

if ($Launch) {
    & $AdbPath shell monkey -p $PackageName 1 | Out-Null
    Start-Sleep -Seconds $StartupDelaySeconds
}

if ($UseLocalCredentialFile) {
    Write-LocalCredentialFile $creds.username $creds.password
    Write-Host "Wrote local Steam credential handoff file."
} else {
    & $AdbPath shell input tap 860 500 | Out-Null
    Start-Sleep -Milliseconds 700
    Send-AdbText $creds.username

    Start-Sleep -Milliseconds 700
    & $AdbPath shell input tap 860 605 | Out-Null
    Start-Sleep -Milliseconds 700
    Send-AdbText $creds.password

    Start-Sleep -Milliseconds 700
    & $AdbPath shell input tap 860 720 | Out-Null
}

$guardCode = $null
if (-not [string]::IsNullOrWhiteSpace($GuardCode)) {
    $guardCode = $GuardCode.Trim().ToUpperInvariant()
} elseif ($PromptForGuardCode) {
    $guardCode = (Read-HiddenText "Enter current Steam Guard code").Trim().ToUpperInvariant()
} elseif ($creds.shared_secret) {
    $guardCode = Get-SteamGuardCode $creds.shared_secret
} elseif ($creds.guard_code) {
    $guardCode = ([string]$creds.guard_code).Trim().ToUpperInvariant()
}

if ($guardCode) {
    if ($guardCode -notmatch '^[A-Z0-9]{5}$') {
        throw "Steam Guard code must be exactly 5 letters or digits."
    }

    Write-GuardCodeFile $guardCode
    Write-Host "Wrote local Steam Guard handoff file."
} else {
    if ($UseLocalCredentialFile) {
        Write-Host "Submitted username/password through local handoff. Pass -GuardCode, use -PromptForGuardCode, or add shared_secret/guard_code to $CredentialsPath to automate 2FA."
    } else {
        Write-Host "Submitted username/password. Pass -GuardCode, use -PromptForGuardCode, or add shared_secret/guard_code to $CredentialsPath to automate 2FA."
    }
}
