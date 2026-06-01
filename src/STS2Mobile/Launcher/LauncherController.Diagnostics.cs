using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

    private void ShowLastErrorPressed()
        => RunDiagnosticsAction(
            "Error summary failed",
            () =>
            {
                var summary = _model.BuildDiagnosticsSummaryForDisplay();
                _view.SetStatus("Last error summary printed in console.");
                _view.AppendLog(summary);
            }
        );

    private void CopyRawLogPressed()
        => RunDiagnosticsAction(
            "Raw error log copy failed",
            () =>
            {
                var rawLog = _model.BuildRawErrorLogForClipboard();
                DisplayServer.ClipboardSet(rawLog);
                _view.SetStatus("Raw error log copied.");
                _view.AppendLog(
                    $"Raw error log copied to clipboard ({rawLog.Length:N0} chars)."
                );
            }
        );

    private void RunDiagnosticsAction(string failureContext, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ShowDiagnosticsActionFailure(failureContext, ex);
        }
    }

    private void ShowDiagnosticsActionFailure(string failureContext, Exception ex)
    {
        LogDiagnosticsFailure(failureContext, ex, logFullException: true);
        _view.SetStatus($"{failureContext}: {ex.Message}");
    }

    private static void LogDiagnosticsFailure(
        string failureContext,
        Exception ex,
        bool logFullException
    )
    {
        PatchHelper.Log(
            $"[Launcher] {failureContext}: {(logFullException ? ex.ToString() : ex.Message)}"
        );
    }
}
