param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir,
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$Branch = "public-beta",
    [string]$DeviceSerial = "",
    [string]$AdbPath = "adb",
    [switch]$ReviewSummary,
    [switch]$FailOnNotReady
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot/android-adb-utils.ps1"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Resolve-RepoPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $root $normalized
}

function ConvertTo-SafeFileName([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "empty"
    }

    return ($Value -replace '[^A-Za-z0-9._-]', '_').Trim('_')
}

function ConvertTo-AndroidShellSingleQuoted([string]$Value) {
    return "'" + (($Value -split "'") -join "'\''") + "'"
}

function Invoke-RunAsText([string]$Command, [switch]$AllowFailure) {
    $quotedCommand = ConvertTo-AndroidShellSingleQuoted $Command
    $args = @("shell", "run-as $PackageName sh -c $quotedCommand")
    $output = Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $script:ResolvedDevice -Arguments $args
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "adb run-as command failed with exit code $exitCode`: $Command`n$($output -join [Environment]::NewLine)"
    }

    return ($output -join [Environment]::NewLine)
}

function Write-RunAsTextFile([string]$Command, [string]$Path, [switch]$AllowFailure) {
    $text = Invoke-RunAsText -Command $Command -AllowFailure:$AllowFailure
    Set-Content -LiteralPath $Path -Value $text -Encoding UTF8
    return $text
}

function Read-BranchFromMarkerText([string]$Text) {
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line -match '^Branch:\s*(.+)$') {
            return $Matches[1].Trim()
        }
    }

    return ""
}

function Read-MarkerValueFromText([string]$Text, [string]$Prefix) {
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $line.Substring($Prefix.Length).Trim()
        }
    }

    return "<missing>"
}

function Read-MarkerIntFromText([string]$Text, [string]$Prefix) {
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $value = $line.Substring($Prefix.Length).Trim()
            $parsed = 0
            if ([int]::TryParse($value, [ref]$parsed)) {
                return $parsed
            }
        }
    }

    return $null
}

function Read-MarkerRowsFromText([string]$Text, [string]$Prefix, [int]$Limit) {
    $rows = New-Object System.Collections.Generic.List[string]
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $rows.Add($line.Trim())
            if ($rows.Count -ge $Limit) {
                break
            }
        }
    }

    if ($rows.Count -eq 0) {
        return @("<none>")
    }

    return $rows.ToArray()
}

function Read-ComparisonMetric([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match "^$([regex]::Escape($Label)):\s*(\d+)$") {
            return [int]$Matches[1]
        }
    }

    return $null
}

function Use-Int([object]$Value) {
    if ($null -eq $Value) {
        return 0
    }

    return [int]$Value
}

function Format-NullableMetric([object]$Value) {
    if ($null -eq $Value) {
        return "<missing>"
    }

    return "$Value"
}

function Get-EvidenceReadinessLines(
    [string]$Classification,
    [string]$PublicMarkerPath,
    [string]$SelectedMarkerPath,
    [bool]$RedownloadMarkerPresent,
    [bool]$RedownloadBranchMatches,
    [bool]$RedownloadClearedSelectedDirectories
) {
    if ($Classification.StartsWith("likely Steam branch availability issue", [System.StringComparison]::OrdinalIgnoreCase)) {
        return @(
            "Evidence readiness: ready for branch-availability classification",
            "Evidence missing/weak: <none for branch-availability classification>"
        )
    }

    $missing = New-Object System.Collections.Generic.List[string]
    if ([string]::IsNullOrWhiteSpace($PublicMarkerPath)) {
        $missing.Add("public/default branch marker not found")
    }

    if ([string]::IsNullOrWhiteSpace($SelectedMarkerPath)) {
        $missing.Add("selected branch marker not found")
    }

    if (-not $RedownloadMarkerPresent) {
        $missing.Add("clean-redownload marker not found")
    } elseif (-not $RedownloadBranchMatches) {
        $missing.Add("clean-redownload marker does not match investigated branch")
    } elseif (-not $RedownloadClearedSelectedDirectories) {
        $missing.Add("clean-redownload marker does not prove selected directories were cleared")
    }

    if ($Classification.StartsWith("inconclusive", [System.StringComparison]::OrdinalIgnoreCase)) {
        $missing.Add("classification is inconclusive")
    } elseif ($Classification.StartsWith("possible", [System.StringComparison]::OrdinalIgnoreCase)) {
        $missing.Add("classification is possible, not likely; inspect runtime logs and visual behavior")
    }

    if ($missing.Count -eq 0) {
        return @(
            "Evidence readiness: ready for manifest/cache/art classification",
            "Evidence missing/weak: <none>"
        )
    }

    return @(
        "Evidence readiness: not ready for final classification",
        "Evidence missing/weak: " + ($missing -join "; ")
    )
}

