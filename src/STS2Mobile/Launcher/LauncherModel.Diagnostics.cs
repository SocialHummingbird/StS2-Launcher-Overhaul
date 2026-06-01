namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal string WriteDiagnosticsReport()
        => LauncherDiagnostics.WriteLauncherDiagnosticsReport(CreateDiagnosticsSnapshot());

    internal string BuildDiagnosticsSummaryForDisplay()
        => LauncherDiagnostics.BuildLauncherDiagnosticsSummary(CreateDiagnosticsSnapshot());

    internal string BuildRawErrorLogForClipboard()
        => LauncherDiagnostics.BuildLauncherRawErrorLog(CreateDiagnosticsSnapshot());

    private LauncherDiagnostics.Snapshot CreateDiagnosticsSnapshot()
        => LauncherDiagnostics.Snapshot.Create(
            _dataDir,
            _credentialStore.AccountNameOrEmpty(),
            _credentialStore.HasUsableCredentials(),
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString(),
            _failReason
        );
}
