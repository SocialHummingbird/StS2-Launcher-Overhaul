[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BundlePath,
    [string]$OutputPath = "",
    [ValidateSet("", "Vanilla", "Modded")]
    [string]$Namespace = "",
    [string]$ExpectedSteamId64 = "",
    [string]$ExpectedRuntimeIdentity = "",
    [string]$ExpectedModSetFingerprint = "",
    [switch]$RequireCloudSyncEnabled
)

$ErrorActionPreference = "Stop"

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

function Normalize-SavePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $normalized = $Path.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        $normalized.StartsWith('/') -or
        $normalized.EndsWith('/') -or
        $normalized.Contains('//') -or
        $normalized -match '(^|/)\.\.?(/|$)') {
        throw "Unsafe or non-canonical save path in recovery bundle: '$Path'."
    }
    return $normalized
}

function Test-AllowedSavePath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$SaveNamespace
    )

    if ($Path -eq 'profile.save') {
        return $true
    }
    $prefix = if ($SaveNamespace -eq 'Modded') { 'modded/' } else { '' }
    $escapedPrefix = [regex]::Escape($prefix)
    return $Path -match (
        '^' + $escapedPrefix +
        'profile[1-3]/saves/(progress\.save|prefs|prefs\.save|current_run\.save|current_run_mp\.save|history/[^/]+\.run)$'
    )
}

function Require-Equal {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [AllowEmptyString()][string]$Actual,
        [AllowEmptyString()][string]$Expected
    )

    if (-not [string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Label mismatch: expected '$Expected', got '$Actual'."
    }
}

function Require-JsonBoolean {
    param(
        [Parameter(Mandatory = $true)]$Document,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $property = $Document.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        throw "$Label is missing mandatory property $PropertyName."
    }
    if ($property.Value -isnot [bool]) {
        throw "$Label property $PropertyName must be a JSON boolean."
    }
    return [bool]$property.Value
}

function Get-Utf8TextSha256 {
    param([AllowEmptyString()][string]$Text)

    return Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes($Text))
}

function Sort-CanonicalFilesOrdinal {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IEnumerable]$Files
    )

    $ordered = [Collections.Generic.List[object]]::new()
    foreach ($file in $Files) {
        $ordered.Add($file)
    }
    $comparison = [Comparison[object]] {
        param($left, $right)

        $primary = [StringComparer]::OrdinalIgnoreCase.Compare(
            [string]$left.path,
            [string]$right.path
        )
        if ($primary -ne 0) {
            return $primary
        }
        return [StringComparer]::Ordinal.Compare(
            [string]$left.path,
            [string]$right.path
        )
    }
    $ordered.Sort($comparison)
    return @($ordered)
}

function Read-EmbeddedJson {
    param(
        [AllowEmptyString()][string]$Content,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($Content)) {
        return $null
    }
    try {
        return $Content | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$Label is not valid JSON."
    }
}

function Convert-VerifiedSaveContextMarker {
    param(
        [AllowEmptyString()][string]$Content,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $marker = Read-EmbeddedJson -Content $Content -Label $Label
    if ($null -eq $marker) {
        throw "$Label is missing."
    }
    if ([int]$marker.Version -ne 1) {
        throw "$Label has an unsupported version."
    }
    [uint64]$parsedSteamId = 0
    $steamId = [string]$marker.SteamId64
    if (-not [uint64]::TryParse($steamId, [ref]$parsedSteamId) -or
        $parsedSteamId -eq 0) {
        throw "$Label has an invalid SteamID64."
    }
    $saveNamespace = ([string]$marker.SaveNamespace).ToLowerInvariant()
    if ($saveNamespace -notin @('vanilla', 'modded')) {
        throw "$Label has an invalid save namespace."
    }
    $runtimeIdentity = [string]$marker.RuntimeIdentity
    $modSetFingerprint = [string]$marker.ModSetFingerprint
    if ([string]::IsNullOrWhiteSpace($runtimeIdentity)) {
        throw "$Label has no runtime identity."
    }
    if (($saveNamespace -eq 'vanilla' -and $modSetFingerprint) -or
        ($saveNamespace -eq 'modded' -and -not $modSetFingerprint)) {
        throw "$Label has an invalid namespace/mod-set combination."
    }
    return [pscustomobject][ordered]@{
        steamId64 = $steamId
        saveNamespace = $saveNamespace
        runtimeIdentity = $runtimeIdentity
        modSetFingerprint = $modSetFingerprint
    }
}

function Require-ContextMatch {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected
    )

    foreach ($field in @(
        'steamId64',
        'saveNamespace',
        'runtimeIdentity',
        'modSetFingerprint'
    )) {
        Require-Equal -Label "$Label $field" `
            -Actual ([string]$Actual.$field) `
            -Expected ([string]$Expected.$field)
    }
}