function Get-BetaIntegrityClassification([string]$MarkerText, [string]$SelectedMarkerPath, [string]$SelectedGameDir, [string]$ComparisonPath, [string]$BranchAvailabilityText, [string]$RedownloadText, [string]$Branch) {
    $availabilityBranch = Read-MarkerValueFromText $BranchAvailabilityText "Selected branch:"
    $availabilityVisibility = Read-MarkerValueFromText $BranchAvailabilityText "Selected branch visibility:"
    $availabilityManifestCountText = Read-MarkerValueFromText $BranchAvailabilityText "Windows depot manifests for selected branch:"
    $availabilityManifestCount = 0
    $availabilityManifestCountParsed = [int]::TryParse($availabilityManifestCountText, [ref]$availabilityManifestCount)
    $availabilityMatchesBranch = $availabilityBranch -ieq $Branch

    if ($availabilityMatchesBranch -and $availabilityVisibility -match "not listed") {
        if ([string]::IsNullOrWhiteSpace($SelectedMarkerPath)) {
            return "likely Steam branch availability issue: app-info says the investigated branch was not listed for this account and no selected branch marker was found"
        }

        return "inconclusive: app-info says the investigated branch was not listed, but a selected branch marker exists; refresh game versions, clean-redownload, and recapture before classifying"
    }

    if ($availabilityMatchesBranch -and $availabilityManifestCountParsed -and $availabilityManifestCount -le 0) {
        if ([string]::IsNullOrWhiteSpace($SelectedMarkerPath)) {
            return "likely Steam branch availability issue: app-info exposed zero Windows depot manifests for the investigated branch and no selected branch marker was found"
        }

        return "inconclusive: app-info exposed zero Windows depot manifests, but a selected branch marker exists; refresh game versions, clean-redownload, and recapture before classifying"
    }

    $redownloadBranch = Read-MarkerValueFromText $RedownloadText "Selected branch:"
    $redownloadGameAfterDelete = Read-MarkerValueFromText $RedownloadText "Game directory exists after delete:"
    $redownloadStateAfterDelete = Read-MarkerValueFromText $RedownloadText "Download state directory exists after delete:"
    if ([string]::IsNullOrWhiteSpace($RedownloadText)) {
        return "inconclusive / clean redownload not proven: last_game_version_redownload.txt was missing; clean-redownload the selected branch before classifying beta integrity"
    }

    if ($redownloadBranch -ine $Branch) {
        return "inconclusive / clean redownload not proven: redownload marker belongs to a different branch; clean-redownload the investigated branch before classifying beta integrity"
    }

    if ($redownloadGameAfterDelete -ine "false" -or $redownloadStateAfterDelete -ine "false") {
        return "inconclusive / clean redownload not proven: redownload marker does not prove selected game and download-state directories were cleared"
    }

    if ([string]::IsNullOrWhiteSpace($SelectedMarkerPath)) {
        return "inconclusive / possible launcher fallback: selected branch marker was not found; redownload the selected branch with the latest app before classifying beta integrity"
    }

    if ([string]::IsNullOrWhiteSpace($SelectedGameDir)) {
        return "inconclusive / possible launcher fallback: selected branch game directory could not be derived from the selected marker"
    }

    $matchingPublic = Read-MarkerIntFromText $MarkerText "Depot manifests matching public count:"
    $differingPublic = Read-MarkerIntFromText $MarkerText "Depot manifests differing from public count:"
    $withoutPublicComparison = Read-MarkerIntFromText $MarkerText "Depot manifests without public comparison count:"
    $inheritedPublic = Read-MarkerIntFromText $MarkerText "Depot manifests inherited from public count:"
    $missingSelected = Read-MarkerIntFromText $MarkerText "Depot manifests missing selected branch manifest count:"

    if ($null -eq $matchingPublic -or $null -eq $differingPublic -or $null -eq $withoutPublicComparison -or $null -eq $inheritedPublic -or $null -eq $missingSelected) {
        return "inconclusive / possible stale cache: selected marker lacks public-vs-selected depot integrity counters; clean-redownload the selected branch with this app build"
    }

    $sameFiles = Read-ComparisonMetric $ComparisonPath "Files present in both with identical hashes"
    $differentFiles = Read-ComparisonMetric $ComparisonPath "Files present in both but different"
    $publicOnly = Read-ComparisonMetric $ComparisonPath "Files only in public"
    $selectedOnly = Read-ComparisonMetric $ComparisonPath "Files only in selected branch"
    $artSame = Read-ComparisonMetric $ComparisonPath "Art/bundle-like files identical in both"
    $artDifferent = Read-ComparisonMetric $ComparisonPath "Art/bundle-like files present in both but different"

    if ($differingPublic -gt 0 -and ($matchingPublic -gt 0 -or $inheritedPublic -gt 0 -or $missingSelected -gt 0)) {
        return "likely Steam partial branch: marker shows branch-specific depot manifest(s) plus public-identical, public-inherited, or missing selected-branch depot evidence"
    }

    if ($inheritedPublic -gt 0 -or $missingSelected -gt 0) {
        return "likely Steam public-inherited branch content: marker shows selected branch depot(s) inherited from public or missing explicit selected-branch manifests"
    }

    if ($differingPublic -gt 0 -and $differentFiles -eq 0 -and $selectedOnly -eq 0 -and $publicOnly -eq 0) {
        return "possible stale cache or runtime remote/config behavior: marker shows branch-specific manifests, but public and selected installed inventories are identical"
    }

    if ($differingPublic -gt 0 -and ((Use-Int $differentFiles) -gt 0 -or (Use-Int $selectedOnly) -gt 0 -or (Use-Int $publicOnly) -gt 0)) {
        return "likely branch-specific installed content: marker and inventory both show selected branch differences; investigate runtime remote/config if in-game behavior still looks public"
    }

    if ($matchingPublic -gt 0 -and $differingPublic -eq 0) {
        return "likely Steam public-identical branch content: selected branch manifests all match public; public-looking art/assets are expected unless runtime config changes them"
    }

    if ((Use-Int $withoutPublicComparison) -gt 0) {
        return "inconclusive: one or more selected depots could not be compared with public manifests"
    }

    if ($null -ne $sameFiles -and $sameFiles -gt 0 -and ((Use-Int $differentFiles) -eq 0) -and ((Use-Int $selectedOnly) -eq 0) -and ((Use-Int $publicOnly) -eq 0)) {
        return "possible runtime remote/config behavior: installed public and selected inventories are identical and marker evidence does not prove branch-specific content"
    }

    return "inconclusive: captured evidence does not map cleanly to Steam partial branch, launcher fallback, stale cache, or runtime remote/config behavior"
}

