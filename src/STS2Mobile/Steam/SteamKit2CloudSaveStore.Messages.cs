using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private static readonly CloudStoreOperation BeginSaveBatchOperation =
        new("BeginSaveBatch");
    private static readonly CloudStoreOperation EndSaveBatchOperation =
        new("EndSaveBatch");
    private static readonly CloudStoreOperation QueueFlushOperation =
        new("Queue flush");
    private static readonly CloudStoreOperation ConnectionFlushOperation =
        new("Connection flush");
    private static readonly CloudStoreOperation CommitOperation =
        new("Commit");

    private readonly struct CloudStoreOperation
    {
        private readonly string _name;

        internal CloudStoreOperation(string name)
        {
            _name = name;
        }

        internal string Throttled(string path, int delayMs)
            => CloudStoreMessage(
                $"{_name} throttled for {path}, retrying in {delayMs / 1000}s..."
            );

        internal string Failed(Exception ex)
            => CloudStoreMessage($"{_name} failed: {ex.Message}");

        internal string FailedForPath(string path, Exception ex)
            => CloudStoreMessage($"{_name} failed for {path}: {ex.Message}");
    }

    private static string BeginSaveBatchFailed(Exception ex) =>
        BeginSaveBatchOperation.Failed(ex);

    private static string EndSaveBatchFailed(Exception ex) =>
        EndSaveBatchOperation.Failed(ex);

    private static string OperationThrottled(string operationName, string path, int delayMs) =>
        new CloudStoreOperation(operationName).Throttled(path, delayMs);

    private static string OperationFailed(string operationName, string path, Exception ex) =>
        new CloudStoreOperation(operationName).FailedForPath(path, ex);

    private static string QueueFlushFailed(Exception ex) =>
        QueueFlushOperation.Failed(ex);

    private static string ConnectionFlushFailed(Exception ex) =>
        ConnectionFlushOperation.Failed(ex);

    private static string Downloaded(
        string path,
        int bytes,
        bool encrypted,
        ulong fileSize,
        ulong rawFileSize) =>
        CloudStoreMessage(
            $"Downloaded {path} ({bytes} bytes, encrypted={encrypted}, file_size={fileSize}, raw_file_size={rawFileSize})"
        );

    private static string Unzipped(string path, int compressedSize, int decompressedSize) =>
        CloudStoreMessage($"Unzipped {path} ({compressedSize} -> {decompressedSize} bytes)");

    private static string RenameDeleteFailed(string sourcePath, Exception ex) =>
        CloudStoreMessage(
            $"RenameFile: delete of {sourcePath} failed (duplicate may exist): {ex.Message}"
        );

    private static string Compressed(string path, uint rawSize, int compressedSize) =>
        CloudStoreMessage($"Compressed {path} ({rawSize} -> {compressedSize} bytes)");

    private static string UploadingUncompressed(string path, uint rawSize) =>
        CloudStoreMessage($"Uploading {path} uncompressed ({rawSize} bytes)");

    private static string UploadSkippedAlreadyUpToDate(string path) =>
        CloudStoreMessage($"Skipped upload for {path} (already up to date)");

    private static string CommitReturnedFalse(string path) =>
        CloudStoreMessage($"Commit returned file_committed=false for {path}");

    private static string CommitFailed(string path, Exception ex) =>
        CommitOperation.FailedForPath(path, ex);

    private static string Wrote(string path, int bytes, bool compressed) =>
        CloudStoreMessage($"Wrote {bytes} bytes to {path} (compressed={compressed})");

    private static string CloudStoreMessage(string message)
        => $"[Cloud] {message}";
}
