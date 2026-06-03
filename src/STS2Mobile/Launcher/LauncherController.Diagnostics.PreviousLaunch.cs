namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string PreviousLaunchWarningStatus =
        "Previous game launch did not finish.";

    private void ShowPreviousLaunchWarningIfNeeded()
    {
        var previousLaunchPhase = LauncherLaunchMarkers.ReadPreviousLaunchPhase();
        if (previousLaunchPhase == null)
            return;

        ShowPreviousLaunchWarning(previousLaunchPhase);
        WriteAutomaticDiagnosticsOnce();
    }

    private void ShowPreviousLaunchWarning(string? previousLaunchPhase)
    {
        _view.SetStatus(PreviousLaunchWarningStatus);
        _view.AppendLog(
            PreviousLaunchWarningStatus + PreviousLaunchPhaseSuffix(previousLaunchPhase)
        );
        _view.AppendLog(
            "The launcher is staying available so you are not trapped on a black screen."
        );
        _view.AppendLog(
            "Tap SHOW LAST ERROR to print the failure summary here, or EXPORT DIAGNOSTICS to share the full report."
        );
    }

    private static string PreviousLaunchPhaseSuffix(string? previousLaunchPhase)
        => string.IsNullOrWhiteSpace(previousLaunchPhase)
            ? ""
            : $" Last phase: {previousLaunchPhase}.";

    private void WriteAutomaticDiagnosticsOnce()
    {
        if (_automaticDiagnosticsWritten)
            return;

        _automaticDiagnosticsWritten = true;
        var path = TryWriteDiagnosticsReport(
            "Automatic diagnostics snapshot failed",
            logFullException: false
        );
        if (path != null)
            _view.AppendLog($"Automatic diagnostics snapshot: {path}");
    }
}
