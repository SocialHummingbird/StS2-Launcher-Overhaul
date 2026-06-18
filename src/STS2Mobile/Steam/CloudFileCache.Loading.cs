using System;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudFileCache
    {
        private const int MaxLoadRetries = 5;

        private readonly SteamConnection _connection;
        private readonly object _loadLock = new();
        private volatile bool _loaded;
        private int _loadRetries;
        private DateTimeOffset _nextLoadRetryTime = DateTimeOffset.MinValue;

        private void EnsureLoaded()
        {
            if (_loaded)
                return;
            if (!CanAttemptLoad(DateTimeOffset.UtcNow))
                return;

            lock (_loadLock)
            {
                if (_loaded || !CanAttemptLoad(DateTimeOffset.UtcNow))
                    return;

                try
                {
                    LoadFileList();
                    _loaded = true;
                }
                catch (Exception ex)
                {
                    RecordLoadFailure(ex);
                }
            }
        }

        private bool CanAttemptLoad(DateTimeOffset now)
        {
            if (_loadRetries >= MaxLoadRetries)
                return false;

            return _loadRetries == 0 || now >= _nextLoadRetryTime;
        }

        private void RecordLoadFailure(Exception ex)
        {
            _loadRetries++;
            var backoffSeconds = Math.Pow(2, _loadRetries);
            _nextLoadRetryTime = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
            PatchHelper.Log(FileEnumerationFailed(_loadRetries, MaxLoadRetries, ex));

            if (_loadRetries >= MaxLoadRetries)
                PatchHelper.Log(FileEnumerationMaxRetriesReached);
        }

        private static readonly string FileEnumerationMaxRetriesReached =
            CloudFileCacheMessage(
                "Max retries reached for cloud file enumeration this session."
            );

        private static string FileEnumerationFailed(int attempt, int maxRetries, Exception ex) =>
            CloudFileCacheMessage(
                $"Failed to enumerate cloud files (attempt {attempt}/{maxRetries}): {ex.Message}"
            );

        private static string CloudFileCacheMessage(string message)
            => $"[Cloud] {message}";
    }
}
