using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal sealed partial class Snapshot
    {
        private enum LauncherStateDetail
        {
            Compact,
            Detailed,
        }

        private readonly struct LauncherStateReport
        {
            internal LauncherStateReport(
                string dataDir,
                string accountName,
                bool hasSavedCredentials,
                bool gameFilesReady,
                string sessionState,
                string failReason
            )
            {
                DataDir = dataDir;
                AccountName = accountName;
                HasSavedCredentials = hasSavedCredentials;
                GameFilesReady = gameFilesReady;
                SessionState = sessionState;
                FailReason = failReason;
            }

            internal string DataDir { get; }
            private string AccountName { get; }
            private bool HasSavedCredentials { get; }
            private bool GameFilesReady { get; }
            private string SessionState { get; }
            private string FailReason { get; }

            internal void AppendTo(StringBuilder sb, LauncherStateDetail detail)
            {
                if (detail == LauncherStateDetail.Detailed)
                    AppendDetailedPrefix(sb);

                AppendCompact(sb);

                if (detail == LauncherStateDetail.Detailed)
                    sb.AppendLine($"Fail reason: {ValueOrMissing(FailReason)}");
            }

            private void AppendDetailedPrefix(StringBuilder sb)
            {
                sb.AppendLine($"Data dir: {ValueOrMissing(DataDir)}");
                sb.AppendLine($"Account: {ValueOrMissing(AccountName)}");
                sb.AppendLine($"Has saved credentials: {HasSavedCredentials}");
            }

            private void AppendCompact(StringBuilder sb)
            {
                sb.AppendLine($"Game files ready: {GameFilesReady}");
                sb.AppendLine($"Session state: {ValueOrMissing(SessionState)}");
            }
        }
    }
}