function Read-Inventory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return @{}
    }

    $result = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $parts = $line -split "`t"
        if ($parts.Count -lt 3) {
            continue
        }

        $result[$parts[0]] = [pscustomobject]@{
            Path = $parts[0]
            Size = $parts[1]
            Sha256 = $parts[2]
        }
    }

    return $result
}

function Write-InventoryComparison([string]$PublicInventoryPath, [string]$SelectedInventoryPath, [string]$OutputPath) {
    $public = Read-Inventory $PublicInventoryPath
    $selected = Read-Inventory $SelectedInventoryPath
    $allPaths = @($public.Keys + $selected.Keys | Sort-Object -Unique)

    $same = 0
    $different = 0
    $publicOnly = 0
    $selectedOnly = 0
    $artSame = 0
    $artDifferent = 0
    $keyRows = New-Object System.Collections.Generic.List[string]

    foreach ($path in $allPaths) {
        $inPublic = $public.ContainsKey($path)
        $inSelected = $selected.ContainsKey($path)
        $isArtOrBundle = $path -match '(?i)\.(pck|png|jpg|jpeg|webp|atlas|json|ttf|otf|wav|ogg|mp3)$'

        if ($inPublic -and $inSelected) {
            if ($public[$path].Sha256 -eq $selected[$path].Sha256 -and $public[$path].Size -eq $selected[$path].Size) {
                $same++
                if ($isArtOrBundle) {
                    $artSame++
                }
            } else {
                $different++
                if ($isArtOrBundle) {
                    $artDifferent++
                }
                if ($keyRows.Count -lt 200) {
                    $keyRows.Add("different`t$path`tpublicSize=$($public[$path].Size)`tselectedSize=$($selected[$path].Size)`tpublicSha256=$($public[$path].Sha256)`tselectedSha256=$($selected[$path].Sha256)")
                }
            }
            continue
        }

        if ($inPublic) {
            $publicOnly++
            if ($keyRows.Count -lt 200) {
                $keyRows.Add("public-only`t$path`tpublicSize=$($public[$path].Size)`tpublicSha256=$($public[$path].Sha256)")
            }
            continue
        }

        $selectedOnly++
        if ($keyRows.Count -lt 200) {
            $keyRows.Add("selected-only`t$path`tselectedSize=$($selected[$path].Size)`tselectedSha256=$($selected[$path].Sha256)")
        }
    }

    $lines = @(
        "Steam beta branch file inventory comparison",
        "Public inventory: $PublicInventoryPath",
        "Selected inventory: $SelectedInventoryPath",
        "Total public files: $($public.Count)",
        "Total selected files: $($selected.Count)",
        "Files identical in both: $same",
        "Files present in both but different: $different",
        "Files only in public: $publicOnly",
        "Files only in selected branch: $selectedOnly",
        "Art/bundle-like files identical in both: $artSame",
        "Art/bundle-like files present in both but different: $artDifferent",
        "",
        "First 200 differing/only file rows:",
        "state`tpath`tdetails"
    ) + $keyRows

    Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
}

