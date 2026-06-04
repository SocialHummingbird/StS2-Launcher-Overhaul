using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

    private enum DiagnosticsExceptionDetail
    {
        MessageOnly,
        FullException,
    }

    private static readonly DiagnosticsAction LastErrorDiagnosticsAction = new(
        "Error summary failed",
        model => model.BuildDiagnosticsSummaryForDisplay(),
        ShowDiagnosticsSummary
    );

    private static readonly DiagnosticsAction RawLogDiagnosticsAction = new(
        "Raw error log copy failed",
        model => model.BuildRawErrorLogForClipboard(),
        CopyRawLogToClipboard
    );

    private readonly struct DiagnosticsAction
    {
        private readonly Func<LauncherModel, string> _buildText;
        private readonly Action<LauncherView, string> _publishText;

        internal DiagnosticsAction(
            string failureContext,
            Func<LauncherModel, string> buildText,
            Action<LauncherView, string> publishText
        )
        {
            FailureContext = failureContext;
            _buildText = buildText;
            _publishText = publishText;
        }

        internal string FailureContext { get; }

        internal void Run(LauncherModel model, LauncherView view)
            => _publishText(view, _buildText(model));
    }

    private readonly struct DiagnosticsFailureHandling
    {
        private readonly string _context;
        private readonly DiagnosticsExceptionDetail _exceptionDetail;
        private readonly Action<string>? _onFailure;

        internal DiagnosticsFailureHandling(
            string context,
            DiagnosticsExceptionDetail exceptionDetail,
            Action<string>? onFailure = null
        )
        {
            _context = context;
            _exceptionDetail = exceptionDetail;
            _onFailure = onFailure;
        }

        internal void Handle(Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] {_context}: {ExceptionText(ex)}"
            );
            _onFailure?.Invoke(ex.Message);
        }

        internal void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Handle(ex);
            }
        }

        internal string? TryRun(Func<string> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                Handle(ex);
                return default;
            }
        }

        private string ExceptionText(Exception ex)
            => _exceptionDetail == DiagnosticsExceptionDetail.FullException
                ? ex.ToString()
                : ex.Message;
    }

    private void ShowLastErrorPressed()
        => RunDiagnosticsAction(LastErrorDiagnosticsAction);

    private void CopyRawLogPressed()
        => RunDiagnosticsAction(RawLogDiagnosticsAction);

    private void RunDiagnosticsAction(DiagnosticsAction action)
        => RunDiagnosticsAction(
            action.FailureContext,
            () => action.Run(_model, _view)
        );

    private static void ShowDiagnosticsSummary(LauncherView view, string summary)
    {
        view.SetStatus("Last error summary printed in console.");
        view.AppendLog(summary);
    }

    private static void CopyRawLogToClipboard(LauncherView view, string rawLog)
    {
        DisplayServer.ClipboardSet(rawLog);
        view.SetStatus("Raw error log copied.");
        view.AppendLog(
            $"Raw error log copied to clipboard ({rawLog.Length:N0} chars)."
        );
    }

    private void RunDiagnosticsAction(string failureContext, Action action)
    {
        var failure = new DiagnosticsFailureHandling(
            failureContext,
            DiagnosticsExceptionDetail.FullException,
            message => _view.SetStatus($"{failureContext}: {message}")
        );

        failure.Run(action);
    }

    private string? TryWriteDiagnosticsReport(
        string failureContext,
        DiagnosticsExceptionDetail exceptionDetail,
        Action<string>? onFailure = null
    )
    {
        var failure = new DiagnosticsFailureHandling(
            failureContext,
            exceptionDetail,
            onFailure
        );

        return failure.TryRun(_model.WriteDiagnosticsReport);
    }
}
