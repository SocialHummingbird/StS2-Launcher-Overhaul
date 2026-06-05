using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void DiagnosticsPressed()
    {
        var path = TryWriteDiagnosticsReport(DiagnosticsReportWrite.ManualExport());
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

        _view.AppendLog(
            LauncherSharedTextFile.Share(path).AndroidShareSheetLogMessage()
        );
    }
}
