param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$godotSource = Join-Path $root "android\src\com\game\sts2launcher\GodotApp.java"
$launcherSource = Join-Path $root "android\src\com\game\sts2launcher\LauncherActivity.java"
$routeGateSource = Join-Path $root (
    "android\src\com\game\sts2launcher\AndroidStartupRouteGate.java"
)
$pendingLaunchStateSource = Join-Path $root (
    "android\src\com\game\sts2launcher\AndroidPendingLaunchState.java"
)
$recoveryControllerSource = Join-Path $root (
    "android\src\com\game\sts2launcher\AndroidNativeRecoveryController.java"
)
$lifecycleTest = Join-Path $root "scripts\tests\GodotActivityLifecycleRegressionTest.java"
$routingTest = Join-Path $root "scripts\tests\AndroidStartupRoutingRegressionTest.java"
$recoveryRoutingTest = Join-Path $root (
    "scripts\tests\AndroidNativeRecoveryRoutingTest.java"
)
$fallbackSource = Join-Path $root (
    "android\src\com\game\sts2launcher\NativeFallbackActivity.java"
)
$output = Join-Path ([System.IO.Path]::GetTempPath()) (
    "sts2-godot-lifecycle-" + [Guid]::NewGuid().ToString("N")
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

    $codexRuntimeRoot = Join-Path $HOME (
        "AppData\Local\Packages\Microsoft.4297127D64EC6_8wekyb3d8bbwe" +
        "\LocalCache\Local\runtime"
    )
    $bundled = Get-ChildItem `
        -Path $codexRuntimeRoot `
        -Filter "$Name.exe" `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($bundled) {
        return $bundled.FullName
    }

    throw "Could not locate $Name. Set JAVA_HOME to a JDK 17 or newer."
}

$javac = Resolve-JavaTool "javac"
$java = Resolve-JavaTool "java"

try {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    & $javac -d $output `
        $routeGateSource `
        $pendingLaunchStateSource `
        $recoveryControllerSource `
        $lifecycleTest `
        $routingTest `
        $recoveryRoutingTest
    if ($LASTEXITCODE -ne 0) {
        throw "javac failed for Android startup lifecycle regression tests."
    }

    & $java `
        -cp $output `
        com.game.sts2launcher.GodotActivityLifecycleRegressionTest `
        $godotSource
    if ($LASTEXITCODE -ne 0) {
        throw "Godot activity lifecycle regression tests failed."
    }

    & $java `
        -cp $output `
        com.game.sts2launcher.AndroidStartupRoutingRegressionTest `
        $launcherSource `
        $godotSource
    if ($LASTEXITCODE -ne 0) {
        throw "Android startup routing regression tests failed."
    }

    & $java `
        -cp $output `
        com.game.sts2launcher.AndroidNativeRecoveryRoutingTest `
        $fallbackSource `
        $launcherSource
    if ($LASTEXITCODE -ne 0) {
        throw "Android native recovery routing tests failed."
    }
} finally {
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}
