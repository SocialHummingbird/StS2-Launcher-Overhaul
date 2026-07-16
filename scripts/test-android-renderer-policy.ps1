param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$source = Join-Path $root "android\src\com\game\sts2launcher\AndroidRendererPolicy.java"
$test = Join-Path $root "scripts\tests\AndroidRendererPolicyTest.java"
$output = Join-Path ([System.IO.Path]::GetTempPath()) ("sts2-renderer-policy-" + [Guid]::NewGuid().ToString("N"))

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

    $codexRuntimeRoot = Join-Path $HOME "AppData\Local\Packages\Microsoft.4297127D64EC6_8wekyb3d8bbwe\LocalCache\Local\runtime"
    $bundled = Get-ChildItem -Path $codexRuntimeRoot -Filter "$Name.exe" -Recurse -ErrorAction SilentlyContinue |
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
    & $javac -d $output $source $test
    if ($LASTEXITCODE -ne 0) {
        throw "javac failed for AndroidRendererPolicy tests."
    }

    & $java -cp $output com.game.sts2launcher.AndroidRendererPolicyTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidRendererPolicy tests failed."
    }
} finally {
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}
