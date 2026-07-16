param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir,
    [string]$VanillaLabel = "final-vanilla",
    [string]$ModdedLabel = "final-modded",
    [string]$DisabledModLabel = "final-disabled-savesmerger",
    [string]$DisabledModNamePattern = "SavesMerger",
    [switch]$RequireScreenshots,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$resolvedEvidenceDir = (Resolve-Path -LiteralPath $EvidenceDir).ProviderPath
$failures = [System.Collections.Generic.List[string]]::new()
$passes = 0

function Join-EvidencePath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $resolvedEvidenceDir $normalized
}

function Add-Pass([string]$Message) {
    $script:passes += 1
    if (-not $Quiet) {
        Write-Host "PASS $Message"
    }
}

function Read-EvidenceText([string]$RelativePath) {
    $path = Join-EvidencePath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing evidence file: $RelativePath")
        return $null
    }

    return Get-Content -LiteralPath $path -Raw
}

function Read-EvidenceJson([string]$RelativePath, [string]$Description) {
    $content = Read-EvidenceText $RelativePath
    if ($null -eq $content) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($content)) {
        $failures.Add("$RelativePath - $Description - file is empty")
        return $null
    }

    try {
        return $content | ConvertFrom-Json
    } catch {
        $failures.Add("$RelativePath - $Description - invalid JSON: $($_.Exception.Message)")
        return $null
    }
}

