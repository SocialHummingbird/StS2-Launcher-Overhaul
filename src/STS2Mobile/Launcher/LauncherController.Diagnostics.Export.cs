using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void DiagnosticsPressed()
    {
        if (!TryWriteDiagnosticsReport(
            "Diagnostics export failed",
            logFullException: true,
            out var path,
            out var failureMessage
        ))
        {
            _view.SetStatus($"Diagnostics export failed: {failureMessage}");
            return;
        }

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

    private bool TryWriteDiagnosticsReport(
        string failureContext,
        bool logFullException,
        out string path,
        out string failureMessage
    )
    {
        try
        {
            path = _model.WriteDiagnosticsReport();
            failureMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            LogDiagnosticsFailure(failureContext, ex, logFullException);
            path = null;
            failureMessage = ex.Message;
            return false;
        }
    }
}
