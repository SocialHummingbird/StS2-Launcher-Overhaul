[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MatrixPath,
    [string]$OutputPath = "",
    [string]$AaptPath = "",
    [string]$ApkSignerPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidVerifier = Join-Path $PSScriptRoot "verify-stage5-android-save-bundle.ps1"
$steamManifestCapture = Join-Path $PSScriptRoot "new-stage5-live-steam-manifest.ps1"
. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

$requiredCandidatePackage = "com.sts2launcher.overhaul.fork.local"
$requiredCandidateSignerSha256 =
    "fd0e3d5acf435c1d23bfc5c426e99aa9eb5808619ff1fc214ffca99cfac7e57a"
$requiredBaselineTag = "v0.2.416-startup-recovery-ime"
$requiredBaselineAsset =
    "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk"
$requiredBaselineSha256 =
    "fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b"
$requiredMinimumVersionCode = 416001

function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [AllowEmptyString()][string]$Actual,
        [AllowEmptyString()][string]$Expected
    )
    if (-not [string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Label mismatch: expected '$Expected', got '$Actual'."
    }
}

function Get-Sha256Hex {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($Bytes) | ForEach-Object {
            $_.ToString("x2")
        }) -join "")
    } finally {
        $sha256.Dispose()
    }
}

function Get-FileSha256Hex {
    param([Parameter(Mandatory = $true)][string]$Path)
    return Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($Path))
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    try {
        return [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($Path)) |
            ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$Label is not valid UTF-8 JSON: $($_.Exception.Message)"
    }
}

function Read-StrictUtf8JsonBytes {
    param(
        [Parameter(Mandatory = $true)][byte[]]$Bytes,
        [Parameter(Mandatory = $true)][string]$Label
    )

    try {
        $strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
        $text = $strictUtf8.GetString($Bytes)
    } catch [Text.DecoderFallbackException] {
        throw "$Label is not valid UTF-8: $($_.Exception.Message)"
    }
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xfeff) {
        $text = $text.Substring(1)
    }
    try {
        return $text | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$Label is not valid UTF-8 JSON: $($_.Exception.Message)"
    }
}

function Resolve-MatrixPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Evidence path is empty."
    }
    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }
    return [IO.Path]::GetFullPath((Join-Path $script:MatrixDirectory $Path))
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Recorded path is empty."
    }
    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }
    return [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Path))
}

function Get-RequiredUtc {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $parsed = [DateTimeOffset]::MinValue
    if ([string]::IsNullOrWhiteSpace($Value) -or
        -not [DateTimeOffset]::TryParse(
            $Value,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$parsed
        )) {
        throw "$Label is not a valid timestamp: '$Value'."
    }
    return $parsed.ToUniversalTime()
}

function Normalize-SavePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $normalized = $Path.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        $normalized.StartsWith('/') -or
        $normalized.EndsWith('/') -or
        $normalized.Contains('//') -or
        $normalized -match '(^|/)\.\.?(/|$)') {
        throw "Unsafe or non-canonical save path: '$Path'."
    }
    return $normalized
}

function New-FileMap {
    param(
        [Parameter(Mandatory = $true)]$Files,
        [Parameter(Mandatory = $true)][string]$Label,
        [switch]$IncludeRole
    )

    $map = [Collections.Generic.Dictionary[string, string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    foreach ($file in @($Files)) {
        $path = Normalize-SavePath -Path ([string]$file.path)
        if ($map.ContainsKey($path)) {
            throw "$Label has a case-insensitive duplicate path: $path"
        }
        $exists = $file.exists -is [bool] -and [bool]$file.exists
        if (-not ($file.exists -is [bool])) {
            throw "$Label has a non-boolean exists value for $path."
        }
        $size = [int64]$file.sizeBytes
        $hash = ([string]$file.sha256).ToLowerInvariant()
        if ($exists) {
            if ($size -lt 0 -or $hash -notmatch '^[0-9a-f]{64}$') {
                throw "$Label has invalid present-file metadata for $path."
            }
        } elseif ($size -ne 0 -or $hash) {
            throw "$Label has non-empty metadata for missing path $path."
        }
        $rolePrefix = if ($IncludeRole) { ([string]$file.role) + "`t" } else { "" }
        $map.Add(
            $path,
            "$rolePrefix$($exists.ToString().ToLowerInvariant())`t$size`t$hash"
        )
    }
    return $map
}

function Get-MapSignature {
    param([Parameter(Mandatory = $true)]$Map)

    $lines = @($Map.Keys | Sort-Object | ForEach-Object {
        "$_`t$($Map[$_])"
    })
    return Get-Sha256Hex -Bytes (
        [Text.Encoding]::UTF8.GetBytes(($lines -join "`n") + "`n")
    )
}

function Assert-MapsEqual {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Left,
        [Parameter(Mandatory = $true)]$Right
    )

    if ($Left.Count -ne $Right.Count) {
        throw "$Label file-count mismatch: $($Left.Count) versus $($Right.Count)."
    }
    foreach ($path in $Left.Keys) {
        if (-not $Right.ContainsKey($path) -or
            -not [string]::Equals($Left[$path], $Right[$path], [StringComparison]::Ordinal)) {
            throw "$Label differs at $path."
        }
    }
}

function Assert-MapsDiffer {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Left,
        [Parameter(Mandatory = $true)]$Right
    )
    if ((Get-MapSignature -Map $Left) -eq (Get-MapSignature -Map $Right)) {
        throw "$Label unexpectedly contains identical bytes/tombstones."
    }
}

function Assert-Context {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected
    )

    Assert-Equal -Label "$Label SteamID64" -Actual ([string]$Actual.steamId64) -Expected ([string]$Expected.steamId64)
    Assert-Equal -Label "$Label namespace" -Actual ([string]$Actual.saveNamespace).ToLowerInvariant() -Expected ([string]$Expected.saveNamespace)
    Assert-Equal -Label "$Label runtime identity" -Actual ([string]$Actual.runtimeIdentity) -Expected ([string]$Expected.runtimeIdentity)
    Assert-Equal -Label "$Label mod-set fingerprint" -Actual ([string]$Actual.modSetFingerprint) -Expected ([string]$Expected.modSetFingerprint)
}

function Assert-ExactProperties {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string[]]$Names,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $actual = @($Object.PSObject.Properties.Name | Sort-Object)
    $expected = @($Names | Sort-Object)
    if (($actual -join "`n") -ne ($expected -join "`n")) {
        throw "$Label fields must be exactly: $($expected -join ', ')."
    }
}

function Get-AndroidSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [string]$RecoveryTreeSha256 = ""
    )

    if (-not $RecoveryTreeSha256) {
        return [pscustomobject]@{
            Context = $Record.Data.context
            Files = New-FileMap -Files $Record.Data.snapshot.files -Label "$($Record.Spec.id) current Android snapshot"
            CapturedUtc = Get-RequiredUtc -Value ([string]$Record.Data.snapshot.capturedUtc) -Label "$($Record.Spec.id) snapshot time"
        }
    }

    if ($RecoveryTreeSha256 -notmatch '^[0-9a-f]{64}$') {
        throw "Android recovery tree hash is not a SHA-256 value: '$RecoveryTreeSha256'."
    }
    $matches = @($Record.Data.recoverySnapshots | Where-Object {
        [string]$_.treeSha256 -eq $RecoveryTreeSha256
    })
    if ($matches.Count -ne 1) {
        throw "$($Record.Spec.id) does not contain exactly one recovery snapshot with tree SHA-256 $RecoveryTreeSha256."
    }
    $snapshot = $matches[0]
    if (-not [string]$snapshot.contextMarker) {
        throw "$($Record.Spec.id) recovery snapshot $RecoveryTreeSha256 has no exact SaveContext marker."
    }
    try {
        $marker = ([string]$snapshot.contextMarker) | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$($Record.Spec.id) recovery snapshot $RecoveryTreeSha256 has an invalid SaveContext marker."
    }
    $context = [pscustomobject]@{
        steamId64 = [string]$marker.SteamId64
        saveNamespace = ([string]$marker.SaveNamespace).ToLowerInvariant()
        runtimeIdentity = [string]$marker.RuntimeIdentity
        modSetFingerprint = [string]$marker.ModSetFingerprint
    }
    return [pscustomobject]@{
        Context = $context
        Files = New-FileMap -Files $snapshot.files -Label "$($Record.Spec.id) recovery snapshot $RecoveryTreeSha256"
        CapturedUtc = Get-RequiredUtc -Value ([string]$snapshot.capturedUtc) -Label "$($Record.Spec.id) recovery snapshot time"
    }
}

function Get-SteamSelectedMap {
    param([Parameter(Mandatory = $true)]$Record)

    $namespace = ([string]$Record.Data.selectedContext.saveNamespace).ToLowerInvariant()
    $role = "$namespace-save"
    $files = @($Record.Data.files | Where-Object {
        [string]$_.path -eq 'profile.save' -or [string]$_.role -eq $role
    })
    return New-FileMap -Files $files -Label "$($Record.Spec.id) selected Steam snapshot"
}

function Get-SteamFullMap {
    param([Parameter(Mandatory = $true)]$Record)
    return New-FileMap -Files $Record.Data.files -Label "$($Record.Spec.id) full Steam snapshot" -IncludeRole
}

function Get-RegexMatchCount {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Pattern
    )
    return [regex]::Matches($Text, $Pattern).Count
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
                Time = $Matches.time
                Pid = [int]$Matches.pid
                Tid = [int]$Matches.tid
                Priority = $Matches.priority
                Tag = $Matches.tag.Trim()
                Message = $Matches.message
                Line = $clean
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

function Get-StructuredSaveEvents {
    param([Parameter(Mandatory = $true)][object[]]$AppEntries)

    $marker = 'STS2_SAVE_EVENT '
    $events = [Collections.Generic.List[object]]::new()
    foreach ($entry in $AppEntries) {
        if (-not $entry.Message.StartsWith($marker, [StringComparison]::Ordinal)) {
            continue
        }
        try {
            $payload = $entry.Message.Substring($marker.Length) |
                ConvertFrom-Json -ErrorAction Stop
        } catch {
            throw "STS2Mobile contains an invalid structured save terminal event."
        }
        $eventName = [string]$payload.Event
        if ($eventName -eq 'automatic-sync-terminal') {
            Assert-ExactProperties -Object $payload -Names @(
                'Event', 'Version', 'Operation', 'Outcome', 'Detail',
                'ContextSha256', 'RemoteVerified'
            ) -Label 'automatic-sync terminal event'
            Assert-True -Condition (Test-IsJsonInteger $payload.Version) -Message 'Automatic-sync terminal Version must be a JSON integer.'
            Assert-True -Condition ([int64]$payload.Version -eq 1) -Message 'Automatic-sync terminal Version must be 1.'
            Assert-True -Condition ($payload.RemoteVerified -is [bool]) -Message 'Automatic-sync terminal RemoteVerified must be a JSON boolean.'
            Assert-True -Condition ([string]$payload.Operation -in @('recover', 'reconcile', 'begin-game')) -Message 'Automatic-sync terminal operation is invalid.'
            Assert-True -Condition ([string]$payload.Outcome -in @(
                'synchronized', 'source-choice-required', 'conflict',
                'game-session-prepared', 'pending-recovery-required', 'failed'
            )) -Message 'Automatic-sync terminal outcome is invalid.'
            Assert-True -Condition ([string]$payload.Detail -in @(
                'verified', 'no-pending-work', 'source-choice-required',
                'local-and-remote-diverged', 'account-mismatch',
                'namespace-mismatch', 'branch-mismatch', 'mod-set-mismatch',
                'context-missing', 'context-unreadable', 'independent-change',
                'session-prepared', 'pending-recovery', 'commit-rejected',
                'remote-readback-mismatch', 'operation-failed'
            )) -Message 'Automatic-sync terminal detail is invalid.'
            $contextSha256 = [string]$payload.ContextSha256
            Assert-True -Condition (-not $contextSha256 -or $contextSha256 -match '^[0-9a-f]{64}$') -Message 'Automatic-sync terminal ContextSha256 is invalid.'
            $verified = [bool]$payload.RemoteVerified
            Assert-True -Condition (
                (-not $verified) -or (
                    [string]$payload.Outcome -eq 'synchronized' -and
                    [string]$payload.Detail -eq 'verified' -and
                    $contextSha256 -match '^[0-9a-f]{64}$'
                )
            ) -Message 'Automatic-sync terminal claims remote verification without a verified synchronized context.'
            Assert-True -Condition (
                [string]$payload.Detail -ne 'verified' -or $verified
            ) -Message 'Automatic-sync terminal reports verified bytes without RemoteVerified=true.'
        } elseif ($eventName -eq 'save-recovery-terminal') {
            Assert-ExactProperties -Object $payload -Names @(
                'Event', 'Version', 'Operation', 'Outcome', 'Detail'
            ) -Label 'save-recovery terminal event'
            Assert-True -Condition (Test-IsJsonInteger $payload.Version) -Message 'Save-recovery terminal Version must be a JSON integer.'
            Assert-True -Condition ([int64]$payload.Version -eq 1) -Message 'Save-recovery terminal Version must be 1.'
            Assert-True -Condition ([string]$payload.Operation -in @('restore', 'undo')) -Message 'Save-recovery terminal operation is invalid.'
            Assert-Equal -Label 'Save-recovery terminal outcome' -Actual ([string]$payload.Outcome) -Expected 'completed'
            Assert-Equal -Label 'Save-recovery terminal detail' -Actual ([string]$payload.Detail) -Expected 'byte-verified-local-only'
        } else {
            throw "STS2Mobile contains an unknown structured save terminal event '$eventName'."
        }
        $events.Add([pscustomobject]@{
            Index = [int]$entry.Index
            Event = $eventName
            Payload = $payload
        })
    }
    return @($events)
}

function Get-CollectorLogSignals {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$PackageName
    )

    $entries = @(Get-TimeLogEntries -Text $Text)
    $appEntries = @($entries | Where-Object { $_.Tag -ceq 'STS2Mobile' })
    $appText = @($appEntries | ForEach-Object { $_.Message }) -join "`n"
    $events = @(Get-StructuredSaveEvents -AppEntries $appEntries)
    $automatic = @($events | Where-Object { $_.Event -eq 'automatic-sync-terminal' })
    $recovery = @($events | Where-Object { $_.Event -eq 'save-recovery-terminal' })
    $contextMismatchDetails = @(
        'account-mismatch', 'namespace-mismatch', 'branch-mismatch',
        'mod-set-mismatch'
    )
    $escapedPackage = [regex]::Escape($PackageName)
    $fatal = $false
    for ($i = 0; $i -lt $entries.Count; $i++) {
        $entry = $entries[$i]
        if ($entry.Tag -cne 'AndroidRuntime' -or
            $entry.Message -notmatch '^FATAL EXCEPTION(?:\s|:)') { continue }
        $last = [Math]::Min($entries.Count - 1, $i + 8)
        for ($j = $i; $j -le $last; $j++) {
            $candidate = $entries[$j]
            if ($candidate.Tag -ceq 'AndroidRuntime' -and
                $candidate.Pid -eq $entry.Pid -and
                $candidate.Message -match "^Process:\s*$escapedPackage(?:,|\s|$)") {
                $fatal = $true
            }
        }
    }
    $anr = [bool](@($entries | Where-Object {
        $_.Tag -in @('ActivityManager', 'ActivityTaskManager', 'am_anr') -and
        $_.Message -match "(?i)(?:\bANR in\s+|\bam_anr\b[^\r\n]*)$escapedPackage(?:/|:|,|\s|$)"
    }).Count)
    $localWriteExceptionPattern =
        '(?im)^[^\r\n]*(?:(?:\[Save\]|Android local save|local-only save|local save write)[^\r\n]*(?:exception|failed|failure|error|could not|unable to|denied|dropped|swallowed|ignored|discarded)|(?:exception|failed|failure|error|could not|unable to|denied)[^\r\n]*(?:Android local save|local save write))[^\r\n]*$'
    return [pscustomobject][ordered]@{
        automaticSyncPendingLogSeen = [bool](@($automatic | Where-Object { $_.Payload.Outcome -in @('conflict', 'pending-recovery-required', 'failed') }).Count)
        automaticSyncVerifiedLogSeen = [bool](@($automatic | Where-Object { $_.Payload.Outcome -eq 'synchronized' -and [bool]$_.Payload.RemoteVerified }).Count)
        automaticSyncConflictLogSeen = [bool](@($automatic | Where-Object { $_.Payload.Outcome -eq 'conflict' }).Count)
        syncedLogSeen = [bool](@($automatic | Where-Object { $_.Payload.Outcome -eq 'synchronized' -and [bool]$_.Payload.RemoteVerified }).Count)
        readBackMismatchSeen = [bool](@($automatic | Where-Object { $_.Payload.Outcome -eq 'failed' -and $_.Payload.Detail -eq 'remote-readback-mismatch' }).Count)
        commitFailureSeen = [bool](@($automatic | Where-Object { $_.Payload.Outcome -eq 'failed' -and $_.Payload.Detail -eq 'commit-rejected' }).Count)
        saveContextMismatchSeen = [bool](@($automatic | Where-Object { $_.Payload.Detail -in $contextMismatchDetails }).Count)
        modSetMismatchSeen = [bool](@($automatic | Where-Object { $_.Payload.Detail -eq 'mod-set-mismatch' }).Count)
        branchMismatchSeen = [bool](@($automatic | Where-Object { $_.Payload.Detail -eq 'branch-mismatch' }).Count)
        recoveryLogSeen = [bool]($recovery.Count -gt 0 -or $appText -match 'STS2_SAVE_EXPORT_COMPLETE ')
        recoveryRestoreLogCount = @($recovery | Where-Object { $_.Payload.Operation -eq 'restore' }).Count
        recoveryUndoLogCount = @($recovery | Where-Object { $_.Payload.Operation -eq 'undo' }).Count
        localSaveBaseSeen = [bool]($appText -match '\[Save\] Android local save base:')
        localSaveWrites = Get-RegexMatchCount -Text $appText -Pattern '\[Save\] Android local save write:'
        localSaveReads = Get-RegexMatchCount -Text $appText -Pattern '\[Save\] Android local save read'
        localSaveExistsChecks = Get-RegexMatchCount -Text $appText -Pattern '\[Save\] Android local save exists:'
        localOnlySaveManagerSeen = [bool]($appText -match '\[Save\] Created Android gameplay SaveManager with local storage only')
        steamGameplaySaveManagerSeen = [bool]($appText -match 'Created .*SaveManager.*Steam|Steam.*gameplay SaveManager')
        fatalExceptionSeen = $fatal
        anrSeen = $anr
        droppedSaveWriteSeen = [bool]($appText -match '(?im)dropped (save )?write|save write[^\r\n]*(ignored|discarded)')
        swallowedFailureSeen = [bool]($appText -match '(?im)swallowed (exception|failure|error)')
        localWriteExceptionCount = Get-RegexMatchCount -Text $appText -Pattern $localWriteExceptionPattern
        entries = $entries
        appEntries = $appEntries
        structuredEvents = $events
    }
}

