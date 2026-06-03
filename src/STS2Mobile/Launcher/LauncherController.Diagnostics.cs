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
            HandleDiagnosticsFailure(
                ex,
                failureContext,
                logFullException: true,
                message => _view.SetStatus($"{failureContext}: {message}")
            );
        }
    }

    private string? TryWriteDiagnosticsReport(
        string failureContext,
        bool logFullException,
        Action<string>? onFailure = null
    )
    {
        try
        {
            return _model.WriteDiagnosticsReport();
        }
        catch (Exception ex)
        {
            HandleDiagnosticsFailure(
                ex,
                failureContext,
                logFullException,
                onFailure
            );
            return default;
        }
    }

    private static void HandleDiagnosticsFailure(
        Exception ex,
        string context,
        bool logFullException,
        Action<string>? onFailure = null
    )
    {
        PatchHelper.Log(
            $"[Launcher] {context}: {(logFullException ? ex.ToString() : ex.Message)}"
        );
        onFailure?.Invoke(ex.Message);
    }
}
