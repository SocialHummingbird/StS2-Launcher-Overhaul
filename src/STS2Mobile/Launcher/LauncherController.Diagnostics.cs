using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _automaticDiagnosticsWritten;

    private void ShowLastErrorPressed()
        => RunDiagnosticsAction(DiagnosticsAction.LastErrorSummary);

    private void CopyRawLogPressed()
        => RunDiagnosticsAction(DiagnosticsAction.RawErrorLog);

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