function Assert-CollectorLogClaim {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)]$Expected
    )

    $property = $Collector.Data.gates.PSObject.Properties[$Name]
    Assert-True -Condition ($null -ne $property) -Message "$($Collector.Spec.id) collector omits derived log claim $Name."
    if ($Expected -is [bool]) {
        Assert-True -Condition ($property.Value -is [bool]) -Message "$($Collector.Spec.id) collector log claim $Name is not boolean."
        Assert-True -Condition ([bool]$property.Value -eq [bool]$Expected) -Message "$($Collector.Spec.id) collector log claim $Name disagrees with the inventoried raw log."
        return
    }
    Assert-True -Condition (
        $property.Value -is [byte] -or $property.Value -is [int16] -or
        $property.Value -is [int32] -or $property.Value -is [int64]
    ) -Message "$($Collector.Spec.id) collector log claim $Name is not an integer."
    Assert-True -Condition ([int64]$property.Value -eq [int64]$Expected) -Message "$($Collector.Spec.id) collector log claim $Name disagrees with the inventoried raw log."
}

function Test-HasSyncedLog {
    param([Parameter(Mandatory = $true)]$Collector)
    return [bool]$Collector.LogSignals.syncedLogSeen
}

function Assert-PendingPresent {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android
    )
    $pending = $Android.Data.launcherState.pending
    Assert-True -Condition ([bool]$pending.present) -Message "$($Android.Spec.id) verified export did not preserve a pending-sync record."
    Assert-True -Condition ([string]$pending.phase -in @('game-running', 'uploading', 'downloading')) -Message "$($Android.Spec.id) pending-sync phase is invalid."
    Assert-Context -Label "$($Android.Spec.id) pending context" -Actual $pending.context -Expected $Android.Data.context
    if ([bool]$Collector.Data.gates.runAsAvailable) {
        Assert-True -Condition ([int]$Collector.StateSignals.pendingSyncDocumentCount -gt 0) -Message "$($Collector.Spec.id) run-as evidence did not see the pending-sync record."
        Assert-True -Condition ([string]$pending.phase -in @($Collector.StateSignals.pendingSyncPhases)) -Message "$($Collector.Spec.id) run-as pending phase disagrees with the verified export."
    }
}

function Assert-PendingCleared {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android
    )
    Assert-True -Condition (-not [bool]$Android.Data.launcherState.pending.present) -Message "$($Android.Spec.id) verified export still has a pending-sync record."
    if ([bool]$Collector.Data.gates.runAsAvailable) {
        Assert-True -Condition ([int]$Collector.StateSignals.pendingSyncDocumentCount -eq 0) -Message "$($Collector.Spec.id) run-as evidence still has a pending-sync record."
    }
}

function Assert-NoSynced {
    param([Parameter(Mandatory = $true)]$Collector)
    Assert-True -Condition (-not [bool]$Collector.LogSignals.automaticSyncVerifiedLogSeen) -Message "$($Collector.Spec.id) reports verified synchronization on a non-success path."
    Assert-True -Condition (-not (Test-HasSyncedLog -Collector $Collector)) -Message "$($Collector.Spec.id) contains a false Synced log."
}

function Assert-CollectorAndroidBytes {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not [bool]$Collector.Data.gates.runAsAvailable) {
        # The signed release candidate is normally non-debuggable. In that case the
        # independently verified in-app recovery export is the Android byte authority.
        return
    }

    $namespace = ([string]$Android.Data.context.saveNamespace).ToLowerInvariant()
    $prefix = if ($namespace -eq 'modded') { 'modded/' } else { '' }
    $captured = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    $hashPath = Resolve-RepoPath -Path ([string]$Collector.Data.localSaveByteHashes)
    $lines = @([IO.File]::ReadAllLines($hashPath, [Text.Encoding]::UTF8))
    Assert-True -Condition ($lines.Count -gt 0 -and $lines[0].TrimStart([char]0xfeff) -eq "sha256`tsizeBytes`tdevicePath") -Message "$Label has an invalid collector local-hash header."
    foreach ($line in $lines | Select-Object -Skip 1) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split "`t", 3
        Assert-True -Condition ($parts.Count -eq 3 -and $parts[0] -match '^[0-9a-fA-F]{64}$' -and $parts[1] -match '^[0-9]+$' -and $parts[2] -match '^files/') -Message "$Label has an invalid collector local-hash row."
        $path = Normalize-SavePath -Path $parts[2].Substring('files/'.Length)
        $selected = $path -eq 'profile.save' -or $path -match ('^' + [regex]::Escape($prefix) + 'profile[1-3]/saves/')
        if ($namespace -eq 'vanilla' -and $path.StartsWith('modded/', [StringComparison]::OrdinalIgnoreCase)) {
            $selected = $false
        }
        if (-not $selected) { continue }
        if ($captured.ContainsKey($path)) {
            throw "$Label collector repeats local path $path."
        }
        $captured.Add($path, "$($parts[1])`t$($parts[0].ToLowerInvariant())")
    }

    $expected = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($Android.Data.snapshot.files | Where-Object { [bool]$_.exists })) {
        $expected.Add([string]$file.path, "$([int64]$file.sizeBytes)`t$(([string]$file.sha256).ToLowerInvariant())")
    }
    Assert-MapsEqual -Label "$Label run-as/export cross-check" -Left $captured -Right $expected
}

function Assert-SuccessCollector {
    param([Parameter(Mandatory = $true)]$Collector)
    Assert-True -Condition ([bool]$Collector.LogSignals.automaticSyncVerifiedLogSeen) -Message "$($Collector.Spec.id) has no verified automatic-sync log."
    Assert-True -Condition (Test-HasSyncedLog -Collector $Collector) -Message "$($Collector.Spec.id) has no Synced log to pair with live Steam bytes."
    Assert-True -Condition (-not [bool]$Collector.LogSignals.automaticSyncConflictLogSeen) -Message "$($Collector.Spec.id) unexpectedly reports a conflict."
    Assert-True -Condition (-not [bool]$Collector.LogSignals.saveContextMismatchSeen) -Message "$($Collector.Spec.id) unexpectedly reports a SaveContext mismatch."
    Assert-True -Condition (-not [bool]$Collector.LogSignals.commitFailureSeen) -Message "$($Collector.Spec.id) unexpectedly reports commit failure."
    Assert-True -Condition (-not [bool]$Collector.LogSignals.readBackMismatchSeen) -Message "$($Collector.Spec.id) unexpectedly reports read-back mismatch."
}

$resolvedMatrix = (Resolve-Path -LiteralPath $MatrixPath).Path
$script:MatrixDirectory = Split-Path -Parent $resolvedMatrix
$script:RepoRoot = $repoRoot
$matrixBytes = [IO.File]::ReadAllBytes($resolvedMatrix)
$matrixSha256 = Get-Sha256Hex -Bytes $matrixBytes
$matrix = Read-JsonFile -Path $resolvedMatrix -Label 'Stage 5 physical matrix'

Assert-True -Condition ([int]$matrix.schemaVersion -eq 1) -Message 'Stage 5 matrix schemaVersion must be 1.'
Assert-Equal -Label 'Stage 5 matrix kind' -Actual ([string]$matrix.kind) -Expected 'stage5-physical-save-matrix'

$binding = $matrix.binding
$candidate = $binding.candidate
Assert-True -Condition ([string]$candidate.sourceCommit -match '^[0-9a-f]{40}$') -Message 'Matrix candidate sourceCommit must be a lowercase 40-character commit.'
Assert-True -Condition ([string]$candidate.apkSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix candidate APK hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.buildInfoSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix candidate build-info hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.signerSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix candidate signer hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.updateBaselineApkSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix update-baseline APK hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.packageName -match '^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$') -Message 'Matrix candidate package name is invalid.'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.versionName)) -Message 'Matrix candidate versionName is empty.'
Assert-True -Condition ([int64]$candidate.versionCode -gt 0) -Message 'Matrix candidate versionCode must be positive.'
Assert-True -Condition ([string]$candidate.candidateRunId -match '^[0-9]+$') -Message 'Matrix candidate run id is invalid.'
Assert-True -Condition ([string]$candidate.candidateRunAttempt -match '^[1-9][0-9]*$') -Message 'Matrix candidate run attempt is invalid.'
Assert-Equal -Label 'Matrix candidate ABI' -Actual ([string]$candidate.abi) -Expected 'arm64-v8a'
Assert-Equal -Label 'Matrix candidate signing channel' -Actual ([string]$candidate.signingChannel) -Expected 'release'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.releaseTag)) -Message 'Matrix candidate release tag is empty.'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.updateBaselineTag)) -Message 'Matrix update-baseline tag is empty.'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.updateBaselineAssetName)) -Message 'Matrix update-baseline asset name is empty.'
Assert-Equal -Label 'Required affected-user package' -Actual ([string]$candidate.packageName) -Expected $requiredCandidatePackage
Assert-Equal -Label 'Required v0.2.416 update signer' -Actual ([string]$candidate.signerSha256) -Expected $requiredCandidateSignerSha256
Assert-True -Condition ([int64]$candidate.versionCode -gt $requiredMinimumVersionCode) -Message "Matrix candidate versionCode must exceed the published v0.2.416 versionCode $requiredMinimumVersionCode."
Assert-Equal -Label 'Required update-baseline tag' -Actual ([string]$candidate.updateBaselineTag) -Expected $requiredBaselineTag
Assert-Equal -Label 'Required update-baseline asset' -Actual ([string]$candidate.updateBaselineAssetName) -Expected $requiredBaselineAsset
Assert-Equal -Label 'Required update-baseline APK SHA-256' -Actual ([string]$candidate.updateBaselineApkSha256) -Expected $requiredBaselineSha256
Assert-True -Condition ([string]$binding.steamId64 -match '^[0-9]{17}$') -Message 'Matrix SteamID64 is invalid.'
Assert-True -Condition ([string]$binding.device.serialSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix device serial hash is invalid.'
foreach ($field in @('manufacturer', 'model', 'androidApi', 'abiList', 'buildFingerprint')) {
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$binding.device.$field)) -Message "Matrix device $field is empty."
}
Assert-True -Condition ([string]$binding.device.abiList -match '(^|,)arm64-v8a(,|$)') -Message 'Bound physical device does not advertise arm64-v8a.'

$candidateApkPath = Resolve-MatrixPath -Path ([string]$candidate.apkPath)
$buildInfoPath = Resolve-MatrixPath -Path ([string]$candidate.buildInfoPath)
Assert-True -Condition (Test-Path -LiteralPath $candidateApkPath -PathType Leaf) -Message 'Exact candidate APK is missing.'
Assert-True -Condition (Test-Path -LiteralPath $buildInfoPath -PathType Leaf) -Message 'Candidate build-info sidecar is missing.'
Assert-Equal -Label 'Exact candidate APK SHA-256' -Actual (Get-FileSha256Hex -Path $candidateApkPath) -Expected ([string]$candidate.apkSha256)
Assert-Equal -Label 'Candidate build-info SHA-256' -Actual (Get-FileSha256Hex -Path $buildInfoPath) -Expected ([string]$candidate.buildInfoSha256)

$buildInfo = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($line in [IO.File]::ReadAllLines($buildInfoPath, [Text.Encoding]::UTF8)) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -notmatch '^([a-z][a-z0-9_]*)=(.*)$') {
        throw "Candidate build-info has an invalid line: $line"
    }
    if ($buildInfo.ContainsKey($Matches[1])) {
        throw "Candidate build-info repeats key $($Matches[1])."
    }
    $buildInfo.Add($Matches[1], $Matches[2])
}
$expectedBuildInfo = [ordered]@{
    version_name = [string]$candidate.versionName
    version_code = [string]$candidate.versionCode
    package_name = [string]$candidate.packageName
    abi = [string]$candidate.abi
    signing_channel = [string]$candidate.signingChannel
    signer_sha256 = ([string]$candidate.signerSha256).ToUpperInvariant()
    release_tag = [string]$candidate.releaseTag
    source_commit = [string]$candidate.sourceCommit
    candidate_run_id = [string]$candidate.candidateRunId
    candidate_run_attempt = [string]$candidate.candidateRunAttempt
    apk_sha256 = ([string]$candidate.apkSha256).ToLowerInvariant()
    update_baseline_tag = [string]$candidate.updateBaselineTag
    update_baseline_asset_name = [string]$candidate.updateBaselineAssetName
    update_baseline_apk_sha256 = ([string]$candidate.updateBaselineApkSha256).ToLowerInvariant()
}
Assert-True -Condition ($buildInfo.Count -eq $expectedBuildInfo.Count) -Message 'Candidate build-info must contain exactly the release-candidate identity fields.'
foreach ($key in $expectedBuildInfo.Keys) {
    Assert-True -Condition ($buildInfo.ContainsKey($key)) -Message "Candidate build-info omits $key."
    $actualValue = $buildInfo[$key]
    if ($key -eq 'signer_sha256') { $actualValue = $actualValue.ToUpperInvariant() }
    if ($key -in @('apk_sha256', 'update_baseline_apk_sha256')) { $actualValue = $actualValue.ToLowerInvariant() }
    Assert-Equal -Label "Candidate build-info $key" -Actual $actualValue -Expected ([string]$expectedBuildInfo[$key])
}

$resolvedAapt = if ($AaptPath) { (Resolve-Path -LiteralPath $AaptPath).Path } else { Resolve-AndroidBuildTool 'aapt' }
$resolvedApkSigner = if ($ApkSignerPath) { (Resolve-Path -LiteralPath $ApkSignerPath).Path } else { Resolve-AndroidBuildTool 'apksigner' }
$apkIdentity = Get-AndroidApkIdentity -Path $candidateApkPath -Aapt $resolvedAapt -ApkSigner $resolvedApkSigner
Assert-Equal -Label 'Actual APK package' -Actual ([string]$apkIdentity.packageName) -Expected ([string]$candidate.packageName)
Assert-Equal -Label 'Actual APK version name' -Actual ([string]$apkIdentity.versionName) -Expected ([string]$candidate.versionName)
Assert-True -Condition ([int64]$apkIdentity.versionCode -eq [int64]$candidate.versionCode) -Message 'Actual APK version code differs from the candidate binding.'
Assert-Equal -Label 'Actual APK signer' -Actual ([string]$apkIdentity.signerSha256).ToLowerInvariant() -Expected ([string]$candidate.signerSha256)
$badging = Invoke-CheckedTool $resolvedAapt @('dump', 'badging', $candidateApkPath)
Assert-True -Condition ($badging -match "(?m)^native-code:.*'arm64-v8a'") -Message 'Actual APK does not contain the bound arm64-v8a native ABI.'

