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
        => new(
            _dataDir,
            _credentialStore.AccountName,
            _credentialStore.HasCredentials,
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString(),
            _failReason
        );
}
