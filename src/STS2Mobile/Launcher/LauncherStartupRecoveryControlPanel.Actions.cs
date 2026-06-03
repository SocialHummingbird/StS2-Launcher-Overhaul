using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryAction
    {
        internal RecoveryAction(
            string logAction,
            string failureTitle,
            Func<string, string> run
        )
        {
            LogAction = logAction;
            FailureTitle = failureTitle;
            Run = run;
        }

        internal string LogAction { get; }
        internal string FailureTitle { get; }
        internal Func<string, string> Run { get; }
    }

    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private void ExportDiagnostics()
        => ShowActionResult(new RecoveryAction(
            "diagnostics export",
            "Diagnostics export failed",
            ExportDiagnosticsForDataDir
        ));

    private void CopyRawErrorLog()
        => ShowActionResult(new RecoveryAction(
            "raw error log copy",
            "Raw error log copy failed",
            CopyRawErrorLogForDataDir
        ));

    private void ShowActionResult(RecoveryAction action)
    {
        try
        {
            _detail.Text = action.Run(OS.GetDataDir());
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery {action.LogAction} failed: {ex}");
            _detail.Text = $"{action.FailureTitle}:\n{ex.GetBaseException().Message}";
        }
    }

    private static string ExportDiagnosticsForDataDir(string dataDir)
    {
        var path = LauncherDiagnostics.WriteStartupRecoveryDiagnosticsReport(dataDir);
        PatchHelper.Log($"Startup recovery diagnostics written: {path}");
        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        return ExportDiagnosticsMessage(path, shared);
    }

    private static string CopyRawErrorLogForDataDir(string dataDir)
    {
        var text = LauncherDiagnostics.BuildStartupRecoveryReport(dataDir);
        DisplayServer.ClipboardSet(text);
        PatchHelper.Log($"Startup recovery raw error log copied ({text.Length:N0} chars)");
        return RawErrorLogCopiedMessage(text.Length);
    }

    private static string ExportDiagnosticsMessage(string path, bool shared)
        => shared
            ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
            : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";

    private static string RawErrorLogCopiedMessage(int length)
        => $"Raw error log copied to clipboard.\n\nLength: {length:N0} characters";
}
