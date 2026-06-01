using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private void ExportDiagnostics()
    {
        try
        {
            var path = LauncherDiagnostics.WriteStartupRecoveryDiagnosticsReport(OS.GetDataDir());
            PatchHelper.Log($"Startup recovery diagnostics written: {path}");
            var shared = AndroidGodotAppBridge.ShareTextFile(path);
            _detail.Text = shared
                ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
                : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";
        }
        catch (Exception ex)
        {
            ShowActionFailure("diagnostics export", "Diagnostics export failed", ex);
        }
    }

    private void CopyRawErrorLog()
    {
        try
        {
            var text = LauncherDiagnostics.BuildStartupRecoveryReport(OS.GetDataDir());
            DisplayServer.ClipboardSet(text);
            PatchHelper.Log($"Startup recovery raw error log copied ({text.Length:N0} chars)");
            _detail.Text = $"Raw error log copied to clipboard.\n\nLength: {text.Length:N0} characters";
        }
        catch (Exception ex)
        {
            ShowActionFailure("raw error log copy", "Raw error log copy failed", ex);
        }
    }

    private void ShowActionFailure(string logAction, string detailTitle, Exception ex)
    {
        PatchHelper.Log($"Startup recovery {logAction} failed: {ex}");
        _detail.Text = $"{detailTitle}:\n{ex.GetBaseException().Message}";
    }
}
