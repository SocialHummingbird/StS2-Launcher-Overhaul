param(
    [Parameter(Mandatory)]
    [ValidateSet('Start','Launch','Status','Pull','ForceStop','Snapshot','Compare','Cleanup')]
    [string]$Action,
    [ValidatePattern('^[a-zA-Z0-9][a-zA-Z0-9-]{0,79}$')]
    [string]$RunId = 'fresh',
    [ValidateSet('download','copied-update','fixture','local-fixture','cdn-diagnostics','sandbox-ui','hold')]
    [string]$Operation = 'download',
    [ValidateSet('default','akamai')]
    [string]$Provider = 'default',
    [ValidatePattern('^[a-zA-Z0-9_-]+$')]
    [string]$Branch = 'public',
    [ValidateSet('fresh','reuse','pck-repair')]
    [string]$Scenario = 'fresh',
    [ValidatePattern('^$|^[a-zA-Z0-9][a-zA-Z0-9-]{0,79}$')]
    [string]$SourceRunId = '',
    [ValidateRange(0, 1099511627776)]
    [long]$CancelAfterBytes = 0,
    [ValidatePattern('^[a-zA-Z0-9][a-zA-Z0-9-]{0,79}$')]
    [string]$Name = 'after',
    [string]$EvidenceDirectory = "",
    [string]$Adb = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')"
)
$ErrorActionPreference = 'Stop'
$serial = 'R3GL70DKNKB'
$package = 'com.sts2launcher.overhaul.fork.local'
$component = 'com.sts2launcher.downloadvalidation/com.sts2launcher.downloadvalidation.Preservation'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$local = if ($EvidenceDirectory) { [IO.Path]::GetFullPath($EvidenceDirectory) } else { Join-Path $repo 'tmp/chunk-download-work/device' }
$ownedRoot = [IO.Path]::GetFullPath((Join-Path $repo 'tmp')) + [IO.Path]::DirectorySeparatorChar
if (-not $local.StartsWith($ownedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Device evidence must stay under repository tmp.' }
$remote = "/storage/emulated/0/Android/data/$package/files/download-validation-evidence"
function Device([string[]]$values) {
    $output = & $Adb -s $serial @values 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Fold-only adb action failed: $($values[0]) $($values[1])" }
    return ($output -join "`n")
}
if ($Action -eq 'Compare') {
    $before = Get-Content -LiteralPath "$local/private/before/inventory.json" -Raw | ConvertFrom-Json
    $after = Get-Content -LiteralPath "$local/private/$Name/inventory.json" -Raw | ConvertFrom-Json
    $expected = @{}
    $actual = @{}
    foreach ($entry in $before.inventory) { if ($entry.kind -ne 'other') { $expected[$entry.path] = $entry } }
    foreach ($entry in $after.inventory) { if ($entry.kind -ne 'other') { $actual[$entry.path] = $entry } }
    $differences = @(
        foreach ($path in $expected.Keys) {
            if (-not $actual.ContainsKey($path)) { [pscustomobject]@{path=$path;kind=$expected[$path].kind;change='removed'} }
            elseif ($expected[$path].size -ne $actual[$path].size -or $expected[$path].sha256 -ne $actual[$path].sha256) {
                [pscustomobject]@{path=$path;kind=$expected[$path].kind;change='content changed'}
            }
        }
        foreach ($path in $actual.Keys) {
            if (-not $expected.ContainsKey($path)) { [pscustomobject]@{path=$path;kind=$actual[$path].kind;change='added'} }
        }
    )
    function PreferenceValues([string]$zipPath, [string]$entryName = 'private/shared_prefs/sts2mobile.xml', [string[]]$runtimeKeys = @('installed_version_code','assembly_cache_runtime_id')) {
        $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
        try {
            $entry = $zip.GetEntry($entryName)
            if (-not $entry) { throw 'Expected preference backup entry is missing.' }
            $reader = [IO.StreamReader]::new($entry.Open())
            try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $values = @{}
            foreach ($child in $xml.map.ChildNodes) {
                $runtimeMetadata = $child.name -in $runtimeKeys
                if ($entryName -eq 'private/shared_prefs/android.app.ActivityThread.IDS.xml' -and $child.name -eq 'IDSCount' -and $child.LocalName -ne 'int') {
                    $runtimeMetadata = $false
                }
                if ($child.NodeType -eq [Xml.XmlNodeType]::Element -and -not $runtimeMetadata) {
                    $values[$child.name] = $child.OuterXml
                }
            }
            return $values
        } finally { $zip.Dispose() }
    }
    $oldPreferences = PreferenceValues "$local/private/before/snapshot.zip"
    $newPreferences = PreferenceValues "$local/private/$Name/snapshot.zip"
    $changedUserKeys = @(
        foreach ($key in $oldPreferences.Keys) { if (-not $newPreferences.ContainsKey($key) -or $oldPreferences[$key] -ne $newPreferences[$key]) { $key } }
        foreach ($key in $newPreferences.Keys) { if (-not $oldPreferences.ContainsKey($key)) { $key } }
    )
    # Fold framework.jar android.app.IdsController owns this exact observation counter.
    # Retain its raw file difference and compare any other typed fields in the same file.
    $oldFrameworkPreferences = PreferenceValues "$local/private/before/snapshot.zip" 'private/shared_prefs/android.app.ActivityThread.IDS.xml' @('IDSCount')
    $newFrameworkPreferences = PreferenceValues "$local/private/$Name/snapshot.zip" 'private/shared_prefs/android.app.ActivityThread.IDS.xml' @('IDSCount')
    $changedFrameworkUserKeys = @(
        foreach ($key in $oldFrameworkPreferences.Keys) { if (-not $newFrameworkPreferences.ContainsKey($key) -or $oldFrameworkPreferences[$key] -ne $newFrameworkPreferences[$key]) { $key } }
        foreach ($key in $newFrameworkPreferences.Keys) { if (-not $oldFrameworkPreferences.ContainsKey($key)) { $key } }
    )
    $materialDifferences = @($differences | Where-Object {
        $_.path -ne 'private/files/profileInstalled' -and -not (
            $_.path -eq 'private/shared_prefs/sts2mobile.xml' -and $_.change -eq 'content changed' -and $changedUserKeys.Count -eq 0) -and -not (
            $_.path -eq 'private/shared_prefs/android.app.ActivityThread.IDS.xml' -and $_.change -eq 'content changed' -and $changedFrameworkUserKeys.Count -eq 0)
    })
    $summary = [ordered]@{
        beforeProtected=$expected.Count; afterProtected=$actual.Count; differences=$differences.Count
        semanticUserDataDifferences=$materialDifferences.Count
        expectedRuntimeMetadataDifferences=($differences.Count - $materialDifferences.Count)
        userPreferenceKeysChecked=$oldPreferences.Count
        userPreferenceKeysChanged=$changedUserKeys.Count
        otherFrameworkPreferenceKeysChanged=$changedFrameworkUserKeys.Count
        credentialUnchanged=(@($differences | Where-Object kind -eq credential).Count -eq 0)
        installedVersionsUnchanged=(@($differences | Where-Object kind -eq installed).Count -eq 0)
        savesAndUserPreferencesUnchanged=(@($materialDifferences | Where-Object kind -eq preserved).Count -eq 0)
    }
    $differences | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$local/private/$Name/preservation-differences.json" -Encoding utf8
    $summary | ConvertTo-Json | Tee-Object -FilePath "$local/preservation-$Name.json"
    if ($materialDifferences.Count) { throw 'Preservation comparison found changed user data; inspect private evidence before any restoration.' }
    return
}
if ((Device @('get-state')).Trim() -ne 'device' -or (Device @('shell','getprop','ro.product.model')).Trim() -ne 'SM-F971B') {
    throw 'The authorised Fold is not available.'
}
if ($Action -eq 'Start' -or $Action -eq 'Launch') {
    $identity = Device @('shell','dumpsys','package',$package)
    if ($identity -notmatch '(?m)^\s*versionName=\d+\.\d+\.\d+-downloads-validation[0-9]*\s*$') {
        throw 'Refusing to launch a package without the explicit downloads-validation version name.'
    }
}
switch ($Action) {
    'Start' {
        $null = Device @('shell','am','force-stop',$package)
        $request = Device @('shell','am','instrument','-w','-r','-e','action','request','-e','runId',$RunId,'-e','operation',$Operation,'-e','provider',$Provider,'-e','branch',$Branch,'-e','scenario',$Scenario,'-e','sourceRunId',($SourceRunId -replace '^$','none'),'-e','cancelAfterBytes',"$CancelAfterBytes",$component)
        if ($request -notmatch 'INSTRUMENTATION_CODE: 0') { throw 'Private validation request was not prepared.' }
        $request | Set-Content -LiteralPath "$local/$RunId-request.txt" -Encoding utf8
        Device @('shell','am','start','-W','-a','android.intent.action.MAIN','-c','android.intent.category.LAUNCHER','-n',"$package/com.game.sts2launcher.LauncherActivity")
    }
    'Launch' {
        Device @('shell','am','start','-W','-a','android.intent.action.MAIN','-c','android.intent.category.LAUNCHER','-n',"$package/com.game.sts2launcher.LauncherActivity")
    }
    'Status' {
        foreach ($filename in @('result.json','progress.json','depot-progress.json','chunk-summary.json','transfer.json')) {
            $path = "$remote/runs/$RunId/$filename"
            $content = & $Adb -s $serial shell cat $path 2>$null
            if ($LASTEXITCODE -eq 0) { $content }
        }
    }
    'Pull' {
        New-Item -ItemType Directory -Force "$local/results/$Name" | Out-Null
        Device @('pull',"$remote/runs/$RunId","$local/results/$Name/$RunId")
    }
    'ForceStop' { Device @('shell','am','force-stop',$package) }
    'Cleanup' {
        if (-not (Test-Path -LiteralPath "$local/private/before/snapshot.zip") -or -not (Test-Path -LiteralPath "$local/preservation-after.json")) {
            throw 'Refusing device evidence cleanup before local backup and final preservation comparison exist.'
        }
        $comparison = Get-Content -LiteralPath "$local/preservation-after.json" -Raw | ConvertFrom-Json
        if ($comparison.semanticUserDataDifferences -ne 0 -or -not $comparison.credentialUnchanged -or -not $comparison.installedVersionsUnchanged -or -not $comparison.savesAndUserPreferencesUnchanged) {
            throw 'Refusing test cleanup while user data, credentials or installed-version differences remain.'
        }
        $null = Device @('shell','am','force-stop',$package)
        $cleanup = Device @('shell','am','instrument','-w','-r','-e','action','cleanup',$component)
        if ($cleanup -notmatch 'INSTRUMENTATION_CODE: 0') { throw 'Test-owned device cleanup failed.' }
        $cleanup | Set-Content -LiteralPath "$local/cleanup.txt" -Encoding utf8
        $cleanup
    }
    'Snapshot' {
        $null = Device @('shell','am','force-stop',$package)
        $snapshot = Device @('shell','am','instrument','-w','-r','-e','action','snapshot','-e','name',$Name,$component)
        if ($snapshot -notmatch 'INSTRUMENTATION_CODE: 0') { throw 'Preservation snapshot did not complete.' }
        $snapshot | Set-Content -LiteralPath "$local/$Name-instrumentation.txt" -Encoding utf8
        New-Item -ItemType Directory -Force "$local/private" | Out-Null
        Device @('pull',"$remote/$Name","$local/private/$Name")
        $snapshot
    }
}