$contexts = [Collections.Generic.Dictionary[string, object]]::new(
    [StringComparer]::Ordinal
)
foreach ($context in @($matrix.contexts)) {
    $id = [string]$context.id
    if ($contexts.ContainsKey($id)) {
        throw "Duplicate expected SaveContext id: $id"
    }
    $contexts.Add($id, $context)
    Assert-Equal -Label "$id SteamID64" -Actual ([string]$context.steamId64) -Expected ([string]$binding.steamId64)
}
$requiredContexts = [ordered]@{
    'vanilla-public' = @('vanilla', 'public', '')
    'vanilla-public-beta' = @('vanilla', 'public-beta', '')
    'modded-exact' = @('modded', 'public', $null)
    'modded-changed' = @('modded', 'public', $null)
}
Assert-True -Condition ($contexts.Count -eq $requiredContexts.Count) -Message 'Matrix must define exactly four expected SaveContexts.'
foreach ($id in $requiredContexts.Keys) {
    Assert-True -Condition ($contexts.ContainsKey($id)) -Message "Matrix is missing expected SaveContext $id."
    $context = $contexts[$id]
    $expected = $requiredContexts[$id]
    Assert-Equal -Label "$id namespace" -Actual ([string]$context.saveNamespace).ToLowerInvariant() -Expected $expected[0]
    Assert-Equal -Label "$id runtime identity" -Actual ([string]$context.runtimeIdentity) -Expected $expected[1]
    if ($expected[0] -eq 'vanilla') {
        Assert-Equal -Label "$id mod-set fingerprint" -Actual ([string]$context.modSetFingerprint) -Expected ''
    } else {
        Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$context.modSetFingerprint)) -Message "$id requires a mod-set fingerprint."
    }
}
Assert-True -Condition (
    [string]$contexts['modded-exact'].modSetFingerprint -ne [string]$contexts['modded-changed'].modSetFingerprint
) -Message 'Exact and changed mod-set contexts must have different fingerprints.'

$evidence = [Collections.Generic.Dictionary[string, object]]::new(
    [StringComparer]::Ordinal
)
$seenEvidencePaths = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
$usedEvidence = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$syncedProofs = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

foreach ($spec in @($matrix.evidence)) {
    $id = [string]$spec.id
    Assert-True -Condition ($id -match '^[a-z0-9][a-z0-9-]{2,95}$') -Message "Invalid evidence id: '$id'."
    if ($evidence.ContainsKey($id)) {
        throw "Duplicate evidence id: $id"
    }
    $kind = [string]$spec.kind
    Assert-True -Condition ($kind -in @('android-manifest', 'steam-manifest', 'collector')) -Message "$id has unsupported evidence kind '$kind'."
    $path = Resolve-MatrixPath -Path ([string]$spec.path)
    Assert-True -Condition (Test-Path -LiteralPath $path -PathType Leaf) -Message "$id evidence file is missing: $path"
    Assert-True -Condition ($seenEvidencePaths.Add($path)) -Message "Evidence path is reused by more than one id: $path"
    $expectedHash = ([string]$spec.sha256).ToLowerInvariant()
    Assert-True -Condition ($expectedHash -match '^[0-9a-f]{64}$') -Message "$id has an invalid recorded SHA-256."
    Assert-Equal -Label "$id evidence SHA-256" -Actual (Get-FileSha256Hex -Path $path) -Expected $expectedHash
    $data = Read-JsonFile -Path $path -Label $id
    $record = [pscustomobject]@{
        Spec = $spec
        Path = $path
        Data = $data
        LogText = ''
        FocusedLogText = ''
        LogSignals = $null
        StateSignals = $null
    }
    $evidence.Add($id, $record)
}

function Validate-AndroidManifest {
    param([Parameter(Mandatory = $true)]$Record)

    $data = $Record.Data
    Assert-True -Condition ([int]$data.schemaVersion -eq 1) -Message "$($Record.Spec.id) Android schemaVersion must be 1."
    Assert-Equal -Label "$($Record.Spec.id) kind" -Actual ([string]$data.kind) -Expected 'stage5-android-save-manifest'
    Assert-Equal -Label "$($Record.Spec.id) authority" -Actual ([string]$data.authority) -Expected 'verified-recovery-export-current-android-snapshot'
    Assert-True -Condition (-not [bool]$data.originalSourcesWereModified) -Message "$($Record.Spec.id) reports modified original sources."
    Assert-True -Condition (-not [bool]$data.steamWasContacted) -Message "$($Record.Spec.id) recovery export contacted Steam."
    Assert-True -Condition ($data.cloudSyncEnabled -is [bool]) -Message "$($Record.Spec.id) has no boolean Cloud Sync setting evidence."
    [void](Get-RequiredUtc -Value ([string]$data.verifiedUtc) -Label "$($Record.Spec.id) verification time")

    $bundlePath = [IO.Path]::GetFullPath([string]$data.sourceBundle)
    Assert-True -Condition (Test-Path -LiteralPath $bundlePath -PathType Leaf) -Message "$($Record.Spec.id) source recovery bundle is missing: $bundlePath"
    $bundleHash = ([string]$data.sourceBundleSha256).ToLowerInvariant()
    Assert-True -Condition ($bundleHash -match '^[0-9a-f]{64}$') -Message "$($Record.Spec.id) source bundle hash is invalid."
    Assert-Equal -Label "$($Record.Spec.id) source bundle SHA-256" -Actual (Get-FileSha256Hex -Path $bundlePath) -Expected $bundleHash

    $namespace = switch (([string]$data.context.saveNamespace).ToLowerInvariant()) {
        'vanilla' { 'Vanilla' }
        'modded' { 'Modded' }
        default { throw "$($Record.Spec.id) has an invalid Android namespace." }
    }
    $temporaryOutput = Join-Path ([IO.Path]::GetTempPath()) (
        "sts2-stage5-review-" + [Guid]::NewGuid().ToString('N') + '.json'
    )
    try {
        & $script:AndroidVerifier `
            -BundlePath $bundlePath `
            -OutputPath $temporaryOutput `
            -Namespace $namespace `
            -ExpectedSteamId64 ([string]$data.context.steamId64) `
            -ExpectedRuntimeIdentity ([string]$data.context.runtimeIdentity) `
            -ExpectedModSetFingerprint ([string]$data.context.modSetFingerprint) *> $null
        $regenerated = Read-JsonFile -Path $temporaryOutput -Label "$($Record.Spec.id) regenerated Android manifest"
    } finally {
        if (Test-Path -LiteralPath $temporaryOutput) {
            Remove-Item -LiteralPath $temporaryOutput -Force
        }
    }

    Assert-Context -Label "$($Record.Spec.id) Android context" -Actual $data.context -Expected $regenerated.context
    Assert-ExactProperties `
        -Object $data.exportBinding `
        -Names @(
            'event', 'version', 'exportId', 'bundleSha256',
            'currentAndroidTreeSha256', 'selectedSaveContextSha256'
        ) `
        -Label "$($Record.Spec.id) export binding"
    foreach ($field in @(
        'event', 'version', 'exportId', 'bundleSha256',
        'currentAndroidTreeSha256', 'selectedSaveContextSha256'
    )) {
        Assert-Equal `
            -Label "$($Record.Spec.id) regenerated export binding $field" `
            -Actual ([string]$data.exportBinding.$field) `
            -Expected ([string]$regenerated.exportBinding.$field)
    }
    Assert-Equal `
        -Label "$($Record.Spec.id) export binding bundle SHA-256" `
        -Actual ([string]$data.exportBinding.bundleSha256) `
        -Expected $bundleHash
    Assert-Equal -Label "$($Record.Spec.id) current tree hash" -Actual ([string]$data.snapshot.treeSha256) -Expected ([string]$regenerated.snapshot.treeSha256)
    Assert-MapsEqual `
        -Label "$($Record.Spec.id) regenerated current Android snapshot" `
        -Left (New-FileMap -Files $data.snapshot.files -Label "$($Record.Spec.id) current snapshot") `
        -Right (New-FileMap -Files $regenerated.snapshot.files -Label "$($Record.Spec.id) regenerated current snapshot")
    [void](Get-RequiredUtc -Value ([string]$data.snapshot.capturedUtc) -Label "$($Record.Spec.id) current snapshot time")

    $recordedRecovery = @($data.recoverySnapshots)
    $regeneratedRecovery = @($regenerated.recoverySnapshots)
    Assert-True -Condition ($recordedRecovery.Count -eq $regeneratedRecovery.Count) -Message "$($Record.Spec.id) recovery snapshot count differs from its source bundle."
    for ($index = 0; $index -lt $regeneratedRecovery.Count; $index++) {
        $expectedSnapshot = $regeneratedRecovery[$index]
        $actualSnapshot = $recordedRecovery[$index]
        $tree = [string]$expectedSnapshot.treeSha256
        Assert-True -Condition ($tree -match '^[0-9a-f]{64}$') -Message "$($Record.Spec.id) recovery snapshot has no recomputed tree identity."
        foreach ($field in @('reportedSnapshotId', 'reportedSnapshotSha256', 'sourceKind', 'sourceLabel', 'classification', 'snapshotPath', 'saveNamespace', 'contextMarker', 'capturedUtc', 'treeSha256')) {
            Assert-Equal -Label "$($Record.Spec.id) recovery $tree $field" -Actual ([string]$actualSnapshot.$field) -Expected ([string]$expectedSnapshot.$field)
        }
        Assert-MapsEqual `
            -Label "$($Record.Spec.id) regenerated recovery snapshot $tree" `
            -Left (New-FileMap -Files $actualSnapshot.files -Label "$($Record.Spec.id) recovery $tree") `
            -Right (New-FileMap -Files $expectedSnapshot.files -Label "$($Record.Spec.id) regenerated recovery $tree")
    }

    foreach ($stateName in @('pending', 'baseline', 'beforeGameSnapshot', 'recoveryHold', 'recoveryJournal')) {
        $actualState = $data.launcherState.$stateName
        $expectedState = $regenerated.launcherState.$stateName
        Assert-True -Condition ($actualState.present -is [bool]) -Message "$($Record.Spec.id) launcherState.$stateName has no boolean presence flag."
        Assert-True -Condition ([bool]$actualState.present -eq [bool]$expectedState.present) -Message "$($Record.Spec.id) launcherState.$stateName presence differs from the source bundle."
        foreach ($field in @('sha256', 'phase', 'treeSha256')) {
            if ($null -ne $actualState.PSObject.Properties[$field] -or $null -ne $expectedState.PSObject.Properties[$field]) {
                Assert-Equal -Label "$($Record.Spec.id) launcherState.$stateName.$field" -Actual ([string]$actualState.$field) -Expected ([string]$expectedState.$field)
            }
        }
        if ($null -ne $actualState.context -or $null -ne $expectedState.context) {
            Assert-True -Condition ($null -ne $actualState.context -and $null -ne $expectedState.context) -Message "$($Record.Spec.id) launcherState.$stateName context presence differs from its source bundle."
            Assert-Context -Label "$($Record.Spec.id) launcherState.$stateName context" -Actual $actualState.context -Expected $expectedState.context
        }
    }
}

function Validate-SteamManifest {
    param([Parameter(Mandatory = $true)]$Record)

    $data = $Record.Data
    Assert-True -Condition ([int]$data.schemaVersion -eq 2) -Message "$($Record.Spec.id) Steam schemaVersion must be 2."
    Assert-Equal -Label "$($Record.Spec.id) kind" -Actual ([string]$data.kind) -Expected 'stage5-live-steam-save-manifest'
    Assert-Equal -Label "$($Record.Spec.id) authority" -Actual ([string]$data.authority) -Expected 'independent-live-steam-client-download'
    Assert-True -Condition ([int]$data.appId -eq 2868840) -Message "$($Record.Spec.id) has the wrong Steam app id."
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$data.captureMethod)) -Message "$($Record.Spec.id) has no independent Steam capture method."
    Assert-True -Condition ([bool]$data.sourceRootRetainedImmutable) -Message "$($Record.Spec.id) does not retain an immutable independent Steam download for review."
    [void](Get-RequiredUtc -Value ([string]$data.capturedUtc) -Label "$($Record.Spec.id) Steam capture time")

    $sourceRoot = [IO.Path]::GetFullPath([string]$data.sourceRoot)
    Assert-True -Condition (Test-Path -LiteralPath $sourceRoot -PathType Container) -Message "$($Record.Spec.id) retained Steam source root is missing."

    $syncEvidencePath = [IO.Path]::GetFullPath([string]$data.steamSyncEvidence.path)
    Assert-True -Condition (Test-Path -LiteralPath $syncEvidencePath -PathType Leaf) -Message "$($Record.Spec.id) Steam client evidence file is missing."
    Assert-True -Condition ([int64]$data.steamSyncEvidence.sizeBytes -eq (Get-Item -LiteralPath $syncEvidencePath).Length) -Message "$($Record.Spec.id) Steam client evidence size changed."
    Assert-Equal -Label "$($Record.Spec.id) Steam client evidence SHA-256" -Actual (Get-FileSha256Hex -Path $syncEvidencePath) -Expected ([string]$data.steamSyncEvidence.sha256).ToLowerInvariant()

    $files = @($data.files)
    $fullMap = New-FileMap -Files $files -Label "$($Record.Spec.id) Steam manifest" -IncludeRole
    $required = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    [void]$required.Add('profile.save')
    foreach ($namespace in @('vanilla', 'modded')) {
        $prefix = if ($namespace -eq 'modded') { 'modded/' } else { '' }
        foreach ($profileId in 1..3) {
            foreach ($name in @('progress.save', 'prefs', 'prefs.save', 'current_run.save', 'current_run_mp.save')) {
                [void]$required.Add("${prefix}profile${profileId}/saves/$name")
            }
        }
        [void]$required.Add(".sts2-launcher/contexts/$namespace.json")
    }
    foreach ($path in $required) {
        Assert-True -Condition ($fullMap.ContainsKey($path)) -Message "$($Record.Spec.id) Steam manifest omits $path."
    }
    foreach ($file in $files) {
        $path = Normalize-SavePath -Path ([string]$file.path)
        $role = [string]$file.role
        $valid = if ($path -eq 'profile.save') {
            $role -eq 'shared-save'
        } elseif ($path -match '^modded/profile[1-3]/saves/(progress\.save|prefs|prefs\.save|current_run\.save|current_run_mp\.save|history/[^/]+\.run)$') {
            $role -eq 'modded-save'
        } elseif ($path -match '^profile[1-3]/saves/(progress\.save|prefs|prefs\.save|current_run\.save|current_run_mp\.save|history/[^/]+\.run)$') {
            $role -eq 'vanilla-save'
        } elseif ($path -eq '.sts2-launcher/contexts/vanilla.json') {
            $role -eq 'vanilla-context-marker'
        } elseif ($path -eq '.sts2-launcher/contexts/modded.json') {
            $role -eq 'modded-context-marker'
        } else {
            $false
        }
        Assert-True -Condition $valid -Message "$($Record.Spec.id) has invalid Steam path/role metadata for $path."
    }

    $sorted = @($files | Sort-Object -Property @{ Expression = {
        ([string]$_.path).ToLowerInvariant()
    } }, @{ Expression = { [string]$_.path } })
    $treeLines = $sorted | ForEach-Object {
        "$($_.path)`t$($_.role)`t$($_.exists.ToString().ToLowerInvariant())`t$($_.sizeBytes)`t$($_.sha256)"
    }
    $treeHash = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes(($treeLines -join "`n") + "`n"))
    Assert-Equal -Label "$($Record.Spec.id) Steam tree SHA-256" -Actual $treeHash -Expected ([string]$data.treeSha256).ToLowerInvariant()

    $selectedNamespace = ([string]$data.selectedContext.saveNamespace).ToLowerInvariant()
    Assert-True -Condition ($selectedNamespace -in @('vanilla', 'modded')) -Message "$($Record.Spec.id) selected Steam namespace is invalid."
    $selectedMarkerPath = ".sts2-launcher/contexts/$selectedNamespace.json"
    Assert-Equal -Label "$($Record.Spec.id) selected marker path" -Actual ([string]$data.selectedContext.markerPath) -Expected $selectedMarkerPath
    Assert-Equal -Label "$($Record.Spec.id) selected marker state path" -Actual ([string]$data.selectedContextMarker.path) -Expected $selectedMarkerPath
    $marker = @($files | Where-Object { [string]$_.path -eq [string]$data.selectedContext.markerPath })
    Assert-True -Condition ($marker.Count -eq 1) -Message "$($Record.Spec.id) selected Steam context marker row is missing."
    $markerState = [string]$data.selectedContextMarker.state
    $missingAfterFailedTransfer = $markerState -eq 'missing-tombstone-after-failed-transfer'
    if ($missingAfterFailedTransfer) {
        Assert-True -Condition ([string]$Record.Spec.id -match '^r9-') -Message "$($Record.Spec.id) may use missing-marker failed-transfer evidence only in row 9."
        Assert-True -Condition (-not [bool]$marker[0].exists -and -not [bool]$data.selectedContextMarker.exists) -Message "$($Record.Spec.id) missing-marker evidence still reports the selected marker present."
        Assert-True -Condition ([string]$data.selectedContextEvidence.kind -in @('before-steam-manifest', 'pending-sync-record')) -Message "$($Record.Spec.id) missing-marker evidence has no independent context source."
    } else {
        Assert-Equal -Label "$($Record.Spec.id) selected marker state" -Actual $markerState -Expected 'present-valid'
        Assert-True -Condition ([bool]$marker[0].exists -and [bool]$data.selectedContextMarker.exists) -Message "$($Record.Spec.id) selected Steam context marker is not present."
        Assert-Equal -Label "$($Record.Spec.id) context evidence kind" -Actual ([string]$data.selectedContextEvidence.kind) -Expected 'selected-context-marker'
        Assert-Equal -Label "$($Record.Spec.id) context evidence path" -Actual ([string]$data.selectedContextEvidence.path) -Expected $selectedMarkerPath
    }
    Assert-True -Condition ([bool]$marker[0].exists -eq [bool]$data.selectedContextMarker.exists) -Message "$($Record.Spec.id) selected marker presence disagrees with its file row."
    Assert-True -Condition ([int64]$marker[0].sizeBytes -eq [int64]$data.selectedContextMarker.sizeBytes) -Message "$($Record.Spec.id) selected marker size disagrees with its file row."
    Assert-Equal -Label "$($Record.Spec.id) selected marker hash" -Actual ([string]$data.selectedContextMarker.sha256) -Expected ([string]$marker[0].sha256)

    $temporaryOutput = Join-Path ([IO.Path]::GetTempPath()) (
        "sts2-stage5-steam-review-" + [Guid]::NewGuid().ToString('N') + '.json'
    )
    $selectedNamespaceArgument = if ($selectedNamespace -eq 'vanilla') { 'Vanilla' } else { 'Modded' }
    try {
        $captureArguments = @{
            SteamCloudRoot = $sourceRoot
            SelectedNamespace = $selectedNamespaceArgument
            ExpectedSteamId64 = [string]$data.selectedContext.steamId64
            ExpectedRuntimeIdentity = [string]$data.selectedContext.runtimeIdentity
            ExpectedModSetFingerprint = [string]$data.selectedContext.modSetFingerprint
            SteamSyncEvidencePath = $syncEvidencePath
            CaptureMethod = [string]$data.captureMethod
            RetainedImmutableSource = $true
            OutputPath = $temporaryOutput
        }
        if ($missingAfterFailedTransfer) {
            $contextEvidencePath = [IO.Path]::GetFullPath([string]$data.selectedContextEvidence.path)
            Assert-True -Condition (Test-Path -LiteralPath $contextEvidencePath -PathType Leaf) -Message "$($Record.Spec.id) failed-transfer context evidence is missing."
            Assert-True -Condition ([int64]$data.selectedContextEvidence.sizeBytes -eq (Get-Item -LiteralPath $contextEvidencePath).Length) -Message "$($Record.Spec.id) failed-transfer context evidence size changed."
            Assert-Equal -Label "$($Record.Spec.id) failed-transfer context evidence hash" -Actual (Get-FileSha256Hex -Path $contextEvidencePath) -Expected ([string]$data.selectedContextEvidence.sha256).ToLowerInvariant()
            $captureArguments.CaptureFailedTransferWithMissingSelectedMarker = $true
            if ([string]$data.selectedContextEvidence.kind -eq 'before-steam-manifest') {
                $captureArguments.BeforeSteamManifestPath = $contextEvidencePath
            } else {
                $captureArguments.PendingSyncRecordPath = $contextEvidencePath
            }
        }
        & $script:SteamManifestCapture @captureArguments *> $null
        $regenerated = Read-JsonFile -Path $temporaryOutput -Label "$($Record.Spec.id) regenerated live Steam manifest"
    } finally {
        if (Test-Path -LiteralPath $temporaryOutput) {
            Remove-Item -LiteralPath $temporaryOutput -Force
        }
    }
    Assert-Context -Label "$($Record.Spec.id) regenerated Steam context" -Actual $data.selectedContext -Expected $regenerated.selectedContext
    Assert-Equal -Label "$($Record.Spec.id) regenerated Steam tree" -Actual ([string]$data.treeSha256) -Expected ([string]$regenerated.treeSha256)
    Assert-MapsEqual `
        -Label "$($Record.Spec.id) retained live Steam raw bytes" `
        -Left (New-FileMap -Files $data.files -Label "$($Record.Spec.id) recorded Steam" -IncludeRole) `
        -Right (New-FileMap -Files $regenerated.files -Label "$($Record.Spec.id) regenerated Steam" -IncludeRole)
}

