using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

    private readonly struct DiagnosticsFailureHandling
    {
        private readonly string _context;
        private readonly bool _logFullException;
        private readonly Action<string>? _onFailure;

        internal DiagnosticsFailureHandling(
            string context,
            bool logFullException,
            Action<string>? onFailure = null
        )
        {
            _context = context;
            _logFullException = logFullException;
            _onFailure = onFailure;
        }

        internal void Handle(Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] {_context}: {(_logFullException ? ex.ToString() : ex.Message)}"
            );
            _onFailure?.Invoke(ex.Message);
        }
    }

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
        var failure = new DiagnosticsFailureHandling(
            failureContext,
            logFullException: true,
            message => _view.SetStatus($"{failureContext}: {message}")
        );

        try
        {
            action();
        }
        catch (Exception ex)
        {
            failure.Handle(ex);
        }
    }

    private string? TryWriteDiagnosticsReport(
        string failureContext,
        bool logFullException,
        Action<string>? onFailure = null
    )
    {
        var failure = new DiagnosticsFailureHandling(
            failureContext,
            logFullException,
            onFailure
        );

        try
        {
            return _model.WriteDiagnosticsReport();
        }
        catch (Exception ex)
        {
            failure.Handle(ex);
            return default;
        }
    }
}
