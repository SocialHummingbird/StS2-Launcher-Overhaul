$SteamGuardCodePattern = '^[A-Z0-9]{5}$'
$SteamLoginCrashPatterns = @(
    "FATAL EXCEPTION",
    "Fatal signal",
    "BUG: Unreferenced static string",
    "CryptoNative",
    "Interop+Crypto",
    "AndroidCryptoNative_",
    "SafeEvpCipherCtxHandle",
    "SafeSslHandle"
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

function Write-SteamGuardCodeFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
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
        & $AdbPath shell mkdir -p $deviceDir | Out-Null
        & $AdbPath push $tempPath $devicePath | Out-Null
    } finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function Write-SteamLoginCredentialFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
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
        & $AdbPath shell mkdir -p $deviceDir | Out-Null
        & $AdbPath push $tempPath $devicePath | Out-Null
    } finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function Send-AndroidInputText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
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

    & $AdbPath shell input text $builder.ToString() | Out-Null
}

function Wait-SteamLoginPostGuardResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [int]$TimeoutSeconds = 180,
        [int]$PollSeconds = 3
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds $PollSeconds
        $currentLog = (& $AdbPath logcat -d -v time | Out-String)

        if ($currentLog -like "*[Auth] Authentication successful*" -or $currentLog -like "*[Launcher] Ownership verified*") {
            Write-Host "Detected post-2FA success signal."
            return
        }

        $matchedCrash = $SteamLoginCrashPatterns | Where-Object { $currentLog -like "*$_*" } | Select-Object -First 1
        if ($matchedCrash) {
            Write-Host "Detected crash signature: $matchedCrash"
            return
        }
    }
}
