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
        _view.SetStatus("Help report ready.");
        _view.AppendLog($"Help report saved: {path}");
        _view.ShowDiagnosticsConsole();
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
