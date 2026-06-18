using System;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private const string BeginSaveBatchOperation = "BeginSaveBatch";
    private const string EndSaveBatchOperation = "EndSaveBatch";
    private const string QueueFlushOperation = "Queue flush";
    private const string ConnectionFlushOperation = "Connection flush";
    private const string CommitOperation = "Commit";
    private const string UploadOperation = "Upload";
    private const string DeleteOperation = "Delete";

    private static string BeginSaveBatchFailed(Exception ex) =>
        OperationFailed(BeginSaveBatchOperation, ex);

    private static string EndSaveBatchFailed(Exception ex) =>
        OperationFailed(EndSaveBatchOperation, ex);

    private static string QueueFlushFailed(Exception ex) =>
        OperationFailed(QueueFlushOperation, ex);

    private static string ConnectionFlushFailed(Exception ex) =>
        OperationFailed(ConnectionFlushOperation, ex);

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
        OperationFailedForPath(CommitOperation, path, ex);

    private static string OperationThrottled(string operation, string path, int delayMs)
        => CloudStoreMessage(
            $"{operation} throttled for {path}, retrying in {delayMs / 1000}s..."
        );

    private static string OperationFailed(string operation, Exception ex)
        => CloudStoreMessage($"{operation} failed: {ex.Message}");

    private static string OperationFailedForPath(
        string operation,
        string path,
        Exception ex
    )
        => CloudStoreMessage($"{operation} failed for {path}: {ex.Message}");

    private static string Wrote(string path, int bytes, bool compressed) =>
        CloudStoreMessage($"Wrote {bytes} bytes to {path} (compressed={compressed})");

    private static string CloudStoreMessage(string message)
        => $"[Cloud] {message}";
}
