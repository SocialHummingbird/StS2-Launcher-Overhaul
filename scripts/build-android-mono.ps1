param(
    [ValidateSet('arm64-v8a', 'x86_64', 'universal')]
    [string]$Abi = 'arm64-v8a',
    [string]$AndroidHome = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain/android-sdk')",
    [string]$PythonPath = 'python',
    [switch]$Stage
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$recipePath = Join-Path $root 'patches/mono/runtime.json'
$patchPath = Join-Path $root 'patches/mono/android-net9.patch'
$extractorPath = Join-Path $PSScriptRoot 'extract-android-mono-source.py'
$recipe = Get-Content -LiteralPath $recipePath -Raw | ConvertFrom-Json
$recipeHash = (Get-FileHash -LiteralPath $recipePath -Algorithm SHA256).Hash
$patchHash = (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash
$scriptHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
$extractorHash = (Get-FileHash -LiteralPath $extractorPath -Algorithm SHA256).Hash
$buildId = "$($recipe.commit)-$($recipeHash.Substring(0,12))-$($patchHash.Substring(0,12))-$($scriptHash.Substring(0,12))-$($extractorHash.Substring(0,12))"
$cache = Join-Path $root "tmp/android-mono/$buildId"
$source = Join-Path $cache 'source'
$toolsRoot = Join-Path $root 'tmp/android-mono-tools'
$ndk = Join-Path $AndroidHome "ndk/$($recipe.ndk)"
$llvm = Join-Path $ndk 'toolchains/llvm/prebuilt/windows-x86_64/bin'
$coreLib = Join-Path $root 'android/assets/dotnet_bcl/System.Private.CoreLib.dll'

if (-not $IsWindows) { throw 'This pinned Mono helper currently requires Windows x64 and its Android NDK.' }
if ((& $PythonPath --version) -ne "Python $($recipe.python)") { throw "Native Mono requires Python $($recipe.python)." }
if (-not (Test-Path -LiteralPath (Join-Path $ndk 'source.properties')) -or
    (Get-Content -LiteralPath (Join-Path $ndk 'source.properties') -Raw) -notmatch "(?m)^Pkg.Revision = $([regex]::Escape($recipe.ndk))\r?$" ) {
    throw "Pinned Android NDK $($recipe.ndk) is required: $ndk"
}
if ((Get-FileHash -LiteralPath $coreLib -Algorithm SHA256).Hash -ne $recipe.coreLibSha256 -or
    [Diagnostics.FileVersionInfo]::GetVersionInfo($coreLib).ProductVersion -ne "$($recipe.version)+$($recipe.commit)") {
    throw 'Native Mono source and staged CoreLib must be the exact pinned runtime pair.'
}

function Get-VerifiedDownload([string]$Url, [string]$Hash, [string]$Directory) {
    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    $path = Join-Path $Directory ([IO.Path]::GetFileName(([Uri]$Url).AbsolutePath))
    if (-not (Test-Path -LiteralPath $path)) { Invoke-WebRequest -Uri $Url -OutFile $path }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Hash) { throw "Pinned archive hash mismatch: $path" }
    return $path
}

foreach ($toolName in @('cmake', 'ninja')) {
    $tool = $recipe.$toolName
    $archive = Get-VerifiedDownload $tool.url $tool.sha256 $toolsRoot
    $destination = Join-Path $toolsRoot "$toolName-$($tool.sha256.Substring(0,12))"
    if (-not (Test-Path -LiteralPath $destination)) {
        & $PythonPath -c 'import sys,zipfile; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' $archive $destination
        if ($LASTEXITCODE) { throw "Native build tool extraction failed: $toolName" }
    }
    & $PythonPath -c 'import hashlib,pathlib,sys,zipfile; z=zipfile.ZipFile(sys.argv[1]); p=pathlib.Path(sys.argv[2]); assert all((p/i.filename).is_file() and hashlib.sha256((p/i.filename).read_bytes()).digest()==hashlib.sha256(z.read(i)).digest() for i in z.infolist() if not i.is_dir()), "Cached native tool differs from its pinned archive"' $archive $destination
    if ($LASTEXITCODE) { throw "Cached native build tool validation failed: $toolName" }
    if ($toolName -eq 'cmake') { $cmake = Join-Path $destination 'cmake/data/bin/cmake.exe' }
    else { $ninja = Join-Path $destination 'ninja-1.11.1.3.data/scripts/ninja.exe' }
}
if ((& $cmake --version | Select-Object -First 1) -ne "cmake version $($recipe.cmake.version)") { throw 'Pinned CMake version mismatch.' }
if ((& $ninja --version) -ne '1.11.1.git.kitware.jobserver-1') { throw 'Pinned Ninja version mismatch.' }

New-Item -ItemType Directory -Force -Path $cache | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $source '.git'))) {
    $archive = Get-VerifiedDownload $recipe.sourceArchiveUrl $recipe.sourceArchiveSha256 (Join-Path $root 'tmp/android-mono-source')
    & $PythonPath $extractorPath $archive $source "runtime-$($recipe.commit)"
    if ($LASTEXITCODE) { throw 'Pinned Mono source extraction failed.' }
    & git -C $source init --quiet
    if ($LASTEXITCODE) { throw 'Mono source cache initialization failed.' }
    & git -C $source config core.autocrlf false
    & git -C $source config core.longpaths true
    & git -C $source add -f -- .
    if ($LASTEXITCODE) { throw 'Mono source indexing failed.' }
    & git -C $source apply --index $patchPath
    if ($LASTEXITCODE) { throw 'Pinned Mono source patch failed.' }
}
function Test-SourceInputs {
    if ((& git -C $source write-tree) -ne $recipe.patchedSourceTree) { throw 'Pinned Mono patched source tree mismatch.' }
    & git -C $source diff --quiet --no-ext-diff
    if ($LASTEXITCODE) { throw "Mono source has local changes: $source" }
    if (@(& git -C $source ls-files --others --exclude-standard).Count) { throw "Mono source has unexpected untracked files: $source" }
}
Test-SourceInputs

