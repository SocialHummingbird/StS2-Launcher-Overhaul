using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int CloudSyncTimeoutMs = 180_000;

    private readonly struct ManualCloudSyncRequest
    {
        private ManualCloudSyncRequest(
            string confirmationMessage,
            string confirmText,
            string cancelText,
            string name,
            string startMessage,
            string completeMessage,
            bool bypassConfirmation,
            Func<Task<string>> run,
            Action? onComplete = null,
            Action<Exception>? onFailed = null
        )
        {
            ConfirmationMessage = confirmationMessage;
            ConfirmText = confirmText;
            CancelText = cancelText;
            Name = name;
            StartMessage = startMessage;
            CompleteMessage = completeMessage;
            BypassConfirmation = bypassConfirmation;
            Run = run;
            OnComplete = onComplete;
            OnFailed = onFailed;
        }

        internal string ConfirmationMessage { get; }
        internal string ConfirmText { get; }
        internal string CancelText { get; }
        internal bool BypassConfirmation { get; }
        private string Name { get; }
        private string StartMessage { get; }
        private string CompleteMessage { get; }
        private Func<Task<string>> Run { get; }
        private Action? OnComplete { get; }
        private Action<Exception>? OnFailed { get; }

        internal static ManualCloudSyncRequest Push(string dataDir, string selectedBranch)
            => new(
                PushConfirmationMessage(dataDir, selectedBranch),
                "PUSH TO CLOUD",
                "CANCEL PUSH",
                "Push",
                "Pushing Android local saves to Steam Cloud...",
                "Push complete. Steam Cloud now reflects Android local saves.",
                false,
                LauncherCloudSaveState.ManualPushAllAsync,
                () => LauncherCloudSyncEvidence.WriteManualPushMarker(dataDir, selectedBranch),
                ex => LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(dataDir, selectedBranch, ex)
            );

        internal static ManualCloudSyncRequest Pull(string dataDir, string selectedBranch)
            => new(
                "Pull Steam Cloud saves to Android local storage?\nThis overwrites Android local saves with the current Steam Cloud state.",
                "PULL FROM CLOUD",
                "CANCEL PULL",
                "Pull",
                "Pulling Steam Cloud saves to Android local storage...",
                "Pull complete. Android local saves now reflect Steam Cloud.",
                true,
                LauncherCloudSaveState.ManualPullAllAsync,
                () => LauncherCloudSyncEvidence.WriteManualPullMarker(dataDir, selectedBranch)
            );

        private static string PushConfirmationMessage(string dataDir, string selectedBranch)
            => "Push Android local saves to Steam Cloud?\n"
                + $"Selected game version: {SteamGameBranch.DisplayName(selectedBranch)}.\n"
                + $"Selected version slot: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)} ({SteamGameBranch.StateDirectoryName(selectedBranch)}).\n"
                + BranchSwitchPushWarning(dataDir, selectedBranch)
                + "This can overwrite Steam Cloud saves for this Steam account. "
                + "Save compatibility across Steam branches is not validated. "
                + "When Local Backup is ON, manual Push backs up important Android local saves and existing Steam Cloud saves before upload. "
                + "Pull from Cloud first and verify the Android saves exist before pushing.";

        private static string BranchSwitchPushWarning(string dataDir, string selectedBranch)
            => LauncherBranchSwitchSafety.HasMarker(dataDir)
                ? "A game version switch was recorded on this install. Treat this Push as cross-version/destructive unless the selected-version safety evidence is current: "
                    + $"Pull-after-switch for {SteamGameBranch.DisplayName(selectedBranch)}, Android local save evidence, backup storage permission, local pre-Push backup evidence, and cloud pre-Push backup evidence.\n"
                : string.Empty;

        internal void ShowStarted(LauncherView view)
        {
            view.SetPushPullDisabled(true);
            view.SetStatus(StartMessage);
            view.AppendLog(StartMessage);
        }

        internal void ShowComplete(LauncherView view, string result)
        {
            OnComplete?.Invoke();
            view.SetStatus(CompleteMessage);
            view.AppendLog($"{CompleteMessage} ({DateTime.Now:HH:mm:ss})");
            if (!string.IsNullOrWhiteSpace(result))
                view.AppendLog(result);
        }

        internal void ShowFailed(LauncherView view, Exception ex)
        {
            OnFailed?.Invoke(ex);
            PatchHelper.Log($"[Cloud] {Name} sync failed: {ex.Message}");
            view.SetStatus($"{Name} failed. See console for details.");
            view.AppendLog($"{Name} failed: {ex.Message}");
        }

        internal void ShowFinished(LauncherView view)
            => view.SetPushPullDisabled(false);

        internal async Task<string> RunWithTimeoutAsync()
        {
            var operationTask = Task.Run(Run);
            await LauncherTimeout.RunOrThrowAsync(
                operationTask,
                CloudSyncTimeoutMs,
                $"{Name} timed out after {CloudSyncTimeoutMs}ms"
            );
            return await operationTask;
        }
    }

    private void CloudSyncToggled(bool pressed)
    {
        LauncherPreferences.SaveCloudSyncEnabled(pressed);
        _view.SetStatus(
            pressed
                ? "Game cloud sync enabled. Manual Push/Pull remains available from the launcher."
                : "Game cloud sync disabled. The game will use Android local saves; manual Push/Pull remains available."
        );
    }

    private void CloudPushPressed()
    {
        if (!CanPushWithBaselineEvidence())
            return;

        if (!CanPushAfterBranchSwitch())
            return;

        RequestCloudSync(ManualCloudSyncRequest.Push(
            _model.DataDir,
            LauncherPreferences.ReadGameBranch()
        ));
    }

    private bool CanArmCloudPush()
    {
        if (!CanPushWithBaselineEvidence())
            return false;

        if (!CanPushAfterBranchSwitch())
            return false;

        return true;
    }

    private bool CanPushWithBaselineEvidence()
    {
        var selectedBranch = LauncherPreferences.ReadGameBranch();
        var selectedVersion = SteamGameBranch.DisplayName(selectedBranch);

        if (
            !LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(_model.DataDir)
            || !LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(_model.DataDir, selectedBranch)
        )
        {
            const string reason = "Manual Push blocked: Pull from Cloud must complete for the selected game version before Push.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus($"Push blocked: Pull from Cloud must complete for selected game version {selectedVersion} before Push.");
            _view.AppendLog($"Push blocked: no current Pull from Cloud evidence exists for selected game version {selectedVersion}.");
            return false;
        }

        if (!LauncherLocalSaveEvidence.HasImportantSaveEvidence(_model.DataDir))
        {
            const string reason = "Manual Push blocked: no Android local save evidence exists before Push.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus($"Push blocked: no Android local save files were found for selected game version {selectedVersion}.");
            _view.AppendLog($"Push blocked: Pull from Cloud first for {selectedVersion}, launch or inspect the game until Android local saves exist, then retry Push.");
            return false;
        }

        if (!LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(_model.DataDir, selectedBranch))
        {
            const string reason = "Manual Push blocked: Android local save origin evidence does not match the selected runtime.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus($"Push blocked: Android local save origin is not verified for the selected {selectedVersion} runtime.");
            _view.AppendLog($"Push blocked: Pull from Cloud for {selectedVersion} must complete against this exact PCK/runtime assembly before Push can upload Android local saves.");
            return false;
        }

        return true;
    }

    private bool CanPushAfterBranchSwitch()
    {
        if (!LauncherBranchSwitchSafety.HasMarker(_model.DataDir))
            return true;

        LauncherPreferences.SaveLocalBackupEnabled(true);
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences());
        var selectedBranch = LauncherPreferences.ReadGameBranch();

        if (!LauncherBranchSwitchSafety.HasRequiredEvidence(_model.DataDir, selectedBranch))
        {
            const string reason = "Manual Push blocked: branch-switch safety marker is incomplete, unreadable, or does not match the selected game version.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus("Push blocked: branch switch marker is missing required safety evidence.");
            _view.AppendLog("Push blocked after branch switch: branch-switch safety marker is incomplete, unreadable, or does not match the selected game version; switch versions again or rebuild validation evidence before pushing.");
            return false;
        }

        if (!LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(_model.DataDir, selectedBranch))
        {
            const string reason = "Manual Push blocked: no current Pull-after-switch evidence exists for the selected game version.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus("Push blocked: branch switch detected. Pull from Cloud must complete after this game-version switch before Push.");
            _view.AppendLog("Push blocked after branch switch: no current manual Pull evidence marker exists for the selected game version.");
            return false;
        }

        if (!LauncherLocalSaveEvidence.HasImportantSaveEvidence(_model.DataDir))
        {
            const string reason = "Manual Push blocked: no Android local save evidence exists after branch switch.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus("Push blocked: branch switch detected but no Android local save files were found.");
            _view.AppendLog("Push blocked after branch switch: Pull from Cloud first, launch or inspect the game until Android local saves exist, then retry Push.");
            return false;
        }

        if (!LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(_model.DataDir, selectedBranch))
        {
            const string reason = "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch.";
            LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(_model.DataDir, selectedBranch, reason);
            _view.SetStatus("Push blocked: branch switch detected but Android local save origin is not verified for the selected runtime.");
            _view.AppendLog("Push blocked after branch switch: Pull from Cloud for the selected version must complete against this exact PCK/runtime assembly before Push can upload Android local saves.");
            return false;
        }

        if (STS2Mobile.AppPaths.HasStoragePermission())
        {
            STS2Mobile.AppPaths.EnsureExternalDirectories();
            return true;
        }

        STS2Mobile.AppPaths.RequestStoragePermission();
        LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(
            _model.DataDir,
            selectedBranch,
            "Manual Push blocked: backup storage permission is unavailable after branch switch."
        );
        _view.SetStatus("Push blocked: branch switch detected. Grant local backup storage permission before pushing to Steam Cloud.");
        _view.AppendLog("Push blocked after branch switch: local-pre-push backup evidence cannot be written until storage permission is granted.");
        return false;
    }

    private void CloudPullPressed()
        => RequestCloudSync(ManualCloudSyncRequest.Pull(
            _model.DataDir,
            LauncherPreferences.ReadGameBranch()
        ));

    private void RequestCloudSync(ManualCloudSyncRequest request)
    {
        _model.RefreshCloudSaveCredentials();

        if (request.BypassConfirmation)
        {
            _ = ExecuteCloudSyncAsync(request);
            return;
        }

        _view.ShowConfirmation(
            request.ConfirmationMessage,
            () => _ = ExecuteCloudSyncAsync(request),
            request.ConfirmText,
            request.CancelText
        );
    }

    private async Task ExecuteCloudSyncAsync(ManualCloudSyncRequest request)
    {
        RunOnMainThread(() => request.ShowStarted(_view));

        try
        {
            var result = await request.RunWithTimeoutAsync();
            RunOnMainThread(() => request.ShowComplete(_view, result));
        }
        catch (Exception ex)
        {
            RunOnMainThread(() => request.ShowFailed(_view, ex));
        }
        finally
        {
            RunOnMainThread(() => request.ShowFinished(_view));
        }
    }

    private void RunOnMainThread(Action action)
        => _runOnMainThread(action);
}
