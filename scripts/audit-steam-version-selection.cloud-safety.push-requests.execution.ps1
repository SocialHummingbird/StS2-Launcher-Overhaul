function Add-SteamVersionSelectionCloudSafetyPushExecutionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.Lifecycle.cs" `
        "keeps manual cloud sync lifecycle UI updates and timeout execution isolated" `
        @(
            "ShowStarted",
            "ShowComplete",
            "ShowFailed",
            "ShowFinished",
            "RunWithTimeoutAsync",
            "SetPushPullDisabled\(true\)",
            "SetPushPullDisabled\(false\)",
            "Open Help & Reports for details",
            "PatchHelper\.Log",
            "LauncherTimeout\.RunOrThrowAsync",
            "CloudSyncTimeoutMs"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Execution.cs" `
        "executes Pull/Push cloud sync requests without bypassing Push confirmation" `
        @(
            "ManualCloudSyncRequest\.Pull\(",
            "RequestCloudSync",
            "ExecuteCloudSyncAsync",
            "request\.BypassConfirmation",
            "ShowConfirmation",
            "RunOnMainThread"
        )
}