function Require-TextPattern([string]$RelativePath, [string]$Description, [string]$Pattern) {
    $content = Read-EvidenceText $RelativePath
    if ($null -eq $content) {
        return
    }

    if ($content -notmatch $Pattern) {
        $failures.Add("$RelativePath - $Description - missing pattern: $Pattern")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Require-NoTextPattern([string]$RelativePath, [string]$Description, [string]$Pattern) {
    $content = Read-EvidenceText $RelativePath
    if ($null -eq $content) {
        return
    }

    if ($content -match $Pattern) {
        $failures.Add("$RelativePath - $Description - forbidden pattern present: $Pattern")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Require-FileExists([string]$RelativePath, [string]$Description) {
    $path = Join-EvidencePath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("$RelativePath - $Description - file is missing")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Get-ModArray($Marker, [string]$Description) {
    if ($null -eq $Marker) {
        return @()
    }

    $property = $Marker.PSObject.Properties["selectedMods"]
    if ($null -eq $property) {
        $failures.Add("$Description - missing selectedMods array")
        return @()
    }

    return @($property.Value)
}

function Get-ActivationArray($Marker, [string]$Description) {
    if ($null -eq $Marker) {
        return @()
    }

    $property = $Marker.PSObject.Properties["activationEvidence"]
    if ($null -eq $property) {
        $failures.Add("$Description - missing activationEvidence array")
        return @()
    }

    return @($property.Value)
}

function Require-MarkerVersionTwo($Marker, [string]$Description) {
    if ($null -eq $Marker -or $null -eq $Marker.PSObject.Properties["version"]) {
        $failures.Add("$Description - missing marker version")
        return
    }

    Require-AtLeast ([int]$Marker.version) 2 "$Description uses activation-evidence schema"
}

function Find-Activation($ActivationEvidence, [string]$Pattern, [string]$Description) {
    foreach ($activation in @($ActivationEvidence)) {
        $text = @($activation.Id, $activation.Title, $activation.Path, $activation.Assembly) -join " "
        if ($text -match $Pattern) {
            Add-Pass "$Description has activation evidence matching $Pattern"
            return $activation
        }
    }

    $failures.Add("$Description - missing activation evidence matching $Pattern")
    return $null
}

function Require-ActivationProperty($Activation, [string]$PropertyName, $Expected, [string]$Description) {
    if ($null -eq $Activation) {
        return
    }

    $property = $Activation.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        $failures.Add("$Description - missing activation property $PropertyName")
        return
    }

    Require-Equals $property.Value $Expected "$Description $PropertyName"
}

function Require-StandardActivationSet($Marker, [string]$Description, [bool]$ExpectSavesMerger) {
    Require-MarkerVersionTwo $Marker $Description
    $activation = Get-ActivationArray $Marker $Description
    Require-Equals $activation.Count ([int]$Marker.enabledMods) "$Description activation count matches selected count"
    Require-Equals $Marker.failedMods 0 "$Description has no failed runtime loads"
    Require-Equals $Marker.inGameVerifiedMods 0 "$Description does not overclaim in-game verification"

    $baseLib = Find-Activation $activation "BaseLib" "$Description BaseLib"
    Require-ActivationProperty $baseLib "CompatibilityMode" "partial-android" "$Description BaseLib"
    Require-ActivationProperty $baseLib "ActivationStatus" "partial-android-compatibility" "$Description BaseLib"
    Require-ActivationProperty $baseLib "PayloadReady" $true "$Description BaseLib"
    Require-ActivationProperty $baseLib "RuntimeLoadSucceeded" $true "$Description BaseLib"
    Require-ActivationProperty $baseLib "ErrorCount" 0 "$Description BaseLib"

    $quickRestart = Find-Activation $activation "Quick\s*Restart|QuickRestart" "$Description Quick Restart"
    Require-ActivationProperty $quickRestart "CompatibilityMode" "native" "$Description Quick Restart"
    Require-ActivationProperty $quickRestart "ActivationStatus" "runtime-patches-installed" "$Description Quick Restart"
    Require-ActivationProperty $quickRestart "PayloadReady" $true "$Description Quick Restart"
    Require-ActivationProperty $quickRestart "RuntimeLoadSucceeded" $true "$Description Quick Restart"
    Require-ActivationProperty $quickRestart "ErrorCount" 0 "$Description Quick Restart"
    if ($null -ne $quickRestart -and [int]$quickRestart.HarmonyTargetCount -lt 1) {
        $failures.Add("$Description Quick Restart - expected at least one installed Harmony target")
    } elseif ($null -ne $quickRestart) {
        Add-Pass "$Description Quick Restart has an installed Harmony target"
    }

    if ($ExpectSavesMerger) {
        $savesMerger = Find-Activation $activation "SavesMerger|UnifiedSavePath" "$Description SavesMerger"
        Require-ActivationProperty $savesMerger "CompatibilityMode" "launcher-substitute" "$Description SavesMerger"
        Require-ActivationProperty $savesMerger "ActivationStatus" "launcher-compatibility-substitute" "$Description SavesMerger"
        Require-ActivationProperty $savesMerger "PayloadReady" $false "$Description SavesMerger"
        Require-ActivationProperty $savesMerger "RuntimeLoadSucceeded" $true "$Description SavesMerger"
    } else {
        $savesMergerEvidence = @($activation | Where-Object {
            (@($_.Id, $_.Title, $_.Path, $_.Assembly) -join " ") -match "SavesMerger|UnifiedSavePath"
        })
        if ($savesMergerEvidence.Count -gt 0) {
            $failures.Add("$Description - disabled SavesMerger still has activation evidence")
        } else {
            Add-Pass "$Description excludes disabled SavesMerger activation evidence"
        }
    }

    return $activation
}

function Require-Property($Marker, [string]$PropertyName, [string]$Description) {
    if ($null -eq $Marker) {
        return $null
    }

    $property = $Marker.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        $failures.Add("$Description - missing JSON property: $PropertyName")
        return $null
    }

    Add-Pass "$Description - has $PropertyName"
    return $property.Value
}

function Require-Equals($Actual, $Expected, [string]$Description) {
    if ("$Actual" -ne "$Expected") {
        $failures.Add("$Description - expected $Expected, got $Actual")
        return
    }

    Add-Pass $Description
}

function Require-Boolean($Actual, [bool]$Expected, [string]$Description) {
    if ([bool]$Actual -ne $Expected) {
        $failures.Add("$Description - expected $Expected, got $Actual")
        return
    }

    Add-Pass $Description
}

function Require-AtLeast([int]$Actual, [int]$Minimum, [string]$Description) {
    if ($Actual -lt $Minimum) {
        $failures.Add("$Description - expected >= $Minimum, got $Actual")
        return
    }

    Add-Pass $Description
}

function Require-ModNamePresence($Mods, [string]$Pattern, [bool]$ExpectedPresent, [string]$Description) {
    $present = $false
    foreach ($mod in @($Mods)) {
        $text = @($mod.Key, $mod.Id, $mod.Title, $mod.Path) -join " "
        if ($text -match $Pattern) {
            $present = $true
            break
        }
    }

    if ($present -ne $ExpectedPresent) {
        $expected = if ($ExpectedPresent) { "present" } else { "absent" }
        $failures.Add("$Description - expected $Pattern to be $expected")
        return
    }

    Add-Pass $Description
}

function Require-ModRootSnapshots($Mods, [string]$Description) {
    foreach ($mod in @($Mods)) {
        $name = (@($mod.Key, $mod.Id, $mod.Title) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace([string]$name)) {
            $name = "<unknown>"
        }

        $rootProperty = $mod.PSObject.Properties["Root"]
        if ($null -eq $rootProperty) {
            $failures.Add("$Description - selected mod $name is missing Root snapshot")
            continue
        }

        $root = $rootProperty.Value
        foreach ($propertyName in @("Exists", "ManifestCount", "PckCount", "DllCount")) {
            if ($null -eq $root.PSObject.Properties[$propertyName]) {
                $failures.Add("$Description - selected mod $name Root is missing $propertyName")
                continue
            }
        }

        if ($null -ne $root.PSObject.Properties["Exists"] -and [bool]$root.Exists -ne $true) {
            $failures.Add("$Description - selected mod $name root does not exist")
            continue
        }

        if ($null -ne $root.PSObject.Properties["ManifestCount"] -and [int]$root.ManifestCount -lt 1) {
            $failures.Add("$Description - selected mod $name root has no manifest JSON")
            continue
        }

        Add-Pass "$Description - selected mod $name has root snapshot"
    }
}

function Require-CommonMarkerSafety([string]$Label, $Marker) {
    $cloudLocked = Require-Property $Marker "workshopModdedSaveCloudPushLocked" "$Label marker"
    $pushPerformed = Require-Property $Marker "steamCloudPushPerformed" "$Label marker"
    if ($null -ne $pushPerformed) {
        Require-Boolean $pushPerformed $false "$Label marker proves Steam Cloud Push was not performed"
    }

    return $cloudLocked
}

function Require-CommonLaunchLogSafety([string]$Label) {
    $logPath = "logs/$Label-focused.txt"
    Require-FileExists $logPath "$Label focused log is captured"
    Require-NoTextPattern $logPath "$Label launch avoided fallback/crash signatures" "(?i)NativeFallback|FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|signal 11|Unhandled exception"
    Require-NoTextPattern $logPath "$Label did not perform Steam Cloud Push" "(?i)Steam Cloud Push performed|Manual Push completed|PushToCloud|last_manual_cloud_push"
}

function Require-ScenarioScreenshot([string]$Label) {
    if (-not $RequireScreenshots) {
        return
    }

    Require-FileExists "screenshots/$Label-screen.png" "$Label screenshot is captured"
}

$vanilla = Read-EvidenceJson "diagnostics/$VanillaLabel-last-mod-launch.json" "$VanillaLabel launch marker"
$modded = Read-EvidenceJson "diagnostics/$ModdedLabel-last-mod-launch.json" "$ModdedLabel launch marker"
$disabled = Read-EvidenceJson "diagnostics/$DisabledModLabel-last-mod-launch.json" "$DisabledModLabel launch marker"

if ($null -ne $vanilla) {
    Require-MarkerVersionTwo $vanilla "$VanillaLabel marker"
    Require-Equals $vanilla.playMode "vanilla" "$VanillaLabel marker play mode"
    Require-Equals $vanilla.scannedRoots 0 "$VanillaLabel scanned zero mod roots"
    Require-Equals $vanilla.enabledMods 0 "$VanillaLabel enabled zero mods"
    $vanillaMods = Get-ModArray $vanilla "$VanillaLabel marker"
    Require-Equals $vanillaMods.Count 0 "$VanillaLabel selected zero mods"
    $vanillaActivation = Get-ActivationArray $vanilla "$VanillaLabel marker"
    Require-Equals $vanillaActivation.Count 0 "$VanillaLabel has zero activation entries"
    $vanillaCloudLocked = Require-CommonMarkerSafety $VanillaLabel $vanilla
    if ($null -ne $vanillaCloudLocked) {
        Require-Boolean $vanillaCloudLocked $false "$VanillaLabel leaves Cloud Push unlocked"
    }
    Require-CommonLaunchLogSafety $VanillaLabel
    Require-TextPattern "logs/$VanillaLabel-focused.txt" "$VanillaLabel skipped Android mod scan" "(?i)Android mod scan skipped|Play Vanilla"
    Require-ScenarioScreenshot $VanillaLabel
}

if ($null -ne $modded) {
    Require-Equals $modded.playMode "modded" "$ModdedLabel marker play mode"
    Require-AtLeast ([int]$modded.scannedRoots) 1 "$ModdedLabel scanned at least one mod root"
    Require-AtLeast ([int]$modded.enabledMods) 1 "$ModdedLabel enabled at least one mod"
    $moddedMods = Get-ModArray $modded "$ModdedLabel marker"
    Require-Equals $moddedMods.Count ([int]$modded.enabledMods) "$ModdedLabel selected mod count matches enabledMods"
    Require-ModNamePresence $moddedMods "BaseLib" $true "$ModdedLabel includes BaseLib"
    Require-ModNamePresence $moddedMods $DisabledModNamePattern $true "$ModdedLabel includes $DisabledModNamePattern"
    Require-ModRootSnapshots $moddedMods "$ModdedLabel marker"
    Require-StandardActivationSet $modded "$ModdedLabel marker" $true | Out-Null
    $moddedCloudLocked = Require-CommonMarkerSafety $ModdedLabel $modded
    if ($null -ne $moddedCloudLocked) {
        Require-Boolean $moddedCloudLocked $true "$ModdedLabel locks Cloud Push for selected mods"
    }
    Require-CommonLaunchLogSafety $ModdedLabel
    Require-TextPattern "logs/$ModdedLabel-focused.txt" "$ModdedLabel captured activation summary" "(?i)Activation evidence:"
    Require-ScenarioScreenshot $ModdedLabel
}

if ($null -ne $disabled) {
    Require-Equals $disabled.playMode "modded" "$DisabledModLabel marker play mode"
    Require-AtLeast ([int]$disabled.scannedRoots) 1 "$DisabledModLabel scanned at least one mod root"
    Require-AtLeast ([int]$disabled.enabledMods) 1 "$DisabledModLabel retained at least one enabled mod"
    $disabledMods = Get-ModArray $disabled "$DisabledModLabel marker"
    Require-Equals $disabledMods.Count ([int]$disabled.enabledMods) "$DisabledModLabel selected mod count matches enabledMods"
    Require-ModNamePresence $disabledMods "BaseLib" $true "$DisabledModLabel still includes BaseLib"
    Require-ModNamePresence $disabledMods $DisabledModNamePattern $false "$DisabledModLabel excludes disabled $DisabledModNamePattern"
    Require-ModRootSnapshots $disabledMods "$DisabledModLabel marker"
    Require-StandardActivationSet $disabled "$DisabledModLabel marker" $false | Out-Null
    if ($null -ne $modded -and [int]$modded.enabledMods -le [int]$disabled.enabledMods) {
        $failures.Add("$DisabledModLabel did not reduce enabled mod count below $ModdedLabel")
    } elseif ($null -ne $modded) {
        Add-Pass "$DisabledModLabel reduced enabled mod count below $ModdedLabel"
    }
    $disabledCloudLocked = Require-CommonMarkerSafety $DisabledModLabel $disabled
    if ($null -ne $disabledCloudLocked) {
        Require-Boolean $disabledCloudLocked $true "$DisabledModLabel keeps Cloud Push locked while other mods remain active"
    }
    Require-CommonLaunchLogSafety $DisabledModLabel
    Require-TextPattern "logs/$DisabledModLabel-focused.txt" "$DisabledModLabel skipped disabled mod" "(?i)Skipping disabled launcher-selected mod.+$DisabledModNamePattern|$DisabledModNamePattern.+disabled"
    Require-ScenarioScreenshot $DisabledModLabel
}

foreach ($label in @($VanillaLabel, $ModdedLabel, $DisabledModLabel)) {
    Require-FileExists "diagnostics/$label-mod-selection.json" "$label mod selection marker is captured"
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Mod selector evidence review failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    throw "Mod selector evidence review failed with $($failures.Count) failure(s)."
}

Write-Host "Mod selector evidence review passed ($passes checks): $resolvedEvidenceDir"