function Resolve-InventoryRoot {
    param([Parameter(Mandatory = $true)][string]$RecordedRoot)
    return Resolve-RepoPath -Path $RecordedRoot
}

function Validate-CollectorInventory {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$InventoryPath,
        [Parameter(Mandatory = $true)]$Inventory
    )

    Assert-True -Condition ([int]$Inventory.schemaVersion -eq 1) -Message "$($Record.Spec.id) inventory schemaVersion must be 1."
    Assert-Equal -Label "$($Record.Spec.id) inventory kind" -Actual ([string]$Inventory.kind) -Expected 'stage5-android-regression-evidence-inventory'
    Assert-Equal -Label "$($Record.Spec.id) inventory source commit" -Actual ([string]$Inventory.sourceCommit) -Expected ([string]$script:Binding.candidate.sourceCommit)
    Assert-Equal -Label "$($Record.Spec.id) inventory APK hash" -Actual ([string]$Inventory.apkSha256).ToLowerInvariant() -Expected ([string]$script:Binding.candidate.apkSha256)
    $expectedDeviceText = "$($script:Binding.device.manufacturer) $($script:Binding.device.model); Android API $($script:Binding.device.androidApi); ABI $($script:Binding.device.abiList)"
    Assert-Equal -Label "$($Record.Spec.id) inventory device identity" -Actual ([string]$Inventory.deviceIdentity) -Expected $expectedDeviceText

    $root = Resolve-InventoryRoot -RecordedRoot ([string]$Inventory.evidenceRoot)
    $collectorRoot = Resolve-RepoPath -Path ([string]$Record.Data.output)
    Assert-Equal -Label "$($Record.Spec.id) inventory evidence root" -Actual $root -Expected $collectorRoot
    Assert-True -Condition (Test-Path -LiteralPath $root -PathType Container) -Message "$($Record.Spec.id) inventory evidence root is missing."

    $rootPrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $recorded = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    $canonicalLines = [Collections.Generic.List[string]]::new()
    $totalBytes = [int64]0
    foreach ($entry in @($Inventory.entries)) {
        $relative = Normalize-SavePath -Path ([string]$entry.relativePath)
        if ($recorded.ContainsKey($relative)) {
            throw "$($Record.Spec.id) inventory has duplicate path $relative."
        }
        $recorded.Add($relative, $entry)
        $full = [IO.Path]::GetFullPath((Join-Path $root $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
        Assert-True -Condition ($full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message "$($Record.Spec.id) inventory path escaped its root."
        Assert-True -Condition (Test-Path -LiteralPath $full -PathType Leaf) -Message "$($Record.Spec.id) inventory file is missing: $relative"
        $item = Get-Item -LiteralPath $full
        Assert-True -Condition ([int64]$entry.sizeBytes -eq $item.Length) -Message "$($Record.Spec.id) inventory size changed for $relative."
        Assert-Equal -Label "$($Record.Spec.id) inventory hash for $relative" -Actual (Get-FileSha256Hex -Path $full) -Expected ([string]$entry.sha256).ToLowerInvariant()
        $canonical = [ordered]@{
            relativePath = $relative
            sizeBytes = [int64]$entry.sizeBytes
            sha256 = ([string]$entry.sha256).ToLowerInvariant()
            modifiedUtc = [string]$entry.modifiedUtc
        }
        $canonicalLines.Add(($canonical | ConvertTo-Json -Compress))
        $totalBytes += $item.Length
    }
    $actualFiles = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force)
    Assert-True -Condition ($actualFiles.Count -eq $recorded.Count) -Message "$($Record.Spec.id) evidence tree no longer matches its complete inventory."
    foreach ($file in $actualFiles) {
        $relative = $file.FullName.Substring($rootPrefix.Length).Replace('\', '/')
        Assert-True -Condition ($recorded.ContainsKey($relative)) -Message "$($Record.Spec.id) evidence tree has unrecorded file $relative."
    }
    Assert-True -Condition ([int]$Inventory.fileCount -eq $recorded.Count) -Message "$($Record.Spec.id) inventory fileCount is wrong."
    Assert-True -Condition ([int64]$Inventory.totalBytes -eq $totalBytes) -Message "$($Record.Spec.id) inventory totalBytes is wrong."
    $manifestHash = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $canonicalLines)))
    Assert-Equal -Label "$($Record.Spec.id) inventory manifest SHA-256" -Actual $manifestHash -Expected ([string]$Inventory.manifestSha256).ToLowerInvariant()

    $expectedManifestPath = Join-Path $root 'manifest.json'
    Assert-Equal -Label "$($Record.Spec.id) collector manifest location" -Actual $Record.Path -Expected $expectedManifestPath
    Assert-True -Condition ($recorded.ContainsKey('manifest.json')) -Message "$($Record.Spec.id) inventory does not cover its collector manifest."

    foreach ($field in @('logcat', 'filteredLogcat', 'summary', 'package', 'localSaveByteHashes', 'syncRecoveryStateByteHashes', 'persistedSteamByteHashes')) {
        $referenced = Resolve-RepoPath -Path ([string]$Record.Data.$field)
        Assert-True -Condition ($referenced.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message "$($Record.Spec.id) $field is outside its evidence root."
        $relative = $referenced.Substring($rootPrefix.Length).Replace('\', '/')
        Assert-True -Condition ($recorded.ContainsKey($relative)) -Message "$($Record.Spec.id) inventory does not cover $field."
    }
}

function Get-CollectorTsvRows {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedHeader,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $lines = @([IO.File]::ReadAllLines($Path, [Text.Encoding]::UTF8))
    Assert-True -Condition ($lines.Count -gt 0) -Message "$Label is empty."
    Assert-Equal -Label "$Label header" -Actual $lines[0].TrimStart([char]0xfeff) -Expected $ExpectedHeader
    return @($lines | Select-Object -Skip 1 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Assert-CollectorStateClaim {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][int64]$Expected
    )

    $property = $Collector.Data.gates.PSObject.Properties[$Name]
    Assert-True -Condition ($null -ne $property) -Message "$($Collector.Spec.id) collector omits derived state claim $Name."
    Assert-True -Condition (
        $property.Value -is [byte] -or $property.Value -is [int16] -or
        $property.Value -is [int32] -or $property.Value -is [int64]
    ) -Message "$($Collector.Spec.id) collector state claim $Name is not an integer."
    Assert-True -Condition ([int64]$property.Value -eq $Expected) -Message "$($Collector.Spec.id) collector state claim $Name disagrees with the inventoried files."
}

function Get-CollectorStateSignals {
    param([Parameter(Mandatory = $true)]$Record)

    $root = Resolve-RepoPath -Path ([string]$Record.Data.output)
    $rootPrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $localRows = @(Get-CollectorTsvRows `
        -Path (Resolve-RepoPath -Path ([string]$Record.Data.localSaveByteHashes)) `
        -ExpectedHeader "sha256`tsizeBytes`tdevicePath" `
        -Label "$($Record.Spec.id) local-save hash table")
    $stateRows = @(Get-CollectorTsvRows `
        -Path (Resolve-RepoPath -Path ([string]$Record.Data.syncRecoveryStateByteHashes)) `
        -ExpectedHeader "sha256`tsizeBytes`tdevicePath" `
        -Label "$($Record.Spec.id) sync/recovery-state hash table")
    $persistedRows = @(Get-CollectorTsvRows `
        -Path (Resolve-RepoPath -Path ([string]$Record.Data.persistedSteamByteHashes)) `
        -ExpectedHeader "documentPath`tmanifestRole`tsavePath`texists`thashKind`tsha256" `
        -Label "$($Record.Spec.id) persisted-Steam hash table")

    foreach ($row in $localRows) {
        Assert-True -Condition ($row -match '^[0-9a-fA-F]{64}\t[0-9]+\tfiles/') -Message "$($Record.Spec.id) local-save hash row is invalid."
    }
    $stateFiles = [Collections.Generic.Dictionary[string, object]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($row in $stateRows) {
        Assert-True -Condition ($row -match '^[0-9a-fA-F]{64}\t[0-9]+\tfiles/\.sts2-launcher/') -Message "$($Record.Spec.id) sync/recovery-state hash row is invalid."
        $parts = $row -split "`t", 3
        $stateSha256 = $parts[0].ToLowerInvariant()
        $stateSizeBytes = [int64]$parts[1]
        $statePath = $parts[2]
        Assert-True -Condition (-not $stateFiles.ContainsKey($statePath)) -Message "$($Record.Spec.id) sync/recovery-state hash table repeats $statePath."
        $stateFiles.Add($statePath, [pscustomobject][ordered]@{
            sha256 = $stateSha256
            sizeBytes = $stateSizeBytes
        })
    }

    $byteHashCount = 0
    $legacyHashCount = 0
    foreach ($row in $persistedRows) {
        $parts = $row -split "`t", 6
        Assert-True -Condition ($parts.Count -eq 6) -Message "$($Record.Spec.id) persisted-Steam hash row is invalid."
        if ($parts[4] -eq 'byte-sha256') { $byteHashCount++ }
        elseif ($parts[4] -eq 'legacy-text-sha256') { $legacyHashCount++ }
        elseif ($parts[4] -ne 'missing') { throw "$($Record.Spec.id) persisted-Steam hash row has unknown hash kind '$($parts[4])'." }
    }

    $stateIndexPath = Join-Path $root 'sync-state-index.json'
    Assert-True -Condition (Test-Path -LiteralPath $stateIndexPath -PathType Leaf) -Message "$($Record.Spec.id) inventoried sync-state index is missing."
    $stateIndexDocument = Read-JsonFile -Path $stateIndexPath -Label "$($Record.Spec.id) sync-state index"
    $stateIndex = @($stateIndexDocument | Where-Object { $null -ne $_ })
    $eligibleStatePaths = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($statePath in $stateFiles.Keys) {
        if ($statePath -match '^files/\.sts2-launcher/(automatic-sync|recovery)/' -and
            -not $statePath.Contains('..') -and
            $statePath.EndsWith('.json', [StringComparison]::OrdinalIgnoreCase)) {
            [void]$eligibleStatePaths.Add($statePath)
        }
    }

    $indexedStatePaths = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::Ordinal
    )
    $capturedRelativePaths = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    $indexedDocuments = [Collections.Generic.Dictionary[string, object]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($entry in $stateIndex) {
        Assert-ExactProperties -Object $entry -Names @(
            'devicePath', 'capturedFile', 'deviceSha256', 'deviceSizeBytes',
            'capturedSha256', 'capturedSizeBytes', 'byteIdentityVerified',
            'parsed', 'error'
        ) -Label "$($Record.Spec.id) sync-state index entry"

        $devicePath = [string]$entry.devicePath
        Assert-True -Condition ($indexedStatePaths.Add($devicePath)) -Message "$($Record.Spec.id) sync-state index repeats $devicePath."
        Assert-True -Condition ($eligibleStatePaths.Contains($devicePath)) -Message "$($Record.Spec.id) sync-state index references an un-inventoried or ineligible device file: $devicePath."
        $deviceInventory = $stateFiles[$devicePath]

        $deviceSha256 = ([string]$entry.deviceSha256).ToLowerInvariant()
        Assert-True -Condition ($deviceSha256 -match '^[0-9a-f]{64}$') -Message "$($Record.Spec.id) sync-state index has an invalid device SHA-256 for $devicePath."
        Assert-True -Condition (Test-IsJsonInteger $entry.deviceSizeBytes) -Message "$($Record.Spec.id) sync-state index has a non-integer device size for $devicePath."
        Assert-Equal -Label "$($Record.Spec.id) indexed device SHA-256 for $devicePath" -Actual $deviceSha256 -Expected ([string]$deviceInventory.sha256)
        Assert-True -Condition ([int64]$entry.deviceSizeBytes -eq [int64]$deviceInventory.sizeBytes) -Message "$($Record.Spec.id) indexed device size for $devicePath disagrees with the device hash table."

        Assert-True -Condition ($entry.byteIdentityVerified -is [bool]) -Message "$($Record.Spec.id) sync-state byteIdentityVerified is not boolean for $devicePath."
        Assert-True -Condition ($entry.parsed -is [bool]) -Message "$($Record.Spec.id) sync-state parsed is not boolean for $devicePath."
        Assert-True -Condition (Test-IsJsonInteger $entry.capturedSizeBytes) -Message "$($Record.Spec.id) sync-state captured size is not an integer for $devicePath."
        $capturedSha256 = ([string]$entry.capturedSha256).ToLowerInvariant()
        Assert-True -Condition ($capturedSha256 -match '^[0-9a-f]{64}$') -Message "$($Record.Spec.id) sync-state capture has no valid SHA-256 for $devicePath."
        Assert-True -Condition ([bool]$entry.byteIdentityVerified) -Message "$($Record.Spec.id) sync-state capture was not byte-verified for ${devicePath}: $([string]$entry.error)"
        Assert-True -Condition ([bool]$entry.parsed) -Message "$($Record.Spec.id) sync-state capture was not parsed for ${devicePath}: $([string]$entry.error)"
        Assert-True -Condition ([string]::IsNullOrEmpty([string]$entry.error)) -Message "$($Record.Spec.id) sync-state capture reports an error for $devicePath."

        $capturedRelative = Normalize-SavePath -Path ([string]$entry.capturedFile)
        Assert-True -Condition ($capturedRelativePaths.Add($capturedRelative)) -Message "$($Record.Spec.id) sync-state index reuses captured file $capturedRelative."
        $capturedPath = [IO.Path]::GetFullPath((Join-Path $root $capturedRelative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
        Assert-True -Condition ($capturedPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message "$($Record.Spec.id) sync-state capture escaped its evidence root."
        Assert-True -Condition (Test-Path -LiteralPath $capturedPath -PathType Leaf) -Message "$($Record.Spec.id) sync-state captured document is missing for $devicePath."

        $capturedBytes = [IO.File]::ReadAllBytes($capturedPath)
        $actualCapturedSha256 = Get-Sha256Hex -Bytes $capturedBytes
        $actualCapturedSizeBytes = [int64]$capturedBytes.LongLength
        Assert-Equal -Label "$($Record.Spec.id) indexed captured SHA-256 for $devicePath" -Actual $capturedSha256 -Expected $actualCapturedSha256
        Assert-True -Condition ([int64]$entry.capturedSizeBytes -eq $actualCapturedSizeBytes) -Message "$($Record.Spec.id) indexed captured size for $devicePath disagrees with the exact captured file."
        Assert-Equal -Label "$($Record.Spec.id) captured bytes versus device inventory for $devicePath" -Actual $actualCapturedSha256 -Expected ([string]$deviceInventory.sha256)
        Assert-True -Condition ($actualCapturedSizeBytes -eq [int64]$deviceInventory.sizeBytes) -Message "$($Record.Spec.id) captured byte size for $devicePath disagrees with the device hash table."

        $document = Read-StrictUtf8JsonBytes -Bytes $capturedBytes -Label "$($Record.Spec.id) sync-state captured document $devicePath"
        $indexedDocuments.Add($devicePath, $document)
    }
    foreach ($eligibleStatePath in $eligibleStatePaths) {
        Assert-True -Condition ($indexedStatePaths.Contains($eligibleStatePath)) -Message "$($Record.Spec.id) eligible sync/recovery state file has no exact captured index entry: $eligibleStatePath."
    }
    Assert-True -Condition ($indexedStatePaths.Count -eq $eligibleStatePaths.Count) -Message "$($Record.Spec.id) sync-state index does not exactly cover eligible device JSON files."

    $pendingStatePaths = @($stateFiles.Keys | Where-Object { $_ -match '/pending-sync\.json$' })
    $journalStatePaths = @($stateFiles.Keys | Where-Object { $_ -match '/last-restore\.json$' })
    $pendingDocuments = @($indexedDocuments.Keys | Where-Object { $_ -match '/pending-sync\.json$' })
    Assert-True -Condition ($pendingDocuments.Count -eq $pendingStatePaths.Count) -Message "$($Record.Spec.id) pending-sync state index disagrees with the inventoried state hashes."
    $pendingPhases = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($devicePath in $pendingDocuments) {
        $document = $indexedDocuments[$devicePath]
        $phase = [string]$document.Phase
        Assert-True -Condition ($phase -in @('before-game', 'game-running', 'uploading', 'downloading')) -Message "$($Record.Spec.id) pending-sync captured document has invalid phase '$phase'."
        [void]$pendingPhases.Add($phase)
    }

    $signals = [pscustomobject][ordered]@{
        localSaveByteHashCount = $localRows.Count
        syncRecoveryStateFileHashCount = $stateRows.Count
        persistedSteamByteHashCount = $byteHashCount
        persistedSteamLegacyTextHashCount = $legacyHashCount
        pendingSyncDocumentCount = $pendingStatePaths.Count
        pendingSyncPhases = @($pendingPhases | Sort-Object)
        recoveryJournalCount = $journalStatePaths.Count
    }
    foreach ($name in @(
        'localSaveByteHashCount', 'syncRecoveryStateFileHashCount',
        'persistedSteamByteHashCount', 'persistedSteamLegacyTextHashCount',
        'pendingSyncDocumentCount', 'recoveryJournalCount'
    )) {
        Assert-CollectorStateClaim -Collector $Record -Name $name -Expected ([int64]$signals.$name)
    }
    $claimedPhaseProperty = $Record.Data.gates.PSObject.Properties['pendingSyncPhases']
    Assert-True -Condition ($null -ne $claimedPhaseProperty) -Message "$($Record.Spec.id) collector omits derived state claim pendingSyncPhases."
    $rawClaimedPhases = @($claimedPhaseProperty.Value | ForEach-Object { [string]$_ })
    $claimedPhases = @($rawClaimedPhases | Sort-Object -Unique)
    Assert-True -Condition ($rawClaimedPhases.Count -eq $claimedPhases.Count) -Message "$($Record.Spec.id) collector repeats a pending-sync phase claim."
    Assert-Equal -Label "$($Record.Spec.id) collector pending-sync phases" -Actual ($claimedPhases -join "`n") -Expected (@($signals.pendingSyncPhases) -join "`n")
    return $signals
}

function Validate-Collector {
    param([Parameter(Mandatory = $true)]$Record)

    $data = $Record.Data
    $spec = $Record.Spec
    Assert-True -Condition ([int]$data.schemaVersion -eq 2) -Message "$($spec.id) collector schemaVersion must be 2."
    Assert-Equal -Label "$($spec.id) kind" -Actual ([string]$data.kind) -Expected 'stage5-android-save-validation-capture'
    Assert-True -Condition ([int]$spec.row -ge 1 -and [int]$spec.row -le 10) -Message "$($spec.id) has an invalid matrix row."
    Assert-True -Condition ([string]$spec.phase -match '^[a-z0-9][a-z0-9-]{0,63}$') -Message "$($spec.id) has an invalid matrix phase."

    foreach ($scope in @($data.captureBinding, $data.gates)) {
        Assert-Equal -Label "$($spec.id) source commit" -Actual ([string]$scope.sourceCommit) -Expected ([string]$script:Binding.candidate.sourceCommit)
        Assert-True -Condition (-not [bool]$scope.sourceWorktreeDirty) -Message "$($spec.id) was captured from a dirty source worktree."
        Assert-Equal -Label "$($spec.id) candidate APK hash" -Actual ([string]$scope.candidateApkSha256).ToLowerInvariant() -Expected ([string]$script:Binding.candidate.apkSha256)
        Assert-Equal -Label "$($spec.id) installed APK hash" -Actual ([string]$scope.installedApkSha256).ToLowerInvariant() -Expected ([string]$script:Binding.candidate.apkSha256)
        Assert-True -Condition ([bool]$scope.candidateMatchesInstalled) -Message "$($spec.id) candidate did not match the installed base APK."
        Assert-Equal -Label "$($spec.id) Stage 5 row" -Actual ([string]$scope.stage5Row) -Expected ([string]$spec.row)
        Assert-Equal -Label "$($spec.id) evidence phase" -Actual ([string]$scope.evidencePhase) -Expected ([string]$spec.phase)
    }
    Assert-Equal -Label "$($spec.id) package" -Actual ([string]$data.captureBinding.packageName) -Expected ([string]$script:Binding.candidate.packageName)
    Assert-True -Condition ([bool]$data.gates.authorizedDevice) -Message "$($spec.id) device was not authorized."
    Assert-True -Condition (-not [bool]$data.clearedLogcatAfterPreservingBuffer) -Message "$($spec.id) cleared logcat during Stage 5 evidence collection."
    Assert-True -Condition ([string]$data.logcatSince -match '^\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}$') -Message "$($spec.id) is not scenario-windowed with a valid LogcatSince marker."
    Assert-Equal -Label "$($spec.id) logcatSince gate" -Actual ([string]$data.gates.logcatSince) -Expected ([string]$data.logcatSince)

    foreach ($field in @('serialSha256', 'manufacturer', 'model', 'androidApi', 'abiList', 'buildFingerprint')) {
        Assert-Equal -Label "$($spec.id) device $field" -Actual ([string]$data.device.$field) -Expected ([string]$script:Binding.device.$field)
    }
    Assert-Equal -Label "$($spec.id) recomputed serial hash" -Actual (Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes([string]$data.device.serial))) -Expected ([string]$script:Binding.device.serialSha256)

    $candidatePath = Resolve-RepoPath -Path ([string]$data.captureBinding.candidateApkPath)
    Assert-True -Condition (Test-Path -LiteralPath $candidatePath -PathType Leaf) -Message "$($spec.id) exact candidate APK is unavailable for hash review."
    Assert-Equal -Label "$($spec.id) exact candidate APK path" -Actual $candidatePath -Expected $script:CandidateApkPath
    Assert-Equal -Label "$($spec.id) exact candidate file hash" -Actual (Get-FileSha256Hex -Path $candidatePath) -Expected ([string]$script:Binding.candidate.apkSha256)

    $inventoryPath = Resolve-MatrixPath -Path ([string]$spec.inventory.path)
    Assert-True -Condition (Test-Path -LiteralPath $inventoryPath -PathType Leaf) -Message "$($spec.id) collector inventory is missing."
    $inventoryHash = ([string]$spec.inventory.sha256).ToLowerInvariant()
    Assert-True -Condition ($inventoryHash -match '^[0-9a-f]{64}$') -Message "$($spec.id) collector inventory hash is invalid."
    Assert-Equal -Label "$($spec.id) collector inventory SHA-256" -Actual (Get-FileSha256Hex -Path $inventoryPath) -Expected $inventoryHash
    Assert-Equal -Label "$($spec.id) collector inventory path" -Actual (Resolve-RepoPath -Path ([string]$data.evidenceInventory)) -Expected $inventoryPath
    $inventory = Read-JsonFile -Path $inventoryPath -Label "$($spec.id) collector inventory"
    Validate-CollectorInventory -Record $Record -InventoryPath $inventoryPath -Inventory $inventory

    $logPath = Resolve-RepoPath -Path ([string]$data.logcat)
    $focusedLogPath = Resolve-RepoPath -Path ([string]$data.filteredLogcat)
    $Record.LogText = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($logPath))
    $Record.FocusedLogText = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($focusedLogPath))
    $rawLineSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($line in @($Record.LogText -split '\r?\n')) {
        if (-not [string]::IsNullOrWhiteSpace($line)) { [void]$rawLineSet.Add($line.TrimStart([char]0xfeff)) }
    }
    foreach ($line in @($Record.FocusedLogText -split '\r?\n')) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        Assert-True -Condition ($rawLineSet.Contains($line.TrimStart([char]0xfeff))) -Message "$($spec.id) focused log contains a line absent from its inventoried raw log."
    }

    $Record.LogSignals = Get-CollectorLogSignals `
        -Text $Record.LogText `
        -PackageName ([string]$script:Binding.candidate.packageName)
    foreach ($name in @(
        'automaticSyncPendingLogSeen', 'automaticSyncVerifiedLogSeen',
        'automaticSyncConflictLogSeen', 'syncedLogSeen', 'readBackMismatchSeen',
        'commitFailureSeen', 'saveContextMismatchSeen', 'modSetMismatchSeen',
        'branchMismatchSeen', 'recoveryLogSeen', 'recoveryRestoreLogCount',
        'recoveryUndoLogCount', 'localSaveBaseSeen', 'localSaveWrites',
        'localSaveReads', 'localSaveExistsChecks', 'localOnlySaveManagerSeen',
        'steamGameplaySaveManagerSeen', 'fatalExceptionSeen', 'anrSeen',
        'droppedSaveWriteSeen', 'swallowedFailureSeen', 'localWriteExceptionCount'
    )) {
        Assert-CollectorLogClaim -Collector $Record -Name $name -Expected $Record.LogSignals.$name
    }
    $Record.StateSignals = Get-CollectorStateSignals -Record $Record

    if ([bool]$data.gates.runAsAvailable) {
        Assert-True -Condition ([int]$Record.StateSignals.localSaveByteHashCount -gt 0) -Message "$($spec.id) advertised run-as but the inventoried evidence has no Android-local save hashes."
    } else {
        Assert-True -Condition ([int]$Record.StateSignals.localSaveByteHashCount -eq 0) -Message "$($spec.id) has local hashes despite run-as being unavailable."
    }
    Assert-True -Condition (-not [bool]$Record.LogSignals.steamGameplaySaveManagerSeen) -Message "$($spec.id) raw log observed a Steam-backed gameplay SaveManager."
    Assert-True -Condition (-not [bool]$Record.LogSignals.fatalExceptionSeen) -Message "$($spec.id) raw log contains a fatal exception."
    Assert-True -Condition (-not [bool]$Record.LogSignals.anrSeen) -Message "$($spec.id) raw log contains an ANR."
    Assert-True -Condition (-not [bool]$Record.LogSignals.droppedSaveWriteSeen) -Message "$($spec.id) raw log indicates a dropped save write."
    Assert-True -Condition (-not [bool]$Record.LogSignals.swallowedFailureSeen) -Message "$($spec.id) raw log indicates a swallowed failure."
    Assert-True -Condition ([int]$Record.LogSignals.localWriteExceptionCount -eq 0) -Message "$($spec.id) raw log contains a local-save write exception or failure."
}

