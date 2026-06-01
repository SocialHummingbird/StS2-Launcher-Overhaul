namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal string WriteDiagnosticsReport()
    {
        var report = LauncherDiagnostics.BuildLauncherDiagnosticsReport(
            _dataDir,
            _credentialStore.AccountName,
            _credentialStore.HasCredentials,
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString(),
            _failReason
        );
        return LauncherDiagnostics.WriteLauncherDiagnosticsReport(_dataDir, report);
    }

    internal string BuildDiagnosticsSummaryForDisplay()
        => LauncherDiagnostics.BuildLauncherDiagnosticsSummary(
            _dataDir,
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString()
        );

    internal string BuildRawErrorLogForClipboard()
        => LauncherDiagnostics.BuildLauncherRawErrorLog(
            _dataDir,
            _credentialStore.AccountName,
            _credentialStore.HasCredentials,
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString(),
            _failReason
        );
}
