namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private enum DiagnosticsReadinessScope
    {
        FullRuntime,
        DownloadedState,
    }

    internal string WriteDiagnosticsReport()
        => CreateDiagnosticsSnapshot(DiagnosticsReadinessScope.FullRuntime).WriteDiagnosticsReport();

    internal string BuildDiagnosticsSummaryForDisplay()
        => CreateDiagnosticsSnapshot(DiagnosticsReadinessScope.DownloadedState).BuildDiagnosticsSummary();

    internal string BuildRawErrorLogForClipboard()
        => CreateDiagnosticsSnapshot(DiagnosticsReadinessScope.DownloadedState).BuildRawErrorLog();

    internal string BuildRedactedIssueLog()
        => LauncherGitHubIssue.Redact(BuildRawErrorLogForClipboard(), _credentialStore.AccountNameOrEmpty(), _dataDir);

    private LauncherDiagnostics.Snapshot CreateDiagnosticsSnapshot(DiagnosticsReadinessScope readinessScope)
    {
        var branch = LauncherPreferences.ReadGameBranch();
        var readiness = readinessScope switch
        {
            DiagnosticsReadinessScope.FullRuntime => LauncherLaunchReadiness.Evaluate(
                _dataDir,
                branch,
                "diagnostics snapshot readiness"
            ),
            _ => LauncherLaunchReadiness.EvaluateDownloadedState(
                _dataDir,
                branch,
                "diagnostics display downloaded-state readiness"
            ),
        };
        return new LauncherDiagnostics.Snapshot(
            _dataDir,
            _credentialStore.AccountNameOrEmpty(),
            _credentialStore.HasUsableCredentials(),
            readiness,
            _sessionState.ToString(),
            _failReason
        );
    }
}
