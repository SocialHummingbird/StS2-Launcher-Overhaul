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

function Require-ContextMarker {
    param(
        [Parameter(Mandatory = $true)][string]$MarkerPath,
        [Parameter(Mandatory = $true)][string]$ExpectedNamespace
    )

    $fullPath = Resolve-ChildPath -Root $script:ResolvedRoot -RelativePath $MarkerPath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Selected Steam Cloud context marker is missing: $MarkerPath"
    }
    try {
        $marker = [IO.File]::ReadAllText($fullPath) |
            ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Selected Steam Cloud context marker is invalid JSON: $($_.Exception.Message)"
    }
    if ([int]$marker.Version -ne 1) {
        throw "Selected Steam Cloud context marker has an unsupported version."
    }
    if ([string]$marker.SteamId64 -ne $ExpectedSteamId64) {
        throw "Selected Steam Cloud context marker belongs to another Steam account."
    }
    if ([string]$marker.SaveNamespace -ne $ExpectedNamespace.ToLowerInvariant()) {
        throw "Selected Steam Cloud context marker has the wrong namespace."
    }
    if ([string]$marker.RuntimeIdentity -ne $ExpectedRuntimeIdentity) {
        throw "Selected Steam Cloud context marker has the wrong runtime/branch identity."
    }
    if ([string]$marker.ModSetFingerprint -ne $ExpectedModSetFingerprint) {
        throw "Selected Steam Cloud context marker has the wrong mod-set fingerprint."
    }
    return $marker
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
            } | Sort-Object -Property @{ Expression = { $_.Name.ToLowerInvariant() } }, Name)
            foreach ($history in $histories) {
                Add-ManifestPath -RelativePath "$historyRelative/$($history.Name)" -Role $role -Rows $rows
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
$selectedMarker = Require-ContextMarker `
    -MarkerPath $selectedMarkerPath `
    -ExpectedNamespace $SelectedNamespace

$sortedRows = @($rows | Sort-Object -Property @{ Expression = {
    $_.path.ToLowerInvariant()
} }, @{ Expression = { $_.path } })
$treeLines = $sortedRows | ForEach-Object {
    "$($_.path)`t$($_.role)`t$($_.exists.ToString().ToLowerInvariant())`t$($_.sizeBytes)`t$($_.sha256)"
}
$treeSha256 = Get-Sha256Hex -Bytes (
    [Text.Encoding]::UTF8.GetBytes(($treeLines -join "`n") + "`n")
)
$evidenceBytes = [IO.File]::ReadAllBytes($resolvedEvidence)
$evidenceSha256 = Get-Sha256Hex -Bytes $evidenceBytes

$manifest = [ordered]@{
    schemaVersion = 1
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
        steamId64 = [string]$selectedMarker.SteamId64
        saveNamespace = [string]$selectedMarker.SaveNamespace
        runtimeIdentity = [string]$selectedMarker.RuntimeIdentity
        modSetFingerprint = [string]$selectedMarker.ModSetFingerprint
        markerPath = $selectedMarkerPath
    }
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
