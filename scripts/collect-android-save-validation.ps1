[CmdletBinding()]
param(
    [string]$DeviceSerial = "",
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)+$')]
    [string]$PackageName,
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$OutputRoot = "artifacts\android",
    [string]$ApkPath = "",
    [string]$SourceCommit = "",
    [ValidateSet("", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10")]
    [string]$Stage5Row = "",
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')]
    [string]$EvidencePhase = "capture",
    [string]$LogcatSince = "",
    [int]$WaitSeconds = 0,
    [int]$LogcatTailLines = 100000,
    [switch]$ClearLogcat,
    [switch]$EnableVerboseSaveDiagnostics,
    [switch]$DumpSaveFiles
)

$ErrorActionPreference = "Stop"

if (-not [string]::IsNullOrWhiteSpace($Stage5Row) -and
    ($ClearLogcat -or $EnableVerboseSaveDiagnostics)) {
    throw "Stage 5 evidence capture is read-only: -ClearLogcat and -EnableVerboseSaveDiagnostics are forbidden when -Stage5Row is set."
}

function Invoke-AdbRaw {
    param([Parameter(Mandatory = $true)][string[]]$AdbArguments)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = if ([string]::IsNullOrWhiteSpace($script:DeviceSerial)) {
            @(& $script:AdbPath @AdbArguments 2>&1)
        } else {
            @(& $script:AdbPath -s $script:DeviceSerial @AdbArguments 2>&1)
        }
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    [pscustomobject]@{
        ExitCode = $exitCode
        Output = $output
    }
}

function Invoke-Adb {
    param([Parameter(Mandatory = $true)][string[]]$AdbArguments)

    $result = Invoke-AdbRaw -AdbArguments $AdbArguments
    if ($result.ExitCode -ne 0) {
        $detail = ($result.Output | Select-Object -Last 8) -join "`n"
        throw "adb $($AdbArguments -join ' ') failed with exit code $($result.ExitCode).`n$detail"
    }
    return $result.Output
}

function Get-AdbText {
    param([Parameter(Mandatory = $true)][string[]]$AdbArguments)

    return ((Invoke-Adb -AdbArguments $AdbArguments) | Out-String).Trim()
}

function Get-TextSha256 {
    param([Parameter(Mandatory = $true)][string]$Text)

    $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) -join "")
    } finally {
        $sha256.Dispose()
    }
}

function Get-ByteSha256 {
    param([Parameter(Mandatory = $true)][AllowEmptyCollection()][byte[]]$Bytes)

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($Bytes) | ForEach-Object { $_.ToString("x2") }) -join "")
    } finally {
        $sha256.Dispose()
    }
}

function Resolve-AuthorizedDevice {
    $savedSerial = $script:DeviceSerial
    $script:DeviceSerial = ""
    try {
        $result = Invoke-AdbRaw -AdbArguments @("devices", "-l")
    } finally {
        $script:DeviceSerial = $savedSerial
    }
    if ($result.ExitCode -ne 0) {
        throw "adb devices -l failed; no Android evidence was captured."
    }

    $targets = @(
        $result.Output |
            ForEach-Object { ([string]$_).Trim() } |
            Where-Object { $_ -and $_ -notmatch '^List of devices' -and $_ -notmatch '^\*' } |
            ForEach-Object {
                if ($_ -match '^(\S+)\s+(\S+)(?:\s|$)') {
                    [pscustomobject]@{ Serial = $Matches[1]; State = $Matches[2] }
                }
            }
    )

    if ($savedSerial) {
        $selected = @($targets | Where-Object { $_.Serial -eq $savedSerial })
        if ($selected.Count -ne 1) {
            throw "Requested Android target is not attached; no evidence was captured."
        }
        if ($selected[0].State -ne "device") {
            throw "Requested Android target is $($selected[0].State), not authorized; no evidence was captured."
        }
        return $savedSerial
    }

    $authorized = @($targets | Where-Object { $_.State -eq "device" })
    if ($authorized.Count -eq 1) {
        return $authorized[0].Serial
    }
    if ($authorized.Count -gt 1) {
        throw "Multiple authorized Android targets are attached. Pass -DeviceSerial; no evidence was captured."
    }

    $states = @($targets | ForEach-Object { $_.State } | Sort-Object -Unique)
    $stateText = if ($states.Count -gt 0) { $states -join ", " } else { "none" }
    throw "No authorized Android target is available (attached states: $stateText); no evidence was captured."
}

function Add-PersistedRemoteManifestRows {
    param(
        [Parameter(Mandatory = $true)]$Document,
        [Parameter(Mandatory = $true)][string]$DocumentPath,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)]$Rows
    )

    $property = $Document.PSObject.Properties[$Role]
    if (-not $property -or -not $property.Value) {
        return
    }
    foreach ($entry in @($property.Value.Entries)) {
        if (-not $entry) {
            continue
        }
        $byteSha256 = [string]$entry.ByteSha256
        $legacyTextSha256 = [string]$entry.Sha256
        $Rows.Add([pscustomobject]@{
            DocumentPath = $DocumentPath
            ManifestRole = $Role
            SavePath = [string]$entry.Path
            Exists = [bool]$entry.Exists
            HashKind = if (-not [bool]$entry.Exists) {
                "missing"
            } elseif (-not [string]::IsNullOrWhiteSpace($byteSha256)) {
                "byte-sha256"
            } else {
                "legacy-text-sha256"
            }
            Sha256 = if (-not [bool]$entry.Exists) {
                ""
            } elseif (-not [string]::IsNullOrWhiteSpace($byteSha256)) {
                $byteSha256.ToLowerInvariant()
            } else {
                $legacyTextSha256.ToLowerInvariant()
            }
        })
    }
}

