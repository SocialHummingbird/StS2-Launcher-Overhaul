param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^emulator-[0-9]+$')]
    [string]$DeviceSerial,
    [ValidatePattern('^[a-zA-Z0-9_.]+$')]
    [string]$PackageName = 'com.sts2launcher.overhaul.fork.local',
    [string]$Adb = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$OutputDirectory = '',
    [ValidateRange(1, 5)]
    [int]$ColdLaunches = 2
)

# Exercises the installed production x86 fallback. It cannot validate Godot, the
# managed launcher, the title screen, or gameplay. Never installs, clears data,
# repairs a branch, or changes persistent Android settings.
$ErrorActionPreference = 'Stop'
$runId = 'sts2_' + [Guid]::NewGuid().ToString('N')
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot "../artifacts/emulator-validation/$runId"
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

function Invoke-TestAdb([string[]]$Arguments) {
    $result = & $Adb -s $DeviceSerial @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "adb -s $DeviceSerial $($Arguments -join ' ') failed: $($result -join ' ')"
    }
    return ($result -join "`n")
}

function Save-TestText([string]$Name, [string]$Content) {
    [IO.File]::WriteAllText((Join-Path $OutputDirectory $Name), $Content)
}

function Get-TestUi([string]$Name) {
    $remote = "/sdcard/$runId.xml"
    try {
        $null = Invoke-TestAdb @('shell', 'uiautomator', 'dump', $remote)
        $content = Invoke-TestAdb @('shell', 'cat', $remote)
        Save-TestText "$Name.xml" $content
        return [xml]$content
    } finally {
        $null = Invoke-TestAdb @('shell', 'rm', '-f', $remote)
    }
}

function Save-TestScreen([string]$Name) {
    $remote = "/sdcard/$runId.png"
    try {
        $null = Invoke-TestAdb @('shell', 'screencap', '-p', $remote)
        $null = Invoke-TestAdb @('pull', $remote, (Join-Path $OutputDirectory "$Name.png"))
    } finally {
        $null = Invoke-TestAdb @('shell', 'rm', '-f', $remote)
    }
}

function Assert-TestFallback([string]$Name) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $target = [regex]::Escape($PackageName) + '/com\.game\.sts2launcher\.NativeFallbackActivity'
    do {
        $activities = Invoke-TestAdb @('shell', 'dumpsys', 'activity', 'activities')
        if ($activities -match "(?:topResumedActivity|mResumedActivity)=[^\r\n]*$target") {
            Save-TestText "$Name.activities.txt" $activities
            return
        }
        Start-Sleep -Milliseconds 250
    } while ($timer.Elapsed.TotalSeconds -lt 30)
    Save-TestText "$Name.activities.txt" $activities
    throw "Native fallback did not become the resumed activity within 30 seconds ($Name)."
}

function Start-TestLauncher([string]$Name, [switch]$LaunchGame) {
    $null = Invoke-TestAdb @('shell', 'log', '-p', 'i', '-t', 'STS2Validation', "${runId}_${Name}_begin")
    $arguments = @('shell', 'am', 'start', '-W', '-a', 'android.intent.action.MAIN', '-c', 'android.intent.category.LAUNCHER', '-f', '0x10200000', '-n', "$PackageName/com.game.sts2launcher.LauncherActivity")
    if ($LaunchGame) { $arguments += @('--ez', 'sts2_launch_game', 'true') }
    $result = Invoke-TestAdb $arguments
    Save-TestText "$Name.am-start.txt" $result
    if ($result -match '(?m)^Error|Status:\s*(?!ok)\S+') { throw "Launch failed ($Name): $result" }
    Assert-TestFallback $Name
}

