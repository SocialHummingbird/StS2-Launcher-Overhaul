param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sourceRoot = Join-Path $root "src\STS2Mobile"
$allowlistPath = Join-Path $sourceRoot "Steam\SaveTransferAllowlist.cs"
$manualTransferPath = Join-Path $sourceRoot (
    "Steam\CloudSyncCoordinator.ManualSync.Transfer.cs"
)
$automaticManifestPath = Join-Path $sourceRoot (
    "Steam\CloudSyncCoordinator.AutomaticSync.Manifest.cs"
)
$removedSeedingPaths = @(
    (Join-Path $sourceRoot "Steam\CloudSyncCoordinator.ManualSync.ModdedSaveSeed.cs"),
    (Join-Path $sourceRoot "Steam\ModdedSaveSeedPlan.cs")
)

foreach ($path in @(
    $allowlistPath,
    $manualTransferPath,
    $automaticManifestPath
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required save-namespace guardrail input is missing: $path"
    }
}

foreach ($path in $removedSeedingPaths) {
    if (Test-Path -LiteralPath $path) {
        throw "Removed vanilla-to-modded seeding source returned: $path"
    }
}

$productionFiles = Get-ChildItem `
    -LiteralPath $sourceRoot `
    -Recurse `
    -File `
    -Filter "*.cs" |
    Sort-Object FullName
$production = ($productionFiles | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw
}) -join [Environment]::NewLine
$allowlist = Get-Content -LiteralPath $allowlistPath -Raw
$manualTransfer = Get-Content -LiteralPath $manualTransferPath -Raw
$automaticManifest = Get-Content -LiteralPath $automaticManifestPath -Raw

$forbiddenProductionPatterns = @(
    '(?i)\bModdedSaveSeed\b',
    '(?i)\bSeedModded\b',
    '(?i)\bSeededCopies\b',
    '(?i)\bDirectCloudModdedTargets\b',
    '(?i)\bCloudAuthoritativeNamespaces\b',
    '(?i)\bManualPullPrivateBackupRelativeDirectory\b',
    '(?i)\bManualPullModdedSaveSeedEvidence\b',
    '(?im)^.*\b(?:seed|copy|clone)\w*\b.*\bvanilla\b.*\bmodded\b.*$',
    '(?im)^.*\b(?:seed|copy|clone)\w*\b.*\bmodded\b.*\bvanilla\b.*$',
    '(?im)^.*\b(?:write|copy|move|rename)\w*\b.*(?:"modded/"|SaveNamespace\.Modded).*$',
    '(?im)^.*(?:"modded/"|SaveNamespace\.Modded).*\b(?:write|copy|move|rename)\w*\b.*$'
)
foreach ($pattern in $forbiddenProductionPatterns) {
    if ($production -match $pattern) {
        throw "Production contains a vanilla-to-modded seeding/copy signature: $($Matches[0])"
    }
}

foreach ($text in @($allowlist, $manualTransfer, $automaticManifest)) {
    if ($text -match '(?i)modded/profile\.save') {
        throw "Transfer code treats legacy modded/profile.save as transferable."
    }
}

$requiredAllowlistPatterns = @(
    'SharedProfilePath\s*=\s*"profile\.save"',
    '(?s)string\.Equals\(\s*canonical,\s*SharedProfilePath,.*?\)\s*\{\s*return true;',
    'saveNamespace\s*==\s*SaveNamespace\.Modded\s*\?\s*"modded/"\s*:\s*""'
)
foreach ($pattern in $requiredAllowlistPatterns) {
    if ($allowlist -notmatch $pattern) {
        throw "Required explicit save-namespace allowlist rule is missing: $pattern"
    }
}

foreach ($entry in @(
    @{ Label = "manual transfer"; Text = $manualTransfer },
    @{ Label = "automatic snapshot"; Text = $automaticManifest }
)) {
    if ($entry.Text -notmatch 'SaveTransferAllowlist\.SharedProfilePath') {
        throw "$($entry.Label) no longer includes the intentionally shared root profile.save."
    }
    if ($entry.Text -notmatch '(?s)SaveTransferAllowlist\.SaveDirectory\(\s*context\.Namespace,\s*profileId\s*\)') {
        throw "$($entry.Label) no longer derives profile paths from the selected namespace."
    }
}

Write-Host "PASS removed vanilla-to-modded seeding sources remain absent"
Write-Host "PASS production contains no vanilla-to-modded mutation signature"
Write-Host "PASS vanilla and modded profile paths derive from the selected namespace"
Write-Host "PASS root profile.save remains intentionally shared"
Write-Host "Save namespace isolation guardrail passed 4/4."
