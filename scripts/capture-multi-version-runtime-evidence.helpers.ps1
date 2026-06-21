function Invoke-AdbText {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$AllowFailure
    )

    $prefix = @()
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $prefix += @("-s", $DeviceSerial)
    }

    $output = & $AdbPath @prefix @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | Out-String).TrimEnd()
    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "adb $($Arguments -join ' ') failed with exit code $exitCode`: $text"
    }

    return $text
}

function Invoke-RunAsText {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [switch]$AllowFailure
    )

    $quotedCommand = ConvertTo-AndroidShellSingleQuoted $Command
    $remoteCommand = "run-as $PackageName sh -c $quotedCommand"
    return Invoke-AdbText `
        -Arguments @("shell", $remoteCommand) `
        -AllowFailure:$AllowFailure
}

function Save-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [AllowNull()][string]$Text
    )

    if ($null -eq $Text) {
        $Text = ""
    }

    Set-Content -LiteralPath $Path -Value $Text -Encoding UTF8
}

function Save-AdbText {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$AllowFailure
    )

    Save-Text -Path $Path -Text (Invoke-AdbText -Arguments $Arguments -AllowFailure:$AllowFailure)
}

function Save-RunAsText {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Command,
        [switch]$AllowFailure
    )

    Save-Text -Path $Path -Text (Invoke-RunAsText -Command $Command -AllowFailure:$AllowFailure)
}

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    try {
        return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    } catch {
        return $null
    }
}

