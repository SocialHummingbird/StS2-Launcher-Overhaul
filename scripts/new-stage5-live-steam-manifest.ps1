[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SteamCloudRoot,
    [Parameter(Mandatory = $true)]
    [ValidateSet("Vanilla", "Modded")]
    [string]$SelectedNamespace,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedSteamId64,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRuntimeIdentity,
    [string]$ExpectedModSetFingerprint = "",
    [Parameter(Mandatory = $true)]
    [string]$SteamSyncEvidencePath,
    [Parameter(Mandatory = $true)]
    [string]$CaptureMethod,
    [Parameter(Mandatory = $true)]
    [switch]$RetainedImmutableSource,
    [switch]$CaptureFailedTransferWithMissingSelectedMarker,
    [string]$BeforeSteamManifestPath = "",
    [string]$PendingSyncRecordPath = "",
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$appId = 2868840

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

function Normalize-RelativePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $normalized = $Path.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        $normalized.StartsWith('/') -or
        $normalized.EndsWith('/') -or
        $normalized.Contains('//') -or
        $normalized -match '(^|/)\.\.?(/|$)') {
        throw "Unsafe or non-canonical Steam Cloud path: '$Path'."
    }
    return $normalized
}

function Resolve-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $canonical = Normalize-RelativePath -Path $RelativePath
    $candidate = [IO.Path]::GetFullPath((Join-Path $Root ($canonical.Replace('/', [IO.Path]::DirectorySeparatorChar))))
    $rootPrefix = $Root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Steam Cloud path escaped its declared root: $RelativePath"
    }
    return $candidate
}

function Add-ManifestPath {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)]$Rows
    )

    $canonical = Normalize-RelativePath -Path $RelativePath
    if (-not $script:SeenPaths.Add($canonical)) {
        throw "Case-insensitive duplicate Steam Cloud path: $canonical"
    }
    [void]$script:ExactSeenPaths.Add($canonical)
    $fullPath = Resolve-ChildPath -Root $script:ResolvedRoot -RelativePath $canonical
    if (Test-Path -LiteralPath $fullPath -PathType Container) {
        throw "Expected Steam Cloud file is a directory: $canonical"
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $Rows.Add([ordered]@{
            path = $canonical
            role = $Role
            exists = $false
            sizeBytes = 0
            sha256 = ""
        })
        return
    }

    $bytes = [IO.File]::ReadAllBytes($fullPath)
    $Rows.Add([ordered]@{
        path = $canonical
        role = $Role
        exists = $true
        sizeBytes = $bytes.Length
        sha256 = Get-Sha256Hex -Bytes $bytes
    })
}

function Convert-ContextMarker {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ([int]$Marker.Version -ne 1) {
        throw "$Label has an unsupported version."
    }
    [uint64]$parsedSteamId64 = 0
    $steamId64 = [string]$Marker.SteamId64
    if (-not [uint64]::TryParse($steamId64, [ref]$parsedSteamId64) -or
        $parsedSteamId64 -eq 0) {
        throw "$Label has an invalid SteamID64."
    }
    $saveNamespace = ([string]$Marker.SaveNamespace).ToLowerInvariant()
    if ($saveNamespace -notin @('vanilla', 'modded')) {
        throw "$Label has an invalid namespace."
    }
    $runtimeIdentity = [string]$Marker.RuntimeIdentity
    if ([string]::IsNullOrWhiteSpace($runtimeIdentity)) {
        throw "$Label has no runtime/branch identity."
    }
    $modSetFingerprint = [string]$Marker.ModSetFingerprint
    if (($saveNamespace -eq 'vanilla' -and $modSetFingerprint) -or
        ($saveNamespace -eq 'modded' -and -not $modSetFingerprint)) {
        throw "$Label has an invalid namespace/mod-set combination."
    }
    return [pscustomobject][ordered]@{
        steamId64 = $steamId64
        saveNamespace = $saveNamespace
        runtimeIdentity = $runtimeIdentity
        modSetFingerprint = $modSetFingerprint
    }
}

function Read-JsonEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
        throw "$Label is not a file: $resolvedPath"
    }
    $bytes = [IO.File]::ReadAllBytes($resolvedPath)
    try {
        $text = [Text.UTF8Encoding]::new($false, $true).GetString($bytes)
        $document = $text | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$Label is not valid UTF-8 JSON: $($_.Exception.Message)"
    }
    return [pscustomobject]@{
        Path = $resolvedPath
        Bytes = $bytes
        Document = $document
    }
}

