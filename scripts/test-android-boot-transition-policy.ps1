param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sources = @(
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootIdentityLayout.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootIdentityState.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootSequence.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionCue.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionSound.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionSoundSession.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\SilentBootTransitionSound.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionPolicy.java")
)
$tests = @(
    (Join-Path $root "scripts\tests\AndroidBootIdentityLayoutTest.java"),
    (Join-Path $root "scripts\tests\AndroidBootIdentityStateTest.java"),
    (Join-Path $root "scripts\tests\AndroidBootSequenceTest.java"),
    (Join-Path $root "scripts\tests\AndroidBootTransitionSoundTest.java"),
    (Join-Path $root "scripts\tests\AndroidBootTransitionPolicyTest.java")
)
$output = Join-Path ([System.IO.Path]::GetTempPath()) ("sts2-boot-transition-policy-" + [Guid]::NewGuid().ToString("N"))

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

function Assert-SourceContains(
    [string]$Text,
    [string]$Expected,
    [string]$Message
) {
    if (-not $Text.Contains($Expected)) {
        throw $Message
    }
}

$soundSources = @(
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionCue.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionSound.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionSoundSession.java"),
    (Join-Path $root "android\src\com\game\sts2launcher\SilentBootTransitionSound.java")
)
$forbiddenAudioApi = Select-String -Path $soundSources -Pattern @(
    "android\.media",
    "AudioFocus",
    "AudioManager",
    "AudioTrack",
    "MediaPlayer",
    "SoundPool"
)
if ($forbiddenAudioApi) {
    throw "Silent boot transition boundary references an Android playback or audio-focus API."
}

$controllerSource = Get-Content -LiteralPath (
    Join-Path $root "android\src\com\game\sts2launcher\AndroidBootTransitionController.java"
) -Raw
$godotAppSource = Get-Content -LiteralPath (
    Join-Path $root "android\src\com\game\sts2launcher\GodotApp.java"
) -Raw
$sequenceSource = Get-Content -LiteralPath (
    Join-Path $root "android\src\com\game\sts2launcher\AndroidBootSequence.java"
) -Raw

Assert-SourceContains $sequenceSource "FULL_DURATION_MS = 4_000L;" `
    "Full boot transition duration must remain exactly 4,000 ms."
Assert-SourceContains $sequenceSource "REDUCED_MOTION_DURATION_MS = 200L;" `
    "Reduced-motion boot transition duration must remain exactly 200 ms."
Assert-SourceContains $controllerSource "WATCHDOG_MS = 30_000L;" `
    "Boot transition must retain its 30-second fail-open watchdog."
Assert-SourceContains $controllerSource "FALLBACK_SPLASH_CONTAINER_DP = 198;" `
    "Boot transition must retain the Samsung splash sizing fallback."
Assert-SourceContains $controllerSource "processTransitionConsumed = true;" `
    "Boot transition must remain consumed after the first process launch."
Assert-SourceContains $controllerSource "overlay.setClickable(true);" `
    "Boot overlay must remain clickable while it blocks launcher input."
Assert-SourceContains $controllerSource "overlay.setFocusableInTouchMode(true);" `
    "Boot overlay must retain keyboard focus suppression."
Assert-SourceContains $controllerSource "overlay.setOnTouchListener((view, event) -> true);" `
    "Boot overlay must consume pointer input while visible."
Assert-SourceContains $controllerSource "inputMethodManager.hideSoftInputFromWindow" `
    "Boot overlay must continue hiding the soft keyboard."
Assert-SourceContains $controllerSource "mainHandler.postDelayed(watchdog, WATCHDOG_MS);" `
    "Boot overlay attachment must arm the fail-open watchdog."
Assert-SourceContains $controllerSource "destroyed || readinessGate.isTerminal()" `
    "Terminal or destroyed transitions must reject repeated splash-exit callbacks."
Assert-SourceContains $controllerSource "mainHandler.removeCallbacks(watchdog);" `
    "Boot completion and destruction must cancel watchdog callbacks."
Assert-SourceContains $controllerSource "ViewParentCompat.removeFromParent(overlay);" `
    "Boot completion must remove the input-blocking overlay from the view hierarchy."
Assert-SourceContains $controllerSource "soundSession.pause();" `
    "Activity pause must reach the future boot sound boundary."
Assert-SourceContains $controllerSource "soundSession.resume();" `
    "Activity resume must reach the future boot sound boundary."
Assert-SourceContains $godotAppSource "bootTransitionController.pauseSound();" `
    "GodotApp.onPause must forward to the boot sound boundary."
Assert-SourceContains $godotAppSource "bootTransitionController.resumeSound();" `
    "GodotApp.onResume must forward to the boot sound boundary."
Assert-SourceContains $godotAppSource "bootTransitionController.destroy();" `
    "GodotApp.onDestroy must retain boot transition cleanup."
Assert-SourceContains $godotAppSource `
    "intent.putExtra(AndroidBootTransitionPolicy.SKIP_INTENT_EXTRA, true);" `
    "Deliberate app restarts must retain the boot transition skip extra."

try {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    & $javac -d $output @sources @tests
    if ($LASTEXITCODE -ne 0) {
        throw "javac failed for Android boot transition tests."
    }

    & $java -cp $output com.game.sts2launcher.AndroidBootTransitionPolicyTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidBootTransitionPolicy tests failed."
    }
    & $java -cp $output com.game.sts2launcher.AndroidBootIdentityLayoutTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidBootIdentityLayout tests failed."
    }
    & $java -cp $output com.game.sts2launcher.AndroidBootIdentityStateTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidBootIdentityState tests failed."
    }
    & $java -cp $output com.game.sts2launcher.AndroidBootSequenceTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidBootSequence tests failed."
    }
    & $java -cp $output com.game.sts2launcher.AndroidBootTransitionSoundTest
    if ($LASTEXITCODE -ne 0) {
        throw "AndroidBootTransitionSound tests failed."
    }
} finally {
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}
