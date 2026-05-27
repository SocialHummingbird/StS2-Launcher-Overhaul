param(
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$OutputLogcatPath = "tmp\login-boundary-post-2fa-logcat.txt",
    [string]$OutputScreenshotPath = "tmp\login-boundary-post-2fa.png",
    [string]$GuardCode = "",
    [switch]$GuiPrompt,
    [int]$PostGuardResultTimeoutSeconds = 180,
    [int]$PostGuardPollSeconds = 3
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
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

function Send-AdbText([string]$Text) {
    $escaped = $Text `
        -replace ' ', '%s' `
        -replace '&', '\&' `
        -replace '<', '\<' `
        -replace '>', '\>' `
        -replace '\|', '\|' `
        -replace ';', '\;'
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

function Read-GuardCodeGui() {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    while ($true) {
        $form = New-Object System.Windows.Forms.Form
        $form.Text = "Steam Guard local handoff"
        $form.StartPosition = "CenterScreen"
        $form.TopMost = $true
        $form.Width = 430
        $form.Height = 190
        $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
        $form.MaximizeBox = $false
        $form.MinimizeBox = $false

        $label = New-Object System.Windows.Forms.Label
        $label.Text = "Enter the current 5-character Steam Guard code. This stays local and is not printed."
        $label.Left = 15
        $label.Top = 15
        $label.Width = 385
        $label.Height = 40
        $form.Controls.Add($label)

        $textBox = New-Object System.Windows.Forms.TextBox
        $textBox.Left = 15
        $textBox.Top = 65
        $textBox.Width = 385
        $textBox.MaxLength = 5
        $textBox.CharacterCasing = [System.Windows.Forms.CharacterCasing]::Upper
        $textBox.UseSystemPasswordChar = $true
        $form.Controls.Add($textBox)

        $okButton = New-Object System.Windows.Forms.Button
        $okButton.Text = "Submit"
        $okButton.Left = 235
        $okButton.Top = 105
        $okButton.Width = 80
        $okButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $form.AcceptButton = $okButton
        $form.Controls.Add($okButton)

        $cancelButton = New-Object System.Windows.Forms.Button
        $cancelButton.Text = "Cancel"
        $cancelButton.Left = 320
        $cancelButton.Top = 105
        $cancelButton.Width = 80
        $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
        $form.CancelButton = $cancelButton
        $form.Controls.Add($cancelButton)

        $form.Add_Shown({ $textBox.Focus() })
        $result = $form.ShowDialog()
        $candidate = $textBox.Text.Trim().ToUpperInvariant()
        $form.Dispose()

        if ($result -ne [System.Windows.Forms.DialogResult]::OK) {
            throw "Steam Guard code entry cancelled."
        }

        if ($candidate -match '^[A-Z0-9]{5}$') {
            return $candidate
        }

        [System.Windows.Forms.MessageBox]::Show(
            "Steam Guard code must be exactly 5 letters or digits.",
            "Invalid Steam Guard code",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        ) | Out-Null
    }
}

function Read-GuardCode() {
    if (-not [string]::IsNullOrWhiteSpace($GuardCode)) {
        $candidate = $GuardCode.Trim().ToUpperInvariant()
        if ($candidate -match '^[A-Z0-9]{5}$') {
            return $candidate
        }

        throw "Steam Guard code must be exactly 5 letters or digits."
    }

    if ($GuiPrompt) {
        return Read-GuardCodeGui
    }

    Write-Host ""
    Write-Host "Steam Guard local handoff is waiting."
    Write-Host "Type the current 5-character Steam Guard code in this PowerShell window, then press Enter."
    Write-Host "Input is hidden. Do not type the code in chat."
    Write-Host ""

    while ($true) {
        $candidate = (Read-HiddenText "Steam Guard code").Trim().ToUpperInvariant()
        if ($candidate -match '^[A-Z0-9]{5}$') {
            return $candidate
        }

        Write-Warning "Steam Guard code must be exactly 5 letters or digits. Try again."
    }
}

$guardCode = Read-GuardCode

New-Item -ItemType Directory -Force (Split-Path -Parent $OutputLogcatPath) | Out-Null
New-Item -ItemType Directory -Force (Split-Path -Parent $OutputScreenshotPath) | Out-Null

& $AdbPath logcat -c

Write-GuardCodeFile $guardCode

Write-Host "Wrote local Steam Guard handoff file for $PackageName. Waiting up to $PostGuardResultTimeoutSeconds seconds for auth/ownership success or a crash signature..."

$crashPatterns = @(
    "FATAL EXCEPTION",
    "Fatal signal",
    "BUG: Unreferenced static string",
    "CryptoNative",
    "Interop+Crypto",
    "AndroidCryptoNative_",
    "SafeEvpCipherCtxHandle",
    "SafeSslHandle"
)

$deadline = (Get-Date).AddSeconds($PostGuardResultTimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $PostGuardPollSeconds
    $currentLog = (& $AdbPath logcat -d -v time | Out-String)

    if ($currentLog -like "*[Auth] Authentication successful*" -or $currentLog -like "*[Launcher] Ownership verified*") {
        Write-Host "Detected post-2FA success signal."
        break
    }

    $matchedCrash = $crashPatterns | Where-Object { $currentLog -like "*$_*" } | Select-Object -First 1
    if ($matchedCrash) {
        Write-Host "Detected crash signature: $matchedCrash"
        break
    }
}

& $AdbPath logcat -d -v time > $OutputLogcatPath
& $AdbPath shell screencap -p /sdcard/sts2-login-boundary-post-2fa.png | Out-Null
& $AdbPath pull /sdcard/sts2-login-boundary-post-2fa.png $OutputScreenshotPath | Out-Null

.\scripts\check-login-crash-log.ps1 -LogcatPath $OutputLogcatPath -RequirePostSteamGuard

Write-Host "Captured logcat: $OutputLogcatPath"
Write-Host "Captured screenshot: $OutputScreenshotPath"
