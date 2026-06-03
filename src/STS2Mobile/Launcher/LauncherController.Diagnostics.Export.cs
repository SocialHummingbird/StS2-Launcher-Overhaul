using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct DiagnosticsExportResult
    {
        private DiagnosticsExportResult(string path)
        {
            Path = path;
        }

        private string Path { get; }

        internal static DiagnosticsExportResult Create(string path)
            => new(path);

        internal void Apply(LauncherView view)
        {
            view.SetStatus("Diagnostics exported.");
            view.AppendLog($"Diagnostics exported: {Path}");
            ShareIfAndroid(view);
        }

        private void ShareIfAndroid(LauncherView view)
        {
            if (!OperatingSystem.IsAndroid())
                return;

            var shared = AndroidGodotAppBridge.ShareTextFile(Path);
            view.AppendLog(
                shared ? "Android share sheet opened." : "Could not open Android share sheet."
            );
        }
    }

    private void DiagnosticsPressed()
    {
        var path = TryGetDiagnosticsResult(
            "Diagnostics export failed",
            logFullException: true,
            _model.WriteDiagnosticsReport,
            message => _view.SetStatus($"Diagnostics export failed: {message}")
        );
        if (path == null)
            return;

        DiagnosticsExportResult.Create(path).Apply(_view);
    }
}
