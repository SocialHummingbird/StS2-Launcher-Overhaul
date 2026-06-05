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

    private void ShowPreviousLaunchWarning(string previousLaunchPhase)
        => new PreviousLaunchWarning(previousLaunchPhase).Show(_view);

    private void WriteAutomaticDiagnosticsOnce()
    {
        if (_automaticDiagnosticsWritten)
            return;

        _automaticDiagnosticsWritten = true;
        var path = TryWriteDiagnosticsReport(
            DiagnosticsReportWrite.AutomaticSnapshot()
        );
        if (path != null)
            _view.AppendLog($"Automatic diagnostics snapshot: {path}");
    }

    private readonly struct PreviousLaunchWarning
    {
        private const string LauncherAvailableMessage =
            "The launcher is staying available so you are not trapped on a black screen.";
        private const string DiagnosticsActionMessage =
            "Tap SHOW LAST ERROR to print the failure summary here, or EXPORT DIAGNOSTICS to share the full report.";

        internal PreviousLaunchWarning(string previousLaunchPhase)
        {
            PreviousLaunchPhase = previousLaunchPhase;
        }

        private string PreviousLaunchPhase { get; }

        internal void Show(LauncherView view)
        {
            view.SetStatus(PreviousLaunchWarningStatus);

            foreach (var line in LogLines())
                view.AppendLog(line);
        }

        private string[] LogLines()
            => new[]
            {
                PreviousLaunchWarningStatus + PreviousLaunchPhaseSuffix(),
                LauncherAvailableMessage,
                DiagnosticsActionMessage,
            };

        private string PreviousLaunchPhaseSuffix()
            => string.IsNullOrWhiteSpace(PreviousLaunchPhase)
                ? ""
                : $" Last phase: {PreviousLaunchPhase}.";
    }
}
