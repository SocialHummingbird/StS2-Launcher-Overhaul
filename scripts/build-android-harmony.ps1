param(
    [string]$DotNetPath,
    [switch]$Stage
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$recipePath = Join-Path $root 'patches/harmony/runtime.json'
$patchPath = Join-Path $root 'patches/harmony/android-net9.patch'
$nugetConfig = Join-Path $root 'patches/harmony/NuGet.Config'
$recipe = Get-Content -LiteralPath $recipePath -Raw | ConvertFrom-Json
$recipeHash = (Get-FileHash -LiteralPath $recipePath -Algorithm SHA256).Hash
$patchHash = (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash
$inputHashes = [ordered]@{ 'NuGet.Config' = (Get-FileHash -LiteralPath $nugetConfig -Algorithm SHA256).Hash }
foreach ($relative in $recipe.lockFiles) {
    $inputHashes[$relative] = (Get-FileHash -LiteralPath (Join-Path $root "patches/harmony/locks/$relative") -Algorithm SHA256).Hash
}
$inputBytes = [System.Text.Encoding]::UTF8.GetBytes(($inputHashes | ConvertTo-Json -Compress))
$inputsHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($inputBytes))
$buildId = "$($recipe.commit)-$($recipeHash.Substring(0,12))-$($patchHash.Substring(0,12))-$($inputsHash.Substring(0,12))"
$cache = Join-Path $root "tmp/android-harmony/$buildId"
$source = Join-Path $cache 'source'
$output = Join-Path $source 'Lib.Harmony.Thin/bin/Release/net9.0'
$provenancePath = Join-Path $cache 'runtime-build.json'

if (-not $DotNetPath) {
    $sdkRoot = Join-Path $root "tmp/dotnet-sdk-$($recipe.sdk)"
    $DotNetPath = Join-Path $sdkRoot 'dotnet.exe'
    if (-not (Test-Path -LiteralPath $DotNetPath)) {
        New-Item -ItemType Directory -Force -Path $sdkRoot | Out-Null
        $archive = Join-Path $root "tmp/dotnet-sdk-$($recipe.sdk).zip"
        if (-not (Test-Path -LiteralPath $archive)) {
            Invoke-WebRequest -Uri $recipe.sdkWindowsX64Url -OutFile $archive
        }
        if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $recipe.sdkWindowsX64Sha512) {
            throw 'Pinned .NET SDK archive hash mismatch.'
        }
        Expand-Archive -LiteralPath $archive -DestinationPath $sdkRoot -Force
    }
}
$DotNetPath = (Resolve-Path -LiteralPath $DotNetPath).Path
if ((& $DotNetPath --version) -ne $recipe.sdk) { throw "Harmony requires SDK $($recipe.sdk)." }

New-Item -ItemType Directory -Force -Path $cache | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $source '.git'))) {
    & git clone --no-checkout $recipe.source $source | Out-Host
    if ($LASTEXITCODE) { throw 'Harmony source clone failed.' }
    & git -C $source checkout --detach $recipe.commit | Out-Host
    if ($LASTEXITCODE) { throw 'Pinned Harmony source checkout failed.' }
    & git -C $source apply --index $patchPath | Out-Host
    if ($LASTEXITCODE) { throw 'Pinned Android Harmony patch failed.' }
    foreach ($relative in $recipe.lockFiles) {
        Copy-Item -LiteralPath (Join-Path $root "patches/harmony/locks/$relative") -Destination (Join-Path $source $relative) -Force
        & git -C $source add -f -- $relative
        if ($LASTEXITCODE) { throw "Harmony lockfile staging failed: $relative" }
    }
}
if ((& git -C $source rev-parse HEAD) -ne $recipe.commit) { throw 'Harmony source revision mismatch.' }
if ((& git -C $source write-tree) -ne $recipe.patchedSourceTree) { throw 'Harmony patched source tree mismatch.' }
& git -C $source diff --quiet --no-ext-diff
if ($LASTEXITCODE) { throw "Harmony source has unstaged changes. Inspect $source before rebuilding." }
if (@(& git -C $source ls-files --others --exclude-standard).Count) { throw "Harmony source has unexpected untracked files: $source" }

