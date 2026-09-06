using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDiagnosticsCoordinator
{
    internal void DiagnosticsPressed()
    {
        var path = TryWriteDiagnosticsReport(DiagnosticsReportWrite.ManualExport());
        if (path == null)
            return;

        ShowDiagnosticsExportResult(path);
    }

    private void ShowDiagnosticsExportResult(string path)
    {
        _view.SetStatus("Support report ready. Review it before sharing.", LauncherStatusSeverity.Information);
        _view.AppendLog($"Support report saved: {path}");
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