$script:AndroidVerifier = $androidVerifier
$script:SteamManifestCapture = $steamManifestCapture
$script:Binding = $binding
$script:CandidateApkPath = $candidateApkPath
$seenInventoryPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($record in $evidence.Values) {
    switch ([string]$record.Spec.kind) {
        'android-manifest' { Validate-AndroidManifest -Record $record }
        'steam-manifest' { Validate-SteamManifest -Record $record }
        'collector' {
            $inventoryPath = Resolve-MatrixPath -Path ([string]$record.Spec.inventory.path)
            Assert-True -Condition ($seenInventoryPaths.Add($inventoryPath)) -Message "Collector inventory path is reused: $inventoryPath"
            Validate-Collector -Record $record
        }
    }
}

function Convert-CollectorExportCompletionBindings {
    param([Parameter(Mandatory = $true)]$Collector)

    $marker = 'STS2_SAVE_EXPORT_COMPLETE '
    $bindings = [Collections.Generic.List[object]]::new()
    $seenExportIds = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($entry in @($Collector.LogSignals.appEntries)) {
        $markerIndex = $entry.Message.IndexOf(
            $marker,
            [StringComparison]::Ordinal
        )
        if ($markerIndex -lt 0) { continue }
        Assert-True `
            -Condition ($entry.Message.StartsWith(
                '[Recovery] ' + $marker,
                [StringComparison]::Ordinal
            )) `
            -Message "$($Collector.Spec.id) save-export marker is not an exact STS2Mobile recovery event."
        $payloadText = $entry.Message.Substring(
            $markerIndex + $marker.Length
        ).Trim()
        try {
            $payload = $payloadText | ConvertFrom-Json -ErrorAction Stop
        } catch {
            throw "$($Collector.Spec.id) contains an invalid structured save-export completion line."
        }
        Assert-ExactProperties `
            -Object $payload `
            -Names @(
                'Event', 'Version', 'ExportId', 'BundleSha256',
                'CurrentAndroidTreeSha256', 'SelectedSaveContextSha256'
            ) `
            -Label "$($Collector.Spec.id) save-export completion"
        Assert-Equal `
            -Label "$($Collector.Spec.id) save-export completion event" `
            -Actual ([string]$payload.Event) `
            -Expected 'save-recovery-export-complete'
        Assert-True `
            -Condition (Test-IsJsonInteger $payload.Version) `
            -Message "$($Collector.Spec.id) save-export completion version must be a JSON integer."
        Assert-True `
            -Condition ([int64]$payload.Version -eq 1) `
            -Message "$($Collector.Spec.id) save-export completion version must be 1."
        $exportId = [string]$payload.ExportId
        Assert-True `
            -Condition ($exportId -match '^[0-9a-f]{32}$') `
            -Message "$($Collector.Spec.id) save-export completion has an invalid ExportId."
        Assert-True `
            -Condition ($seenExportIds.Add($exportId)) `
            -Message "$($Collector.Spec.id) repeats save-export completion ExportId $exportId."
        foreach ($field in @(
            'BundleSha256', 'CurrentAndroidTreeSha256',
            'SelectedSaveContextSha256'
        )) {
            Assert-True `
                -Condition ([string]$payload.$field -match '^[0-9a-f]{64}$') `
                -Message "$($Collector.Spec.id) save-export completion has an invalid $field."
        }
        $bindings.Add([pscustomobject]@{
            ExportId = $exportId
            BundleSha256 = [string]$payload.BundleSha256
            CurrentAndroidTreeSha256 =
                [string]$payload.CurrentAndroidTreeSha256
            SelectedSaveContextSha256 =
                [string]$payload.SelectedSaveContextSha256
            Index = [int]$entry.Index
            CollectorId = [string]$Collector.Spec.id
        })
    }
    return @($bindings)
}

