param(
    [string]$PackageName = "",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$DeviceSerial = "",
    [string]$OutputLogcatPath = "tmp\login-boundary-post-2fa-logcat.txt",
    [string]$OutputScreenshotPath = "tmp\login-boundary-post-2fa.png",
    [string]$GuardCode = "",
    [switch]$GuiPrompt,
    [int]$PostGuardResultTimeoutSeconds = 180,
    [int]$PostGuardPollSeconds = 3
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "steam-login-utils.ps1")

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
    $env:ANDROID_SERIAL = $DeviceSerial
    Write-Host "Using Android device serial: $DeviceSerial"
}

$PackageName = Resolve-SteamLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

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

        if ($candidate -match $SteamGuardCodePattern) {
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
        return Normalize-SteamGuardCode $GuardCode
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
        try {
            return Normalize-SteamGuardCode (Read-HiddenText "Steam Guard code")
        } catch {
            Write-Warning "$($_.Exception.Message) Try again."
        }
    }
}

$guardCode = Read-GuardCode

New-Item -ItemType Directory -Force (Split-Path -Parent $OutputLogcatPath) | Out-Null
New-Item -ItemType Directory -Force (Split-Path -Parent $OutputScreenshotPath) | Out-Null

Write-SteamGuardCodeFile -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName -Code $guardCode

Write-Host "Wrote local Steam Guard handoff file for $PackageName. Waiting up to $PostGuardResultTimeoutSeconds seconds for auth/ownership success or a crash signature..."
Wait-SteamLoginPostGuardResult -AdbPath $AdbPath -DeviceSerial $DeviceSerial -TimeoutSeconds $PostGuardResultTimeoutSeconds -PollSeconds $PostGuardPollSeconds

Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-d", "-v", "time") > $OutputLogcatPath
Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "screencap", "-p", "/sdcard/sts2-login-boundary-post-2fa.png") | Out-Null
Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("pull", "/sdcard/sts2-login-boundary-post-2fa.png", $OutputScreenshotPath) | Out-Null

.\scripts\check-login-crash-log.ps1 -LogcatPath $OutputLogcatPath -RequirePostSteamGuard

Write-Host "Captured logcat: $OutputLogcatPath"
Write-Host "Captured screenshot: $OutputScreenshotPath"
