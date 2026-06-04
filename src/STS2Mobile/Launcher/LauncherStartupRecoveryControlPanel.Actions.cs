using System;
using Godot;
using STS2Mobile;
using StartupRecoveryReport =
    STS2Mobile.Launcher.LauncherDiagnostics.StartupRecoveryDiagnosticsReport;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static readonly StartupRecoveryAction ExportDiagnosticsAction =
        StartupRecoveryAction.DiagnosticsExport(ExportDiagnosticsReport);

    private static readonly StartupRecoveryAction CopyRawErrorLogAction =
        StartupRecoveryAction.RawErrorLogCopy(CopyReportTextToClipboard);

    private sealed class StartupRecoveryAction
    {
        private StartupRecoveryAction(
            string logAction,
            string failureTitle,
            Func<StartupRecoveryReport, string> run
        )
        {
            LogAction = logAction;
            FailureTitle = failureTitle;
            Run = run;
        }

        private string LogAction { get; }
        private string FailureTitle { get; }
        private Func<StartupRecoveryReport, string> Run { get; }

        internal static StartupRecoveryAction DiagnosticsExport(
            Func<StartupRecoveryReport, string> run
        )
            => new(
                "diagnostics export",
                "Diagnostics export failed",
                run
            );

        internal static StartupRecoveryAction RawErrorLogCopy(
            Func<StartupRecoveryReport, string> run
        )
            => new(
                "raw error log copy",
                "Raw error log copy failed",
                run
            );

        internal string RunAndDescribe(StartupRecoveryReport report)
        {
            try
            {
                return Run(report);
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
        => _detail.Text = action.RunAndDescribe(CreateStartupRecoveryReport());

    private static StartupRecoveryReport CreateStartupRecoveryReport()
        => LauncherDiagnostics.StartupRecoveryReport(OS.GetDataDir());

    private static string ExportDiagnosticsReport(StartupRecoveryReport report)
    {
        var path = report.Write();
        PatchHelper.Log($"Startup recovery diagnostics written: {path}");
        var shared = AndroidGodotAppBridge.ShareTextFile(path);
        return ExportDiagnosticsMessage(path, shared);
    }

    private static string CopyReportTextToClipboard(StartupRecoveryReport report)
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
