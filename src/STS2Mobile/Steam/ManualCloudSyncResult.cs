namespace STS2Mobile.Steam;

internal readonly record struct ManualCloudSyncResult(
    CloudOperationKind Kind,
    int TransferredPathCount,
    int BackupCreatedCount,
    string Detail
);
