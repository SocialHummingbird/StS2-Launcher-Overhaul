using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void RunDiagnosticsAction(string failureContext, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            LogDiagnosticsFailure(failureContext, ex.ToString());
            _view.SetStatus($"{failureContext}: {ex.Message}");
        }
    }

    private string TryWriteDiagnosticsReport(
        string failureContext,
        Action<string> onFailure = null
    )
    {
        try
        {
            return _model.WriteDiagnosticsReport();
        }
        catch (Exception ex)
        {
            LogDiagnosticsFailure(failureContext, ex.Message);
            if (onFailure != null)
                onFailure(ex.Message);
            return default;
        }
    }

    private static void LogDiagnosticsFailure(
        string context,
        string detail
    )
        => PatchHelper.Log($"[Launcher] {context}: {detail}");
}
