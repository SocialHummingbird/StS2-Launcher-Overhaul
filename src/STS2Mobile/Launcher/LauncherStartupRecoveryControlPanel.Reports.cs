using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static string ExportDiagnosticsReport()
    {
        var path = LauncherDiagnostics.WriteStartupRecoveryDiagnosticsReport(
            OS.GetDataDir()
        );
        PatchHelper.Log($"Startup recovery diagnostics written: {path}");
        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        return ExportDiagnosticsMessage(path, shared);
    }

    private static string CopyRawErrorLogReport()
    {
        var text = LauncherDiagnostics.BuildStartupRecoveryDiagnosticsText(
            OS.GetDataDir()
        );
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
