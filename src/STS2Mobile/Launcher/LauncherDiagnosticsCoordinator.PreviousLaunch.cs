namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDiagnosticsCoordinator
{
    private const string PreviousLaunchWarningStatus =
        "Game startup failed last time.";

    internal void ShowPreviousLaunchWarningIfNeeded()
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
        PatchHelper.Log(
            "[Launcher] Previous launch warning shown; automatic diagnostics deferred to avoid blocking launcher display."
        );
        _view.AppendLog(
            "Help report was not collected automatically to keep the launcher responsive. Use Help Report after the launcher is visible."
        );
    }

    private readonly struct PreviousLaunchWarning
    {
        private const string LauncherAvailableMessage =
            "The launcher stayed open so you are not stuck on a black screen.";
        private const string DiagnosticsActionMessage =
            "Tap Last Problem to show what happened, or Help Report to share details.";

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