function Resolve-SnapshotNamespace {
    param(
        [Parameter(Mandatory = $true)]$Snapshot,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $paths = @($Snapshot.Manifest.Entries | ForEach-Object {
        Normalize-SavePath -Path ([string]$_.Path)
    })
    $hasModded = @($paths | Where-Object {
        $_.StartsWith('modded/profile', [StringComparison]::OrdinalIgnoreCase)
    }).Count -gt 0
    $hasVanilla = @($paths | Where-Object {
        $_ -match '^profile[1-3]/saves/'
    }).Count -gt 0
    if ($hasModded -eq $hasVanilla) {
        throw "$Label does not identify exactly one vanilla/modded allowlist."
    }
    return $(if ($hasModded) { 'Modded' } else { 'Vanilla' })
}

function Convert-VerifiedSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Snapshot,
        [Parameter(Mandatory = $true)][ValidateSet('Vanilla', 'Modded')]
        [string]$SaveNamespace,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ([int]$Snapshot.Version -ne 2) {
        throw "$Label has an unsupported version; expected Version 2."
    }
    if ([string]$Snapshot.Coverage -ne 'full') {
        throw "$Label is not a full/tombstone-aware snapshot."
    }
    $manifestEntries = @($Snapshot.Manifest.Entries)
    $contentEntries = @($Snapshot.Files)
    if ($manifestEntries.Count -lt 16) {
        throw "$Label is missing fixed allowlist/tombstone entries."
    }

    $contents = [Collections.Generic.Dictionary[string, object]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    foreach ($file in $contentEntries) {
        $path = Normalize-SavePath -Path ([string]$file.Path)
        if (-not (Test-AllowedSavePath -Path $path -SaveNamespace $SaveNamespace)) {
            throw "$Label contains a path outside the $SaveNamespace allowlist: $path"
        }
        if ($contents.ContainsKey($path)) {
            throw "$Label contains a case-insensitive duplicate file path: $path"
        }
        try {
            $bytes = [Convert]::FromBase64String([string]$file.ContentBase64)
        } catch {
            throw "$Label contains invalid base64 bytes for $path."
        }
        $sha256 = Get-Sha256Hex -Bytes $bytes
        Require-Equal -Label "$Label file byte hash for $path" `
            -Actual $sha256 `
            -Expected ([string]$file.ByteSha256).ToLowerInvariant()
        $contents.Add($path, [pscustomobject]@{
            Path = $path
            Bytes = $bytes
            Sha256 = $sha256
        })
    }

    $seenManifest = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    $canonicalFiles = [Collections.Generic.List[object]]::new()
    foreach ($entry in $manifestEntries) {
        $path = Normalize-SavePath -Path ([string]$entry.Path)
        if (-not (Test-AllowedSavePath -Path $path -SaveNamespace $SaveNamespace)) {
            throw "$Label manifest contains a path outside the $SaveNamespace allowlist: $path"
        }
        if (-not $seenManifest.Add($path)) {
            throw "$Label manifest contains a case-insensitive duplicate path: $path"
        }

        $exists = [bool]$entry.Exists
        if ($exists) {
            if (-not $contents.ContainsKey($path)) {
                throw "$Label manifest says $path exists but its bytes are absent."
            }
            $content = $contents[$path]
            Require-Equal -Label "$Label manifest byte hash for $path" `
                -Actual $content.Sha256 `
                -Expected ([string]$entry.ByteSha256).ToLowerInvariant()
            $canonicalFiles.Add([ordered]@{
                path = $path
                exists = $true
                sizeBytes = $content.Bytes.Length
                sha256 = $content.Sha256
            })
        } else {
            if ($contents.ContainsKey($path)) {
                throw "$Label manifest marks $path missing but also contains bytes for it."
            }
            $canonicalFiles.Add([ordered]@{
                path = $path
                exists = $false
                sizeBytes = 0
                sha256 = ""
            })
        }
    }

    foreach ($path in $contents.Keys) {
        if (-not $seenManifest.Contains($path)) {
            throw "$Label has unmanifested bytes for $path."
        }
    }

    $required = [Collections.Generic.List[string]]::new()
    $required.Add('profile.save')
    $namespacePrefix = if ($SaveNamespace -eq 'Modded') { 'modded/' } else { '' }
    foreach ($profileId in 1..3) {
        foreach ($name in @(
            'progress.save',
            'prefs',
            'prefs.save',
            'current_run.save',
            'current_run_mp.save'
        )) {
            $required.Add("${namespacePrefix}profile${profileId}/saves/$name")
        }
    }
    foreach ($path in $required) {
        if (-not $seenManifest.Contains($path)) {
            throw "$Label manifest omits required tombstone-aware path: $path"
        }
    }

    $sortedFiles = @(Sort-CanonicalFilesOrdinal -Files $canonicalFiles)
    $treeLines = $sortedFiles | ForEach-Object {
        "$($_.path)`t$($_.exists.ToString().ToLowerInvariant())`t$($_.sizeBytes)`t$($_.sha256)"
    }
    $treeBytes = [Text.Encoding]::UTF8.GetBytes(($treeLines -join "`n") + "`n")
    return [pscustomobject]@{
        Namespace = $SaveNamespace
        CapturedUtc = [string]$Snapshot.CapturedUtc
        Coverage = [string]$Snapshot.Coverage
        SourceKind = [string]$Snapshot.SourceKind
        SourceLabel = [string]$Snapshot.SourceLabel
        ContextMarker = [string]$Snapshot.ContextMarker
        TreeSha256 = Get-Sha256Hex -Bytes $treeBytes
        Files = $sortedFiles
    }
}

$resolvedBundle = (Resolve-Path -LiteralPath $BundlePath).Path
$bundleBytes = [IO.File]::ReadAllBytes($resolvedBundle)
$bundleSha256 = Get-Sha256Hex -Bytes $bundleBytes
try {
    $bundle = [Text.Encoding]::UTF8.GetString($bundleBytes) |
        ConvertFrom-Json -ErrorAction Stop
} catch {
    throw "Recovery bundle is not valid UTF-8 JSON: $($_.Exception.Message)"
}

if ([int]$bundle.Version -ne 2) {
    throw "Recovery bundle has an unsupported version; expected Version 2."
}
$exportId = [string]$bundle.ExportId
$reportedCurrentTreeSha256 = [string]$bundle.CurrentAndroidTreeSha256
$reportedContextSha256 = [string]$bundle.SelectedSaveContextSha256
if ($exportId -notmatch '^[0-9a-f]{32}$') {
    throw "Recovery bundle has no valid lowercase ExportId."
}
if ($reportedCurrentTreeSha256 -notmatch '^[0-9a-f]{64}$') {
    throw "Recovery bundle has no valid current Android tree identity."
}
if ($reportedContextSha256 -notmatch '^[0-9a-f]{64}$') {
    throw "Recovery bundle has no valid selected SaveContext identity."
}

$originalSourcesWereModified = Require-JsonBoolean `
    -Document $bundle `
    -PropertyName 'OriginalSourcesWereModified' `
    -Label 'Recovery bundle'
$steamWasContacted = Require-JsonBoolean `
    -Document $bundle `
    -PropertyName 'SteamWasContacted' `
    -Label 'Recovery bundle'
$cloudSyncEnabled = Require-JsonBoolean `
    -Document $bundle `
    -PropertyName 'CloudSyncEnabled' `
    -Label 'Recovery bundle'

if ($originalSourcesWereModified) {
    throw "Recovery bundle claims that original sources were modified."
}
if ($steamWasContacted) {
    throw "Recovery bundle claims that Steam was contacted."
}

$snapshot = $bundle.CurrentAndroidSnapshot
if (-not $snapshot) {
    throw "Recovery bundle has no CurrentAndroidSnapshot."
}
if ([int]$snapshot.Version -ne 2) {
    throw "Current Android snapshot has an unsupported version; expected Version 2."
}
if ([string]$snapshot.Coverage -ne 'full') {
    throw "Current Android snapshot is not a full/tombstone-aware snapshot."
}

$selectedContext = $bundle.SelectedSaveContext
if (-not $selectedContext) {
    throw "Recovery bundle has no SelectedSaveContext binding."
}
$bundleNamespace = switch -Regex ([string]$selectedContext.SaveNamespace) {
    '^(?i:vanilla)$' { 'Vanilla'; break }
    '^(?i:modded)$' { 'Modded'; break }
    default { throw "Recovery bundle has an invalid selected save namespace." }
}
$effectiveNamespace = if ($Namespace) { $Namespace } else { $bundleNamespace }
Require-Equal -Label 'Selected save namespace' -Actual $bundleNamespace -Expected $effectiveNamespace

$steamId64 = [string]$selectedContext.SteamId64
$runtimeIdentity = [string]$selectedContext.RuntimeIdentity
$modSetFingerprint = [string]$selectedContext.ModSetFingerprint
if ($ExpectedSteamId64) {
    Require-Equal -Label 'SteamID64' -Actual $steamId64 -Expected $ExpectedSteamId64
}
if ($ExpectedRuntimeIdentity) {
    Require-Equal -Label 'Runtime identity' -Actual $runtimeIdentity -Expected $ExpectedRuntimeIdentity
}
if ($PSBoundParameters.ContainsKey('ExpectedModSetFingerprint')) {
    Require-Equal -Label 'Mod-set fingerprint' -Actual $modSetFingerprint -Expected $ExpectedModSetFingerprint
}
if ($effectiveNamespace -eq 'Vanilla' -and $modSetFingerprint) {
    throw "Vanilla recovery bundle unexpectedly carries a mod-set fingerprint."
}
if ($effectiveNamespace -eq 'Modded' -and -not $modSetFingerprint) {
    throw "Modded recovery bundle is missing its mod-set fingerprint."
}
if ([string]::IsNullOrWhiteSpace($runtimeIdentity)) {
    throw "Recovery bundle is missing its runtime/branch identity."
}
if ($RequireCloudSyncEnabled -and -not $cloudSyncEnabled) {
    throw "Recovery bundle records cloud_sync_enabled=false."
}
$contextIdentityText =
    "$steamId64`0$($effectiveNamespace.ToLowerInvariant())`0$runtimeIdentity`0$modSetFingerprint`0"
$contextSha256 = Get-Utf8TextSha256 -Text $contextIdentityText
Require-Equal -Label 'Selected SaveContext SHA-256' `
    -Actual $reportedContextSha256 `
    -Expected $contextSha256
$selectedExpectedContext = [pscustomobject][ordered]@{
    steamId64 = $steamId64
    saveNamespace = $effectiveNamespace.ToLowerInvariant()
    runtimeIdentity = $runtimeIdentity
    modSetFingerprint = $modSetFingerprint
}

$verifiedCurrent = Convert-VerifiedSnapshot `
    -Snapshot $snapshot `
    -SaveNamespace $effectiveNamespace `
    -Label 'Current Android snapshot'
$sortedFiles = $verifiedCurrent.Files
$treeSha256 = $verifiedCurrent.TreeSha256
Require-Equal -Label 'Current Android tree SHA-256' `
    -Actual $reportedCurrentTreeSha256 `
    -Expected $treeSha256

$currentContextMarker = [string]$verifiedCurrent.ContextMarker
if ($steamId64 -eq '0') {
    if (-not [string]::IsNullOrWhiteSpace($currentContextMarker)) {
        throw "Current Android snapshot has an exact context marker but the bundle reports no exact Steam account."
    }
} else {
    if ([string]::IsNullOrWhiteSpace($currentContextMarker)) {
        throw "Current Android snapshot is missing its exact save-context marker."
    }
    $snapshotContext = Convert-VerifiedSaveContextMarker `
        -Content $currentContextMarker `
        -Label 'Current Android snapshot context marker'
    Require-ContextMatch `
        -Label 'Current Android snapshot context' `
        -Actual $snapshotContext `
        -Expected $selectedExpectedContext
}

$verifiedRecoverySnapshots = [Collections.Generic.List[object]]::new()
$recoveryItems = @($bundle.RecoverySnapshots | Where-Object { $null -ne $_ })
foreach ($recovery in $recoveryItems) {
    if (-not $recovery.Snapshot) {
        throw "Recovery bundle contains a recovery entry without its snapshot."
    }
    $recoveryNamespace = Resolve-SnapshotNamespace `
        -Snapshot $recovery.Snapshot `
        -Label 'Recovery snapshot'
    $verified = Convert-VerifiedSnapshot `
        -Snapshot $recovery.Snapshot `
        -SaveNamespace $recoveryNamespace `
        -Label "Recovery snapshot $([string]$recovery.CandidateId)"
    $reportedSnapshotId = [string]$recovery.CandidateId
    $reportedSnapshotSha256 = [string]$recovery.SnapshotSha256
    if ($reportedSnapshotId -notmatch '^[0-9a-fA-F]{64}$' -or
        $reportedSnapshotSha256 -notmatch '^[0-9a-fA-F]{64}$') {
        throw "Recovery snapshot has no reported content-addressed identity."
    }
    Require-Equal `
        -Label 'Recovery snapshot reported identity' `
        -Actual $reportedSnapshotId.ToLowerInvariant() `
        -Expected $reportedSnapshotSha256.ToLowerInvariant()
    $verifiedRecoverySnapshots.Add([ordered]@{
        reportedSnapshotId = $reportedSnapshotId.ToLowerInvariant()
        reportedSnapshotSha256 = $reportedSnapshotSha256.ToLowerInvariant()
        sourceKind = [string]$recovery.SourceKind
        sourceLabel = [string]$recovery.SourceLabel
        classification = [string]$recovery.Classification
        snapshotPath = [string]$recovery.SnapshotPath
        saveNamespace = $recoveryNamespace.ToLowerInvariant()
        contextMarker = $verified.ContextMarker
        capturedUtc = $verified.CapturedUtc
        treeSha256 = $verified.TreeSha256
        files = $verified.Files
    })
}

$pendingRaw = [string]$bundle.AutomaticSyncPendingJson
$pendingDocument = Read-EmbeddedJson `
    -Content $pendingRaw `
    -Label 'Automatic sync pending record'
$pendingSummary = [ordered]@{
    present = $null -ne $pendingDocument
    sha256 = $(if ($null -ne $pendingDocument) {
        Get-Utf8TextSha256 -Text $pendingRaw
    } else { '' })
    phase = ''
    context = $null
}
if ($null -ne $pendingDocument) {
    if ([int]$pendingDocument.Version -ne 1) {
        throw "Automatic sync pending record has an unsupported version."
    }
    $pendingPhase = [string]$pendingDocument.Phase
    if ($pendingPhase -notin @('game-running', 'uploading', 'downloading')) {
        throw "Automatic sync pending record has an invalid phase."
    }
    $pendingContext = Convert-VerifiedSaveContextMarker `
        -Content ([string]$pendingDocument.ContextMarker) `
        -Label 'Automatic sync pending context marker'
    Require-ContextMatch `
        -Label 'Automatic sync pending context' `
        -Actual $pendingContext `
        -Expected $selectedExpectedContext
    $pendingSummary.phase = $pendingPhase
    $pendingSummary.context = $pendingContext
}

$baselineRaw = [string]$bundle.AutomaticSyncBaselineJson
$baselineDocument = Read-EmbeddedJson `
    -Content $baselineRaw `
    -Label 'Automatic sync baseline'
$baselineSummary = [ordered]@{
    present = $null -ne $baselineDocument
    sha256 = $(if ($null -ne $baselineDocument) {
        Get-Utf8TextSha256 -Text $baselineRaw
    } else { '' })
}
if ($null -ne $baselineDocument) {
    if ([int]$baselineDocument.Version -ne 1) {
        throw "Automatic sync baseline has an unsupported version."
    }
    $baselineContext = Convert-VerifiedSaveContextMarker `
        -Content ([string]$baselineDocument.ContextMarker) `
        -Label 'Automatic sync baseline context marker'
    Require-ContextMatch `
        -Label 'Automatic sync baseline context' `
        -Actual $baselineContext `
        -Expected $selectedExpectedContext
}

$beforeGameRaw = [string]$bundle.AutomaticSyncBeforeGameSnapshotJson
$beforeGameDocument = Read-EmbeddedJson `
    -Content $beforeGameRaw `
    -Label 'Automatic sync before-game snapshot'
$beforeGameSummary = [ordered]@{
    present = $null -ne $beforeGameDocument
    sha256 = $(if ($null -ne $beforeGameDocument) {
        Get-Utf8TextSha256 -Text $beforeGameRaw
    } else { '' })
    treeSha256 = ''
}
if ($null -ne $beforeGameDocument) {
    $verifiedBeforeGame = Convert-VerifiedSnapshot `
        -Snapshot $beforeGameDocument `
        -SaveNamespace $effectiveNamespace `
        -Label 'Automatic sync before-game snapshot'
    $beforeGameContext = Convert-VerifiedSaveContextMarker `
        -Content ([string]$verifiedBeforeGame.ContextMarker) `
        -Label 'Automatic sync before-game context marker'
    Require-ContextMatch `
        -Label 'Automatic sync before-game context' `
        -Actual $beforeGameContext `
        -Expected $selectedExpectedContext
    $beforeGameSummary.treeSha256 = $verifiedBeforeGame.TreeSha256
}

$recoveryHoldRaw = [string]$bundle.RecoveryHoldJson
$recoveryHoldPresent = -not [string]::IsNullOrWhiteSpace($recoveryHoldRaw)
if ($recoveryHoldPresent -and
    $null -eq (Read-EmbeddedJson -Content $recoveryHoldRaw -Label 'Legacy recovery hold')) {
    throw "Legacy recovery hold must contain a JSON value."
}
$recoveryHoldSummary = [ordered]@{
    present = $recoveryHoldPresent
    sha256 = $(if ($recoveryHoldPresent) {
        Get-Utf8TextSha256 -Text $recoveryHoldRaw
    } else { '' })
}

$recoveryJournalRaw = [string]$bundle.RecoveryJournalJson
$recoveryJournal = Read-EmbeddedJson `
    -Content $recoveryJournalRaw `
    -Label 'Recovery journal'
$recoveryJournalSummary = [ordered]@{
    present = $null -ne $recoveryJournal
    sha256 = $(if ($null -ne $recoveryJournal) {
        Get-Utf8TextSha256 -Text $recoveryJournalRaw
    } else { '' })
    phase = ''
    context = $null
}
if ($null -ne $recoveryJournal) {
    if ([int]$recoveryJournal.Version -ne 2) {
        throw "Recovery journal has an unsupported version."
    }
    $recoveryPhase = [string]$recoveryJournal.Phase
    if ($recoveryPhase -notin @(
        'restoring',
        'validation-required',
        'undoing',
        'approved',
        'undone'
    )) {
        throw "Recovery journal has an invalid phase."
    }
    $journalNamespace = ([string]$recoveryJournal.SaveNamespace).ToLowerInvariant()
    $journalRuntime = [string]$recoveryJournal.RuntimeIdentity
    $journalFingerprint = [string]$recoveryJournal.ModSetFingerprint
    Require-Equal -Label 'Recovery journal save namespace' `
        -Actual $journalNamespace `
        -Expected $selectedExpectedContext.saveNamespace
    Require-Equal -Label 'Recovery journal runtime identity' `
        -Actual $journalRuntime `
        -Expected $selectedExpectedContext.runtimeIdentity
    Require-Equal -Label 'Recovery journal mod-set fingerprint' `
        -Actual $journalFingerprint `
        -Expected $selectedExpectedContext.modSetFingerprint

    $journalMarker = [string]$recoveryJournal.TargetContextMarker
    if (-not [string]::IsNullOrWhiteSpace($journalMarker)) {
        $journalContext = Convert-VerifiedSaveContextMarker `
            -Content $journalMarker `
            -Label 'Recovery journal target context marker'
        Require-ContextMatch `
            -Label 'Recovery journal target context' `
            -Actual $journalContext `
            -Expected $selectedExpectedContext
        $recoveryJournalSummary.context = $journalContext
    }
    foreach ($field in @(
        'SourceSnapshotSha256',
        'UndoSnapshotSha256',
        'AppliedSnapshotSha256'
    )) {
        $value = [string]$recoveryJournal.$field
        if ($value -and $value -notmatch '^[0-9a-fA-F]{64}$') {
            throw "Recovery journal $field is not a SHA-256 digest."
        }
    }
    $recoveryJournalSummary.phase = $recoveryPhase
}