function Get-TimeLogEntries {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)

    $entries = [Collections.Generic.List[object]]::new()
    $index = 0
    foreach ($line in @($Text -split '\r?\n')) {
        $clean = $line.TrimStart([char]0xfeff)
        if ($clean -match '^(?<time>\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\s+(?<pid>\d+)\s+(?<tid>\d+)\s+(?<priority>[VDIWEF])\s+(?<tag>[^:]+?)\s*:\s(?<message>.*)$') {
            $entries.Add([pscustomobject]@{
                Index = $index
                Pid = [int]$Matches.pid
                Tag = $Matches.tag.Trim()
                Message = $Matches.message
            })
        }
        $index++
    }
    return @($entries)
}

function Test-IsJsonInteger {
    param($Value)
    return $Value -is [byte] -or $Value -is [int16] -or
        $Value -is [int32] -or $Value -is [int64]
}

function Assert-ExactJsonProperties {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string[]]$Names,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $actual = @($Object.PSObject.Properties.Name | Sort-Object)
    $expected = @($Names | Sort-Object)
    if (($actual -join "`n") -cne ($expected -join "`n")) {
        throw "$Label has an unexpected structured schema."
    }
}

function Get-StructuredLogAnalysis {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$PackageName
    )

    $entries = @(Get-TimeLogEntries -Text $Text)
    $appEntries = @($entries | Where-Object { $_.Tag -ceq 'STS2Mobile' })
    $appText = @($appEntries | ForEach-Object { $_.Message }) -join "`n"
    $automatic = [Collections.Generic.List[object]]::new()
    $recovery = [Collections.Generic.List[object]]::new()
    $exports = [Collections.Generic.List[object]]::new()
    foreach ($entry in $appEntries) {
        if ($entry.Message.StartsWith('STS2_SAVE_EVENT ', [StringComparison]::Ordinal)) {
            try {
                $payload = $entry.Message.Substring('STS2_SAVE_EVENT '.Length) |
                    ConvertFrom-Json -ErrorAction Stop
            } catch {
                throw 'STS2Mobile contains an invalid structured save terminal event.'
            }
            if ([string]$payload.Event -eq 'automatic-sync-terminal') {
                Assert-ExactJsonProperties -Object $payload -Names @(
                    'Event', 'Version', 'Operation', 'Outcome', 'Detail',
                    'ContextSha256', 'RemoteVerified'
                ) -Label 'Automatic-sync terminal event'
                if (-not (Test-IsJsonInteger $payload.Version) -or
                    [int64]$payload.Version -ne 1 -or
                    $payload.RemoteVerified -isnot [bool] -or
                    [string]$payload.Operation -notin @('recover', 'reconcile', 'begin-game') -or
                    [string]$payload.Outcome -notin @('synchronized', 'source-choice-required', 'conflict', 'game-session-prepared', 'pending-recovery-required', 'failed') -or
                    [string]$payload.Detail -notin @('verified', 'no-pending-work', 'source-choice-required', 'local-and-remote-diverged', 'account-mismatch', 'namespace-mismatch', 'branch-mismatch', 'mod-set-mismatch', 'context-missing', 'context-unreadable', 'independent-change', 'session-prepared', 'pending-recovery', 'commit-rejected', 'remote-readback-mismatch', 'operation-failed') -or
                    ([string]$payload.ContextSha256 -and [string]$payload.ContextSha256 -notmatch '^[0-9a-f]{64}$') -or
                    ([bool]$payload.RemoteVerified -and ([string]$payload.Outcome -ne 'synchronized' -or [string]$payload.Detail -ne 'verified' -or [string]$payload.ContextSha256 -notmatch '^[0-9a-f]{64}$')) -or
                    ([string]$payload.Detail -eq 'verified' -and -not [bool]$payload.RemoteVerified)) {
                    throw 'STS2Mobile contains an invalid automatic-sync terminal event.'
                }
                $automatic.Add($payload)
            } elseif ([string]$payload.Event -eq 'save-recovery-terminal') {
                Assert-ExactJsonProperties -Object $payload -Names @(
                    'Event', 'Version', 'Operation', 'Outcome', 'Detail'
                ) -Label 'Save-recovery terminal event'
                if (-not (Test-IsJsonInteger $payload.Version) -or
                    [int64]$payload.Version -ne 1 -or
                    [string]$payload.Operation -notin @('restore', 'undo') -or
                    [string]$payload.Outcome -cne 'completed' -or
                    [string]$payload.Detail -cne 'byte-verified-local-only') {
                    throw 'STS2Mobile contains an invalid save-recovery terminal event.'
                }
                $recovery.Add($payload)
            } else {
                throw "STS2Mobile contains an unknown structured save terminal event '$([string]$payload.Event)'."
            }
        } elseif ($entry.Message.StartsWith('[Recovery] STS2_SAVE_EXPORT_COMPLETE ', [StringComparison]::Ordinal)) {
            try {
                $payload = $entry.Message.Substring('[Recovery] STS2_SAVE_EXPORT_COMPLETE '.Length) |
                    ConvertFrom-Json -ErrorAction Stop
            } catch {
                throw 'STS2Mobile contains an invalid structured save-export completion event.'
            }
            Assert-ExactJsonProperties -Object $payload -Names @(
                'Event', 'Version', 'ExportId', 'BundleSha256',
                'CurrentAndroidTreeSha256', 'SelectedSaveContextSha256'
            ) -Label 'Save-export completion event'
            if ([string]$payload.Event -cne 'save-recovery-export-complete' -or
                -not (Test-IsJsonInteger $payload.Version) -or
                [int64]$payload.Version -ne 1 -or
                [string]$payload.ExportId -notmatch '^[0-9a-f]{32}$' -or
                [string]$payload.BundleSha256 -notmatch '^[0-9a-f]{64}$' -or
                [string]$payload.CurrentAndroidTreeSha256 -notmatch '^[0-9a-f]{64}$' -or
                [string]$payload.SelectedSaveContextSha256 -notmatch '^[0-9a-f]{64}$') {
                throw 'STS2Mobile contains an invalid save-export completion event.'
            }
            $exports.Add($payload)
        }
    }

    $escapedPackage = [regex]::Escape($PackageName)
    $fatal = $false
    for ($i = 0; $i -lt $entries.Count; $i++) {
        if ($entries[$i].Tag -cne 'AndroidRuntime' -or
            $entries[$i].Message -notmatch '^FATAL EXCEPTION(?:\s|:)') { continue }
        $last = [Math]::Min($entries.Count - 1, $i + 8)
        for ($j = $i; $j -le $last; $j++) {
            if ($entries[$j].Tag -ceq 'AndroidRuntime' -and
                $entries[$j].Pid -eq $entries[$i].Pid -and
                $entries[$j].Message -match "^Process:\s*$escapedPackage(?:,|\s|$)") {
                $fatal = $true
            }
        }
    }
    $anr = [bool](@($entries | Where-Object {
        $_.Tag -in @('ActivityManager', 'ActivityTaskManager', 'am_anr') -and
        $_.Message -match "(?i)(?:\bANR in\s+|\bam_anr\b[^\r\n]*)$escapedPackage(?:/|:|,|\s|$)"
    }).Count)
    $contextMismatchDetails = @('account-mismatch', 'namespace-mismatch', 'branch-mismatch', 'mod-set-mismatch')
    $localWriteExceptionPattern = '(?im)^[^\r\n]*(?:(?:\[Save\]|Android local save|local-only save|local save write)[^\r\n]*(?:exception|failed|failure|error|could not|unable to|denied|dropped|swallowed|ignored|discarded)|(?:exception|failed|failure|error|could not|unable to|denied)[^\r\n]*(?:Android local save|local save write))[^\r\n]*$'
    return [pscustomobject][ordered]@{
        automaticSyncPendingLogSeen = [bool](@($automatic | Where-Object { $_.Outcome -in @('conflict', 'pending-recovery-required', 'failed') }).Count)
        automaticSyncVerifiedLogSeen = [bool](@($automatic | Where-Object { $_.Outcome -eq 'synchronized' -and [bool]$_.RemoteVerified }).Count)
        automaticSyncConflictLogSeen = [bool](@($automatic | Where-Object { $_.Outcome -eq 'conflict' }).Count)
        syncedLogSeen = [bool](@($automatic | Where-Object { $_.Outcome -eq 'synchronized' -and [bool]$_.RemoteVerified }).Count)
        readBackMismatchSeen = [bool](@($automatic | Where-Object { $_.Outcome -eq 'failed' -and $_.Detail -eq 'remote-readback-mismatch' }).Count)
        commitFailureSeen = [bool](@($automatic | Where-Object { $_.Outcome -eq 'failed' -and $_.Detail -eq 'commit-rejected' }).Count)
        saveContextMismatchSeen = [bool](@($automatic | Where-Object { $_.Detail -in $contextMismatchDetails }).Count)
        modSetMismatchSeen = [bool](@($automatic | Where-Object { $_.Detail -eq 'mod-set-mismatch' }).Count)
        branchMismatchSeen = [bool](@($automatic | Where-Object { $_.Detail -eq 'branch-mismatch' }).Count)
        recoveryLogSeen = [bool]($recovery.Count -gt 0 -or $exports.Count -gt 0)
        recoveryRestoreLogCount = @($recovery | Where-Object { $_.Operation -eq 'restore' }).Count
        recoveryUndoLogCount = @($recovery | Where-Object { $_.Operation -eq 'undo' }).Count
        localSaveBaseSeen = [bool]($appText -match '\[Save\] Android local save base:')
        localSaveWrites = ([regex]::Matches($appText, '\[Save\] Android local save write:')).Count
        localSaveReads = ([regex]::Matches($appText, '\[Save\] Android local save read')).Count
        localSaveExistsChecks = ([regex]::Matches($appText, '\[Save\] Android local save exists:')).Count
        localOnlySaveManagerSeen = [bool]($appText -match '\[Save\] Created Android gameplay SaveManager with local storage only')
        steamGameplaySaveManagerSeen = [bool]($appText -match 'Created .*SaveManager.*Steam|Steam.*gameplay SaveManager')
        fatalExceptionSeen = $fatal
        anrSeen = $anr
        droppedSaveWriteSeen = [bool]($appText -match '(?im)dropped (save )?write|save write[^\r\n]*(ignored|discarded)')
        swallowedFailureSeen = [bool]($appText -match '(?im)swallowed (exception|failure|error)')
        localWriteExceptionCount = ([regex]::Matches($appText, $localWriteExceptionPattern)).Count
    }
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}
if ($Stage5Row -and [string]::IsNullOrWhiteSpace($EvidencePhase)) {
    throw "-EvidencePhase is required when -Stage5Row is supplied."
}
if ($LogcatSince -and
    $LogcatSince -notmatch '^\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}$') {
    throw "-LogcatSince must use Android logcat time format MM-dd HH:mm:ss.fff."
}

