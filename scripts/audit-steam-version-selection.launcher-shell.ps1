function Add-SteamVersionSelectionLauncherShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.cs" `
        "keeps launcher root control as a thin partial wrapper around MVC state" `
        @(
            "internal sealed partial class LauncherUI : Control",
            "AutoLaunchVariable",
            "AutoSafeLaunchVariable",
            "LauncherZIndex",
            "DefaultViewportSize",
            "ConcurrentQueue<Action>",
            "LauncherModel _model",
            "LauncherView _view",
            "LauncherController _controller",
            "SetGameMode",
            "WaitForLaunch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.Lifecycle.cs" `
        "isolates launcher lifecycle registration and controller startup" `
        @(
            "internal void Initialize",
            "AndroidBridgeDispatcher\.RegisterCurrentThread",
            "LauncherLayoutProfile\.ForViewport",
            "ResolveLauncherDataDirectory",
            "new LauncherController",
            "tree\.AutoAcceptQuit = false",
            "tree\.ProcessFrame \+= OnProcessFrame",
            "Callable\.From\(StartControllerSafely\)\.CallDeferred",
            "StartControllerSafely",
            "AutoLaunchIfRequested",
            "TreeExiting \+= OnExitTree",
            "AndroidBridgeDispatcher\.UnregisterCurrentThread"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.MainThread.cs" `
        "isolates main-thread queue pumping from lifecycle wiring" `
        @(
            "OnProcessFrame",
            "AndroidBridgeDispatcher\.Pump",
            "DrainMainThreadActions",
            "SyncViewportSize",
            "UpdateKeyboardOffset",
            "EnqueueMainThreadAction",
            "_mainThreadActions\.Enqueue",
            "_mainThreadActions\.TryDequeue",
            "UI update error"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.Viewport.cs" `
        "isolates viewport synchronization and fallback sizing" `
        @(
            "SyncViewportSize",
            "GetViewportSize",
            "DistanceSquaredTo",
            "UpdateViewportSize",
            "DefaultViewportSize"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.Paths.cs" `
        "isolates launcher data-directory resolution for Android and desktop" `
        @(
            "ResolveLauncherDataDirectory",
            "OperatingSystem\.IsAndroid",
            "AndroidGodotAppBridge\.GetInternalFilesDirPath",
            "STS2_ANDROID_FILES_DIR",
            "System\.Environment\.GetEnvironmentVariable",
            "OS\.GetDataDir",
            "BootstrapTrace\.ResolveFallbackDataDirectory"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.AutoLaunch.cs" `
        "isolates launch-request environment handling from controller startup" `
        @(
            "AutoLaunchIfRequested",
            "_inGameMode",
            "Environment\.GetEnvironmentVariable\(AutoLaunchVariable\)",
            "Environment\.SetEnvironmentVariable\(AutoLaunchVariable, ""0""\)",
            "Environment\.GetEnvironmentVariable\(AutoSafeLaunchVariable\)",
            "Environment\.SetEnvironmentVariable\(AutoSafeLaunchVariable, ""0""\)",
            "LaunchSafe",
            "_model\.Launch"
        )
}
