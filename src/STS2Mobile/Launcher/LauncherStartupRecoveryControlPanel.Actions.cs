using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryDetailAction
    {
        private RecoveryDetailAction(
            string logAction,
            string failureTitle,
            Func<string> run
        )
        {
            LogAction = logAction;
            FailureTitle = failureTitle;
            Run = run;
        }

        private string LogAction { get; }
        private string FailureTitle { get; }
        private Func<string> Run { get; }

        internal static RecoveryDetailAction Create(
            string logAction,
            string failureTitle,
            Func<string> run
        )
            => new(logAction, failureTitle, run);

        internal static RecoveryDetailAction ExportDiagnostics()
            => Create(
                "diagnostics export",
                "Diagnostics export failed",
                () => StartupRecoveryDiagnosticsAction
                    .ForCurrentDataDir()
                    .ExportAndShare()
            );

        internal static RecoveryDetailAction CopyRawErrorLog()
            => Create(
                "raw error log copy",
                "Raw error log copy failed",
                () => StartupRecoveryDiagnosticsAction
                    .ForCurrentDataDir()
                    .CopyRawErrorLog()
            );

        internal void Apply(Label detail)
        {
            try
            {
                detail.Text = Run();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Startup recovery {LogAction} failed: {ex}");
                detail.Text = $"{FailureTitle}:\n{ex.GetBaseException().Message}";
            }
        }
    }

    private readonly struct StartupRecoveryDiagnosticsAction
    {
        private StartupRecoveryDiagnosticsAction(string dataDir)
        {
            DataDir = dataDir;
        }

        private string DataDir { get; }

        internal static StartupRecoveryDiagnosticsAction ForCurrentDataDir()
            => new(OS.GetDataDir());

        internal string ExportAndShare()
        {
            var path = LauncherDiagnostics.WriteStartupRecoveryDiagnosticsReport(DataDir);
            PatchHelper.Log($"Startup recovery diagnostics written: {path}");
            var shared = AndroidGodotAppBridge.ShareTextFile(path);
            return ExportDiagnosticsMessage(path, shared);
        }

        internal string CopyRawErrorLog()
        {
            var text = LauncherDiagnostics.BuildStartupRecoveryReport(DataDir);
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

    private void ExportDiagnostics()
        => RecoveryDetailAction.ExportDiagnostics().Apply(_detail);

    private void CopyRawErrorLog()
        => RecoveryDetailAction.CopyRawErrorLog().Apply(_detail);
}
