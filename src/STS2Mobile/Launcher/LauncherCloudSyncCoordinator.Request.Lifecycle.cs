using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly partial struct ManualCloudSyncRequest
    {
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
            view.SetStatus($"{Name} failed. Open Help & Reports for details.");
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
}