function Test-KeyAssetPath([string]$Path) {
    $normalized = ($Path -replace '\\', '/').ToLowerInvariant()
    return $normalized -eq "slaythespire2.pck" `
        -or $normalized -match '(^|/)(art|assets|asset|sprites|textures|texture|audio|sounds|music|fonts|localization|data|json)(/|$)' `
        -or $normalized -match '\.(pck|pack|pak|bundle|bank|png|jpg|jpeg|webp|ogg|wav|mp3|json|csv|txt|ttf|otf|fnt)$'
}

function Write-KeyAssetComparison([string]$PublicInventoryPath, [string]$SelectedInventoryPath, [string]$OutputPath) {
    $public = Read-Inventory $PublicInventoryPath
    $selected = Read-Inventory $SelectedInventoryPath
    $paths = @{}

    foreach ($path in $public.Keys) {
        if (Test-KeyAssetPath $path) {
            $paths[$path] = $true
        }
    }

    foreach ($path in $selected.Keys) {
        if (Test-KeyAssetPath $path) {
            $paths[$path] = $true
        }
    }

    $rows = New-Object System.Collections.Generic.List[string]
    foreach ($path in ($paths.Keys | Sort-Object)) {
        $publicItem = $public[$path]
        $selectedItem = $selected[$path]
        if ($null -eq $publicItem) {
            $state = "selected-only"
            $publicHash = "<missing>"
            $selectedHash = $selectedItem.Sha256
        } elseif ($null -eq $selectedItem) {
            $state = "public-only"
            $publicHash = $publicItem.Sha256
            $selectedHash = "<missing>"
        } elseif ($publicItem.Sha256 -eq $selectedItem.Sha256) {
            $state = "same"
            $publicHash = $publicItem.Sha256
            $selectedHash = $selectedItem.Sha256
        } else {
            $state = "different"
            $publicHash = $publicItem.Sha256
            $selectedHash = $selectedItem.Sha256
        }

        $rows.Add("$state`t$path`t$publicHash`t$selectedHash")
    }

    $lines = @(
        "Public vs selected key asset comparison",
        "state`tpath`tpublic_sha256`tselected_sha256"
    ) + $rows

    Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
}

function Read-ChangedKeyAssetRows([string]$Path, [int]$Limit) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return @("<missing key asset comparison>")
    }

    $rows = New-Object System.Collections.Generic.List[string]
    foreach ($line in Get-Content -LiteralPath $Path) {
        $parts = $line -split "`t"
        if ($parts.Count -gt 0 -and @("different", "public-only", "selected-only") -contains $parts[0]) {
            $rows.Add($line)
            if ($rows.Count -ge $Limit) {
                break
            }
        }
    }

    if ($rows.Count -eq 0) {
        return @("<none>")
    }

    return $rows.ToArray()
}

