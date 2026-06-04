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

    private static readonly DiagnosticsAction LastErrorDiagnosticsAction =
        DiagnosticsAction.LastErrorSummary(ShowDiagnosticsSummary);

    private static readonly DiagnosticsAction RawLogDiagnosticsAction =
        DiagnosticsAction.RawErrorLog(CopyRawLogToClipboard);

    private readonly struct DiagnosticsAction
    {
        private readonly Func<LauncherModel, string> _buildText;
        private readonly Action<LauncherView, string> _publishText;

        private DiagnosticsAction(
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

        internal static DiagnosticsAction LastErrorSummary(
            Action<LauncherView, string> publishText
        )
            => new(
                "Error summary failed",
                model => model.BuildDiagnosticsSummaryForDisplay(),
                publishText
            );

        internal static DiagnosticsAction RawErrorLog(
            Action<LauncherView, string> publishText
        )
            => new(
                "Raw error log copy failed",
                model => model.BuildRawErrorLogForClipboard(),
                publishText
            );

        internal void Run(LauncherModel model, LauncherView view)
            => _publishText(view, _buildText(model));
    }

    private readonly struct DiagnosticsFailureHandling
    {
        private readonly string _context;
        private readonly DiagnosticsExceptionDetail _exceptionDetail;
        private readonly Action<string>? _onFailure;

        private DiagnosticsFailureHandling(
            string context,
            DiagnosticsExceptionDetail exceptionDetail,
            Action<string>? onFailure = null
        )
        {
            _context = context;
            _exceptionDetail = exceptionDetail;
            _onFailure = onFailure;
        }

        internal static DiagnosticsFailureHandling FullException(
            string context,
            Action<string>? onFailure = null
        )
            => new(context, DiagnosticsExceptionDetail.FullException, onFailure);

        internal static DiagnosticsFailureHandling WithDetail(
            string context,
            DiagnosticsExceptionDetail exceptionDetail,
            Action<string>? onFailure = null
        )
            => new(context, exceptionDetail, onFailure);

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
        var failure = DiagnosticsFailureHandling.FullException(
            failureContext,
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
        var failure = DiagnosticsFailureHandling.WithDetail(
            failureContext,
            exceptionDetail,
            onFailure
        );

        return failure.TryRun(_model.WriteDiagnosticsReport);
    }
}