# Authorization is checked before creating an evidence folder, clearing logcat,
# or touching the optional verbose-diagnostics marker.
$DeviceSerial = Resolve-AuthorizedDevice
$script:DeviceSerial = $DeviceSerial
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($SourceCommit)) {
    $SourceCommit = ((& git -C $repoRoot rev-parse HEAD 2>$null) | Out-String).Trim()
}
$sourceWorktreeDirty = [bool](@(& git -C $repoRoot status --porcelain 2>$null).Count)

$candidateApkSha256 = ""
$resolvedApkPath = ""
if ($ApkPath) {
    $resolvedApkPath = (Resolve-Path -LiteralPath $ApkPath).Path
    $candidateApkSha256 = (Get-FileHash -LiteralPath $resolvedApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $OutputRoot "save-validation-$timestamp"
if (Test-Path -LiteralPath $outDir) {
    throw "Refusing to overwrite an existing Android evidence capture: $outDir"
}
New-Item -ItemType Directory -Path $outDir | Out-Null

$verboseMarkerEnabled = $false
try {
    if ($ClearLogcat) {
        $preClearLog = Get-AdbText -AdbArguments @("logcat", "-d", "-v", "time")
        [IO.File]::WriteAllText(
            (Join-Path $outDir "logcat-before-clear.txt"),
            $preClearLog,
            [Text.UTF8Encoding]::new($false)
        )
        Invoke-Adb -AdbArguments @("logcat", "-c") | Out-Null
    }

    $runAsProbe = Invoke-AdbRaw -AdbArguments @("shell", "run-as", $PackageName, "sh", "-c", "echo RUN_AS_OK")
    $runAsAvailable = $runAsProbe.ExitCode -eq 0 -and
        (($runAsProbe.Output -join "`n") -match 'RUN_AS_OK')
    if ($EnableVerboseSaveDiagnostics -and $runAsAvailable) {
        Invoke-Adb -AdbArguments @("shell", "run-as", $PackageName, "sh", "-c", "touch files/.sts2_verbose_save_diagnostics") | Out-Null
        $verboseMarkerEnabled = $true
    }

    if ($WaitSeconds -gt 0) {
        Write-Host "Waiting $WaitSeconds seconds before collecting logcat..."
        Start-Sleep -Seconds $WaitSeconds
    }

    $manufacturer = Get-AdbText -AdbArguments @("shell", "getprop", "ro.product.manufacturer")
    $model = Get-AdbText -AdbArguments @("shell", "getprop", "ro.product.model")
    $androidApi = Get-AdbText -AdbArguments @("shell", "getprop", "ro.build.version.sdk")
    $abiList = Get-AdbText -AdbArguments @("shell", "getprop", "ro.product.cpu.abilist")
    $buildFingerprint = Get-AdbText -AdbArguments @("shell", "getprop", "ro.build.fingerprint")
    $deviceIdentity = [ordered]@{
        serial = $DeviceSerial
        serialSha256 = Get-TextSha256 -Text $DeviceSerial
        manufacturer = $manufacturer
        model = $model
        androidApi = $androidApi
        abiList = $abiList
        buildFingerprint = $buildFingerprint
    }

    $properties = Get-AdbText -AdbArguments @("shell", "getprop")
    [IO.File]::WriteAllText(
        (Join-Path $outDir "getprop.txt"),
        $properties,
        [Text.UTF8Encoding]::new($false)
    )

    $packageDump = Get-AdbText -AdbArguments @("shell", "dumpsys", "package", $PackageName)
    [IO.File]::WriteAllText(
        (Join-Path $outDir "package.txt"),
        $packageDump,
        [Text.UTF8Encoding]::new($false)
    )

    $installedApkPath = ""
    $installedApkSha256 = ""
    $pmPathResult = Invoke-AdbRaw -AdbArguments @("shell", "pm", "path", $PackageName)
    if ($pmPathResult.ExitCode -eq 0) {
        $installedApkPath = @(
            $pmPathResult.Output |
                ForEach-Object { ([string]$_).Trim() } |
                Where-Object { $_ -match '^package:.+base\.apk$' } |
                ForEach-Object { $_.Substring("package:".Length) }
        ) | Select-Object -First 1
    }
    if ($installedApkPath) {
        $installedHashResult = Invoke-AdbRaw -AdbArguments @("shell", "sha256sum", $installedApkPath)
        if ($installedHashResult.ExitCode -eq 0 -and
            (($installedHashResult.Output -join " ") -match '([0-9a-fA-F]{64})')) {
            $installedApkSha256 = $Matches[1].ToLowerInvariant()
        }
    }

    if ($LogcatSince) {
        $rawLog = Get-AdbText -AdbArguments @(
            "logcat", "-d", "-v", "time", "-T", $LogcatSince
        )
    } elseif ($LogcatTailLines -gt 0) {
        $rawLog = Get-AdbText -AdbArguments @("logcat", "-d", "-v", "time", "-t", [string]$LogcatTailLines)
    } else {
        $rawLog = Get-AdbText -AdbArguments @("logcat", "-d", "-v", "time")
    }
    $rawLogPath = Join-Path $outDir "logcat.txt"
    [IO.File]::WriteAllText($rawLogPath, $rawLog, [Text.UTF8Encoding]::new($false))

    $patterns = @(
        "Assembly cache diagnostics",
        "Assembly cache required file",
        "Android startup freshness",
        "New version detected",
        "re-copying all assemblies",
        "cache-hit",
        "expectedBytes",
        "expectedSource",
        "\[Cloud\]",
        "\[Save\]",
        "\[Recovery\]",
        "STS2_SAVE_EXPORT_COMPLETE",
        "STS2_SAVE_EVENT",
        "automatic save sync",
        "automatic save synchronization",
        "pending-sync",
        "Synced",
        "synchronized and verified",
        "read-back",
        "Destination deletion could not be verified",
        "file_committed",
        "commit",
        "save-context mismatch",
        "mod set",
        "branch mismatch",
        "Steam gameplay SaveManager",
        "dropped save write",
        "dropped write",
        "swallowed exception",
        "swallowed failure",
        "local save write exception",
        "local save write failed",
        "Restore",
        "Undo",
        "AndroidRuntime",
        "FATAL EXCEPTION",
        "Application Not Responding",
        "ANR in",
        "am_anr",
        "Input dispatching timed out"
    )
    $filtered = Select-String -LiteralPath $rawLogPath -Pattern $patterns -CaseSensitive:$false
    $filteredLines = @($filtered | ForEach-Object { $_.Line })
    $filteredLines | Set-Content -LiteralPath (Join-Path $outDir "filtered-logcat.txt") -Encoding UTF8

    $localSaveHashRows = @()
    $stateHashRows = @()
    $stateDocumentIndex = [System.Collections.Generic.List[object]]::new()
    $persistedRemoteRows = [System.Collections.Generic.List[object]]::new()
    $pendingPhases = [System.Collections.Generic.List[string]]::new()
    if ($runAsAvailable) {
        $localHashCommand = @'
find files -maxdepth 9 -type f \( -name 'profile.save' -o -name 'progress.save' -o -name 'prefs' -o -name 'prefs.save' -o -name 'current_run.save' -o -name 'current_run_mp.save' -o -name '*.run' \) ! -path 'files/.sts2-launcher/*' ! -path 'files/.launcher_backups/*' ! -path 'files/game/*' ! -path 'files/cache/*' ! -path 'files/tmp/*' -print 2>/dev/null | sort | while IFS= read -r f; do h=$(sha256sum "$f"); h=${h%% *}; s=$(wc -c < "$f"); printf '%s\t%s\t%s\n' "$h" "$s" "$f"; done
'@
        $localHashResult = Invoke-AdbRaw -AdbArguments @("shell", "run-as", $PackageName, "sh", "-c", $localHashCommand)
        if ($localHashResult.ExitCode -eq 0) {
            $localSaveHashRows = @(
                $localHashResult.Output |
                    ForEach-Object { ([string]$_).Trim() } |
                    Where-Object { $_ -match '^[0-9a-fA-F]{64}\t[0-9]+\tfiles/' }
            )
        }
        @("sha256`tsizeBytes`tdevicePath") + $localSaveHashRows |
            Set-Content -LiteralPath (Join-Path $outDir "local-save-byte-hashes.tsv") -Encoding UTF8
        if ($DumpSaveFiles) {
            $localSaveHashRows |
                ForEach-Object { ($_ -split "`t", 3)[2] } |
                Set-Content -LiteralPath (Join-Path $outDir "save-files.txt") -Encoding UTF8
        }

        $stateHashCommand = @'
{ for root in files/.sts2-launcher/automatic-sync files/.sts2-launcher/recovery; do if [ -d "$root" ]; then find "$root" -type f -print 2>/dev/null; fi; done; } | sort | while IFS= read -r f; do h=$(sha256sum "$f"); h=${h%% *}; s=$(wc -c < "$f"); printf '%s\t%s\t%s\n' "$h" "$s" "$f"; done
'@
        $stateHashResult = Invoke-AdbRaw -AdbArguments @("shell", "run-as", $PackageName, "sh", "-c", $stateHashCommand)
        if ($stateHashResult.ExitCode -eq 0) {
            $stateHashRows = @(
                $stateHashResult.Output |
                    ForEach-Object { ([string]$_).Trim() } |
                    Where-Object { $_ -match '^[0-9a-fA-F]{64}\t[0-9]+\tfiles/\.sts2-launcher/' }
            )
        }
        @("sha256`tsizeBytes`tdevicePath") + $stateHashRows |
            Set-Content -LiteralPath (Join-Path $outDir "sync-recovery-state-byte-hashes.tsv") -Encoding UTF8

        $stateCaptureDir = Join-Path $outDir "sync-state"
        New-Item -ItemType Directory -Force -Path $stateCaptureDir | Out-Null
        foreach ($stateHashRow in $stateHashRows) {
            $parts = $stateHashRow -split "`t", 3
            $devicePath = $parts[2]
            if ($devicePath -notmatch '^files/\.sts2-launcher/(automatic-sync|recovery)/' -or
                $devicePath.Contains("..") -or
                -not $devicePath.EndsWith(".json", [StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $deviceSha256 = $parts[0].ToLowerInvariant()
            $deviceSizeBytes = [int64]$parts[1]
            $nameHash = (Get-TextSha256 -Text $devicePath).Substring(0, 16)
            $capturedName = "$nameHash-$([IO.Path]::GetFileName($devicePath))"
            $capturedPath = Join-Path $stateCaptureDir $capturedName
            $documentResult = Invoke-AdbRaw -AdbArguments @(
                "exec-out", "run-as", $PackageName, "base64", $devicePath
            )
            if ($documentResult.ExitCode -ne 0) {
                $stateDocumentIndex.Add([pscustomobject]@{
                    devicePath = $devicePath
                    capturedFile = ""
                    deviceSha256 = $deviceSha256
                    deviceSizeBytes = $deviceSizeBytes
                    capturedSha256 = ""
                    capturedSizeBytes = [int64]0
                    byteIdentityVerified = $false
                    parsed = $false
                    error = "read failed"
                })
                continue
            }

            $encodedDocument = @(
                $documentResult.Output | ForEach-Object { [string]$_ }
            ) -join ""
            $encodedDocument = $encodedDocument -replace '\s', ''
            $validBase64 = $encodedDocument.Length % 4 -eq 0 -and
                $encodedDocument -match '^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$'
            if (-not $validBase64) {
                $stateDocumentIndex.Add([pscustomobject]@{
                    devicePath = $devicePath
                    capturedFile = ""
                    deviceSha256 = $deviceSha256
                    deviceSizeBytes = $deviceSizeBytes
                    capturedSha256 = ""
                    capturedSizeBytes = [int64]0
                    byteIdentityVerified = $false
                    parsed = $false
                    error = "malformed base64"
                })
                continue
            }

            try {
                [byte[]]$documentBytes = [Convert]::FromBase64String($encodedDocument)
            } catch {
                $stateDocumentIndex.Add([pscustomobject]@{
                    devicePath = $devicePath
                    capturedFile = ""
                    deviceSha256 = $deviceSha256
                    deviceSizeBytes = $deviceSizeBytes
                    capturedSha256 = ""
                    capturedSizeBytes = [int64]0
                    byteIdentityVerified = $false
                    parsed = $false
                    error = "malformed base64"
                })
                continue
            }

            $capturedSha256 = Get-ByteSha256 -Bytes $documentBytes
            $capturedSizeBytes = [int64]$documentBytes.LongLength
            [IO.File]::WriteAllBytes($capturedPath, $documentBytes)
            $byteIdentityVerified = $capturedSha256 -ceq $deviceSha256 -and
                $capturedSizeBytes -eq $deviceSizeBytes
            if (-not $byteIdentityVerified) {
                $stateDocumentIndex.Add([pscustomobject]@{
                    devicePath = $devicePath
                    capturedFile = "sync-state/$capturedName"
                    deviceSha256 = $deviceSha256
                    deviceSizeBytes = $deviceSizeBytes
                    capturedSha256 = $capturedSha256
                    capturedSizeBytes = $capturedSizeBytes
                    byteIdentityVerified = $false
                    parsed = $false
                    error = "captured bytes differ from device inventory"
                })
                continue
            }

            $parsed = $false
            $parseError = ""
            try {
                $documentText = [Text.UTF8Encoding]::new($false, $true).GetString($documentBytes)
                if ($documentText.Length -gt 0 -and $documentText[0] -eq [char]0xfeff) {
                    $documentText = $documentText.Substring(1)
                }
                $document = $documentText | ConvertFrom-Json -ErrorAction Stop
                $parsed = $true
                Add-PersistedRemoteManifestRows -Document $document -DocumentPath $devicePath -Role "RemoteManifest" -Rows $persistedRemoteRows
                Add-PersistedRemoteManifestRows -Document $document -DocumentPath $devicePath -Role "RemoteBaseline" -Rows $persistedRemoteRows
                if ([IO.Path]::GetFileName($devicePath) -eq "pending-sync.json" -and $document.Phase) {
                    $pendingPhases.Add([string]$document.Phase)
                    if ([string]$document.Phase -eq "uploading") {
                        Add-PersistedRemoteManifestRows -Document $document -DocumentPath $devicePath -Role "ExpectedDestinationManifest" -Rows $persistedRemoteRows
                    } elseif ([string]$document.Phase -eq "downloading") {
                        Add-PersistedRemoteManifestRows -Document $document -DocumentPath $devicePath -Role "SourceManifest" -Rows $persistedRemoteRows
                    }
                }
            } catch {
                $parseError = $_.Exception.Message
            }
            $stateDocumentIndex.Add([pscustomobject]@{
                devicePath = $devicePath
                capturedFile = "sync-state/$capturedName"
                deviceSha256 = $deviceSha256
                deviceSizeBytes = $deviceSizeBytes
                capturedSha256 = $capturedSha256
                capturedSizeBytes = $capturedSizeBytes
                byteIdentityVerified = $byteIdentityVerified
                parsed = $parsed
                error = $parseError
            })
        }
    } else {
        "sha256`tsizeBytes`tdevicePath" |
            Set-Content -LiteralPath (Join-Path $outDir "local-save-byte-hashes.tsv") -Encoding UTF8
        "sha256`tsizeBytes`tdevicePath" |
            Set-Content -LiteralPath (Join-Path $outDir "sync-recovery-state-byte-hashes.tsv") -Encoding UTF8
        @(
            "run-as unavailable for $PackageName",
            "No private Android files or byte hashes were captured.",
            "This evidence cannot satisfy Review 5 local/remote file inspection."
        ) | Set-Content -LiteralPath (Join-Path $outDir "private-storage-unavailable.txt") -Encoding UTF8
    }

    ConvertTo-Json -InputObject @($stateDocumentIndex) -Depth 5 |
        Set-Content -LiteralPath (Join-Path $outDir "sync-state-index.json") -Encoding UTF8
    @("documentPath`tmanifestRole`tsavePath`texists`thashKind`tsha256") + @(
        $persistedRemoteRows | ForEach-Object {
            "$($_.DocumentPath)`t$($_.ManifestRole)`t$($_.SavePath)`t$($_.Exists)`t$($_.HashKind)`t$($_.Sha256)"
        }
    ) | Set-Content -LiteralPath (Join-Path $outDir "persisted-steam-byte-hashes.tsv") -Encoding UTF8

    $persistedSteamByteHashCount = @(
        $persistedRemoteRows | Where-Object { $_.HashKind -eq "byte-sha256" }
    ).Count
    $persistedSteamLegacyTextHashCount = @(
        $persistedRemoteRows | Where-Object { $_.HashKind -eq "legacy-text-sha256" }
    ).Count
    $logAnalysis = Get-StructuredLogAnalysis `
        -Text $rawLog `
        -PackageName $PackageName

    $summary = [ordered]@{
        output = $outDir
        authorizedDevice = $true
        runAsAvailable = $runAsAvailable
        sourceCommit = $SourceCommit
        sourceWorktreeDirty = $sourceWorktreeDirty
        stage5Row = $Stage5Row
        evidencePhase = $EvidencePhase
        logcatSince = $LogcatSince
        candidateApkSha256 = $candidateApkSha256
        installedApkSha256 = $installedApkSha256
        candidateMatchesInstalled = [bool](
            $candidateApkSha256 -and
            $installedApkSha256 -and
            $candidateApkSha256 -eq $installedApkSha256
        )
        localSaveByteHashCount = $localSaveHashRows.Count
        syncRecoveryStateFileHashCount = $stateHashRows.Count
        syncStateCapturedDocumentCount = @($stateDocumentIndex | Where-Object { $_.capturedFile }).Count
        syncStateByteIdentityFailureCount = @($stateDocumentIndex | Where-Object { -not $_.byteIdentityVerified }).Count
        syncStateParseFailureCount = @($stateDocumentIndex | Where-Object { -not $_.parsed }).Count
        persistedSteamByteHashCount = $persistedSteamByteHashCount
        persistedSteamLegacyTextHashCount = $persistedSteamLegacyTextHashCount
        persistedSteamHashesAreLiveReadAtCapture = $false
        pendingSyncDocumentCount = @($stateDocumentIndex | Where-Object { $_.devicePath -match '/pending-sync\.json$' }).Count
        pendingSyncPhases = @($pendingPhases | Sort-Object -Unique)
        recoveryJournalCount = @($stateDocumentIndex | Where-Object { $_.devicePath -match '/last-restore\.json$' }).Count
        automaticSyncPendingLogSeen = [bool]$logAnalysis.automaticSyncPendingLogSeen
        automaticSyncVerifiedLogSeen = [bool]$logAnalysis.automaticSyncVerifiedLogSeen
        automaticSyncConflictLogSeen = [bool]$logAnalysis.automaticSyncConflictLogSeen
        syncedLogSeen = [bool]$logAnalysis.syncedLogSeen
        readBackMismatchSeen = [bool]$logAnalysis.readBackMismatchSeen
        commitFailureSeen = [bool]$logAnalysis.commitFailureSeen
        saveContextMismatchSeen = [bool]$logAnalysis.saveContextMismatchSeen
        modSetMismatchSeen = [bool]$logAnalysis.modSetMismatchSeen
        branchMismatchSeen = [bool]$logAnalysis.branchMismatchSeen
        recoveryLogSeen = [bool]$logAnalysis.recoveryLogSeen
        recoveryRestoreLogCount = [int]$logAnalysis.recoveryRestoreLogCount
        recoveryUndoLogCount = [int]$logAnalysis.recoveryUndoLogCount
        localSaveBaseSeen = [bool]$logAnalysis.localSaveBaseSeen
        localSaveWrites = [int]$logAnalysis.localSaveWrites
        localSaveReads = [int]$logAnalysis.localSaveReads
        localSaveExistsChecks = [int]$logAnalysis.localSaveExistsChecks
        localOnlySaveManagerSeen = [bool]$logAnalysis.localOnlySaveManagerSeen
        steamGameplaySaveManagerSeen = [bool]$logAnalysis.steamGameplaySaveManagerSeen
        fatalExceptionSeen = [bool]$logAnalysis.fatalExceptionSeen
        anrSeen = [bool]$logAnalysis.anrSeen
        droppedSaveWriteSeen = [bool]$logAnalysis.droppedSaveWriteSeen
        swallowedFailureSeen = [bool]$logAnalysis.swallowedFailureSeen
        localWriteExceptionCount = [int]$logAnalysis.localWriteExceptionCount
    }

    $summaryText = @(
        "Android Stage 5 save validation capture",
        "Output: $outDir",
        "Source commit: $SourceCommit",
        "Source worktree dirty: $sourceWorktreeDirty",
        "Stage 5 row: $Stage5Row",
        "Evidence phase: $EvidencePhase",
        "Scenario logcat since: $LogcatSince",
        "Candidate APK SHA-256: $candidateApkSha256",
        "Installed APK SHA-256: $installedApkSha256",
        "Candidate matches installed: $($summary.candidateMatchesInstalled)",
        "Authorized device: True",
        "run-as/private storage available: $runAsAvailable",
        "Local save byte hashes: $($summary.localSaveByteHashCount)",
        "Sync/recovery state file hashes: $($summary.syncRecoveryStateFileHashCount)",
        "Sync-state documents captured byte-for-byte: $($summary.syncStateCapturedDocumentCount)",
        "Sync-state byte-identity failures: $($summary.syncStateByteIdentityFailureCount)",
        "Sync-state parse failures: $($summary.syncStateParseFailureCount)",
        "Persisted Steam byte hashes: $($summary.persistedSteamByteHashCount)",
        "Persisted Steam legacy text hashes: $($summary.persistedSteamLegacyTextHashCount)",
        "Persisted Steam hashes live-read during capture: False",
        "Pending sync documents: $($summary.pendingSyncDocumentCount)",
        "Pending sync phases: $($summary.pendingSyncPhases -join ', ')",
        "Recovery journals: $($summary.recoveryJournalCount)",
        "Verified automatic-sync log seen: $($summary.automaticSyncVerifiedLogSeen)",
        "Any Synced log seen: $($summary.syncedLogSeen)",
        "Read-back mismatch seen: $($summary.readBackMismatchSeen)",
        "Commit failure seen: $($summary.commitFailureSeen)",
        "SaveContext mismatch seen: $($summary.saveContextMismatchSeen)",
        "Mod-set mismatch seen: $($summary.modSetMismatchSeen)",
        "Branch mismatch seen: $($summary.branchMismatchSeen)",
        "Restore action lines: $($summary.recoveryRestoreLogCount)",
        "Undo action lines: $($summary.recoveryUndoLogCount)",
        "Local-only SaveManager seen: $($summary.localOnlySaveManagerSeen)",
        "Steam gameplay SaveManager seen: $($summary.steamGameplaySaveManagerSeen)",
        "Fatal exception seen: $($summary.fatalExceptionSeen)",
        "ANR seen: $($summary.anrSeen)",
        "Dropped save write seen: $($summary.droppedSaveWriteSeen)",
        "Swallowed failure seen: $($summary.swallowedFailureSeen)",
        "Local write exceptions/failures: $($summary.localWriteExceptionCount)",
        "Review 5 note: persisted remote hashes are not an independent live Steam query."
    )
    $summaryText | Set-Content -LiteralPath (Join-Path $outDir "summary.txt") -Encoding UTF8

    $inventoryPath = "$outDir-evidence-inventory.json"
    $captureManifest = [ordered]@{
        schemaVersion = 2
        kind = "stage5-android-save-validation-capture"
        capturedUtc = [DateTime]::UtcNow.ToString("o")
        output = $outDir
        captureBinding = [ordered]@{
            sourceCommit = $SourceCommit
            sourceWorktreeDirty = $sourceWorktreeDirty
            candidateApkPath = $resolvedApkPath
            candidateApkSha256 = $candidateApkSha256
            installedApkPath = $installedApkPath
            installedApkSha256 = $installedApkSha256
            candidateMatchesInstalled = $summary.candidateMatchesInstalled
            packageName = $PackageName
            stage5Row = $Stage5Row
            evidencePhase = $EvidencePhase
        }
        device = $deviceIdentity
        waitedSeconds = $WaitSeconds
        logcatSince = $LogcatSince
        clearedLogcatAfterPreservingBuffer = [bool]$ClearLogcat
        logcat = $rawLogPath
        filteredLogcat = (Join-Path $outDir "filtered-logcat.txt")
        summary = (Join-Path $outDir "summary.txt")
        package = (Join-Path $outDir "package.txt")
        localSaveByteHashes = (Join-Path $outDir "local-save-byte-hashes.tsv")
        syncRecoveryStateByteHashes = (Join-Path $outDir "sync-recovery-state-byte-hashes.tsv")
        persistedSteamByteHashes = (Join-Path $outDir "persisted-steam-byte-hashes.tsv")
        evidenceInventory = $inventoryPath
        evidenceLimitations = @(
            "A missing run-as capability means actual private Android files were not inspected.",
            "Persisted Steam hashes came from launcher baseline/pending documents and are not an independent live Steam query at capture time.",
            "A UI message or log line alone is not proof of synchronized remote bytes."
        )
        gates = $summary
    }
    $captureManifest |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $outDir "manifest.json") -Encoding UTF8

    $inventoryScript = Join-Path $PSScriptRoot "new-stage5-android-evidence-inventory.ps1"
    & $inventoryScript `
        -EvidenceRoot $outDir `
        -OutputPath $inventoryPath `
        -SourceCommit $SourceCommit `
        -ApkSha256 $(if ($candidateApkSha256) { $candidateApkSha256 } else { $installedApkSha256 }) `
        -DeviceIdentity "$manufacturer $model; Android API $androidApi; ABI $abiList"

    Write-Host "Saved Android Stage 5 save-validation evidence to $outDir"
} finally {
    if ($verboseMarkerEnabled) {
        $cleanup = Invoke-AdbRaw -AdbArguments @("shell", "run-as", $PackageName, "sh", "-c", "rm -f files/.sts2_verbose_save_diagnostics")
        if ($cleanup.ExitCode -ne 0) {
            Write-Warning "Could not remove the verbose save diagnostics marker after capture."
        }
    }
}
