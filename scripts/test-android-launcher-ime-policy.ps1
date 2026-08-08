param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$policySource = Join-Path $root (
    "android\src\com\game\sts2launcher\AndroidLauncherImePolicy.java"
)
$policyTest = Join-Path $root (
    "scripts\tests\AndroidLauncherImePolicyTest.java"
)
$godotAppSource = Get-Content -LiteralPath (
    Join-Path $root "android\src\com\game\sts2launcher\GodotApp.java"
) -Raw
$imeControllerSource = Get-Content -LiteralPath (
    Join-Path $root (
        "android\src\com\game\sts2launcher\AndroidLauncherImeController.java"
    )
) -Raw
$bootControllerSource = Get-Content -LiteralPath (
    Join-Path $root (
        "android\src\com\game\sts2launcher\AndroidBootTransitionController.java"
    )
) -Raw
$managedBridgeSource = Get-Content -LiteralPath (
    Join-Path $root "src\STS2Mobile\AndroidGodotAppBridge.cs"
) -Raw
$lineEditSource = Get-Content -LiteralPath (
    Join-Path $root (
        "src\STS2Mobile\Launcher\Components\StyledLineEdit.cs"
    )
) -Raw
$launcherUiSource = Get-Content -LiteralPath (
    Join-Path $root "src\STS2Mobile\Launcher\LauncherUI.Lifecycle.cs"
) -Raw
$launcherViewKeyboardSource = Get-Content -LiteralPath (
    Join-Path $root (
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs"
    )
) -Raw
$output = Join-Path ([System.IO.Path]::GetTempPath()) (
    "sts2-launcher-ime-policy-" + [Guid]::NewGuid().ToString("N")
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

    $localToolchain = Join-Path `
        -Path $HOME `
        -ChildPath ".w40k-android-toolchain\jdk-17\bin\$Name.exe"
    if (Test-Path -LiteralPath $localToolchain) {
        return $localToolchain
    }

    throw "Could not locate $Name. Set JAVA_HOME to a JDK 17 or newer."
}

function Assert-Contains(
    [string]$Text,
    [string]$Expected,
    [string]$Message
) {
    if (-not $Text.Contains($Expected)) {
        throw $Message
    }
}

function Assert-Before(
    [string]$Text,
    [string]$First,
    [string]$Second,
    [string]$Message
) {
    $firstIndex = $Text.IndexOf($First)
    $secondIndex = $Text.IndexOf($Second)
    if ($firstIndex -lt 0 -or $secondIndex -lt 0 -or $firstIndex -ge $secondIndex) {
        throw $Message
    }
}

Assert-Contains $godotAppSource "launcherImeController.onLauncherStartup();" `
    "GodotApp must apply launcher IME policy during startup."
Assert-Contains $godotAppSource "launcherImeController.onResume();" `
    "GodotApp must apply launcher IME policy on resume."
Assert-Contains $godotAppSource `
    "launcherImeController.onWindowFocusChanged(hasFocus);" `
    "GodotApp must apply launcher IME policy when window focus changes."
Assert-Contains $godotAppSource "launcherImeController.onBootTransitionCleanup()" `
    "Boot-overlay cleanup must return focus through launcher IME policy."
Assert-Contains $godotAppSource "notifyLauncherTextEditingRequested" `
    "GodotApp must expose explicit launcher text-editing intent."
Assert-Contains $godotAppSource "notifyLauncherUiActive" `
    "GodotApp must expose launcher visibility to avoid suppressing game input."
Assert-Contains $imeControllerSource "SOFT_INPUT_STATE_ALWAYS_HIDDEN" `
    "Launcher IME suppression must set a persistent hidden soft-input state."
Assert-Contains $imeControllerSource "WindowInsets.Type.ime()" `
    "Launcher IME suppression must hide API 30+ IME window insets."
Assert-Contains $imeControllerSource "EDITOR_EXIT_GRACE_MS = 80L" `
    "Launcher editor release must retain a short field-to-field focus grace."
Assert-Contains $imeControllerSource "DELAYED_SUPPRESSION_MS = 240L" `
    "Launcher IME suppression must use one bounded delayed verification."
Assert-Contains $imeControllerSource "cancelPendingSuppressionLocked();" `
    "Launcher IME lifecycle changes must cancel pending suppression callbacks."
Assert-Contains $bootControllerSource "presentationCleanup.run();" `
    "Boot transition completion and timeout must notify IME cleanup."
Assert-Contains $managedBridgeSource "NotifyLauncherTextEditingRequested" `
    "Managed Android bridge must expose launcher text-editing intent."
Assert-Contains $managedBridgeSource "NotifyLauncherUiActive" `
    "Managed Android bridge must expose launcher visibility."
Assert-Contains $lineEditSource "FocusEntered += OnFocusEntered;" `
    "Launcher LineEdit focus must declare genuine text-editing intent."
Assert-Contains $lineEditSource "FocusExited += OnFocusExited;" `
    "Launcher LineEdit focus exit must clear text-editing intent."
Assert-Contains $launcherUiSource "NotifyLauncherUiActive(true);" `
    "Managed launcher initialization must activate launcher-only IME policy."
Assert-Contains $launcherUiSource "NotifyLauncherUiActive(false);" `
    "Managed launcher teardown must release launcher-only IME policy."
Assert-Before $launcherUiSource `
    "AndroidBridgeDispatcher.RegisterCurrentThread();" `
    "AndroidGodotAppBridge.NotifyLauncherUiActive(true);" `
    "Managed launcher must register its Android bridge dispatcher before signalling IME state."
Assert-Contains $launcherViewKeyboardSource "DisplayServer.VirtualKeyboardHide();" `
    "Outside taps must explicitly dismiss Godot's virtual keyboard state."
Assert-Contains $launcherViewKeyboardSource `
    "NotifyLauncherTextEditingRequested(false);" `
    "Outside taps must clear launcher text-editing intent."

$javac = Resolve-JavaTool "javac"
$java = Resolve-JavaTool "java"
try {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    & $javac -d $output $policySource $policyTest
    if ($LASTEXITCODE -ne 0) {
        throw "javac failed for Android launcher IME policy tests."
    }

    & $java -cp $output com.game.sts2launcher.AndroidLauncherImePolicyTest
    if ($LASTEXITCODE -ne 0) {
        throw "Android launcher IME policy tests failed."
    }
} finally {
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}
