function Add-SteamVersionSelectionStartupWarmupStatusCleanupChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.cs" `
        "cleans up the full Android startup status card after observed game startup" `
        @(
            "internal static bool QueueFree\(Label label\)",
            "FindStatusRoot\(label\)",
            "for \(Node current = label; current != null; current = current\.GetParent\(\)\)",
            "current\.Name == NodeName",
            "target\.QueueFree\(\)",
            "Startup status cleanup failed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.State.cs" `
        "uses startup-status root cleanup instead of freeing only the message label" `
        @(
            "LauncherStartupStatus\.QueueFree\(StartupStatus\)",
            "Post-startup recovery UI cleanup finished after game startup was observed",
            "statusCleared"
        )
}
