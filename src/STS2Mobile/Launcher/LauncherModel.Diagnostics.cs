namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal string WriteDiagnosticsReport()
        => CreateDiagnosticsSnapshot().WriteDiagnosticsReport();

    internal string BuildDiagnosticsSummaryForDisplay()
        => CreateDiagnosticsSnapshot().BuildDiagnosticsSummary();

    internal string BuildRawErrorLogForClipboard()
        => CreateDiagnosticsSnapshot().BuildRawErrorLog();

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
