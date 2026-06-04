using System;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal string WriteDiagnosticsReport()
        => WithDiagnosticsSnapshot(snapshot => snapshot.WriteDiagnosticsReport());

    internal string BuildDiagnosticsSummaryForDisplay()
        => WithDiagnosticsSnapshot(snapshot => snapshot.BuildDiagnosticsSummary());

    internal string BuildRawErrorLogForClipboard()
        => WithDiagnosticsSnapshot(snapshot => snapshot.BuildRawErrorLog());

    private string WithDiagnosticsSnapshot(
        Func<LauncherDiagnostics.Snapshot, string> build
    )
        => build(CreateDiagnosticsSnapshot());

    private LauncherDiagnostics.Snapshot CreateDiagnosticsSnapshot()
        => new(
            _dataDir,
            _credentialStore.AccountNameOrEmpty(),
            _credentialStore.HasUsableCredentials(),
            LauncherGameFiles.Ready(_dataDir),
            _sessionState.ToString(),
            _failReason
        );
}
