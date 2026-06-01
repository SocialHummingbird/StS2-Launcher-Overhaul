using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void ShowPreviousLaunchWarningIfNeeded()
    {
        if (!LauncherLaunchMarkers.PreviousGameLaunchIncomplete(out var phase))
            return;

        const string status = "Previous game launch did not finish.";
        var phaseSuffix = string.IsNullOrWhiteSpace(phase) ? "" : $" Last phase: {phase}.";

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
        if (TryWriteDiagnosticsReport(
            "Automatic diagnostics snapshot failed",
            logFullException: false,
            out var path,
            out _
        ))
            _view.AppendLog($"Automatic diagnostics snapshot: {path}");
    }
}