$provenance = $null
if (Test-Path -LiteralPath $provenancePath) {
    $provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
    if ($provenance.buildId -ne $buildId -or $provenance.patchedSourceTree -ne $recipe.patchedSourceTree) {
        $provenance = $null
    }
    foreach ($assembly in $recipe.assemblies) {
        $file = Join-Path $output $assembly
        if (-not $provenance -or -not (Test-Path -LiteralPath $file) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $provenance.assemblies.$assembly) {
            $provenance = $null
            break
        }
    }
}
if (-not $provenance) {
    Push-Location $source
    try {
        & $DotNetPath restore Lib.Harmony.Thin/Lib.Harmony.Thin.csproj --locked-mode --configfile $nugetConfig -p:RestorePackagesWithLockFile=true | Out-Host
        if ($LASTEXITCODE) { throw 'Locked Android Harmony restore failed.' }
        & $DotNetPath build Lib.Harmony.Thin/Lib.Harmony.Thin.csproj -c Release -f $recipe.harmonyFramework --no-restore -t:Rebuild -p:GeneratePackageOnBuild=false | Out-Host
        if ($LASTEXITCODE) { throw 'Android Harmony source build failed.' }
    } finally { Pop-Location }
    $hashes = [ordered]@{}
    foreach ($assembly in $recipe.assemblies) {
        $hashes[$assembly] = (Get-FileHash -LiteralPath (Join-Path $output $assembly) -Algorithm SHA256).Hash
    }
    & git -C $source diff --quiet --no-ext-diff
    if ($LASTEXITCODE) { throw 'Harmony build unexpectedly changed locked source files.' }
    $provenance = [ordered]@{ buildId = $buildId; commit = $recipe.commit; patchedSourceTree = $recipe.patchedSourceTree; recipeSha256 = $recipeHash; patchSha256 = $patchHash; inputsSha256 = $inputHashes; sdk = $recipe.sdk; assemblies = $hashes }
    $provenance | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $provenancePath -Encoding utf8
}

if ($Stage) {
    $nativeRecipePath = Join-Path $root 'patches/mono/runtime.json'
    $nativeManifestPath = Join-Path $root 'android/assets/dotnet_bcl/mono-runtime-build.json'
    if (-not (Test-Path -LiteralPath $nativeManifestPath)) {
        throw 'Stage the pinned native runtime with build-android-mono.ps1 -Stage before staging Harmony.'
    }
    $nativeRecipe = Get-Content -LiteralPath $nativeRecipePath -Raw | ConvertFrom-Json
    $nativeManifest = Get-Content -LiteralPath $nativeManifestPath -Raw | ConvertFrom-Json
    if ($nativeManifest.commit -ne $nativeRecipe.commit -or
        $nativeManifest.recipeSha256 -ne (Get-FileHash -LiteralPath $nativeRecipePath -Algorithm SHA256).Hash -or
        $nativeManifest.patchSha256 -ne (Get-FileHash -LiteralPath (Join-Path $root 'patches/mono/android-net9.patch') -Algorithm SHA256).Hash -or
        $nativeManifest.buildScriptSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'build-android-mono.ps1') -Algorithm SHA256).Hash) {
        throw 'Staged native Mono provenance differs from the pinned source build. Run build-android-mono.ps1 -Stage.'
    }
    foreach ($library in $nativeManifest.libraries.PSObject.Properties) {
        $nativePath = Join-Path $root "android/libs/release/$($library.Name)/libmonosgen-2.0.so"
        if ((Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash -ne $library.Value) {
            throw "Staged native Mono hash mismatch: $($library.Name)"
        }
    }
    $destinations = @(
        (Join-Path $root 'upstream/godot-export/.godot/mono/publish/arm64'),
        (Join-Path $root 'android/assets/dotnet_bcl')
    )
    foreach ($destination in $destinations) {
        New-Item -ItemType Directory -Force -Path $destination | Out-Null
        foreach ($assembly in $recipe.assemblies) {
            Copy-Item -LiteralPath (Join-Path $output $assembly) -Destination (Join-Path $destination $assembly) -Force
            if ((Get-FileHash -LiteralPath (Join-Path $destination $assembly) -Algorithm SHA256).Hash -ne $provenance.assemblies.$assembly) {
                throw "Staged Harmony assembly hash mismatch: $destination/$assembly"
            }
        }
        Copy-Item -LiteralPath $provenancePath -Destination (Join-Path $destination 'harmony-runtime-build.json') -Force
    }
}
$output
