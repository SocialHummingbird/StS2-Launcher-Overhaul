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

            private static BackupSource Cloud { get; } = new("cloud");
            private static BackupSource Local { get; } = new("local");
            private static BackupSource CloudPrePush { get; } = new("cloud-pre-push");
            private static BackupSource LocalPrePull { get; } = new("local-pre-pull");

            private string Label { get; }

            public override string ToString()
                => Label;
        }
    }
}
