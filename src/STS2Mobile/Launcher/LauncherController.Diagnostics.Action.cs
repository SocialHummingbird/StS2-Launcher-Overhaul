using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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

        internal static DiagnosticsAction LastErrorSummary { get; } =
            new(
                "Error summary failed",
                model => model.BuildDiagnosticsSummaryForDisplay(),
                ShowDiagnosticsSummary
            );

        internal static DiagnosticsAction RawErrorLog { get; } =
            new(
                "Raw error log copy failed",
                model => model.BuildRawErrorLogForClipboard(),
                CopyRawLogToClipboard
            );

        internal string FailureContext { get; }

        internal void Run(LauncherModel model, LauncherView view)
            => _publishText(view, _buildText(model));
    }
}
