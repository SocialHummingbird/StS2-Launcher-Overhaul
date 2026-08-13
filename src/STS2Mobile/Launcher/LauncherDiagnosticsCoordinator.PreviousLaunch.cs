namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDiagnosticsCoordinator
{
    private const string PreviousLaunchWarningStatus =
        "Game startup failed last time.";
    private bool _previousLaunchWarningChecked;

    internal void ShowPreviousLaunchWarningIfNeeded()
    {
        if (_previousLaunchWarningChecked)
            return;

        _previousLaunchWarningChecked = true;
        var previousLaunchPhase = LauncherLaunchMarkers.ReadPreviousLaunchPhase();
        if (previousLaunchPhase == null)
            return;

        ShowPreviousLaunchWarning(
            previousLaunchPhase,
            LauncherLaunchMarkers.ReadLastLaunchAttempt()
        );
        WriteAutomaticDiagnosticsOnce();
    }

    private void ShowPreviousLaunchWarning(
        string previousLaunchPhase,
        LaunchAttemptSummary launchAttempt
    )
        => new PreviousLaunchWarning(previousLaunchPhase, launchAttempt).Show(_view);

    private void WriteAutomaticDiagnosticsOnce()
    {
        if (_automaticDiagnosticsWritten)
            return;

        _automaticDiagnosticsWritten = true;
        PatchHelper.Log(
            "[Launcher] Previous launch warning shown; automatic diagnostics deferred to avoid blocking launcher display."
        );
        _view.AppendLog(
            "A support report was not collected automatically to keep the launcher responsive. Use Create support report after the launcher is visible."
        );
    }

    private readonly struct PreviousLaunchWarning
    {
        private const string LauncherAvailableMessage =
            "The launcher stayed open so you are not stuck on a black screen.";
        private const string DiagnosticsActionMessage =
            "Open Help to view the last error or create a support report.";

        internal PreviousLaunchWarning(
            string previousLaunchPhase,
            LaunchAttemptSummary launchAttempt
        )
        {
            PreviousLaunchPhase = previousLaunchPhase;
            LaunchAttempt = launchAttempt;
        }

        private string PreviousLaunchPhase { get; }
        private LaunchAttemptSummary LaunchAttempt { get; }

        internal void Show(LauncherView view)
        {
            view.SetStatus(PreviousLaunchWarningStatus, LauncherStatusSeverity.Warning);
            view.ShowHomeHelpAction();

            foreach (var line in LogLines())
                view.AppendLog(line);
        }

        private string[] LogLines()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                PreviousLaunchWarningStatus + PreviousLaunchPhaseSuffix(),
                LauncherAvailableMessage,
                DiagnosticsActionMessage
            };

            if (LaunchAttempt.Present)
            {
                AddIfPresent(lines, LaunchAttempt.ShortLine());
                AddIfPresent(lines, LaunchAttempt.RuntimeLine());
                AddIfPresent(lines, LaunchAttempt.PathLine());
                AddIfPresent(lines, LaunchAttempt.IdentityLine());
                AddIfPresent(lines, LaunchAttempt.MarkerLine());
                AddIfPresent(lines, LaunchAttempt.ModLine());
                AddIfPresent(lines, LaunchAttempt.TimingLine());
                AddIfPresent(lines, LaunchAttempt.RecoveryHint());
            }

            return lines.ToArray();
        }

        private static void AddIfPresent(
            System.Collections.Generic.List<string> lines,
            string value
        )
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value);
        }

        private string PreviousLaunchPhaseSuffix()
            => string.IsNullOrWhiteSpace(PreviousLaunchPhase)
                ? ""
                : $" Last phase: {PreviousLaunchPhase}.";
    }
}