function Write-FocusedLogcatEvidence([string]$OutputPath) {
    $header = @(
        "Focused beta-integrity logcat evidence",
        "Best-effort filtered logcat only; manually review before sharing publicly.",
        "Filters target selected branch routing, branch markers, manifest provenance, fallback, and public-inherited evidence.",
        ""
    )

    try {
        $logcat = Invoke-AndroidAdbCapture `
            -AdbPath $AdbPath `
            -DeviceSerial $script:ResolvedDevice `
            -Arguments @("logcat", "-d", "-v", "time", "-t", "2000")
        $pattern = "Steam branch|branch marker|manifestSource|manifestRequestBranch|public-inherited|selectedMatchesPublic|effectiveMatchesPublic|fallback|Resolved startup game directory|Selected Steam branch|Blocking selected game version startup|Depot manifest|public-beta|game_versions|SlayTheSpire2\.pck"
        $filtered = @($logcat | Where-Object { $_ -match $pattern })
        if ($filtered.Count -eq 0) {
            $filtered = @("<no focused beta-integrity logcat lines matched>")
        }

        Set-Content -LiteralPath $OutputPath -Value ($header + $filtered) -Encoding UTF8
    } catch {
        Set-Content -LiteralPath $OutputPath -Value ($header + @("Logcat capture failed: $($_.Exception.Message)")) -Encoding UTF8
    }
}

$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
$script:ResolvedDevice = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds 2

$runAsProbe = Invoke-AndroidAdbCapture `
    -AdbPath $AdbPath `
    -DeviceSerial $script:ResolvedDevice `
    -Arguments @("shell", "run-as", $PackageName, "sh", "-c", "pwd")
if ($LASTEXITCODE -ne 0) {
    $probeText = ($runAsProbe -join [Environment]::NewLine).Trim()
    throw "Cannot capture app-private beta integrity evidence because adb run-as is unavailable for package '$PackageName'. Install a debuggable local evidence build, for example: .\scripts\build-android-local.ps1 -Install -EvidenceDebuggable -VersionCode <higher-than-installed> -VersionName <local-evidence-version>. run-as output: $probeText"
}

$resolvedEvidenceDir = $EvidenceDir
if (-not [System.IO.Path]::IsPathRooted($resolvedEvidenceDir)) {
    $resolvedEvidenceDir = Resolve-RepoPath $resolvedEvidenceDir
}

New-Item -ItemType Directory -Force $resolvedEvidenceDir | Out-Null
$markersDir = Join-Path $resolvedEvidenceDir "branch-markers"
$inventoryDir = Join-Path $resolvedEvidenceDir "inventories"
$logsDir = Join-Path $resolvedEvidenceDir "logs"
$summaryPath = Join-Path $resolvedEvidenceDir "beta-integrity-summary.txt"
New-Item -ItemType Directory -Force $markersDir | Out-Null
New-Item -ItemType Directory -Force $inventoryDir | Out-Null
New-Item -ItemType Directory -Force $logsDir | Out-Null

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"
$markerList = Write-RunAsTextFile `
    -Command "find files -name steam_branch.txt -type f 2>/dev/null | sort || true" `
    -Path (Join-Path $markersDir "steam-branch-marker-list.txt") `
    -AllowFailure

$branchAvailabilityMarkerPath = Join-Path $markersDir "last_steam_branch_availability.txt"
$branchAvailabilityMarkerText = Write-RunAsTextFile `
    -Command "cat files/last_steam_branch_availability.txt 2>/dev/null || true" `
    -Path $branchAvailabilityMarkerPath `
    -AllowFailure
$redownloadMarkerPath = Join-Path $markersDir "last_game_version_redownload.txt"
$redownloadMarkerText = Write-RunAsTextFile `
    -Command "cat files/last_game_version_redownload.txt 2>/dev/null || true" `
    -Path $redownloadMarkerPath `
    -AllowFailure

$publicMarkerPath = ""
$publicMarkerText = ""
$selectedMarkerPath = ""
$selectedMarkerText = ""
foreach ($markerPath in ($markerList -split '\r?\n')) {
    $markerPath = $markerPath.Trim()
    if ([string]::IsNullOrWhiteSpace($markerPath)) {
        continue
    }

    $safeName = ConvertTo-SafeFileName $markerPath
    $localMarkerPath = Join-Path $markersDir "$safeName.txt"
    $markerText = Write-RunAsTextFile -Command "cat '$markerPath' 2>/dev/null || true" -Path $localMarkerPath -AllowFailure
    $markerBranch = Read-BranchFromMarkerText $markerText
    if ($markerBranch -ieq "public") {
        $publicMarkerPath = $markerPath
        $publicMarkerText = $markerText
    }

    if ($markerBranch -ieq $Branch) {
        $selectedMarkerPath = $markerPath
        $selectedMarkerText = $markerText
    }
}

$selectedGameDir = ""
if (-not [string]::IsNullOrWhiteSpace($selectedMarkerPath)) {
    $selectedGameDir = [regex]::Replace($selectedMarkerPath, '/steam_branch\.txt$', '')
}

$inventoryCommand = @'
dir="$1"
if [ ! -d "$dir" ]; then
  exit 0
fi
cd "$dir" || exit 0
find . -type f | sort | while IFS= read -r f; do
  rel="${f#./}"
  size="$(wc -c < "$f" 2>/dev/null | tr -d ' ')"
  hash="$(sha256sum "$f" 2>/dev/null | awk '{print $1}')"
  printf '%s\t%s\t%s\n' "$rel" "$size" "$hash"
done
'@
$inventoryCommand = $inventoryCommand -replace "`r", ""
$escapedInventoryCommand = $inventoryCommand.Replace("'", "'\''")

$publicInventoryPath = Join-Path $inventoryDir "public-files.tsv"
$publicCacheTreePath = Join-Path $inventoryDir "public-cache-tree.txt"
$selectedInventoryPath = Join-Path $inventoryDir "$(ConvertTo-SafeFileName $Branch)-files.tsv"
$selectedCacheTreePath = Join-Path $inventoryDir "$(ConvertTo-SafeFileName $Branch)-cache-tree.txt"
Write-RunAsTextFile `
    -Command "sh -c '$escapedInventoryCommand' sh files/game" `
    -Path $publicInventoryPath `
    -AllowFailure | Out-Null
Write-RunAsTextFile `
    -Command "find files/game -maxdepth 3 \( -type f -o -type d \) 2>/dev/null | sort || true" `
    -Path $publicCacheTreePath `
    -AllowFailure | Out-Null

if (-not [string]::IsNullOrWhiteSpace($selectedGameDir)) {
    Write-RunAsTextFile `
        -Command "sh -c '$escapedInventoryCommand' sh '$selectedGameDir'" `
        -Path $selectedInventoryPath `
        -AllowFailure | Out-Null
    Write-RunAsTextFile `
        -Command "find '$selectedGameDir' -maxdepth 3 \( -type f -o -type d \) 2>/dev/null | sort || true" `
        -Path $selectedCacheTreePath `
        -AllowFailure | Out-Null
} else {
    Set-Content -LiteralPath $selectedInventoryPath -Value "" -Encoding UTF8
    Set-Content -LiteralPath $selectedCacheTreePath -Value "" -Encoding UTF8
}

$comparisonPath = Join-Path $inventoryDir "public-vs-$(ConvertTo-SafeFileName $Branch)-comparison.txt"
Write-InventoryComparison -PublicInventoryPath $publicInventoryPath -SelectedInventoryPath $selectedInventoryPath -OutputPath $comparisonPath
$keyAssetComparisonPath = Join-Path $inventoryDir "public-vs-$(ConvertTo-SafeFileName $Branch)-key-assets.tsv"
Write-KeyAssetComparison -PublicInventoryPath $publicInventoryPath -SelectedInventoryPath $selectedInventoryPath -OutputPath $keyAssetComparisonPath
$focusedLogcatPath = Join-Path $logsDir "beta-integrity-logcat-focused.txt"
Write-FocusedLogcatEvidence -OutputPath $focusedLogcatPath

$publicMarkerStatus = if ([string]::IsNullOrWhiteSpace($publicMarkerPath)) { "not found" } else { $publicMarkerPath }
$selectedMarkerStatus = if ([string]::IsNullOrWhiteSpace($selectedMarkerPath)) { "not found" } else { $selectedMarkerPath }
$classification = Get-BetaIntegrityClassification `
    -MarkerText $selectedMarkerText `
    -SelectedMarkerPath $selectedMarkerPath `
    -SelectedGameDir $selectedGameDir `
    -ComparisonPath $comparisonPath `
    -BranchAvailabilityText $branchAvailabilityMarkerText `
    -RedownloadText $redownloadMarkerText `
    -Branch $Branch
$summaryMatchingPublic = Read-MarkerIntFromText $selectedMarkerText "Depot manifests matching public count:"
$summaryDifferingPublic = Read-MarkerIntFromText $selectedMarkerText "Depot manifests differing from public count:"
$summaryWithoutPublicComparison = Read-MarkerIntFromText $selectedMarkerText "Depot manifests without public comparison count:"
$summaryInheritedPublic = Read-MarkerIntFromText $selectedMarkerText "Depot manifests inherited from public count:"
$summaryMissingSelected = Read-MarkerIntFromText $selectedMarkerText "Depot manifests missing selected branch manifest count:"
$summarySameFiles = Read-ComparisonMetric $comparisonPath "Files present in both with identical hashes"
$summaryDifferentFiles = Read-ComparisonMetric $comparisonPath "Files present in both but different"
$summaryPublicOnlyFiles = Read-ComparisonMetric $comparisonPath "Files only in public"
$summarySelectedOnlyFiles = Read-ComparisonMetric $comparisonPath "Files only in selected branch"
$summaryArtSameFiles = Read-ComparisonMetric $comparisonPath "Art/bundle-like files identical in both"
$summaryArtDifferentFiles = Read-ComparisonMetric $comparisonPath "Art/bundle-like files present in both but different"
$redownloadMarkerPresent = -not [string]::IsNullOrWhiteSpace($redownloadMarkerText)
$redownloadSelectedBranch = Read-MarkerValueFromText $redownloadMarkerText "Selected branch:"
$redownloadBranchMatches = $redownloadSelectedBranch -ieq $Branch
$redownloadGameAfterDelete = Read-MarkerValueFromText $redownloadMarkerText "Game directory exists after delete:"
$redownloadStateAfterDelete = Read-MarkerValueFromText $redownloadMarkerText "Download state directory exists after delete:"
$redownloadClearedSelectedDirectories = ($redownloadGameAfterDelete -ieq "false" -and $redownloadStateAfterDelete -ieq "false")
$branchAvailabilityMarkerPresent = -not [string]::IsNullOrWhiteSpace($branchAvailabilityMarkerText)
$branchAvailabilitySelectedBranch = Read-MarkerValueFromText $branchAvailabilityMarkerText "Selected branch:"
$branchAvailabilityBranchMatches = $branchAvailabilitySelectedBranch -ieq $Branch
$publicDepotManifestRows = Read-MarkerRowsFromText $publicMarkerText "Depot manifest:" 32
$selectedDepotManifestRows = Read-MarkerRowsFromText $selectedMarkerText "Depot manifest:" 32
$changedKeyAssetRows = Read-ChangedKeyAssetRows $keyAssetComparisonPath 64
$evidenceReadinessLines = Get-EvidenceReadinessLines `
    -Classification $classification `
    -PublicMarkerPath $publicMarkerPath `
    -SelectedMarkerPath $selectedMarkerPath `
    -RedownloadMarkerPresent $redownloadMarkerPresent `
    -RedownloadBranchMatches $redownloadBranchMatches `
    -RedownloadClearedSelectedDirectories $redownloadClearedSelectedDirectories
Set-Content -LiteralPath $summaryPath -Value @(
    "Steam beta branch integrity evidence",
    "Public sharing warning: manually review this summary, focused logcat, branch markers, cache tree, and inventory paths before posting publicly; they can include device paths, local usernames, account-visible branch metadata, or other identifying details.",
    "Timestamp: $timestamp",
    "Package: $PackageName",
    "Device serial: $script:ResolvedDevice",
    "Branch under investigation: $Branch",
    "Public branch marker: $publicMarkerStatus",
    "Selected branch marker: $selectedMarkerStatus",
    "Clean redownload marker: $(if ($redownloadMarkerPresent) { $redownloadMarkerPath } else { "not found" })",
    "Clean redownload selected branch: $redownloadSelectedBranch",
    "Clean redownload matches investigated branch: $($redownloadBranchMatches.ToString().ToLowerInvariant())",
    "Clean redownload selected version slot kind: $(Read-MarkerValueFromText $redownloadMarkerText "Selected version slot kind:")",
    "Clean redownload selected version slot directory: $(Read-MarkerValueFromText $redownloadMarkerText "Selected version slot directory:")",
    "Clean redownload game directory existed before delete: $(Read-MarkerValueFromText $redownloadMarkerText "Game directory existed before delete:")",
    "Clean redownload game directory exists after delete: $redownloadGameAfterDelete",
    "Clean redownload download state existed before delete: $(Read-MarkerValueFromText $redownloadMarkerText "Download state directory existed before delete:")",
    "Clean redownload download state exists after delete: $redownloadStateAfterDelete",
    "Clean redownload selected directories cleared: $($redownloadClearedSelectedDirectories.ToString().ToLowerInvariant())",
    "Branch availability marker: $(if ($branchAvailabilityMarkerPresent) { $branchAvailabilityMarkerPath } else { "not found" })",
    "Branch availability selected branch: $branchAvailabilitySelectedBranch",
    "Branch availability matches investigated branch: $($branchAvailabilityBranchMatches.ToString().ToLowerInvariant())",
    "Branch availability selected branch visibility: $(Read-MarkerValueFromText $branchAvailabilityMarkerText "Selected branch visibility:")",
    "Branch availability selected branch Windows depot manifests: $(Read-MarkerValueFromText $branchAvailabilityMarkerText "Windows depot manifests for selected branch:")",
    "Branch availability visible branch count: $(Read-MarkerValueFromText $branchAvailabilityMarkerText "Visible branch count:")",
    "Selected branch game directory: $selectedGameDir",
    "Public inventory: $publicInventoryPath",
    "Public cache tree: $publicCacheTreePath",
    "Selected inventory: $selectedInventoryPath",
    "Selected cache tree: $selectedCacheTreePath",
    "Comparison: $comparisonPath",
    "Key asset comparison: $keyAssetComparisonPath",
    "Focused logcat: $focusedLogcatPath",
    "Classification: $classification",
    $evidenceReadinessLines,
    "",
    "Classification inputs:",
    "Depot manifests matching public count: $(Format-NullableMetric $summaryMatchingPublic)",
    "Depot manifests differing from public count: $(Format-NullableMetric $summaryDifferingPublic)",
    "Depot manifests without public comparison count: $(Format-NullableMetric $summaryWithoutPublicComparison)",
    "Depot manifests inherited from public count: $(Format-NullableMetric $summaryInheritedPublic)",
    "Depot manifests missing selected branch manifest count: $(Format-NullableMetric $summaryMissingSelected)",
    "Files present in both with identical hashes: $(Format-NullableMetric $summarySameFiles)",
    "Files present in both but different: $(Format-NullableMetric $summaryDifferentFiles)",
    "Files only in public: $(Format-NullableMetric $summaryPublicOnlyFiles)",
    "Files only in selected branch: $(Format-NullableMetric $summarySelectedOnlyFiles)",
    "Art/bundle-like files identical in both: $(Format-NullableMetric $summaryArtSameFiles)",
    "Art/bundle-like files present in both but different: $(Format-NullableMetric $summaryArtDifferentFiles)",
    "",
    "Public branch depot manifest rows (first 32):",
    $publicDepotManifestRows,
    "",
    "Selected branch depot manifest rows (first 32):",
    $selectedDepotManifestRows,
    "",
    "Changed key asset rows (first 64):",
    "state`tpath`tpublic_sha256`tselected_sha256",
    $changedKeyAssetRows,
    "",
    "Interpretation:",
    "- If branch marker depot manifests include selectedMatchesPublic=true and selectedMatchesPublic=false, Steam is serving a partial branch for at least some depots.",
    "- If selected inventory contains stale public-only files after a clean selected-branch redownload, cache cleanup/download deletion needs investigation.",
    "- If marker evidence shows no public fallback and inventories match Steam manifests, remaining mixed behavior may be runtime remote/config or expected partial Steam branch content.",
    "- If selected branch marker is missing, redownload the selected branch before using this evidence."
) -Encoding UTF8

Write-Host "Steam beta integrity evidence written to: $resolvedEvidenceDir"
if ($ReviewSummary -or $FailOnNotReady) {
    & "$PSScriptRoot/review-beta-integrity-summary.ps1" -SummaryPath $summaryPath -FailOnNotReady:$FailOnNotReady
}
