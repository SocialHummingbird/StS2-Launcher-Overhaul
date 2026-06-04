using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static readonly StartupRecoveryAction ExportDiagnosticsAction = new(
        "diagnostics export",
        "Diagnostics export failed",
        ExportDiagnosticsReport
    );

    private static readonly StartupRecoveryAction CopyRawErrorLogAction = new(
        "raw error log copy",
        "Raw error log copy failed",
        CopyReportTextToClipboard
    );

    private sealed class StartupRecoveryAction
    {
        internal StartupRecoveryAction(
            string logAction,
            string failureTitle,
            Func<LauncherDiagnostics.StartupRecoveryDiagnosticsReport, string> run
        )
        {
            LogAction = logAction;
            FailureTitle = failureTitle;
            Run = run;
        }

        private string LogAction { get; }
        private string FailureTitle { get; }
        private Func<LauncherDiagnostics.StartupRecoveryDiagnosticsReport, string> Run { get; }

        internal string RunAndDescribe()
        {
            try
            {
                return Run(LauncherDiagnostics.StartupRecoveryReport(OS.GetDataDir()));
            }
            catch (Exception ex)
            {
                return FailureMessage(ex);
            }
        }

        private string FailureMessage(Exception ex)
        {
            PatchHelper.Log($"Startup recovery {LogAction} failed: {ex}");
            return $"{FailureTitle}:\n{ex.GetBaseException().Message}";
        }
    }

    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private void ExportDiagnostics()
        => ShowActionResult(ExportDiagnosticsAction);

    private void CopyRawErrorLog()
        => ShowActionResult(CopyRawErrorLogAction);

    private void ShowActionResult(StartupRecoveryAction action)
        => _detail.Text = action.RunAndDescribe();

    private static string ExportDiagnosticsReport(
        LauncherDiagnostics.StartupRecoveryDiagnosticsReport report
    )
    {
        var path = report.Write();
        PatchHelper.Log($"Startup recovery diagnostics written: {path}");
        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        return ExportDiagnosticsMessage(path, shared);
    }

    private static string CopyReportTextToClipboard(
        LauncherDiagnostics.StartupRecoveryDiagnosticsReport report
    )
    {
        var text = report.BuildText();
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
