using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void DiagnosticsPressed()
    {
        var path = TryWriteDiagnosticsReport(
            "Diagnostics export failed",
            message => _view.SetStatus($"Diagnostics export failed: {message}")
        );
        if (path == null)
            return;

        ShowDiagnosticsExportResult(path);
    }

    private void ShowDiagnosticsExportResult(string path)
    {
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
}
