using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void ShowPreviousLaunchWarningIfNeeded()
    {
        var previousLaunchPhase = LauncherLaunchMarkers.ReadPreviousLaunchPhase();
        if (previousLaunchPhase == null)
            return;

        const string status = "Previous game launch did not finish.";
        var phaseSuffix = string.IsNullOrWhiteSpace(previousLaunchPhase)
            ? ""
            : $" Last phase: {previousLaunchPhase}.";

        _view.SetStatus(status);
        _view.AppendLog(status + phaseSuffix);
        _view.AppendLog(
            "The launcher is staying available so you are not trapped on a black screen."
        );
        _view.AppendLog(
            "Tap SHOW LAST ERROR to print the failure summary here, or EXPORT DIAGNOSTICS to share the full report."
        );
        WriteAutomaticDiagnosticsOnce();
    }

    private void WriteAutomaticDiagnosticsOnce()
    {
        if (_automaticDiagnosticsWritten)
            return;

        _automaticDiagnosticsWritten = true;
        var path = TryGetDiagnosticsResult(
            "Automatic diagnostics snapshot failed",
            logFullException: false,
            _model.WriteDiagnosticsReport
        );
        if (path != null)
            _view.AppendLog($"Automatic diagnostics snapshot: {path}");
    }
}
