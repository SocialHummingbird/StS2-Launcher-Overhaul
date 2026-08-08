param(
    [string]$GameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$ExpectedAssemblySha256 = "A1F9E653F1E28E4076558FEE1E60D218619CB7E057B887C6417F62C62C6D7A52",
    [long]$ExpectedPckBytes = 1901378340
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-OnlyMethod {
    param(
        [Mono.Cecil.TypeDefinition]$Type,
        [string]$Name,
        [int]$ParameterCount
    )

    $matches = @(
        $Type.Methods | Where-Object {
            $_.Name -eq $Name -and $_.Parameters.Count -eq $ParameterCount
        }
    )
    Assert-Condition ($matches.Count -eq 1) (
        "Expected exactly one $($Type.FullName).$Name method with $ParameterCount parameters; found $($matches.Count)"
    )
    return $matches[0]
}

function Test-CallsMethod {
    param(
        [Mono.Cecil.MethodDefinition]$Method,
        [string]$DeclaringType,
        [string]$Name
    )

    return @(
        $Method.Body.Instructions | Where-Object {
            ($_.Operand -is [Mono.Cecil.MethodReference]) -and
            ($_.Operand.DeclaringType.FullName -eq $DeclaringType) -and
            ($_.Operand.Name -eq $Name)
        }
    ).Count -gt 0
}

function Get-AllTypeDefinitions {
    param([System.Collections.IEnumerable]$Types)

    foreach ($type in $Types) {
        Write-Output $type
        if ($type.HasNestedTypes) {
            Get-AllTypeDefinitions $type.NestedTypes
        }
    }
}

function Read-PckDirectory {
    param([string]$Path)

    $stream = [System.IO.File]::OpenRead($Path)
    $reader = [System.IO.BinaryReader]::new($stream)
    try {
        $magic = $reader.ReadUInt32()
        $format = $reader.ReadUInt32()
        $major = $reader.ReadUInt32()
        $minor = $reader.ReadUInt32()
        $patch = $reader.ReadUInt32()
        $flags = $reader.ReadUInt32()
        $fileBase = $reader.ReadInt64()
        $directoryBase = $reader.ReadInt64()
        $stream.Position += 64

        Assert-Condition ($magic -eq 0x43504447) "PCK magic is not GDPC"
        Assert-Condition ($format -eq 3) "Unsupported PCK format $format"
        Assert-Condition ($directoryBase -gt 0 -and $directoryBase -lt $stream.Length) (
            "Invalid PCK directory offset $directoryBase"
        )

        $stream.Position = $directoryBase
        $fileCount = $reader.ReadUInt32()
        $entries = [System.Collections.Generic.Dictionary[string, object]]::new(
            [System.StringComparer]::Ordinal
        )

        for ($index = 0; $index -lt $fileCount; $index++) {
            $pathLength = $reader.ReadUInt32()
            Assert-Condition ($pathLength -gt 0 -and $pathLength -le 8192) (
                "Invalid PCK path length $pathLength at entry $index"
            )

            $entryPath = [System.Text.Encoding]::UTF8.GetString(
                $reader.ReadBytes([int]$pathLength)
            ).TrimEnd([char]0)
            $offset = $reader.ReadInt64()
            $size = $reader.ReadInt64()
            [void]$reader.ReadBytes(16)
            [void]$reader.ReadUInt32()
            $entries[$entryPath] = [pscustomobject]@{
                Offset = $offset
                Size = $size
            }
        }

        return [pscustomobject]@{
            Version = "$major.$minor.$patch"
            Flags = $flags
            FileBase = $fileBase
            Entries = $entries
        }
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Read-PckTextEntry {
    param(
        [System.IO.BinaryReader]$Reader,
        [object]$Pck,
        [string]$EntryPath
    )

    $entry = $Pck.Entries[$EntryPath]
    Assert-Condition ($null -ne $entry) "Missing PCK entry $EntryPath"
    Assert-Condition ($entry.Size -ge 0 -and $entry.Size -le 1MB) (
        "Refusing to read non-text-sized PCK entry $EntryPath ($($entry.Size) bytes)"
    )

    $relativeOffsets = ($Pck.Flags -band 2) -ne 0
    $Reader.BaseStream.Position = if ($relativeOffsets) {
        $Pck.FileBase + $entry.Offset
    }
    else {
        $entry.Offset
    }
    return [System.Text.Encoding]::UTF8.GetString(
        $Reader.ReadBytes([int]$entry.Size)
    ).TrimEnd([char]0)
}

function Get-FallbackImports {
    param(
        [string]$Atlas,
        [string]$Sprite
    )

    $candidates = [System.Collections.Generic.List[string]]::new()
    switch ($Atlas) {
        { $_ -in "relic_atlas", "relic_outline_atlas" } {
            $candidates.Add("images/relics/$Sprite.png.import")
            $candidates.Add("images/relics/beta/$Sprite.png.import")
            break
        }
        "power_atlas" {
            $candidates.Add("images/powers/$Sprite.png.import")
            $candidates.Add("images/powers/beta/$Sprite.png.import")
            break
        }
        "card_atlas" {
            $candidates.Add("images/packed/card_portraits/$Sprite.png.import")
            $separator = $Sprite.LastIndexOf("/")
            if ($separator -gt 0) {
                $category = $Sprite.Substring(0, $separator)
                $name = $Sprite.Substring($separator + 1)
                $candidates.Add("images/packed/card_portraits/$category/beta/$name.png.import")
            }
            break
        }
        { $_ -in "potion_atlas", "potion_outline_atlas" } {
            $candidates.Add("images/potions/$Sprite.png.import")
            break
        }
    }
    return $candidates.ToArray()
}

$assemblyPath = Join-Path $GameDirectory "data_sts2_windows_x86_64\sts2.dll"
$pckPath = Join-Path $GameDirectory "SlayTheSpire2.pck"
Assert-Condition (Test-Path -LiteralPath $assemblyPath -PathType Leaf) (
    "Reporter-matching sts2.dll was not found at $assemblyPath"
)
Assert-Condition (Test-Path -LiteralPath $pckPath -PathType Leaf) (
    "Reporter-matching PCK was not found at $pckPath"
)

$assemblyHash = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
Assert-Condition ($assemblyHash -eq $ExpectedAssemblySha256) (
    "sts2.dll hash mismatch: expected $ExpectedAssemblySha256, got $assemblyHash"
)
$pckFile = Get-Item -LiteralPath $pckPath
Assert-Condition ($pckFile.Length -eq $ExpectedPckBytes) (
    "PCK size mismatch: expected $ExpectedPckBytes, got $($pckFile.Length)"
)

$cecilPath = Join-Path $HOME ".nuget\packages\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Assert-Condition (Test-Path -LiteralPath $cecilPath -PathType Leaf) (
    "Mono.Cecil is unavailable at $cecilPath; restore src/STS2Mobile first"
)
Add-Type -Path $cecilPath

$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
try {
    $atlasManager = $assembly.MainModule.GetType("MegaCrit.Sts2.Core.Assets.AtlasManager")
    $atlasLoader = $assembly.MainModule.GetType("MegaCrit.Sts2.Core.Assets.AtlasResourceLoader")
    $oneTime = $assembly.MainModule.GetType("MegaCrit.Sts2.Core.Helpers.OneTimeInitialization")
    Assert-Condition ($null -ne $atlasManager) "AtlasManager type is missing"
    Assert-Condition ($null -ne $atlasLoader) "AtlasResourceLoader type is missing"
    Assert-Condition ($null -ne $oneTime) "OneTimeInitialization type is missing"

    [void](Get-OnlyMethod $atlasManager "LoadAllAtlases" 0)
    $atlasExists = Get-OnlyMethod $atlasLoader "_Exists" 1
    $atlasLoad = Get-OnlyMethod $atlasLoader "_Load" 4
    $executeDeferred = Get-OnlyMethod $oneTime "ExecuteDeferred" 0
    Assert-Condition (Test-CallsMethod $executeDeferred $atlasManager.FullName "LoadAllAtlases") (
        "ExecuteDeferred no longer calls AtlasManager.LoadAllAtlases; review the compatibility patch"
    )
    Assert-Condition (Test-CallsMethod $atlasExists $atlasManager.FullName "LoadAtlas") (
        "AtlasResourceLoader._Exists no longer loads missing atlases; review the compatibility patch"
    )
    Assert-Condition (Test-CallsMethod $atlasLoad $atlasManager.FullName "LoadAtlas") (
        "AtlasResourceLoader._Load no longer loads missing atlases; review the compatibility patch"
    )

    $loadAtlasCallers = @(
        foreach ($type in Get-AllTypeDefinitions $assembly.MainModule.Types) {
            foreach ($method in $type.Methods) {
                if ($method.HasBody -and (Test-CallsMethod $method $atlasManager.FullName "LoadAtlas")) {
                    "$($type.FullName)::$($method.Name)"
                }
            }
        }
    )
    $expectedCallers = @(
        "$($atlasManager.FullName)::LoadAllAtlases"
        "$($atlasManager.FullName)::LoadEssentialAtlases"
        "$($atlasLoader.FullName)::_Exists"
        "$($atlasLoader.FullName)::_Load"
    )
    $unexpectedCallers = @($loadAtlasCallers | Where-Object { $_ -notin $expectedCallers })
    $unexpectedCallerMessage = (
        "Unexpected direct AtlasManager.LoadAtlas call sites can bypass the compatibility hooks: " +
        ($unexpectedCallers -join ", ")
    )
    Assert-Condition ($unexpectedCallers.Count -eq 0) $unexpectedCallerMessage
}
finally {
    $assembly.Dispose()
}

$pck = Read-PckDirectory $pckPath
$targetAtlases = @(
    "card_atlas",
    "relic_atlas",
    "relic_outline_atlas",
    "power_atlas",
    "potion_atlas",
    "potion_outline_atlas"
)
$coverage = @{}
$cardFallbackImports = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::Ordinal
)
foreach ($atlas in $targetAtlases) {
    $coverage[$atlas] = [pscustomobject]@{ Total = 0; Individual = 0; Missing = @() }
}

foreach ($entryPath in $pck.Entries.Keys) {
    if ($entryPath -notmatch '^images/atlases/(?<atlas>[^/]+)\.sprites/(?<sprite>.+)\.tres$') {
        continue
    }
    $atlas = $Matches.atlas
    $sprite = $Matches.sprite
    if ($atlas -notin $targetAtlases) {
        continue
    }

    $item = $coverage[$atlas]
    $item.Total++
    $fallback = @(
        Get-FallbackImports $atlas $sprite | Where-Object { $pck.Entries.ContainsKey($_) }
    ) | Select-Object -First 1
    if ($null -ne $fallback) {
        $item.Individual++
        if ($atlas -eq "card_atlas") {
            [void]$cardFallbackImports.Add($fallback)
        }
    }
    else {
        $item.Missing += $sprite
    }
}

foreach ($atlas in $targetAtlases) {
    $item = $coverage[$atlas]
    Assert-Condition ($item.Total -gt 0) "No sprite resources found for $atlas"
    Write-Output (
        "{0}: individual fallbacks {1}/{2}; lazy atlas-only sprites {3}" -f
        $atlas, $item.Individual, $item.Total, $item.Missing.Count
    )
}
$missingCardMessage = "The card atlas has sprites without individual fallbacks: " + (
    $coverage.card_atlas.Missing -join ", "
)
Assert-Condition ($coverage.card_atlas.Missing.Count -eq 0) $missingCardMessage

$stream = [System.IO.File]::OpenRead($pckPath)
$reader = [System.IO.BinaryReader]::new($stream)
try {
    foreach ($importPath in $cardFallbackImports) {
        $importText = Read-PckTextEntry $reader $pck $importPath
        Assert-Condition ($importText -notmatch 'path\.(bptc|s3tc)=') (
            "Card fallback still selects a desktop VRAM format: $importPath"
        )
    }
}
finally {
    $reader.Dispose()
    $stream.Dispose()
}

Write-Output "PASS: exact reporter assembly hash $assemblyHash"
Write-Output "PASS: exact reporter PCK size $($pckFile.Length), Godot PCK $($pck.Version)"
Write-Output "PASS: AtlasManager/AtlasResourceLoader Harmony targets exist and deferred startup calls LoadAllAtlases"
Write-Output "PASS: all $($coverage.card_atlas.Total) card sprites use non-BPTC/S3TC individual imports"

$patchAssemblyPath = Join-Path $PSScriptRoot "..\src\STS2Mobile\bin\Release\net9.0\STS2Mobile.dll"
$godotAssemblyPath = Join-Path $GameDirectory "data_sts2_windows_x86_64\GodotSharp.dll"
Assert-Condition (Test-Path -LiteralPath $patchAssemblyPath -PathType Leaf) (
    "Built STS2Mobile.dll is unavailable at $patchAssemblyPath; build the Release project first"
)
Assert-Condition (Test-Path -LiteralPath $godotAssemblyPath -PathType Leaf) (
    "Reporter-matching GodotSharp.dll is unavailable at $godotAssemblyPath"
)

dotnet run --project (
    Join-Path $PSScriptRoot "..\tools\AndroidAtlasCompatibilityProbe\AndroidAtlasCompatibilityProbe.csproj"
) --configuration Release -- $patchAssemblyPath $assemblyPath $godotAssemblyPath
Assert-Condition ($LASTEXITCODE -eq 0) "Harmony compatibility probe failed"
