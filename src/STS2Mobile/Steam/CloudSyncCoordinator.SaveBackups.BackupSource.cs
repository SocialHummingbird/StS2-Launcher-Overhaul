namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct BackupSource
        {
            private BackupSource(string label)
            {
                Label = label;
            }

            internal static BackupSource Cloud { get; } = new("cloud");
            internal static BackupSource Local { get; } = new("local");
            internal static BackupSource CloudPrePush { get; } = new("cloud-pre-push");
            internal static BackupSource LocalPrePush { get; } = new("local-pre-push");
            internal static BackupSource LocalPrePull { get; } = new("local-pre-pull");

            private string Label { get; }

            public override string ToString()
                => Label;
        }
    }
}
