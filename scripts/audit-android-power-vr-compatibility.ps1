param(
    [switch]$Quiet,
    [switch]$ThrowOnFailure
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-Check `
    "patches\godot\power-vr-transform-feedback-cache.patch" `
    "backports the Godot 4.5.2 all-PowerVR transform-feedback cache workaround" `
    @(
        'rendering_device_name\.contains\(\"PowerVR\"\)',
        'disable_transform_feedback_shader_cache = true',
        'PowerVR renderer detected; transform feedback shader cache disabled'
    )

Add-ForbiddenCheck `
    "patches\godot\power-vr-transform-feedback-cache.patch" `
    "does not retain the single-model PowerVR exclusion" `
    @('(?m)^\+.*rendering_device_name == \"PowerVR Rogue GE8320\"')

Add-Check `
    ".github\workflows\android-release.yml" `
    "builds the repository Godot patch set for release APKs" `
    @(
        'Build patched Godot Android runtime',
        'scripts/setup-godot-source\.sh',
        'scripts/build-godot\.sh',
        'ARCHES: arm64'
    )

Add-ForbiddenCheck `
    ".github\workflows\android-release.yml" `
    "does not restore a stale historical Godot binary" `
    @('patched_godot_apk_url', 'v0\.2\.88-apk-native-verify')

Add-Check `
    "android\src\com\game\sts2launcher\AndroidRendererPolicy.java" `
    "defines truthful Auto, Vulkan, OpenGL, and Safe Start command-line plans" `
    @(
        'static final String AUTO = \"auto\"',
        'return new Plan\(VULKAN, VULKAN, false, \"vulkan\", \"mobile\"\)',
        'return new Plan\(OPENGL, OPENGL, false, \"opengl3\", \"gl_compatibility\"\)',
        'return new Plan\(normalizedPreference, AUTO, true, null, null\)'
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "applies and persists the selected renderer plan" `
    @(
        'AndroidRendererPolicy\.resolve',
        'rendererPlan\.appendCommandLine\(commands\)',
        'recordRendererAttempt\(rendererPlan, safeLaunch\)',
        'LAST_RENDERER_ATTEMPT_FILE'
    )

Add-ForbiddenCheck `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "removes the unreachable game-startup-completed renderer branch" `
    @('previousStartupPhaseWas', 'boolean useDefaultRenderer')

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "captures historical Android process exit reasons and traces" `
    @(
        'getHistoricalProcessExitReasons',
        'ApplicationExitInfo\.REASON_CRASH_NATIVE',
        'ApplicationExitInfo\.REASON_LOW_MEMORY',
        'getTraceInputStream',
        'LAST_PROCESS_EXIT_INFO_FILE'
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Renderer.cs" `
    "exposes a mutually exclusive renderer mode control" `
    @(
        'new ButtonGroup',
        'LauncherRendererMode\.Auto',
        'LauncherRendererMode\.Vulkan',
        'LauncherRendererMode\.OpenGl'
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.AttachmentLists.cs" `
    "includes renderer and process-exit evidence in support logs" `
    @(
        'RendererAttempt\(dataDir\)',
        'ProcessExitInfo\(dataDir\)'
    )

Complete-StaticAudit `
    -FailureHeading "Android PowerVR compatibility audit failed" `
    -SuccessMessage "Android PowerVR compatibility audit passed ({0} checks)." `
    -ThrowOnFailure:$ThrowOnFailure