function Read-ContextMarkerFile {
    param([Parameter(Mandatory = $true)][string]$MarkerPath)

    $fullPath = Resolve-ChildPath -Root $script:ResolvedRoot -RelativePath $MarkerPath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Selected Steam Cloud context marker is missing: $MarkerPath"
    }
    try {
        $marker = [IO.File]::ReadAllText(
            $fullPath,
            [Text.UTF8Encoding]::new($false, $true)
        ) | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Selected Steam Cloud context marker is invalid UTF-8 JSON: $($_.Exception.Message)"
    }
    return Convert-ContextMarker `
        -Marker $marker `
        -Label 'Selected Steam Cloud context marker'
}

function Assert-ContextMatch {
    param(
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Label
    )

    foreach ($field in @(
        'steamId64',
        'saveNamespace',
        'runtimeIdentity',
        'modSetFingerprint'
    )) {
        if ([string]$Actual.$field -cne [string]$Expected.$field) {
            throw "$Label has a mismatched $field."
        }
    }
}

$resolvedRoot = (Resolve-Path -LiteralPath $SteamCloudRoot).Path
$script:ResolvedRoot = $resolvedRoot
$resolvedEvidence = (Resolve-Path -LiteralPath $SteamSyncEvidencePath).Path
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$rootPrefix = $resolvedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if ($resolvedOutput.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Output manifest must be outside the read-only Steam Cloud source root."
}
if (Test-Path -LiteralPath $resolvedOutput) {
    throw "Refusing to overwrite an existing live Steam manifest: $resolvedOutput"
}
if ([string]::IsNullOrWhiteSpace($CaptureMethod)) {
    throw "CaptureMethod must identify how the independent PC obtained current Steam Cloud bytes."
}
if (-not $RetainedImmutableSource) {
    throw "Review 5 requires a retained immutable copy of the independent Steam download."
}
if ($SelectedNamespace -eq 'Vanilla' -and $ExpectedModSetFingerprint) {
    throw "Vanilla context cannot carry a mod-set fingerprint."
}
if ($SelectedNamespace -eq 'Modded' -and -not $ExpectedModSetFingerprint) {
    throw "Modded context requires a mod-set fingerprint."
}
$expectedContext = Convert-ContextMarker `
    -Marker ([pscustomobject]@{
        Version = 1
        SteamId64 = $ExpectedSteamId64
        SaveNamespace = $SelectedNamespace
        RuntimeIdentity = $ExpectedRuntimeIdentity
        ModSetFingerprint = $ExpectedModSetFingerprint
    }) `
    -Label 'Expected SaveContext'
$beforeEvidenceProvided = -not [string]::IsNullOrWhiteSpace($BeforeSteamManifestPath)
$pendingEvidenceProvided = -not [string]::IsNullOrWhiteSpace($PendingSyncRecordPath)
if ($CaptureFailedTransferWithMissingSelectedMarker) {
    if ($beforeEvidenceProvided -eq $pendingEvidenceProvided) {
        throw "A failed-transfer missing-marker capture requires exactly one independent context source: BeforeSteamManifestPath or PendingSyncRecordPath."
    }
} elseif ($beforeEvidenceProvided -or $pendingEvidenceProvided) {
    throw "Before/pending context evidence is only valid for an explicit failed-transfer missing-marker capture."
}