function Get-ExportBindingSignature {
    param([Parameter(Mandatory = $true)]$Binding)

    return @(
        [string]$Binding.ExportId,
        [string]$Binding.BundleSha256,
        [string]$Binding.CurrentAndroidTreeSha256,
        [string]$Binding.SelectedSaveContextSha256
    ) -join "`t"
}

function Get-ExpectedContextSha256 {
    param([Parameter(Mandatory = $true)]$Context)

    $text =
        "$([string]$Context.steamId64)`0$(([string]$Context.saveNamespace).ToLowerInvariant())`0$([string]$Context.runtimeIdentity)`0$([string]$Context.modSetFingerprint)`0"
    return Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes($text))
}

function Get-RequiredAutomaticTerminal {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Operation,
        [Parameter(Mandatory = $true)][string]$Outcome,
        [Parameter(Mandatory = $true)][string]$Detail,
        [Parameter(Mandatory = $true)][bool]$RemoteVerified,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $contextSha256 = Get-ExpectedContextSha256 -Context $Context
    $matches = @($Collector.LogSignals.structuredEvents | Where-Object {
        $_.Event -eq 'automatic-sync-terminal' -and
        $_.Payload.Operation -eq $Operation -and
        $_.Payload.Outcome -eq $Outcome -and
        $_.Payload.Detail -eq $Detail -and
        $_.Payload.ContextSha256 -eq $contextSha256 -and
        [bool]$_.Payload.RemoteVerified -eq $RemoteVerified
    })
    Assert-True -Condition ($matches.Count -eq 1) -Message "$Label requires exactly one context-bound automatic terminal $Operation/$Outcome/$Detail (RemoteVerified=$RemoteVerified)."
    return $matches[0]
}

function Get-RequiredRecoveryTerminal {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)][ValidateSet('restore', 'undo')][string]$Operation,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $matches = @($Collector.LogSignals.structuredEvents | Where-Object {
        $_.Event -eq 'save-recovery-terminal' -and
        $_.Payload.Operation -eq $Operation -and
        $_.Payload.Outcome -eq 'completed' -and
        $_.Payload.Detail -eq 'byte-verified-local-only'
    })
    Assert-True -Condition ($matches.Count -eq 1) -Message "$Label requires exactly one structured $Operation completion event."
    return $matches[0]
}

function Assert-ExportRelativeToEvent {
    param(
        [Parameter(Mandatory = $true)]$Android,
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Event,
        [Parameter(Mandatory = $true)][ValidateSet('before', 'after')][string]$Position,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $binding = $Android.Data.exportBinding
    $signature = Get-ExportBindingSignature -Binding ([pscustomobject]@{
        ExportId = [string]$binding.exportId
        BundleSha256 = [string]$binding.bundleSha256
        CurrentAndroidTreeSha256 = [string]$binding.currentAndroidTreeSha256
        SelectedSaveContextSha256 = [string]$binding.selectedSaveContextSha256
    })
    $match = @($collectorExportBindings[[string]$Collector.Spec.id] | Where-Object {
        (Get-ExportBindingSignature -Binding $_) -eq $signature
    })
    Assert-True -Condition ($match.Count -eq 1) -Message "$Label export binding is missing from its collector."
    $ordered = if ($Position -eq 'before') {
        [int]$match[0].Index -lt [int]$Event.Index
    } else {
        [int]$match[0].Index -gt [int]$Event.Index
    }
    Assert-True -Condition $ordered -Message "$Label export must occur $Position its exact structured terminal event."
}

$collectorExportBindings = @{}
$allCollectorExportBindings = [Collections.Generic.List[object]]::new()
foreach ($collector in @($evidence.Values | Where-Object {
    [string]$_.Spec.kind -eq 'collector'
})) {
    $bindings = @(
        Convert-CollectorExportCompletionBindings -Collector $collector
    )
    $collectorExportBindings[[string]$collector.Spec.id] = $bindings
    foreach ($binding in $bindings) {
        $allCollectorExportBindings.Add($binding)
    }
}

$seenAndroidExportIds = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::Ordinal
)
$phaseBoundAndroidExportIds = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::Ordinal
)
foreach ($android in @($evidence.Values | Where-Object {
    [string]$_.Spec.kind -eq 'android-manifest'
})) {
    $id = [string]$android.Spec.id
    Assert-True `
        -Condition ($id -match '^r(10|[1-9])-') `
        -Message "$id cannot be bound to a Stage 5 collector row."
    $binding = $android.Data.exportBinding
    $exportId = [string]$binding.exportId
    Assert-True `
        -Condition ($seenAndroidExportIds.Add($exportId)) `
        -Message "Android ExportId $exportId is reused by more than one matrix manifest."
}

function Use-Evidence {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Kind,
        [Parameter(Mandatory = $true)][int]$Row,
        [string]$Phase = ""
    )

    Assert-True -Condition ($script:Evidence.ContainsKey($Id)) -Message "Check references unknown evidence id $Id."
    $record = $script:Evidence[$Id]
    Assert-Equal -Label "$Id evidence kind" -Actual ([string]$record.Spec.kind) -Expected $Kind
    if ($Kind -eq 'collector') {
        Assert-True -Condition ([int]$record.Spec.row -eq $Row) -Message "$Id is bound to Stage 5 row $($record.Spec.row), not row $Row."
        Assert-Equal -Label "$Id evidence phase" -Actual ([string]$record.Spec.phase) -Expected $Phase
    }
    if ($Kind -eq 'android-manifest' -and $Row -ne 10) {
        Assert-True -Condition ([bool]$record.Data.cloudSyncEnabled) -Message "$Id does not show the existing Cloud Sync setting enabled for Stage 5 row $Row."
    }
    [void]$script:UsedEvidence.Add($Id)
    return $record
}

function Assert-AndroidExportBoundToCollector {
    param(
        [Parameter(Mandatory = $true)]$Android,
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $androidId = [string]$Android.Spec.id
    $collectorId = [string]$Collector.Spec.id
    Assert-Equal -Label "$Label Android evidence kind" -Actual ([string]$Android.Spec.kind) -Expected 'android-manifest'
    Assert-Equal -Label "$Label collector evidence kind" -Actual ([string]$Collector.Spec.kind) -Expected 'collector'
    $binding = $Android.Data.exportBinding
    $expectedSignature = Get-ExportBindingSignature -Binding ([pscustomobject]@{
        ExportId = [string]$binding.exportId
        BundleSha256 = [string]$binding.bundleSha256
        CurrentAndroidTreeSha256 = [string]$binding.currentAndroidTreeSha256
        SelectedSaveContextSha256 = [string]$binding.selectedSaveContextSha256
    })
    $matches = @($collectorExportBindings[$collectorId] | Where-Object {
        (Get-ExportBindingSignature -Binding $_) -eq $expectedSignature
    })
    $globalMatches = @($allCollectorExportBindings | Where-Object {
        (Get-ExportBindingSignature -Binding $_) -eq $expectedSignature
    })
    Assert-True `
        -Condition ($globalMatches.Count -eq 1) `
        -Message "$Label Android export $androidId must occur in exactly one collector globally; found $($globalMatches.Count)."
    Assert-True `
        -Condition ($matches.Count -eq 1) `
        -Message "$Label Android export $androidId has no exact ExportId/bundle/tree/context match in the inventoried raw log for collector $collectorId."
    Assert-True `
        -Condition ($phaseBoundAndroidExportIds.Add($androidId)) `
        -Message "$Label binds Android export $androidId more than once."
}

function Use-Context {
    param([Parameter(Mandatory = $true)][string]$Id)
    Assert-True -Condition ($script:Contexts.ContainsKey($Id)) -Message "Check references unknown expected SaveContext $Id."
    return $script:Contexts[$Id]
}

function Assert-EvidenceContext {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Expected
    )
    $actual = if ([string]$Record.Spec.kind -eq 'android-manifest') {
        $Record.Data.context
    } else {
        $Record.Data.selectedContext
    }
    Assert-Context -Label $Record.Spec.id -Actual $actual -Expected $Expected
}

function Assert-SteamCapturedAfter {
    param(
        [Parameter(Mandatory = $true)]$Steam,
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $steamTime = Get-RequiredUtc -Value ([string]$Steam.Data.capturedUtc) -Label "$($Steam.Spec.id) Steam capture time"
    $collectorTime = Get-RequiredUtc -Value ([string]$Collector.Data.capturedUtc) -Label "$($Collector.Spec.id) collector time"
    Assert-True -Condition ($steamTime -gt $collectorTime) -Message "$Label requires an independent live Steam capture later than the collector."
}

function Add-SyncedProof {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android,
        [Parameter(Mandatory = $true)]$Steam,
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Assert-EvidenceContext -Record $Android -Expected $Context
    Assert-EvidenceContext -Record $Steam -Expected $Context
    Assert-MapsEqual `
        -Label "$Label Android/live-Steam byte equality" `
        -Left (Get-AndroidSnapshot -Record $Android).Files `
        -Right (Get-SteamSelectedMap -Record $Steam)
    Assert-SteamCapturedAfter -Steam $Steam -Collector $Collector -Label $Label
    [void]$script:SyncedProofs.Add([string]$Collector.Spec.id)
}

function Assert-SteamMutationIsolated {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After,
        [Parameter(Mandatory = $true)][ValidateSet('vanilla', 'modded')][string]$Namespace,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $beforeFiles = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($Before.Data.files)) { $beforeFiles.Add([string]$file.path, $file) }
    $afterFiles = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($After.Data.files)) { $afterFiles.Add([string]$file.path, $file) }
    Assert-True -Condition ($beforeFiles.Count -eq $afterFiles.Count) -Message "$Label changed the Steam manifest path set."
    $namespaceSaveChanged = $false
    foreach ($path in $beforeFiles.Keys) {
        Assert-True -Condition ($afterFiles.ContainsKey($path)) -Message "$Label removed Steam manifest path $path."
        $beforeFile = $beforeFiles[$path]
        $afterFile = $afterFiles[$path]
        $beforeValue = "$($beforeFile.role)`t$($beforeFile.exists)`t$($beforeFile.sizeBytes)`t$($beforeFile.sha256)"
        $afterValue = "$($afterFile.role)`t$($afterFile.exists)`t$($afterFile.sizeBytes)`t$($afterFile.sha256)"
        if ($beforeValue -eq $afterValue) {
            continue
        }
        $role = [string]$beforeFile.role
        $allowed = $role -eq "$Namespace-save" -or
            $role -eq 'shared-save' -or
            $role -eq "$Namespace-context-marker"
        Assert-True -Condition $allowed -Message "$Label changed out-of-context Steam path $path ($role)."
        if ($role -eq "$Namespace-save") {
            $namespaceSaveChanged = $true
        }
    }
    Assert-True -Condition $namespaceSaveChanged -Message "$Label did not exercise a $Namespace save-file mutation."
}

function Assert-RequiredRefs {
    param(
        [Parameter(Mandatory = $true)]$Refs,
        [Parameter(Mandatory = $true)][string[]]$Names,
        [Parameter(Mandatory = $true)][string]$Label
    )
    Assert-ExactProperties -Object $Refs -Names $Names -Label "$Label refs"
    foreach ($name in $Names) {
        Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$Refs.$name)) -Message "$Label ref $name is empty."
    }
}

$script:Evidence = $evidence
$script:UsedEvidence = $usedEvidence
$script:Contexts = $contexts
$script:SyncedProofs = $syncedProofs

$requiredChecks = [ordered]@{
    '1' = [ordered]@{ 'r1-verified-upload' = 'verified-upload' }
    '2' = [ordered]@{ 'r2-safe-download' = 'safe-download-with-backup' }
    '3' = [ordered]@{ 'r3-divergence-preserved' = 'divergence-preserved' }
    '4' = [ordered]@{ 'r4-modded-namespace-isolation' = 'modded-namespace-isolation' }
    '5' = [ordered]@{ 'r5-changed-mod-set-blocked' = 'changed-mod-set-blocked' }
    '6' = [ordered]@{ 'r6-two-way-branch-isolation' = 'two-way-branch-isolation' }
    '7' = [ordered]@{ 'r7-offline-retry' = 'offline-retry' }
    '8' = [ordered]@{ 'r8-crash-resume' = 'crash-resume' }
    '9' = [ordered]@{
        'r9-commit-failure' = 'commit-failure-no-success'
        'r9-readback-failure' = 'readback-failure-no-success'
    }
    '10' = [ordered]@{ 'r10-restore-undo-exact' = 'restore-undo-exact' }
}

$rows = [Collections.Generic.Dictionary[int, object]]::new()
foreach ($row in @($matrix.rows)) {
    Assert-ExactProperties -Object $row -Names @('row', 'checks') -Label 'Matrix row'
    $number = [int]$row.row
    Assert-True -Condition ($number -ge 1 -and $number -le 10) -Message "Invalid Stage 5 row number $number."
    if ($rows.ContainsKey($number)) {
        throw "Duplicate Stage 5 row $number."
    }
    $rows.Add($number, $row)
}
Assert-True -Condition ($rows.Count -eq 10) -Message 'Stage 5 matrix must contain all ten rows exactly once.'

foreach ($number in 1..10) {
    Assert-True -Condition ($rows.ContainsKey($number)) -Message "Stage 5 matrix is missing row $number."
    $expectedChecks = $requiredChecks[[string]$number]
    $actualChecks = @($rows[$number].checks)
    Assert-True -Condition ($actualChecks.Count -eq $expectedChecks.Count) -Message "Stage 5 row $number has the wrong number of semantic checks."
    $seenChecks = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($check in $actualChecks) {
        Assert-ExactProperties -Object $check -Names @('id', 'type', 'refs') -Label "Stage 5 row $number check"
        $checkId = [string]$check.id
        Assert-True -Condition ($expectedChecks.Contains($checkId)) -Message "Stage 5 row $number has unknown or misplaced semantic check $checkId."
        Assert-Equal -Label "$checkId semantic type" -Actual ([string]$check.type) -Expected ([string]$expectedChecks[$checkId])
        Assert-True -Condition ($seenChecks.Add($checkId)) -Message "Stage 5 row $number repeats semantic check $checkId."
    }
    foreach ($checkId in $expectedChecks.Keys) {
        Assert-True -Condition ($seenChecks.Contains($checkId)) -Message "Stage 5 row $number omits semantic check $checkId."
    }
}

