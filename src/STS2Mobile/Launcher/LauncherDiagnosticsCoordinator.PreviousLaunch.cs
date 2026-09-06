namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDiagnosticsCoordinator
{
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
            "No report was collected automatically. Use Create support report after the launcher is visible so it includes this attempt."
        );
    }

    private readonly struct PreviousLaunchWarning
    {
        private const string LauncherAvailableMessage =
            "The launcher stayed open so you can recover or collect diagnostics.";
        private const string DiagnosticsActionMessage =
            "Open Help to create a new support report for this attempt.";

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
            view.SetStatus(StatusMessage(), LauncherStatusSeverity.Warning);
            view.ShowHomeHelpAction();

            foreach (var line in LogLines())
                view.AppendLog(line);
        }

        private string[] LogLines()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                StatusMessage() + PreviousLaunchPhaseSuffix(),
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

        private string StatusMessage()
        {
            var phase = (LaunchAttempt.Phase ?? string.Empty) + " " + (PreviousLaunchPhase ?? string.Empty);
            if (phase.IndexOf("setup", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Launch setup failed last time. Try again, then create a new support report if it repeats.";

            if (string.Equals(LaunchAttempt.FilesReady, "false", System.StringComparison.OrdinalIgnoreCase)
                || phase.IndexOf("readiness", System.StringComparison.OrdinalIgnoreCase) >= 0
                || phase.IndexOf("blocked", System.StringComparison.OrdinalIgnoreCase) >= 0
                || phase.IndexOf("checking", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Game preparation failed last time. Repair selected branch, then try again.";

            return "The game did not appear last time. Try Safe Start.";
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
