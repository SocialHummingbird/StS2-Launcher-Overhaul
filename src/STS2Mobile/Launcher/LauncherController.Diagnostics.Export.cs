using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void DiagnosticsPressed()
    {
        var path = WriteDiagnosticsReportOrNull(
            "Diagnostics export failed",
            logFullException: true,
            message => _view.SetStatus($"Diagnostics export failed: {message}")
        );
        if (path == null)
            return;

        _view.SetStatus("Diagnostics exported.");
        _view.AppendLog($"Diagnostics exported: {path}");
        ShareDiagnosticsIfAndroid(path);
    }

    private void ShareDiagnosticsIfAndroid(string path)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        _view.AppendLog(
            shared ? "Android share sheet opened." : "Could not open Android share sheet."
        );
    }

    private string? WriteDiagnosticsReportOrNull(
        string failureContext,
        bool logFullException,
        Action<string>? onFailure = null
    )
    {
        try
        {
            return _model.WriteDiagnosticsReport();
        }
        catch (Exception ex)
        {
            LogDiagnosticsFailure(failureContext, ex, logFullException);
            onFailure?.Invoke(ex.Message);
            return null;
        }
    }
}