$state = Invoke-TestAdb @('get-state')
if ($state.Trim() -ne 'device') { throw 'The selected emulator is not online.' }
$qemu = Invoke-TestAdb @('shell', 'getprop', 'ro.kernel.qemu')
if ($qemu.Trim() -ne '1') { throw 'Refusing to run against a non-emulator target.' }
$boot = Invoke-TestAdb @('shell', 'getprop', 'sys.boot_completed')
if ($boot.Trim() -ne '1') { throw 'The emulator has not finished booting.' }
$abis = Invoke-TestAdb @('shell', 'getprop', 'ro.product.cpu.abilist')
if ($abis -notmatch 'x86') { throw 'This script specifically verifies the production x86 fallback.' }
$package = Invoke-TestAdb @('shell', 'dumpsys', 'package', $PackageName)
if ($package -notmatch 'versionCode=') { throw "Package is not installed: $PackageName" }
Save-TestText 'package.txt' $package
Save-TestText 'environment.txt' "Serial: $DeviceSerial`nABI: $abis`nRun: $runId`nScope: native x86 fallback; no game/title-screen validation.`n"
$capture = Start-Process -FilePath $Adb -ArgumentList @(
    '-s', $DeviceSerial, 'logcat', '-v', 'threadtime', 'STS2Mobile:I',
    'STS2Validation:I', 'AndroidRuntime:E', 'libc:F', 'DEBUG:F', 'ActivityManager:W', '*:S'
) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $OutputDirectory 'capture.log') `
    -RedirectStandardError (Join-Path $OutputDirectory 'capture.stderr.log') -PassThru
$null = Invoke-TestAdb @('shell', 'log', '-p', 'i', '-t', 'STS2Validation', "${runId}_begin")
$result = [ordered]@{ scope = 'Native x86 fallback only'; serial = $DeviceSerial; abi = $abis.Trim(); coldLaunches = $ColdLaunches; passed = $false }
try {
    for ($attempt = 1; $attempt -le $ColdLaunches; $attempt++) {
        $null = Invoke-TestAdb @('shell', 'am', 'force-stop', $PackageName)
        Start-TestLauncher "cold-$attempt"
    }

    # Querying and activating a real button verifies application input handling.
    $ui = Get-TestUi 'diagnostics-before'
    if (@($ui.SelectNodes('//node')) | Where-Object { $_.text -like '*isn*t responding*' }) {
        throw 'An Android ANR dialog obstructed the test. Inspect diagnostics-before.xml for the affected process.'
    }
    $button = @($ui.SelectNodes('//node')) | Where-Object { $_.text -eq 'Show diagnostics' } | Select-Object -First 1
    if (-not $button -or $button.bounds -notmatch '^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$') {
        throw 'The native diagnostics toggle is missing.'
    }
    $tapX = [int](([int]$Matches[1] + [int]$Matches[3]) / 2)
    $tapY = [int](([int]$Matches[2] + [int]$Matches[4]) / 2)
    $null = Invoke-TestAdb @('shell', 'input', 'tap', "$tapX", "$tapY")
    $ui = Get-TestUi 'diagnostics-expanded'
    if (-not (@($ui.SelectNodes('//node')) | Where-Object { $_.text -eq 'Hide diagnostics' })) {
        throw 'The diagnostics toggle did not respond to input.'
    }
    Save-TestScreen 'diagnostics-expanded'

    $null = Invoke-TestAdb @('shell', 'input', 'keyevent', 'KEYCODE_HOME')
    Start-TestLauncher 'home-resume'
    Save-TestScreen 'home-resume'

    $null = Invoke-TestAdb @('shell', 'am', 'force-stop', $PackageName)
    Start-TestLauncher 'game-request' -LaunchGame
    $null = Get-TestUi 'game-request'
    Save-TestScreen 'game-request'
    $null = Invoke-TestAdb @('shell', 'log', '-p', 'i', '-t', 'STS2Validation', "${runId}_end")
    Start-Sleep -Milliseconds 300
    if (-not $capture.HasExited) { Stop-Process -Id $capture.Id }
    $capture.WaitForExit()
    $log = [IO.File]::ReadAllText((Join-Path $OutputDirectory 'capture.log'))
    $boundary = $log.IndexOf("${runId}_begin", [StringComparison]::Ordinal)
    if ($boundary -lt 0) { throw 'Run boundary was lost from logcat; cannot validate this run.' }
    $log = $log.Substring($boundary)
    Save-TestText 'run.log' $log
    $critical = @($log -split "`n" | Where-Object {
        $_ -match 'FATAL EXCEPTION|SuperNotCalledException|Ignoring duplicate native startup route' -or
        $_ -match "ANR in $([regex]::Escape($PackageName))" -or
        $_ -match 'Fatal signal.*(?:sts2|STS2|godot)'
    })
    Save-TestText 'critical-matches.txt' ($critical -join "`n")
    if ($critical.Count) { throw "Found $($critical.Count) critical log lines. Inspect run.log." }
    if ($log -notmatch 'pendingGameLaunch=true') { throw 'The explicit game launch request was not observed.' }
    if ($log -match 'native assembly setup|NGame.GameStartup completed|phase=native route selected detail=GodotApp') {
        throw 'The x86 fallback unexpectedly entered the managed runtime.'
    }
    $result.passed = $true
    Write-Host "PASS: $ColdLaunches cold launches, diagnostics input, Home/resume, explicit game request; no critical log matches."
    Write-Host 'Scope: native x86 fallback. This does not prove title-screen or gameplay stability.'
} catch {
    $result.error = $_.Exception.Message
    throw
} finally {
    if (-not $capture.HasExited) { Stop-Process -Id $capture.Id }
    $capture.Dispose()
    Save-TestText 'result.json' ($result | ConvertTo-Json)
}
