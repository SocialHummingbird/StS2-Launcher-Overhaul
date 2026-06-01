using System;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal string WriteDiagnosticsReport()
        => BuildDiagnosticsOutput(LauncherDiagnostics.WriteLauncherDiagnosticsReport);

    internal string BuildDiagnosticsSummaryForDisplay()
        => BuildDiagnosticsOutput(LauncherDiagnostics.BuildLauncherDiagnosticsSummary);

    internal string BuildRawErrorLogForClipboard()
        => BuildDiagnosticsOutput(LauncherDiagnostics.BuildLauncherRawErrorLog);

    private string BuildDiagnosticsOutput(
        Func<string, string, bool, bool, string, string, string> build
    )
        => build(
            _dataDir,
            _credentialStore.AccountName,
            _credentialStore.HasCredentials,
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString(),
            _failReason
        );
}
