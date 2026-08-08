param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sources = @(
    (Join-Path $root "android\src\com\game\sts2launcher\SteamBranchInfo.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidAssemblyBootstrapper.java"),
    (Join-Path $root "scripts\tests\stubs\BuildConfig.java"),
    (Join-Path $root "scripts\tests\AndroidAssemblyBootstrapperTest.java")
)
$jsonSources = @(
    (Join-Path $root "scripts\tests\stubs\org\json\JsonParser.java"),
    (Join-Path $root "scripts\tests\stubs\org\json\JSONArray.java"),
    (Join-Path $root "scripts\tests\stubs\org\json\JSONObject.java")
)
$output = Join-Path ([System.IO.Path]::GetTempPath()) (
    "sts2-assembly-bootstrapper-" + [Guid]::NewGuid().ToString("N")
)

function Resolve-JavaTool([string]$Name) {
    if ($env:JAVA_HOME) {
        $fromJavaHome = Join-Path $env:JAVA_HOME "bin\$Name.exe"
        if (Test-Path -LiteralPath $fromJavaHome) {
            return $fromJavaHome
        }
    }

    $fromPath = Get-Command $Name -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    $localToolchain = Join-Path $HOME ".w40k-android-toolchain\jdk-17\bin\$Name.exe"
    if (Test-Path -LiteralPath $localToolchain) {
        return $localToolchain
    }

    throw "Could not locate $Name. Set JAVA_HOME to a JDK 17 or newer."
}

function Resolve-AndroidJar {
    $sdkRoots = @(
        $env:ANDROID_SDK_ROOT,
        $env:ANDROID_HOME,
        (Join-Path $HOME ".w40k-android-toolchain\android-sdk")
    ) | Where-Object {
        $_ -and (Test-Path -LiteralPath $_ -PathType Container)
    }

    foreach ($sdkRoot in $sdkRoots) {
        $platforms = Join-Path $sdkRoot "platforms"
        if (-not (Test-Path -LiteralPath $platforms -PathType Container)) {
            continue
        }
        $androidJar = Get-ChildItem `
            -LiteralPath $platforms `
            -Directory |
            Sort-Object {
                if ($_.Name -match "^android-(\d+)$") {
                    [int]$Matches[1]
                } else {
                    0
                }
            } -Descending |
            ForEach-Object {
                Join-Path $_.FullName "android.jar"
            } |
            Where-Object {
                Test-Path -LiteralPath $_ -PathType Leaf
            } |
            Select-Object -First 1
        if ($androidJar) {
            return $androidJar
        }
    }

    throw "Could not locate Android SDK android.jar. Set ANDROID_SDK_ROOT."
}

$bootstrapperSource = Get-Content -LiteralPath $sources[1] -Raw
$godotAppSource = Get-Content -LiteralPath (
    Join-Path $root "android\src\com\game\sts2launcher\GodotApp.java"
) -Raw
$launcherActivitySource = Get-Content -LiteralPath (
    Join-Path $root "android\src\com\game\sts2launcher\LauncherActivity.java"
) -Raw

if (
    $bootstrapperSource.Contains("GodotApp") -or
    $bootstrapperSource.Contains("GodotActivity")
) {
    throw "AndroidAssemblyBootstrapper must remain independent of GodotApp."
}
if (
    $godotAppSource.Contains("private void setupAssemblies(") -or
    $godotAppSource.Contains("private void resetAssemblyCacheState(") -or
    $godotAppSource.Contains("private void logAssemblyCacheState(") -or
    $godotAppSource.Contains("AndroidAssemblyBootstrapper")
) {
    throw "Assembly preparation implementation must not remain in GodotApp."
}
if (
    -not $launcherActivitySource.Contains(
        "new AndroidAssemblyBootstrapper("
    ) -or
    -not $launcherActivitySource.Contains(
        "assemblyBootstrapper.prepare();"
    )
) {
    throw "LauncherActivity must own Android assembly preparation."
}

$javac = Resolve-JavaTool "javac"
$java = Resolve-JavaTool "java"
$androidJar = Resolve-AndroidJar
$classpath = "$output$([System.IO.Path]::PathSeparator)$androidJar"

try {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    & $javac -cp $androidJar -d $output @jsonSources
    if ($LASTEXITCODE -ne 0) {
        throw "javac failed for desktop org.json test support."
    }

    & $javac -cp $classpath -d $output @sources
    if ($LASTEXITCODE -ne 0) {
        throw "javac failed for AndroidAssemblyBootstrapper tests."
    }

    & $java `
        -cp $classpath `
        com.game.sts2launcher.AndroidAssemblyBootstrapperTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidAssemblyBootstrapper tests failed."
    }
} finally {
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}
