using Godot;
using STS2Mobile;
using StartupRecoveryReport =
    STS2Mobile.Launcher.LauncherDiagnostics.StartupRecoveryDiagnosticsReport;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static StartupRecoveryReport CaptureStartupRecoveryReport()
        => LauncherDiagnostics.StartupRecoveryReport(OS.GetDataDir());

    private static string ExportDiagnosticsReport(StartupRecoveryReport report)
    {
        var path = report.Write();
        PatchHelper.Log($"Startup recovery diagnostics written: {path}");
        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        return ExportDiagnosticsMessage(path, shared);
    }

    private static string CopyRawErrorLogReport(StartupRecoveryReport report)
    {
        var text = report.BuildText();
        DisplayServer.ClipboardSet(text);
        PatchHelper.Log(
            $"Startup recovery raw error log copied ({text.Length:N0} chars)"
        );
        return RawErrorLogCopiedMessage(text.Length);
    }

    private static string ExportDiagnosticsMessage(string path, bool shared)
        => shared
            ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
            : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";

    private static string RawErrorLogCopiedMessage(int length)
        => $"Raw error log copied to clipboard.\n\nLength: {length:N0} characters";
}
