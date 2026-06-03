using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

    private readonly struct DiagnosticsFailure
    {
        private DiagnosticsFailure(
            string context,
            bool logFullException,
            Action<string>? onFailure
        )
        {
            Context = context;
            LogFullException = logFullException;
            OnFailure = onFailure;
        }

        private string Context { get; }
        private bool LogFullException { get; }
        private Action<string>? OnFailure { get; }

        internal static DiagnosticsFailure ReportToStatus(
            string context,
            Action<string> setStatus
        )
            => new(
                context,
                logFullException: true,
                message => setStatus($"{context}: {message}")
            );

        internal static DiagnosticsFailure Create(
            string context,
            bool logFullException,
            Action<string>? onFailure = null
        )
            => new(context, logFullException, onFailure);

        internal void Handle(Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] {Context}: {(LogFullException ? ex.ToString() : ex.Message)}"
            );
            OnFailure?.Invoke(ex.Message);
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
        var failure = DiagnosticsFailure.ReportToStatus(
            failureContext,
            _view.SetStatus
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

    private T? TryGetDiagnosticsResult<T>(
        string failureContext,
        bool logFullException,
        Func<T> action,
        Action<string>? onFailure = null
    )
        where T : class
    {
        var failure = DiagnosticsFailure.Create(
            failureContext,
            logFullException,
            onFailure
        );
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            failure.Handle(ex);
            return default;
        }
    }
}
