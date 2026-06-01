using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private void ExportDiagnostics()
        => RunAction(
            "diagnostics export",
            "Diagnostics export failed",
            ExportDiagnosticsCore
        );

    private void CopyRawErrorLog()
        => RunAction(
            "raw error log copy",
            "Raw error log copy failed",
            CopyRawErrorLogCore
        );

    private string ExportDiagnosticsCore()
    {
        var path = LauncherDiagnostics.WriteStartupRecoveryDiagnosticsReport(DataDir());
        PatchHelper.Log($"Startup recovery diagnostics written: {path}");
        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        return ExportDiagnosticsMessage(path, shared);
    }

    private string CopyRawErrorLogCore()
    {
        var text = LauncherDiagnostics.BuildStartupRecoveryReport(DataDir());
        DisplayServer.ClipboardSet(text);
        PatchHelper.Log($"Startup recovery raw error log copied ({text.Length:N0} chars)");
        return RawErrorLogCopiedMessage(text.Length);
    }

    private void RunAction(string logAction, string failureTitle, Func<string> action)
    {
        try
        {
            _detail.Text = action();
        }
        catch (Exception ex)
        {
            ShowActionFailure(logAction, failureTitle, ex);
        }
    }

    private void ShowActionFailure(string logAction, string failureTitle, Exception ex)
    {
        PatchHelper.Log($"Startup recovery {logAction} failed: {ex}");
        _detail.Text = $"{failureTitle}:\n{ex.GetBaseException().Message}";
    }

    private static string DataDir()
        => OS.GetDataDir();

    private static string ExportDiagnosticsMessage(string path, bool shared)
        => shared
            ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
            : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";

    private static string RawErrorLogCopiedMessage(int length)
        => $"Raw error log copied to clipboard.\n\nLength: {length:N0} characters";
}
