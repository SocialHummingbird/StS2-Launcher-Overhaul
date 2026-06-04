using Godot;
using STS2Mobile;
using StartupRecoveryReport =
    STS2Mobile.Launcher.LauncherDiagnostics.StartupRecoveryDiagnosticsReport;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct StartupRecoveryReportSession
    {
        private StartupRecoveryReportSession(StartupRecoveryReport report)
        {
            Report = report;
        }

        private StartupRecoveryReport Report { get; }

        internal static StartupRecoveryReportSession Capture()
            => new(LauncherDiagnostics.StartupRecoveryReport(OS.GetDataDir()));

        internal string ExportDiagnostics()
        {
            var path = Report.Write();
            PatchHelper.Log($"Startup recovery diagnostics written: {path}");
            var shared = AndroidGodotAppBridge.ShareTextFile(path);
            return ExportDiagnosticsMessage(path, shared);
        }

        internal string CopyRawErrorLog()
        {
            var text = Report.BuildText();
            DisplayServer.ClipboardSet(text);
            PatchHelper.Log(
                $"Startup recovery raw error log copied ({text.Length:N0} chars)"
            );
            return RawErrorLogCopiedMessage(text.Length);
        }
    }
}