function Read-DeviceSha256 {
    param([AllowNull()][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or $Path.StartsWith("<")) {
        return "<missing>"
    }

    $quoted = ConvertTo-AndroidShellPathSingleQuoted -Value $Path
    $text = Invoke-RunAsText -Command "sha256sum $quoted 2>/dev/null | cut -d ' ' -f 1" -AllowFailure
    if ([string]::IsNullOrWhiteSpace($text)) {
        return "<missing>"
    }

    return (($text -split "`r?`n")[0]).Trim()
}

function Read-DeviceRuntimePackDllNames {
    param([AllowNull()][string]$Directory)

    if ([string]::IsNullOrWhiteSpace($Directory) -or $Directory.StartsWith("<")) {
        return @()
    }

    $quoted = ConvertTo-AndroidShellPathSingleQuoted -Value $Directory
    $command = "for f in $quoted/*.dll; do [ -f `"`$f`" ] && basename `"`$f`"; done | sort"
    $text = Invoke-RunAsText -Command $command -AllowFailure
    if ([string]::IsNullOrWhiteSpace($text)) {
        return @()
    }

    return @($text -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-ObjectPropertyMap {
    param([AllowNull()]$Object)

    $map = @{}
    if ($null -eq $Object) {
        return $map
    }

    foreach ($property in $Object.PSObject.Properties) {
        $map[$property.Name] = "$($property.Value)"
    }
    return $map
}

function ConvertTo-EvidenceString {
    param([AllowNull()]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return "$Value"
}

function Test-EvidenceTrue {
    param([AllowNull()]$Value)

    $text = ConvertTo-EvidenceString -Value $Value
    return $text -eq "True" -or $text -eq "true"
}

function Compare-InstalledRuntimeEvidence {
    param(
        [AllowNull()]$RuntimeSlotEvidence,
        [AllowNull()]$RuntimeValidation,
        [AllowNull()]$RuntimePackManifest,
        [AllowNull()][string]$RuntimeCachePck
    )

    if ($null -eq $RuntimeSlotEvidence -or $null -eq $RuntimeValidation) {
        return [pscustomobject]@{
            Matched = $false
            InstalledRuntimeSlotId = ""
            ValidatedRuntimeSlotId = ""
            InstalledBranch = ""
            ValidatedBranch = ""
            InstalledPck = ""
            ValidatedPck = ""
            InstalledSourceAssembly = ""
            ValidatedSourceAssembly = ""
            RuntimePackSourcePck = ""
            RuntimePackSourceSlot = ""
            SlotMatchesRuntimeValidation = $false
            SlotMatchesRuntimePackSource = $false
            PckMatchesRuntimeValidation = $false
            PckMatchesRuntimePackSource = $false
            RuntimeValidationPckMatchesCache = $false
        }
    }

    $installedRuntimeSlotId = ConvertTo-EvidenceString -Value $RuntimeSlotEvidence.runtimeSlotId
    $validatedRuntimeSlotId = ConvertTo-EvidenceString -Value $RuntimeValidation.runtimeSlotId
    $installedBranch = ConvertTo-EvidenceString -Value $RuntimeSlotEvidence.branch
    $validatedBranch = ConvertTo-EvidenceString -Value $RuntimeValidation.selectedBranch
    $installedPck = ConvertTo-EvidenceString -Value $RuntimeSlotEvidence.pckSha256
    $validatedPck = ConvertTo-EvidenceString -Value $RuntimeValidation.selectedPckSha256
    $installedSourceAssembly = ConvertTo-EvidenceString -Value $RuntimeSlotEvidence.sourceAssemblySha256
    $validatedSourceAssembly = ConvertTo-EvidenceString -Value $RuntimeValidation.selectedSourceAssemblySha256
    $runtimePackSourcePck = if ($null -ne $RuntimePackManifest) { ConvertTo-EvidenceString -Value $RuntimePackManifest.sourcePckSha256 } else { "" }
    $runtimePackSourceSlot = if ($null -ne $RuntimePackManifest) { ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceRuntimeSlotId } else { "" }
    $slotMatchesRuntimeValidation = $installedRuntimeSlotId -eq $validatedRuntimeSlotId
    $slotMatchesRuntimePackSource = -not [string]::IsNullOrWhiteSpace($runtimePackSourceSlot) -and $installedRuntimeSlotId -eq $runtimePackSourceSlot
    $pckMatchesRuntimeValidation = $installedPck -eq $validatedPck
    $pckMatchesRuntimePackSource = -not [string]::IsNullOrWhiteSpace($runtimePackSourcePck) -and $installedPck -eq $runtimePackSourcePck
    $runtimeValidationPckMatchesCache = -not [string]::IsNullOrWhiteSpace($RuntimeCachePck) -and $validatedPck -eq $RuntimeCachePck
    $matched =
        -not [string]::IsNullOrWhiteSpace($installedRuntimeSlotId) -and
        $installedBranch -eq $validatedBranch -and
        $installedSourceAssembly -eq $validatedSourceAssembly -and
        ($slotMatchesRuntimeValidation -or $slotMatchesRuntimePackSource) -and
        ($pckMatchesRuntimeValidation -or ($pckMatchesRuntimePackSource -and $runtimeValidationPckMatchesCache))

    return [pscustomobject]@{
        Matched = $matched
        InstalledRuntimeSlotId = $installedRuntimeSlotId
        ValidatedRuntimeSlotId = $validatedRuntimeSlotId
        InstalledBranch = $installedBranch
        ValidatedBranch = $validatedBranch
        InstalledPck = $installedPck
        ValidatedPck = $validatedPck
        InstalledSourceAssembly = $installedSourceAssembly
        ValidatedSourceAssembly = $validatedSourceAssembly
        RuntimePackSourcePck = $runtimePackSourcePck
        RuntimePackSourceSlot = $runtimePackSourceSlot
        SlotMatchesRuntimeValidation = $slotMatchesRuntimeValidation
        SlotMatchesRuntimePackSource = $slotMatchesRuntimePackSource
        PckMatchesRuntimeValidation = $pckMatchesRuntimeValidation
        PckMatchesRuntimePackSource = $pckMatchesRuntimePackSource
        RuntimeValidationPckMatchesCache = $runtimeValidationPckMatchesCache
    }
}

function Test-RuntimeCacheMatchesValidation {
    param(
        [AllowNull()]$RuntimeValidation,
        [AllowNull()][string]$RuntimeCacheBranch,
        [AllowNull()][string]$RuntimeCachePck,
        [AllowNull()][string]$RuntimeCacheSelectedSource,
        [AllowNull()][string]$RuntimeCachePublishAssembly
    )

    return $null -ne $RuntimeValidation -and
        (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedBranch) -eq $RuntimeCacheBranch -and
        (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedPckSha256) -eq $RuntimeCachePck -and
        (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedSourceAssemblySha256) -eq $RuntimeCacheSelectedSource -and
        (ConvertTo-EvidenceString -Value $RuntimeValidation.activeAndroidAssemblySha256) -eq $RuntimeCachePublishAssembly
}

function Compare-SaveOriginEvidence {
    param(
        [AllowNull()]$RuntimeValidation,
        [AllowNull()]$RuntimePackManifest,
        [AllowNull()][string]$RuntimeCachePck,
        [AllowNull()][string]$SaveOriginRuntimeSlotId,
        [AllowNull()][string]$SaveOriginPck,
        [AllowNull()][string]$SaveOriginSourceAssembly,
        [AllowNull()][string]$SaveOriginRuntimePlayable,
        [AllowNull()][string]$SaveOriginRuntimeVerified
    )

    if ($null -eq $RuntimeValidation) {
        return [pscustomobject]@{
            Matched = $false
            IdentityMatches = $false
            PckMatchesRuntime = $false
            RuntimeValidationPckMatchesCache = $false
            PckMatchesRuntimePackSource = $false
            Playable = $false
            Verified = $false
            RuntimePackSourcePck = ""
        }
    }

    $runtimePackSourcePck = if ($null -ne $RuntimePackManifest) { ConvertTo-EvidenceString -Value $RuntimePackManifest.sourcePckSha256 } else { "" }
    $pckMatchesRuntime = $SaveOriginPck -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedPckSha256)
    $runtimeValidationPckMatchesCache = -not [string]::IsNullOrWhiteSpace($RuntimeCachePck) -and (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedPckSha256) -eq $RuntimeCachePck
    $pckMatchesRuntimePackSource =
        -not [string]::IsNullOrWhiteSpace($runtimePackSourcePck) -and
        $SaveOriginPck -eq $runtimePackSourcePck -and
        $runtimeValidationPckMatchesCache
    $identityMatches =
        $SaveOriginRuntimeSlotId -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.runtimeSlotId) -and
        ($pckMatchesRuntime -or $pckMatchesRuntimePackSource) -and
        $SaveOriginSourceAssembly -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedSourceAssemblySha256)
    $playable = $SaveOriginRuntimePlayable -eq "true"
    $verified = $SaveOriginRuntimeVerified -eq "true"

    return [pscustomobject]@{
        Matched = $identityMatches -and $playable -and $verified
        IdentityMatches = $identityMatches
        PckMatchesRuntime = $pckMatchesRuntime
        RuntimeValidationPckMatchesCache = $runtimeValidationPckMatchesCache
        PckMatchesRuntimePackSource = $pckMatchesRuntimePackSource
        Playable = $playable
        Verified = $verified
        RuntimePackSourcePck = $runtimePackSourcePck
    }
}

function Compare-RuntimePackEvidence {
    param(
        [AllowNull()]$RuntimePackManifest,
        [AllowNull()]$RuntimeValidation,
        [AllowNull()]$RuntimeSlotEvidence,
        [AllowNull()]$RuntimePackValidation,
        [AllowNull()]$RuntimePackClosedDllSet,
        [AllowNull()][string]$RuntimeCachePck
    )

    if ($null -eq $RuntimePackManifest -or $null -eq $RuntimeValidation) {
        return [pscustomobject]@{
            Matched = $false
            SlotMatchesRuntimeValidation = $false
            SlotMatchesInstalledSlot = $false
            SlotMatches = $false
            PckMatchesRuntimeValidation = $false
            PckMatchesInstalledSource = $false
            PckMatchesSelectedCache = $false
            PckMatches = $false
            SourceMatches = $false
            AndroidMatches = $false
            PatchPassed = $false
            Clean = $false
            ClosedDllSet = $false
            ReportMatches = $false
        }
    }

    $slotMatchesRuntimeValidation = (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceRuntimeSlotId) -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.runtimeSlotId)
    $slotMatchesInstalledSlot = $null -ne $RuntimeSlotEvidence -and (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceRuntimeSlotId) -eq (ConvertTo-EvidenceString -Value $RuntimeSlotEvidence.runtimeSlotId)
    $slotMatches = $slotMatchesRuntimeValidation -or $slotMatchesInstalledSlot
    $pckMatchesRuntimeValidation = (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourcePckSha256) -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedPckSha256)
    $pckMatchesInstalledSource = $null -ne $RuntimeSlotEvidence -and (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourcePckSha256) -eq (ConvertTo-EvidenceString -Value $RuntimeSlotEvidence.pckSha256)
    $pckMatchesSelectedCache = (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedPckSha256) -eq $RuntimeCachePck
    $pckMatches = $pckMatchesRuntimeValidation -or ($pckMatchesInstalledSource -and $pckMatchesSelectedCache)
    $sourceMatches = (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceAssemblySha256) -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.selectedSourceAssemblySha256)
    $androidMatches = (ConvertTo-EvidenceString -Value $RuntimePackManifest.androidAssemblySha256) -eq (ConvertTo-EvidenceString -Value $RuntimeValidation.activeAndroidAssemblySha256)
    $patchPassed = (ConvertTo-EvidenceString -Value $RuntimePackManifest.patchValidationStatus) -eq "passed"
    $clean = Test-EvidenceTrue -Value $RuntimePackManifest.generatedFromCleanDirectory
    $closedDllSet = $RuntimePackClosedDllSet -and $RuntimePackClosedDllSet.Matched
    $reportMatches = $false
    if ($null -ne $RuntimePackValidation) {
        $reportMatches =
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.status) -eq "passed" -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.runtimePackId) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.packId) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.sourceRuntimeSlotId) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceRuntimeSlotId) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.branch) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceBranch) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.pckSha256) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourcePckSha256) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.sourceAssemblySha256) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.sourceAssemblySha256) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.androidAssemblySha256) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.androidAssemblySha256) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.patchSetVersion) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.patchSetVersion) -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.validationSurfaceVersion) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.validationSurfaceVersion) -and
            "$(ConvertTo-Json -Compress $RuntimePackValidation.supportAssemblies)" -eq "$(ConvertTo-Json -Compress $RuntimePackManifest.supportAssemblies)" -and
            "$(ConvertTo-Json -Compress $RuntimePackValidation.supportAssemblySha256)" -eq "$(ConvertTo-Json -Compress $RuntimePackManifest.supportAssemblySha256)" -and
            (ConvertTo-EvidenceString -Value $RuntimePackValidation.generatedFromCleanDirectory) -eq (ConvertTo-EvidenceString -Value $RuntimePackManifest.generatedFromCleanDirectory)
    }

    return [pscustomobject]@{
        Matched = $slotMatches -and $pckMatches -and $sourceMatches -and $androidMatches -and $patchPassed -and $clean -and $closedDllSet -and $reportMatches
        SlotMatchesRuntimeValidation = $slotMatchesRuntimeValidation
        SlotMatchesInstalledSlot = $slotMatchesInstalledSlot
        SlotMatches = $slotMatches
        PckMatchesRuntimeValidation = $pckMatchesRuntimeValidation
        PckMatchesInstalledSource = $pckMatchesInstalledSource
        PckMatchesSelectedCache = $pckMatchesSelectedCache
        PckMatches = $pckMatches
        SourceMatches = $sourceMatches
        AndroidMatches = $androidMatches
        PatchPassed = $patchPassed
        Clean = $clean
        ClosedDllSet = $closedDllSet
        ReportMatches = $reportMatches
    }
}