$rows = [Collections.Generic.List[object]]::new()
$script:SeenPaths = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
$script:ExactSeenPaths = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::Ordinal
)
Add-ManifestPath -RelativePath 'profile.save' -Role 'shared-save' -Rows $rows
foreach ($namespaceName in @('Vanilla', 'Modded')) {
    $prefix = if ($namespaceName -eq 'Modded') { 'modded/' } else { '' }
    $role = $namespaceName.ToLowerInvariant() + '-save'
    foreach ($profileId in 1..3) {
        $saveDirectory = "${prefix}profile${profileId}/saves"
        foreach ($name in @(
            'progress.save',
            'prefs',
            'prefs.save',
            'current_run.save',
            'current_run_mp.save'
        )) {
            Add-ManifestPath -RelativePath "$saveDirectory/$name" -Role $role -Rows $rows
        }

        $historyRelative = "$saveDirectory/history"
        $historyFull = Resolve-ChildPath -Root $resolvedRoot -RelativePath $historyRelative
        if (Test-Path -LiteralPath $historyFull -PathType Container) {
            $histories = @(Get-ChildItem -LiteralPath $historyFull -File | Where-Object {
                $_.Name.Length -gt '.run'.Length -and
                $_.Name.EndsWith('.run', [StringComparison]::OrdinalIgnoreCase)
            } | ForEach-Object {
                [pscustomobject]@{
                    path = $_.Name
                    File = $_
                }
            })
            $histories = @(Sort-CanonicalFilesOrdinal -Files $histories)
            foreach ($history in $histories) {
                Add-ManifestPath -RelativePath "$historyRelative/$($history.File.Name)" -Role $role -Rows $rows
            }
        }
    }
}
foreach ($namespaceName in @('vanilla', 'modded')) {
    Add-ManifestPath `
        -RelativePath ".sts2-launcher/contexts/$namespaceName.json" `
        -Role "$namespaceName-context-marker" `
        -Rows $rows
}

# This root is a retained, dedicated copy of one independent Steam client
# download. Every remote file must be represented by the explicit save/context
# allowlist above; otherwise device settings or an unknown namespace could be
# silently excluded from Review 5.
$reparsePoint = Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force |
    Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
    Select-Object -First 1
if ($reparsePoint) {
    throw "Retained Steam source contains a reparse point: $($reparsePoint.FullName)"
}
foreach ($sourceFile in Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -Force) {
    $fullName = [IO.Path]::GetFullPath($sourceFile.FullName)
    if (-not $fullName.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Retained Steam source file escaped its root: $fullName"
    }
    $relative = Normalize-RelativePath -Path (
        $fullName.Substring($rootPrefix.Length).Replace('\', '/')
    )
    if (-not $script:ExactSeenPaths.Contains($relative)) {
        throw "Retained Steam source contains a file outside the explicit save allowlist: $relative"
    }
}

$selectedMarkerPath = ".sts2-launcher/contexts/$($SelectedNamespace.ToLowerInvariant()).json"
$selectedMarkerRow = @($rows | Where-Object {
    [string]$_.path -ceq $selectedMarkerPath
})
if ($selectedMarkerRow.Count -ne 1) {
    throw "Selected Steam Cloud context marker row is missing or duplicated."
}

$selectedContext = $null
$selectedContextEvidence = $null
$selectedContextMarkerState = ''
if ($CaptureFailedTransferWithMissingSelectedMarker) {
    if ([bool]$selectedMarkerRow[0].exists) {
        throw "Failed-transfer evidence requires the selected Steam Cloud context marker to be missing/tombstoned."
    }

    if ($beforeEvidenceProvided) {
        $source = Read-JsonEvidence `
            -Path $BeforeSteamManifestPath `
            -Label 'Before-transfer Steam manifest context evidence'
        $document = $source.Document
        if ([int]$document.schemaVersion -ne 2 -or
            [string]$document.kind -ne 'stage5-live-steam-save-manifest' -or
            [string]$document.authority -ne 'independent-live-steam-client-download' -or
            [int]$document.appId -ne $appId -or
            $document.sourceRootRetainedImmutable -isnot [bool] -or
            -not [bool]$document.sourceRootRetainedImmutable -or
            [string]$document.selectedContextMarker.state -ne 'present-valid' -or
            $document.selectedContextMarker.exists -isnot [bool] -or
            -not [bool]$document.selectedContextMarker.exists -or
            [string]$document.selectedContextEvidence.kind -ne
                'selected-context-marker') {
            throw "Before-transfer Steam manifest is not valid present-marker context evidence."
        }
        $selectedContext = Convert-ContextMarker `
            -Marker ([pscustomobject]@{
                Version = 1
                SteamId64 = [string]$document.selectedContext.steamId64
                SaveNamespace = [string]$document.selectedContext.saveNamespace
                RuntimeIdentity = [string]$document.selectedContext.runtimeIdentity
                ModSetFingerprint = [string]$document.selectedContext.modSetFingerprint
            }) `
            -Label 'Before-transfer Steam manifest SaveContext'
        $sourceMarkerPath =
            ".sts2-launcher/contexts/$($selectedContext.saveNamespace).json"
        if ([string]$document.selectedContext.markerPath -cne
                $sourceMarkerPath -or
            [string]$document.selectedContextMarker.path -cne
                $sourceMarkerPath -or
            [string]$document.selectedContextEvidence.path -cne
                $sourceMarkerPath) {
            throw "Before-transfer Steam manifest does not bind its SaveContext to the exact selected marker path."
        }
        $selectedContextEvidence = [ordered]@{
            kind = 'before-steam-manifest'
            path = $source.Path
            sizeBytes = $source.Bytes.Length
            sha256 = Get-Sha256Hex -Bytes $source.Bytes
        }
    } else {
        $source = Read-JsonEvidence `
            -Path $PendingSyncRecordPath `
            -Label 'Pending-sync context evidence'
        $document = $source.Document
        if ([int]$document.Version -ne 1 -or
            [string]$document.Phase -notin @('game-running', 'uploading', 'downloading') -or
            [string]::IsNullOrWhiteSpace([string]$document.ContextMarker)) {
            throw "Pending-sync record is not valid context evidence."
        }
        try {
            $pendingMarker = [string]$document.ContextMarker |
                ConvertFrom-Json -ErrorAction Stop
        } catch {
            throw "Pending-sync context marker is invalid JSON: $($_.Exception.Message)"
        }
        $selectedContext = Convert-ContextMarker `
            -Marker $pendingMarker `
            -Label 'Pending-sync SaveContext marker'
        $selectedContextEvidence = [ordered]@{
            kind = 'pending-sync-record'
            path = $source.Path
            sizeBytes = $source.Bytes.Length
            sha256 = Get-Sha256Hex -Bytes $source.Bytes
        }
    }
    Assert-ContextMatch `
        -Actual $selectedContext `
        -Expected $expectedContext `
        -Label 'Independent failed-transfer context evidence'
    $selectedContextMarkerState = 'missing-tombstone-after-failed-transfer'
} else {
    $selectedContext = Read-ContextMarkerFile -MarkerPath $selectedMarkerPath
    Assert-ContextMatch `
        -Actual $selectedContext `
        -Expected $expectedContext `
        -Label 'Selected Steam Cloud context marker'
    $selectedContextEvidence = [ordered]@{
        kind = 'selected-context-marker'
        path = $selectedMarkerPath
        sizeBytes = [int64]$selectedMarkerRow[0].sizeBytes
        sha256 = [string]$selectedMarkerRow[0].sha256
    }
    $selectedContextMarkerState = 'present-valid'
}

$sortedRows = @(Sort-CanonicalFilesOrdinal -Files $rows)
$treeLines = $sortedRows | ForEach-Object {
    "$($_.path)`t$($_.role)`t$($_.exists.ToString().ToLowerInvariant())`t$($_.sizeBytes)`t$($_.sha256)"
}
$treeSha256 = Get-Sha256Hex -Bytes (
    [Text.Encoding]::UTF8.GetBytes(($treeLines -join "`n") + "`n")
)
$evidenceBytes = [IO.File]::ReadAllBytes($resolvedEvidence)
$evidenceSha256 = Get-Sha256Hex -Bytes $evidenceBytes

$manifest = [ordered]@{
    schemaVersion = 2
    kind = 'stage5-live-steam-save-manifest'
    authority = 'independent-live-steam-client-download'
    appId = $appId
    capturedUtc = [DateTime]::UtcNow.ToString('o')
    captureMethod = $CaptureMethod
    sourceRoot = $resolvedRoot
    sourceRootRetainedImmutable = [bool]$RetainedImmutableSource
    steamSyncEvidence = [ordered]@{
        path = $resolvedEvidence
        sizeBytes = $evidenceBytes.Length
        sha256 = $evidenceSha256
    }
    selectedContext = [ordered]@{
        steamId64 = [string]$selectedContext.steamId64
        saveNamespace = [string]$selectedContext.saveNamespace
        runtimeIdentity = [string]$selectedContext.runtimeIdentity
        modSetFingerprint = [string]$selectedContext.modSetFingerprint
        markerPath = $selectedMarkerPath
    }
    selectedContextMarker = [ordered]@{
        path = $selectedMarkerPath
        state = $selectedContextMarkerState
        exists = [bool]$selectedMarkerRow[0].exists
        sizeBytes = [int64]$selectedMarkerRow[0].sizeBytes
        sha256 = [string]$selectedMarkerRow[0].sha256
    }
    selectedContextEvidence = $selectedContextEvidence
    treeSha256 = $treeSha256
    files = $sortedRows
}

$outputParent = Split-Path -Parent $resolvedOutput
if ($outputParent) {
    New-Item -ItemType Directory -Force -Path $outputParent | Out-Null
}
[IO.File]::WriteAllText(
    $resolvedOutput,
    ($manifest | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false)
)
Write-Host "Captured independent live Steam save manifest: $resolvedOutput"
Write-Host "Steam Cloud tree SHA-256: $treeSha256"
