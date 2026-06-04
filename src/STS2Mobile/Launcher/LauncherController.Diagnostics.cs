using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

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
}
