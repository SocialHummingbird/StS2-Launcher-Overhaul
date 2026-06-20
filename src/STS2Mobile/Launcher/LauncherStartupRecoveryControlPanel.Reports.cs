using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct StartupRecoveryDiagnosticsReport
    {
        private StartupRecoveryDiagnosticsReport(string dataDir)
        {
            DataDir = dataDir;
        }

        private string DataDir { get; }

        private static StartupRecoveryDiagnosticsReport Current()
            => new(OS.GetDataDir());

        private string Export()
        {
            var path = LauncherDiagnostics.WriteStartupRecoveryDiagnosticsReport(
                DataDir
            );
            PatchHelper.Log($"Startup recovery diagnostics written: {path}");
            return DiagnosticsExportResult.MessageFor(
                LauncherSharedTextFile.Share(path)
            );
        }

        private string CopyRawErrorLog()
            => RawErrorLogCopy.CopyAndDescribe(
                LauncherDiagnostics.BuildStartupRecoveryDiagnosticsText(DataDir)
            );

        internal static string ExportCurrent()
            => Current().Export();

        internal static string CopyCurrentRawErrorLog()
            => Current().CopyRawErrorLog();
    }

    private readonly struct DiagnosticsExportResult
    {
        private DiagnosticsExportResult(LauncherSharedTextFile file)
        {
            File = file;
        }

        private LauncherSharedTextFile File { get; }

        private string Message()
            => File.Shared
                ? $"Help report ready and share sheet opened.\n\nSaved at:\n{File.Path}"
                : $"Help report ready, but the share sheet did not open.\n\nSaved at:\n{File.Path}";

        internal static string MessageFor(LauncherSharedTextFile file)
            => new DiagnosticsExportResult(file).Message();
    }

    private readonly struct RawErrorLogCopy
    {
        private RawErrorLogCopy(string text)
        {
            ClipboardText = new LauncherClipboardText(text);
        }

        private LauncherClipboardText ClipboardText { get; }
        private int Length => ClipboardText.Length;

        private void CopyToClipboard()
            => ClipboardText.CopyToClipboard();

        private void LogCopied()
            => PatchHelper.Log(
                $"Startup recovery launcher log copied ({Length:N0} chars)"
            );

        private string Message()
            => "Launcher log copied to clipboard. Review/redact before public posting."
                + $"\n\nLength: {Length:N0} characters";

        internal static string CopyAndDescribe(string text)
        {
            var rawLog = new RawErrorLogCopy(text);
            rawLog.CopyToClipboard();
            rawLog.LogCopied();
            return rawLog.Message();
        }
    }

    private static string ExportDiagnosticsReport()
        => StartupRecoveryDiagnosticsReport.ExportCurrent();

    private static string CopyRawErrorLogReport()
        => StartupRecoveryDiagnosticsReport.CopyCurrentRawErrorLog();
}