$versionDirectory = Join-Path $source 'artifacts/obj'
New-Item -ItemType Directory -Force -Path $versionDirectory | Out-Null
[IO.File]::WriteAllText((Join-Path $versionDirectory '_version.h'), "#define VER_PRODUCTVERSION_STR `"$($recipe.version)+$($recipe.commit)`"`n")
[IO.File]::WriteAllText((Join-Path $versionDirectory '_version.c'), "static char sccsid[] __attribute__((used)) = `"@(#)Version $($recipe.version) @Commit: $($recipe.commit)`";`n")
[IO.File]::WriteAllText((Join-Path $versionDirectory 'runtime_version.h'), @"
#define RuntimeFileMajorVersion 9
#define RuntimeFileMinorVersion 0
#define RuntimeFileBuildVersion 725
#define RuntimeFileRevisionVersion 31616
#define RuntimeProductMajorVersion 9
#define RuntimeProductMinorVersion 0
#define RuntimeProductPatchVersion 7
"@)

$gitCommand = (Get-Command git).Source
$gitShell = Join-Path (Split-Path (Split-Path $gitCommand)) 'usr/bin'
if (-not (Test-Path -LiteralPath (Join-Path $gitShell 'sh.exe'))) { throw 'Git for Windows sh.exe is required by native compiler detection.' }
$previousPath = $env:PATH
$env:PATH = "$llvm;$gitShell;$previousPath"
$targetAbis = if ($Abi -eq 'universal') { @('arm64-v8a', 'x86_64') } else { @($Abi) }
$nativeHashes = [ordered]@{}
$nativeOutputs = [ordered]@{}
$allFlags = [ordered]@{}
try {
    foreach ($targetAbi in $targetAbis) {
        $architecture = if ($targetAbi -eq 'arm64-v8a') { 'arm64' } else { 'x64' }
        $build = Join-Path $cache $targetAbi
        $output = Join-Path $cache "output/$targetAbi/libmonosgen-2.0.so"
        $provenancePath = Join-Path $build 'runtime-build.json'
        $sourceMap = $source.Replace('\', '/')
        $cacheMap = $cache.Replace('\', '/')
        $flags = "$($recipe.cFlags) -ffile-prefix-map=$sourceMap=/src/dotnet-runtime -ffile-prefix-map=$cacheMap=/build/mono"
        if ($targetAbi -eq 'arm64-v8a') { $flags += " $($recipe.arm64Flags)" }
        $cmakeArguments = @('-S', (Join-Path $source 'src/mono'), '-B', $build, '-G', 'Ninja',
            "-DCMAKE_MAKE_PROGRAM=$ninja", "-DCMAKE_TOOLCHAIN_FILE=$ndk/build/cmake/android.toolchain.cmake",
            "-DANDROID_NDK=$ndk", "-DANDROID_ABI=$targetAbi", "-DCLR_CMAKE_HOST_ARCH=$architecture",
            "-DMONO_COMPONENTS_RID=android-$architecture", "-DCMAKE_C_FLAGS=$flags", "-DCMAKE_CXX_FLAGS=$flags") + $recipe.cmakeArguments
        $allFlags[$targetAbi] = @($recipe.cmakeArguments) + @("ANDROID_ABI=$targetAbi", "CLR_CMAKE_HOST_ARCH=$architecture", ($flags -replace [regex]::Escape($sourceMap), '<source>' -replace [regex]::Escape($cacheMap), '<cache>'))
        $provenance = if (Test-Path -LiteralPath $provenancePath) { Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json } else { $null }
        if (-not $provenance -or $provenance.buildId -ne $buildId -or -not (Test-Path -LiteralPath $output) -or
            (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash -ne $provenance.sha256) {
            & $cmake @cmakeArguments | Out-Host
            if ($LASTEXITCODE) { throw "Mono CMake configure failed for $targetAbi." }
            & $cmake --build $build --target monosgen-shared --parallel 8 | Out-Host
            if ($LASTEXITCODE) { throw "Native Mono build failed for $targetAbi." }
            New-Item -ItemType Directory -Force -Path (Split-Path $output) | Out-Null
            & (Join-Path $llvm 'llvm-strip.exe') --strip-unneeded -o $output (Join-Path $build 'mono/mini/libmonosgen-2.0.so')
            if ($LASTEXITCODE) { throw "Native Mono symbol stripping failed for $targetAbi." }
            $exports = & (Join-Path $llvm 'llvm-nm.exe') -D --defined-only $output
            if ($LASTEXITCODE -or ($exports -join "`n") -notmatch "(?m) T $($recipe.export)$") { throw "Native Mono is missing required export $($recipe.export)." }
            $provenance = [ordered]@{ buildId = $buildId; sha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash }
            $provenance | ConvertTo-Json | Set-Content -LiteralPath $provenancePath -Encoding utf8
        }
        $nativeHashes[$targetAbi] = $provenance.sha256
        $nativeOutputs[$targetAbi] = $output
    }
} finally { $env:PATH = $previousPath }
Test-SourceInputs
$manifest = [ordered]@{
    buildId = $buildId; source = $recipe.source; commit = $recipe.commit; version = $recipe.version
    patchedSourceTree = $recipe.patchedSourceTree; sourceArchiveSha256 = $recipe.sourceArchiveSha256
    recipeSha256 = $recipeHash; patchSha256 = $patchHash; buildScriptSha256 = $scriptHash; extractorSha256 = $extractorHash
    coreLibSha256 = $recipe.coreLibSha256; ndk = $recipe.ndk; python = $recipe.python
    cmake = $recipe.cmake.version; ninja = $recipe.ninja.version; flags = $allFlags; libraries = $nativeHashes
}
$manifestPath = Join-Path $cache 'mono-runtime-build.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
if ($Stage) {
    foreach ($targetAbi in $targetAbis) {
        $destination = Join-Path $root "android/libs/release/$targetAbi/libmonosgen-2.0.so"
        New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
        Copy-Item -LiteralPath $nativeOutputs[$targetAbi] -Destination $destination -Force
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $nativeHashes[$targetAbi]) { throw "Staged native Mono hash mismatch: $targetAbi" }
    }
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $root 'android/assets/dotnet_bcl/mono-runtime-build.json') -Force
}
$manifestPath
