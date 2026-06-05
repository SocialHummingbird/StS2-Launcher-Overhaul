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
    }

    private static void CopyRawLogToClipboard(LauncherView view, string rawLog)
    {
        var clipboardText = new LauncherClipboardText(rawLog);
        clipboardText.CopyToClipboard();
        view.SetStatus("Raw error log copied.");
        view.AppendLog(
            $"Raw error log copied to clipboard ({clipboardText.Length:N0} chars)."
        );
    }
}