function Test-RuntimePackClosedDllSet {
    param(
        [AllowNull()]$Manifest,
        [AllowNull()][string]$Directory
    )

    if ($null -eq $Manifest -or [string]::IsNullOrWhiteSpace($Directory) -or $Directory.StartsWith("<")) {
        return [pscustomobject]@{
            Matched = $false
            Status = "missing"
            Evidence = "runtime pack manifest or directory missing"
        }
    }

    $supportAssemblies = @()
    if ($null -ne $Manifest.supportAssemblies) {
        $supportAssemblies = @($Manifest.supportAssemblies | ForEach-Object { "$_" } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
    $supportAssemblySha256 = Get-ObjectPropertyMap -Object $Manifest.supportAssemblySha256
    $expectedDlls = @("sts2.dll") + $supportAssemblies
    $actualDlls = Read-DeviceRuntimePackDllNames -Directory $Directory
    $expectedLookup = @{}
    foreach ($dll in $expectedDlls) {
        $expectedLookup[$dll.ToLowerInvariant()] = $dll
    }
    $actualLookup = @{}
    foreach ($dll in $actualDlls) {
        $actualLookup[$dll.ToLowerInvariant()] = $dll
    }

    $missingDlls = @($expectedDlls | Where-Object { -not $actualLookup.ContainsKey($_.ToLowerInvariant()) })
    $undeclaredDlls = @($actualDlls | Where-Object { -not $expectedLookup.ContainsKey($_.ToLowerInvariant()) })
    $missingHashes = @()
    $hashMismatches = @()
    $extraHashes = @()
    foreach ($supportAssembly in $supportAssemblies) {
        if (-not $supportAssemblySha256.ContainsKey($supportAssembly)) {
            $missingHashes += $supportAssembly
            continue
        }

        $actualSha256 = Read-DeviceSha256 -Path "$Directory/$supportAssembly"
        if ($actualSha256 -ne $supportAssemblySha256[$supportAssembly]) {
            $hashMismatches += "$supportAssembly(manifest=$($supportAssemblySha256[$supportAssembly]),actual=$actualSha256)"
        }
    }
    foreach ($hashName in $supportAssemblySha256.Keys) {
        if (-not ($supportAssemblies | Where-Object { $_ -ieq $hashName })) {
            $extraHashes += $hashName
        }
    }

    $matched = $missingDlls.Count -eq 0 -and $undeclaredDlls.Count -eq 0 -and $missingHashes.Count -eq 0 -and $hashMismatches.Count -eq 0 -and $extraHashes.Count -eq 0
    return [pscustomobject]@{
        Matched = $matched
        Status = $(if ($matched) { "closed" } else { "mismatch" })
        Evidence = "expectedDlls=$($expectedDlls -join ','); actualDlls=$($actualDlls -join ','); missingDlls=$($missingDlls -join ','); undeclaredDlls=$($undeclaredDlls -join ','); missingHashes=$($missingHashes -join ','); hashMismatches=$($hashMismatches -join ','); extraHashes=$($extraHashes -join ',')"
    }
}