if (-not $OutputPath) {
    $OutputPath = "$resolvedBundle.android-save-manifest.json"
}
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
if ([string]::Equals($resolvedOutput, $resolvedBundle, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Output manifest cannot overwrite the recovery bundle."
}
if (Test-Path -LiteralPath $resolvedOutput) {
    throw "Refusing to overwrite an existing Android save manifest: $resolvedOutput"
}
$outputParent = Split-Path -Parent $resolvedOutput
if ($outputParent) {
    New-Item -ItemType Directory -Force -Path $outputParent | Out-Null
}

$result = [ordered]@{
    schemaVersion = 1
    kind = 'stage5-android-save-manifest'
    authority = 'verified-recovery-export-current-android-snapshot'
    verifiedUtc = [DateTime]::UtcNow.ToString('o')
    sourceBundle = $resolvedBundle
    sourceBundleSha256 = $bundleSha256
    originalSourcesWereModified = $originalSourcesWereModified
    steamWasContacted = $steamWasContacted
    cloudSyncEnabled = $cloudSyncEnabled
    exportBinding = [ordered]@{
        event = 'save-recovery-export-complete'
        version = 1
        exportId = $exportId
        bundleSha256 = $bundleSha256
        currentAndroidTreeSha256 = $treeSha256
        selectedSaveContextSha256 = $contextSha256
    }
    context = [ordered]@{
        steamId64 = $steamId64
        saveNamespace = $effectiveNamespace.ToLowerInvariant()
        runtimeIdentity = $runtimeIdentity
        modSetFingerprint = $modSetFingerprint
    }
    snapshot = [ordered]@{
        capturedUtc = [string]$snapshot.CapturedUtc
        coverage = [string]$snapshot.Coverage
        sourceKind = [string]$snapshot.SourceKind
        sourceLabel = [string]$snapshot.SourceLabel
        contextMarker = $currentContextMarker
        treeSha256 = $treeSha256
        files = $sortedFiles
    }
    recoverySnapshots = @($verifiedRecoverySnapshots)
    launcherState = [ordered]@{
        pending = $pendingSummary
        baseline = $baselineSummary
        beforeGameSnapshot = $beforeGameSummary
        recoveryHold = $recoveryHoldSummary
        recoveryJournal = $recoveryJournalSummary
    }
}

[IO.File]::WriteAllText(
    $resolvedOutput,
    ($result | ConvertTo-Json -Depth 12),
    [Text.UTF8Encoding]::new($false)
)
Write-Host "Verified Stage 5 Android save bundle: $resolvedBundle"
Write-Host "Canonical Android save manifest: $resolvedOutput"
Write-Host "Tree SHA-256: $treeSha256"
