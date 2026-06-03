namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct PreviousLaunchWarning
    {
        private const string Status = "Previous game launch did not finish.";

        private PreviousLaunchWarning(string? previousLaunchPhase)
        {
            PreviousLaunchPhase = previousLaunchPhase;
        }

        private string? PreviousLaunchPhase { get; }
        private string PhaseSuffix => string.IsNullOrWhiteSpace(PreviousLaunchPhase)
            ? ""
            : $" Last phase: {PreviousLaunchPhase}.";

        internal static PreviousLaunchWarning? Read()
        {
            var previousLaunchPhase = LauncherLaunchMarkers.ReadPreviousLaunchPhase();
            return previousLaunchPhase == null
                ? null
                : new PreviousLaunchWarning(previousLaunchPhase);
        }

        internal void Apply(LauncherView view)
        {
            view.SetStatus(Status);
            view.AppendLog(Status + PhaseSuffix);
            view.AppendLog(
                "The launcher is staying available so you are not trapped on a black screen."
            );
            view.AppendLog(
                "Tap SHOW LAST ERROR to print the failure summary here, or EXPORT DIAGNOSTICS to share the full report."
            );
        }
    }

    private void ShowPreviousLaunchWarningIfNeeded()
    {
        var warning = PreviousLaunchWarning.Read();
        if (!warning.HasValue)
            return;

        warning.Value.Apply(_view);
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
