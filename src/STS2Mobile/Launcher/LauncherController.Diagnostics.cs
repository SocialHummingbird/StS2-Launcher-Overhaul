namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

    private void ShowLastErrorPressed()
        => RunDiagnosticsAction(
            "Error summary failed",
            () => ShowDiagnosticsSummary(
                _view,
                _model.BuildDiagnosticsSummaryForDisplay()
            )
        );

    private void CopyRawLogPressed()
        => RunDiagnosticsAction(
            "Raw error log copy failed",
            () => CopyRawLogToClipboard(
                _view,
                _model.BuildRawErrorLogForClipboard()
            )
        );

    private static void ShowDiagnosticsSummary(LauncherView view, string summary)
    {
        view.SetStatus("Last error summary printed in console.");
        view.AppendLog(summary);
        view.ShowDiagnosticsConsole();
    }

    private static void CopyRawLogToClipboard(LauncherView view, string rawLog)
    {
        var clipboardText = new LauncherClipboardText(
            "Public sharing warning: review and redact this raw error log before posting publicly.\n"
            + "It may include account names, local paths, device details, save/cloud state, and log excerpts.\n\n"
            + rawLog
        );
        clipboardText.CopyToClipboard();
        view.SetStatus("Raw error log copied. Review/redact before sharing.");
        view.AppendLog(
            $"Raw error log copied to clipboard ({clipboardText.Length:N0} chars). Review/redact before public posting."
        );
        view.ShowDiagnosticsConsole();
    }
}