foreach ($number in 1..10) {
    foreach ($check in @($rows[$number].checks)) {
        $refs = $check.refs
        switch ([string]$check.type) {
            'verified-upload' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'steamBefore', 'androidAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $androidAfter = Use-Evidence -Id $refs.androidAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'after-quit-sync'
                Assert-AndroidExportBoundToCollector -Android $androidAfter -Collector $collector -Label 'Row 1 after Quit'
                foreach ($record in @($steamBefore, $androidAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-MapsDiffer -Label 'Row 1 upload' -Left (Get-SteamSelectedMap -Record $steamBefore) -Right (Get-SteamSelectedMap -Record $steamAfter)
                Assert-SteamMutationIsolated -Before $steamBefore -After $steamAfter -Namespace 'vanilla' -Label 'Row 1 upload'
                Assert-SuccessCollector -Collector $collector
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $context -Operation recover -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 1 verified upload'
                Assert-ExportRelativeToEvent -Android $androidAfter -Collector $collector -Event $terminal -Position after -Label 'Row 1 after-sync export'
                Assert-PendingCleared -Collector $collector -Android $androidAfter
                $quit = @($collector.LogSignals.appEntries | Where-Object { $_.Message -ceq 'NGame.Quit completed final local saves; restarting launcher' })
                Assert-True -Condition ($quit.Count -eq 1 -and [int]$terminal.Index -gt [int]$quit[0].Index) -Message 'Row 1 did not prove an STS2Mobile Quit signal followed by launcher-owned post-game reconciliation.'
                Assert-CollectorAndroidBytes -Collector $collector -Android $androidAfter -Label 'Row 1 Android bytes'
                Add-SyncedProof -Collector $collector -Android $androidAfter -Steam $steamAfter -Context $context -Label 'Row 1 verified upload'
            }
            'safe-download-with-backup' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'androidBefore', 'steamBefore', 'androidAfter', 'destinationBackupTreeSha256', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $androidBefore = Use-Evidence -Id $refs.androidBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $androidAfter = Use-Evidence -Id $refs.androidAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'before-play-reconcile'
                foreach ($android in @($androidBefore, $androidAfter)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $collector -Label 'Row 2 before Play'
                }
                foreach ($record in @($androidBefore, $steamBefore, $androidAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                $beforeSnapshot = Get-AndroidSnapshot -Record $androidBefore
                $afterSnapshot = Get-AndroidSnapshot -Record $androidAfter
                $beforeTreeHash = [string]$androidBefore.Data.snapshot.treeSha256
                Assert-Equal -Label 'Row 2 declared destination-backup tree' -Actual ([string]$refs.destinationBackupTreeSha256).ToLowerInvariant() -Expected $beforeTreeHash
                $backupSnapshot = Get-AndroidSnapshot -Record $androidAfter -RecoveryTreeSha256 $beforeTreeHash
                Assert-Context -Label 'Row 2 destination backup context' -Actual $backupSnapshot.Context -Expected $context
                Assert-MapsEqual -Label 'Row 2 destination backup bytes' -Left $beforeSnapshot.Files -Right $backupSnapshot.Files
                Assert-MapsDiffer -Label 'Row 2 independent remote change' -Left $beforeSnapshot.Files -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 2 downloaded Android bytes' -Left $afterSnapshot.Files -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 2 Steam remained unchanged' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfter)
                Assert-SuccessCollector -Collector $collector
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $context -Operation reconcile -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 2 safe download'
                Assert-ExportRelativeToEvent -Android $androidBefore -Collector $collector -Event $terminal -Position before -Label 'Row 2 before-download export'
                Assert-ExportRelativeToEvent -Android $androidAfter -Collector $collector -Event $terminal -Position after -Label 'Row 2 after-download export'
                Assert-PendingCleared -Collector $collector -Android $androidAfter
                Assert-CollectorAndroidBytes -Collector $collector -Android $androidAfter -Label 'Row 2 Android bytes'
                Add-SyncedProof -Collector $collector -Android $androidAfter -Steam $steamAfter -Context $context -Label 'Row 2 safe download'
            }
            'divergence-preserved' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'baselineAndroid', 'baselineSteam', 'localBefore', 'remoteBefore', 'localAfter', 'remoteAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $baselineAndroid = Use-Evidence -Id $refs.baselineAndroid -Kind 'android-manifest' -Row $number
                $baselineSteam = Use-Evidence -Id $refs.baselineSteam -Kind 'steam-manifest' -Row $number
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $remoteBefore = Use-Evidence -Id $refs.remoteBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $remoteAfter = Use-Evidence -Id $refs.remoteAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'divergence-conflict'
                foreach ($android in @($baselineAndroid, $localBefore, $localAfter)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $collector -Label 'Row 3 divergence'
                }
                foreach ($record in @($baselineAndroid, $baselineSteam, $localBefore, $remoteBefore, $localAfter, $remoteAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                $baseline = (Get-AndroidSnapshot -Record $baselineAndroid).Files
                $local = (Get-AndroidSnapshot -Record $localBefore).Files
                $remote = Get-SteamSelectedMap -Record $remoteBefore
                Assert-MapsEqual -Label 'Row 3 baseline equality' -Left $baseline -Right (Get-SteamSelectedMap -Record $baselineSteam)
                Assert-MapsDiffer -Label 'Row 3 local change' -Left $baseline -Right $local
                Assert-MapsDiffer -Label 'Row 3 remote change' -Left $baseline -Right $remote
                Assert-MapsDiffer -Label 'Row 3 divergent changes' -Left $local -Right $remote
                Assert-MapsEqual -Label 'Row 3 local preserved' -Left $local -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-MapsEqual -Label 'Row 3 remote preserved' -Left (Get-SteamFullMap -Record $remoteBefore) -Right (Get-SteamFullMap -Record $remoteAfter)
                Assert-PendingPresent -Collector $collector -Android $localAfter
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $context -Operation reconcile -Outcome conflict -Detail local-and-remote-diverged -RemoteVerified $false -Label 'Row 3 divergence'
                Assert-ExportRelativeToEvent -Android $localBefore -Collector $collector -Event $terminal -Position before -Label 'Row 3 before-conflict export'
                Assert-ExportRelativeToEvent -Android $localAfter -Collector $collector -Event $terminal -Position after -Label 'Row 3 after-conflict export'
                Assert-True -Condition ([bool]$collector.LogSignals.automaticSyncConflictLogSeen) -Message 'Row 3 did not report a synchronization conflict in the inventoried raw log.'
                Assert-NoSynced -Collector $collector
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 3 Android bytes'
            }
            'modded-namespace-isolation' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'steamBefore', 'androidAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $androidAfter = Use-Evidence -Id $refs.androidAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'after-modded-sync'
                Assert-AndroidExportBoundToCollector -Android $androidAfter -Collector $collector -Label 'Row 4 modded sync'
                foreach ($record in @($steamBefore, $androidAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-MapsDiffer -Label 'Row 4 modded upload' -Left (Get-SteamSelectedMap -Record $steamBefore) -Right (Get-SteamSelectedMap -Record $steamAfter)
                Assert-SteamMutationIsolated -Before $steamBefore -After $steamAfter -Namespace 'modded' -Label 'Row 4 exact mod-set upload'
                Assert-SuccessCollector -Collector $collector
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $context -Operation recover -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 4 modded sync'
                Assert-ExportRelativeToEvent -Android $androidAfter -Collector $collector -Event $terminal -Position after -Label 'Row 4 after-sync export'
                Assert-PendingCleared -Collector $collector -Android $androidAfter
                Assert-CollectorAndroidBytes -Collector $collector -Android $androidAfter -Label 'Row 4 Android bytes'
                Add-SyncedProof -Collector $collector -Android $androidAfter -Steam $steamAfter -Context $context -Label 'Row 4 modded namespace sync'
            }
            'changed-mod-set-blocked' {
                Assert-RequiredRefs -Refs $refs -Names @('localContext', 'remoteContext', 'localBefore', 'steamBefore', 'localAfter', 'steamAfter', 'collector') -Label $check.id
                $localContext = Use-Context -Id $refs.localContext
                $remoteContext = Use-Context -Id $refs.remoteContext
                Assert-True -Condition ([string]$localContext.modSetFingerprint -ne [string]$remoteContext.modSetFingerprint) -Message 'Row 5 contexts do not exercise a changed mod set.'
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'changed-mod-set-blocked'
                foreach ($android in @($localBefore, $localAfter)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $collector -Label 'Row 5 changed mod set'
                }
                foreach ($record in @($localBefore, $localAfter)) { Assert-EvidenceContext -Record $record -Expected $localContext }
                foreach ($record in @($steamBefore, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $remoteContext }
                Assert-MapsEqual -Label 'Row 5 local bytes preserved' -Left (Get-AndroidSnapshot -Record $localBefore).Files -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-MapsEqual -Label 'Row 5 Steam bytes preserved' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfter)
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $localContext -Operation reconcile -Outcome conflict -Detail mod-set-mismatch -RemoteVerified $false -Label 'Row 5 changed mod set'
                Assert-ExportRelativeToEvent -Android $localBefore -Collector $collector -Event $terminal -Position before -Label 'Row 5 before-conflict export'
                Assert-ExportRelativeToEvent -Android $localAfter -Collector $collector -Event $terminal -Position after -Label 'Row 5 after-conflict export'
                Assert-True -Condition ([bool]$collector.LogSignals.saveContextMismatchSeen) -Message 'Row 5 has no SaveContext mismatch in its inventoried raw log.'
                Assert-True -Condition ([bool]$collector.LogSignals.modSetMismatchSeen) -Message 'Row 5 has no changed-mod-set mismatch in its inventoried raw log.'
                Assert-NoSynced -Collector $collector
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 5 Android bytes'
            }
            'two-way-branch-isolation' {
                Assert-RequiredRefs -Refs $refs -Names @('publicContext', 'betaContext', 'publicBefore', 'betaBefore', 'betaAfter', 'betaSteamAfter', 'publicAfter', 'publicSteamAfter', 'betaCollector', 'publicCollector') -Label $check.id
                $publicContext = Use-Context -Id $refs.publicContext
                $betaContext = Use-Context -Id $refs.betaContext
                Assert-True -Condition ([string]$publicContext.runtimeIdentity -ne [string]$betaContext.runtimeIdentity) -Message 'Row 6 contexts do not exercise two branches.'
                $publicBefore = Use-Evidence -Id $refs.publicBefore -Kind 'android-manifest' -Row $number
                $betaBefore = Use-Evidence -Id $refs.betaBefore -Kind 'android-manifest' -Row $number
                $betaAfter = Use-Evidence -Id $refs.betaAfter -Kind 'android-manifest' -Row $number
                $betaSteamAfter = Use-Evidence -Id $refs.betaSteamAfter -Kind 'steam-manifest' -Row $number
                $publicAfter = Use-Evidence -Id $refs.publicAfter -Kind 'android-manifest' -Row $number
                $publicSteamAfter = Use-Evidence -Id $refs.publicSteamAfter -Kind 'steam-manifest' -Row $number
                $betaCollector = Use-Evidence -Id $refs.betaCollector -Kind 'collector' -Row $number -Phase 'after-switch-to-beta'
                $publicCollector = Use-Evidence -Id $refs.publicCollector -Kind 'collector' -Row $number -Phase 'after-switch-to-public'
                foreach ($android in @($publicBefore, $betaBefore, $betaAfter)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $betaCollector -Label 'Row 6 switch to beta'
                }
                Assert-AndroidExportBoundToCollector -Android $publicAfter -Collector $publicCollector -Label 'Row 6 switch to public'
                foreach ($record in @($publicBefore, $publicAfter, $publicSteamAfter)) { Assert-EvidenceContext -Record $record -Expected $publicContext }
                foreach ($record in @($betaBefore, $betaAfter, $betaSteamAfter)) { Assert-EvidenceContext -Record $record -Expected $betaContext }
                $publicBytes = (Get-AndroidSnapshot -Record $publicBefore).Files
                $betaBytes = (Get-AndroidSnapshot -Record $betaBefore).Files
                Assert-MapsDiffer -Label 'Row 6 public/beta fixtures' -Left $publicBytes -Right $betaBytes
                Assert-MapsEqual -Label 'Row 6 beta branch restored in beta direction' -Left $betaBytes -Right (Get-AndroidSnapshot -Record $betaAfter).Files
                Assert-MapsEqual -Label 'Row 6 public branch restored in public direction' -Left $publicBytes -Right (Get-AndroidSnapshot -Record $publicAfter).Files
                Assert-SuccessCollector -Collector $betaCollector
                Assert-SuccessCollector -Collector $publicCollector
                $betaTerminal = Get-RequiredAutomaticTerminal -Collector $betaCollector -Context $betaContext -Operation reconcile -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 6 beta reconciliation'
                Assert-ExportRelativeToEvent -Android $publicBefore -Collector $betaCollector -Event $betaTerminal -Position before -Label 'Row 6 public-before export'
                Assert-ExportRelativeToEvent -Android $betaBefore -Collector $betaCollector -Event $betaTerminal -Position before -Label 'Row 6 beta-before export'
                Assert-ExportRelativeToEvent -Android $betaAfter -Collector $betaCollector -Event $betaTerminal -Position after -Label 'Row 6 beta-after export'
                $publicTerminal = Get-RequiredAutomaticTerminal -Collector $publicCollector -Context $publicContext -Operation reconcile -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 6 public reconciliation'
                Assert-ExportRelativeToEvent -Android $publicAfter -Collector $publicCollector -Event $publicTerminal -Position after -Label 'Row 6 public-after export'
                Assert-PendingCleared -Collector $betaCollector -Android $betaAfter
                Assert-PendingCleared -Collector $publicCollector -Android $publicAfter
                Assert-CollectorAndroidBytes -Collector $betaCollector -Android $betaAfter -Label 'Row 6 beta Android bytes'
                Assert-CollectorAndroidBytes -Collector $publicCollector -Android $publicAfter -Label 'Row 6 public Android bytes'
                Add-SyncedProof -Collector $betaCollector -Android $betaAfter -Steam $betaSteamAfter -Context $betaContext -Label 'Row 6 beta branch'
                Add-SyncedProof -Collector $publicCollector -Android $publicAfter -Steam $publicSteamAfter -Context $publicContext -Label 'Row 6 public branch'
            }
            'offline-retry' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'baselineAndroid', 'baselineSteam', 'localOffline', 'remoteOffline', 'localAfterRetry', 'remoteAfterRetry', 'offlineCollector', 'retryCollector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $baselineAndroid = Use-Evidence -Id $refs.baselineAndroid -Kind 'android-manifest' -Row $number
                $baselineSteam = Use-Evidence -Id $refs.baselineSteam -Kind 'steam-manifest' -Row $number
                $localOffline = Use-Evidence -Id $refs.localOffline -Kind 'android-manifest' -Row $number
                $remoteOffline = Use-Evidence -Id $refs.remoteOffline -Kind 'steam-manifest' -Row $number
                $localAfterRetry = Use-Evidence -Id $refs.localAfterRetry -Kind 'android-manifest' -Row $number
                $remoteAfterRetry = Use-Evidence -Id $refs.remoteAfterRetry -Kind 'steam-manifest' -Row $number
                $offlineCollector = Use-Evidence -Id $refs.offlineCollector -Kind 'collector' -Row $number -Phase 'offline-pending'
                $retryCollector = Use-Evidence -Id $refs.retryCollector -Kind 'collector' -Row $number -Phase 'retry-complete'
                foreach ($android in @($baselineAndroid, $localOffline)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $offlineCollector -Label 'Row 7 offline phase'
                }
                Assert-AndroidExportBoundToCollector -Android $localAfterRetry -Collector $retryCollector -Label 'Row 7 retry phase'
                foreach ($record in @($baselineAndroid, $baselineSteam, $localOffline, $remoteOffline, $localAfterRetry, $remoteAfterRetry)) { Assert-EvidenceContext -Record $record -Expected $context }
                $baseline = (Get-AndroidSnapshot -Record $baselineAndroid).Files
                $local = (Get-AndroidSnapshot -Record $localOffline).Files
                Assert-MapsEqual -Label 'Row 7 baseline equality' -Left $baseline -Right (Get-SteamSelectedMap -Record $baselineSteam)
                Assert-MapsDiffer -Label 'Row 7 offline local gameplay' -Left $baseline -Right $local
                Assert-MapsEqual -Label 'Row 7 remote unchanged while offline' -Left (Get-SteamFullMap -Record $baselineSteam) -Right (Get-SteamFullMap -Record $remoteOffline)
                Assert-MapsEqual -Label 'Row 7 local bytes preserved through retry' -Left $local -Right (Get-AndroidSnapshot -Record $localAfterRetry).Files
                Assert-PendingPresent -Collector $offlineCollector -Android $localOffline
                Assert-NoSynced -Collector $offlineCollector
                $offlineTerminal = Get-RequiredAutomaticTerminal -Collector $offlineCollector -Context $context -Operation recover -Outcome failed -Detail operation-failed -RemoteVerified $false -Label 'Row 7 offline failure'
                Assert-ExportRelativeToEvent -Android $baselineAndroid -Collector $offlineCollector -Event $offlineTerminal -Position before -Label 'Row 7 baseline export'
                Assert-ExportRelativeToEvent -Android $localOffline -Collector $offlineCollector -Event $offlineTerminal -Position after -Label 'Row 7 offline local export'
                Assert-SuccessCollector -Collector $retryCollector
                $retryTerminal = Get-RequiredAutomaticTerminal -Collector $retryCollector -Context $context -Operation recover -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 7 retry'
                Assert-ExportRelativeToEvent -Android $localAfterRetry -Collector $retryCollector -Event $retryTerminal -Position after -Label 'Row 7 retry export'
                Assert-PendingCleared -Collector $retryCollector -Android $localAfterRetry
                Assert-CollectorAndroidBytes -Collector $offlineCollector -Android $localOffline -Label 'Row 7 offline Android bytes'
                Assert-CollectorAndroidBytes -Collector $retryCollector -Android $localAfterRetry -Label 'Row 7 retry Android bytes'
                Add-SyncedProof -Collector $retryCollector -Android $localAfterRetry -Steam $remoteAfterRetry -Context $context -Label 'Row 7 retry'
            }
            'crash-resume' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'baselineAndroid', 'baselineSteam', 'localBeforeCrash', 'remoteBeforeRestart', 'localAfterRestart', 'remoteAfterRestart', 'beforeCollector', 'afterCollector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $baselineAndroid = Use-Evidence -Id $refs.baselineAndroid -Kind 'android-manifest' -Row $number
                $baselineSteam = Use-Evidence -Id $refs.baselineSteam -Kind 'steam-manifest' -Row $number
                $localBeforeCrash = Use-Evidence -Id $refs.localBeforeCrash -Kind 'android-manifest' -Row $number
                $remoteBeforeRestart = Use-Evidence -Id $refs.remoteBeforeRestart -Kind 'steam-manifest' -Row $number
                $localAfterRestart = Use-Evidence -Id $refs.localAfterRestart -Kind 'android-manifest' -Row $number
                $remoteAfterRestart = Use-Evidence -Id $refs.remoteAfterRestart -Kind 'steam-manifest' -Row $number
                $beforeCollector = Use-Evidence -Id $refs.beforeCollector -Kind 'collector' -Row $number -Phase 'pending-before-force-stop'
                $afterCollector = Use-Evidence -Id $refs.afterCollector -Kind 'collector' -Row $number -Phase 'after-restart'
                foreach ($android in @($baselineAndroid, $localBeforeCrash)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $beforeCollector -Label 'Row 8 before force-stop'
                }
                Assert-AndroidExportBoundToCollector -Android $localAfterRestart -Collector $afterCollector -Label 'Row 8 after restart'
                foreach ($record in @($baselineAndroid, $baselineSteam, $localBeforeCrash, $remoteBeforeRestart, $localAfterRestart, $remoteAfterRestart)) { Assert-EvidenceContext -Record $record -Expected $context }
                $baseline = (Get-AndroidSnapshot -Record $baselineAndroid).Files
                $local = (Get-AndroidSnapshot -Record $localBeforeCrash).Files
                Assert-MapsEqual -Label 'Row 8 baseline equality' -Left $baseline -Right (Get-SteamSelectedMap -Record $baselineSteam)
                Assert-MapsDiffer -Label 'Row 8 pending local change' -Left $baseline -Right $local
                Assert-MapsEqual -Label 'Row 8 remote before restart' -Left (Get-SteamFullMap -Record $baselineSteam) -Right (Get-SteamFullMap -Record $remoteBeforeRestart)
                Assert-MapsEqual -Label 'Row 8 local bytes survived force-stop' -Left $local -Right (Get-AndroidSnapshot -Record $localAfterRestart).Files
                Assert-PendingPresent -Collector $beforeCollector -Android $localBeforeCrash
                Assert-NoSynced -Collector $beforeCollector
                Assert-SuccessCollector -Collector $afterCollector
                $restartTerminal = Get-RequiredAutomaticTerminal -Collector $afterCollector -Context $context -Operation recover -Outcome synchronized -Detail verified -RemoteVerified $true -Label 'Row 8 resumed reconciliation'
                Assert-ExportRelativeToEvent -Android $localAfterRestart -Collector $afterCollector -Event $restartTerminal -Position after -Label 'Row 8 restarted export'
                Assert-PendingCleared -Collector $afterCollector -Android $localAfterRestart
                $escapedPackage = [regex]::Escape([string]$binding.candidate.packageName)
                $forceStops = @($afterCollector.LogSignals.entries | Where-Object {
                    $_.Tag -in @('ActivityManager', 'ActivityTaskManager') -and
                    $_.Message -match "(?i)^(?:Force stopping|am_force_stop\b)[^\r\n]*$escapedPackage(?:/|:|,|\s|$)"
                })
                $processStarts = @($afterCollector.LogSignals.entries | Where-Object {
                    $_.Tag -in @('ActivityManager', 'ActivityTaskManager') -and
                    $_.Message -match "(?i)^(?:Start proc|am_proc_start\b|Start process)[^\r\n]*$escapedPackage(?:/|:|,|\s|$)"
                })
                Assert-True -Condition ($forceStops.Count -eq 1 -and $processStarts.Count -eq 1 -and [int]$processStarts[0].Index -gt [int]$forceStops[0].Index -and [int]$restartTerminal.Index -gt [int]$processStarts[0].Index) -Message 'Row 8 does not contain package-bound force-stop, process start, then exact recovery terminal evidence.'
                Assert-CollectorAndroidBytes -Collector $beforeCollector -Android $localBeforeCrash -Label 'Row 8 pre-force-stop Android bytes'
                Assert-CollectorAndroidBytes -Collector $afterCollector -Android $localAfterRestart -Label 'Row 8 resumed Android bytes'
                Add-SyncedProof -Collector $afterCollector -Android $localAfterRestart -Steam $remoteAfterRestart -Context $context -Label 'Row 8 resumed reconciliation'
            }
            'commit-failure-no-success' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'localBefore', 'steamBefore', 'localAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'commit-failure'
                foreach ($android in @($localBefore, $localAfter)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $collector -Label 'Row 9 commit failure'
                }
                foreach ($record in @($localBefore, $steamBefore, $localAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-MapsDiffer -Label 'Row 9 commit-failure attempted upload' -Left (Get-AndroidSnapshot -Record $localBefore).Files -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 9 commit-failure local bytes preserved' -Left (Get-AndroidSnapshot -Record $localBefore).Files -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-PendingPresent -Collector $collector -Android $localAfter
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $context -Operation recover -Outcome failed -Detail commit-rejected -RemoteVerified $false -Label 'Row 9 commit failure'
                Assert-ExportRelativeToEvent -Android $localBefore -Collector $collector -Event $terminal -Position before -Label 'Row 9 commit before export'
                Assert-ExportRelativeToEvent -Android $localAfter -Collector $collector -Event $terminal -Position after -Label 'Row 9 commit after export'
                Assert-True -Condition ([bool]$collector.LogSignals.commitFailureSeen) -Message 'Row 9 commit subcase did not observe file_committed=false/commit failure in the inventoried raw log.'
                Assert-NoSynced -Collector $collector
                Assert-SteamCapturedAfter -Steam $steamAfter -Collector $collector -Label 'Row 9 commit failure'
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 9 commit-failure Android bytes'
            }
            'readback-failure-no-success' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'localBefore', 'steamBefore', 'localAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'readback-failure'
                foreach ($android in @($localBefore, $localAfter)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $collector -Label 'Row 9 read-back failure'
                }
                foreach ($record in @($localBefore, $steamBefore, $localAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                $local = (Get-AndroidSnapshot -Record $localBefore).Files
                Assert-MapsDiffer -Label 'Row 9 read-back attempted upload' -Left $local -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 9 read-back local bytes preserved' -Left $local -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-MapsDiffer -Label 'Row 9 read-back mismatch remains unequal' -Left $local -Right (Get-SteamSelectedMap -Record $steamAfter)
                Assert-PendingPresent -Collector $collector -Android $localAfter
                $terminal = Get-RequiredAutomaticTerminal -Collector $collector -Context $context -Operation recover -Outcome failed -Detail remote-readback-mismatch -RemoteVerified $false -Label 'Row 9 read-back failure'
                Assert-ExportRelativeToEvent -Android $localBefore -Collector $collector -Event $terminal -Position before -Label 'Row 9 read-back before export'
                Assert-ExportRelativeToEvent -Android $localAfter -Collector $collector -Event $terminal -Position after -Label 'Row 9 read-back after export'
                Assert-True -Condition ([bool]$collector.LogSignals.readBackMismatchSeen) -Message 'Row 9 read-back subcase did not observe a remote read-back mismatch in the inventoried raw log.'
                Assert-NoSynced -Collector $collector
                Assert-SteamCapturedAfter -Steam $steamAfter -Collector $collector -Label 'Row 9 read-back failure'
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 9 read-back Android bytes'
            }
            'restore-undo-exact' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'androidOriginal', 'androidRestored', 'androidUndone', 'steamBefore', 'steamAfterRestore', 'steamAfterUndo', 'restoreCollector', 'undoCollector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $original = Use-Evidence -Id $refs.androidOriginal -Kind 'android-manifest' -Row $number
                $restored = Use-Evidence -Id $refs.androidRestored -Kind 'android-manifest' -Row $number
                $undone = Use-Evidence -Id $refs.androidUndone -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $steamAfterRestore = Use-Evidence -Id $refs.steamAfterRestore -Kind 'steam-manifest' -Row $number
                $steamAfterUndo = Use-Evidence -Id $refs.steamAfterUndo -Kind 'steam-manifest' -Row $number
                $restoreCollector = Use-Evidence -Id $refs.restoreCollector -Kind 'collector' -Row $number -Phase 'after-restore'
                $undoCollector = Use-Evidence -Id $refs.undoCollector -Kind 'collector' -Row $number -Phase 'after-undo'
                foreach ($android in @($original, $restored)) {
                    Assert-AndroidExportBoundToCollector -Android $android -Collector $restoreCollector -Label 'Row 10 Restore phase'
                }
                Assert-AndroidExportBoundToCollector -Android $undone -Collector $undoCollector -Label 'Row 10 Undo phase'
                foreach ($record in @($original, $restored, $undone, $steamBefore, $steamAfterRestore, $steamAfterUndo)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-True -Condition ([bool]$original.Data.cloudSyncEnabled) -Message 'Row 10 original capture must begin with the existing Cloud Sync setting enabled.'
                Assert-True -Condition (-not [bool]$restored.Data.cloudSyncEnabled) -Message 'Row 10 Restore did not persistently disable Cloud Sync for local validation.'
                Assert-True -Condition (-not [bool]$undone.Data.cloudSyncEnabled) -Message 'Row 10 Undo unexpectedly re-enabled Cloud Sync.'
                Assert-True -Condition ([bool]$restored.Data.launcherState.recoveryJournal.present) -Message 'Row 10 restored export has no verified recovery journal.'
                Assert-Equal -Label 'Row 10 restored journal phase' -Actual ([string]$restored.Data.launcherState.recoveryJournal.phase) -Expected 'validation-required'
                Assert-Context -Label 'Row 10 restored journal context' -Actual $restored.Data.launcherState.recoveryJournal.context -Expected $context
                Assert-True -Condition (-not [bool]$restored.Data.launcherState.recoveryHold.present) -Message 'Row 10 Restore left the superseded recovery-hold file present.'
                Assert-True -Condition ([bool]$undone.Data.launcherState.recoveryJournal.present) -Message 'Row 10 undone export has no verified recovery journal.'
                Assert-Equal -Label 'Row 10 undone journal phase' -Actual ([string]$undone.Data.launcherState.recoveryJournal.phase) -Expected 'undone'
                Assert-Context -Label 'Row 10 undone journal context' -Actual $undone.Data.launcherState.recoveryJournal.context -Expected $context
                Assert-True -Condition (-not [bool]$undone.Data.launcherState.recoveryHold.present) -Message 'Row 10 Undo left the superseded recovery-hold file present.'
                $originalBytes = (Get-AndroidSnapshot -Record $original).Files
                Assert-MapsDiffer -Label 'Row 10 Restore changed Android bytes' -Left $originalBytes -Right (Get-AndroidSnapshot -Record $restored).Files
                Assert-MapsEqual -Label 'Row 10 Undo exact-byte round trip' -Left $originalBytes -Right (Get-AndroidSnapshot -Record $undone).Files
                Assert-MapsEqual -Label 'Row 10 Steam unchanged by Restore' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfterRestore)
                Assert-MapsEqual -Label 'Row 10 Steam unchanged by Undo' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfterUndo)
                Assert-True -Condition ([int]$restoreCollector.LogSignals.recoveryRestoreLogCount -gt 0) -Message "$($restoreCollector.Spec.id) has no Restore action in its inventoried raw log."
                Assert-True -Condition ([int]$undoCollector.LogSignals.recoveryUndoLogCount -gt 0) -Message "$($undoCollector.Spec.id) has no Undo action in its inventoried raw log."
                $restoreTerminal = Get-RequiredRecoveryTerminal -Collector $restoreCollector -Operation restore -Label 'Row 10 Restore'
                Assert-ExportRelativeToEvent -Android $original -Collector $restoreCollector -Event $restoreTerminal -Position before -Label 'Row 10 original export'
                Assert-ExportRelativeToEvent -Android $restored -Collector $restoreCollector -Event $restoreTerminal -Position after -Label 'Row 10 restored export'
                $undoTerminal = Get-RequiredRecoveryTerminal -Collector $undoCollector -Operation undo -Label 'Row 10 Undo'
                Assert-ExportRelativeToEvent -Android $undone -Collector $undoCollector -Event $undoTerminal -Position after -Label 'Row 10 undone export'
                foreach ($collector in @($restoreCollector, $undoCollector)) {
                    Assert-True -Condition ([bool]$collector.LogSignals.recoveryLogSeen) -Message "$($collector.Spec.id) has no recovery operation in its inventoried raw log."
                    Assert-NoSynced -Collector $collector
                }
                Assert-PendingCleared -Collector $restoreCollector -Android $restored
                Assert-PendingCleared -Collector $undoCollector -Android $undone
                Assert-CollectorAndroidBytes -Collector $restoreCollector -Android $restored -Label 'Row 10 restored Android bytes'
                Assert-CollectorAndroidBytes -Collector $undoCollector -Android $undone -Label 'Row 10 undone Android bytes'
            }
            default {
                throw "Unimplemented Stage 5 semantic check type: $($check.type)"
            }
        }
    }
}

foreach ($record in $evidence.Values | Where-Object { [string]$_.Spec.kind -eq 'collector' }) {
    if (Test-HasSyncedLog -Collector $record) {
        Assert-True -Condition ($syncedProofs.Contains([string]$record.Spec.id)) -Message "$($record.Spec.id) contains a Synced log without a later independent live-Steam equality check."
    }
}

Assert-True -Condition ($usedEvidence.Count -eq $evidence.Count) -Message 'Matrix contains evidence that is not consumed by a required semantic check.'
foreach ($id in $evidence.Keys) {
    Assert-True -Condition ($usedEvidence.Contains($id)) -Message "Matrix evidence $id is not consumed by a required semantic check."
}
$androidEvidence = @($evidence.Values | Where-Object {
    [string]$_.Spec.kind -eq 'android-manifest'
})
Assert-True `
    -Condition ($phaseBoundAndroidExportIds.Count -eq $androidEvidence.Count) `
    -Message 'Not every Android export is bound to the inventoried raw collector log for its exact semantic-check phase.'
foreach ($android in $androidEvidence) {
    $androidId = [string]$android.Spec.id
    Assert-True `
        -Condition ($phaseBoundAndroidExportIds.Contains($androidId)) `
        -Message "Android export $androidId is not bound to the collector named by its semantic check."
}
$androidExportsBoundToRawCollector = $phaseBoundAndroidExportIds.Count

$report = [ordered]@{
    schemaVersion = 1
    kind = 'stage5-physical-save-matrix-review'
    reviewedUtc = [DateTime]::UtcNow.ToString('o')
    result = 'passed'
    sourceMatrix = $resolvedMatrix
    sourceMatrixSha256 = $matrixSha256
    sourceCommit = [string]$candidate.sourceCommit
    candidateApkSha256 = [string]$candidate.apkSha256
    candidateBuildInfoSha256 = [string]$candidate.buildInfoSha256
    packageName = [string]$candidate.packageName
    versionName = [string]$candidate.versionName
    versionCode = [int64]$candidate.versionCode
    signerSha256 = [string]$candidate.signerSha256
    deviceSerialSha256 = [string]$binding.device.serialSha256
    rowsPassed = 10
    semanticChecksPassed = 11
    row9SubcasesPassed = 2
    syncedCollectorsWithLaterLiveSteamProof = $syncedProofs.Count
    androidExportsBoundToRawCollector = $androidExportsBoundToRawCollector
    evidenceFilesVerified = $evidence.Count
}

if ($OutputPath) {
    $resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
    if (Test-Path -LiteralPath $resolvedOutput) {
        throw "Refusing to overwrite an existing Stage 5 matrix review: $resolvedOutput"
    }
    $parent = Split-Path -Parent $resolvedOutput
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    [IO.File]::WriteAllText(
        $resolvedOutput,
        ($report | ConvertTo-Json -Depth 8),
        [Text.UTF8Encoding]::new($false)
    )
}

Write-Host 'Stage 5 physical save matrix passed: 10/10 rows, 11/11 semantic checks.'
Write-Host "Every Synced collector has later independent live-Steam byte equality: $($syncedProofs.Count)/$($syncedProofs.Count)."
