using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    // In-memory cache of cloud file metadata (size, timestamp, persistence flag).
    // Loaded lazily from Steam on first access with exponential backoff on failure.
    private sealed partial class CloudFileCache
    {
        private readonly ConcurrentDictionary<string, CloudFileInfo> _files = new();

        internal CloudFileCache(SteamConnection connection)
        {
            _connection = connection;
        }

        internal bool FileExists(string path)
        {
            return TryGetFileInfo(path, out _);
        }

        internal DateTimeOffset GetLastModifiedTime(string path)
        {
            return TryGetFileInfo(path, out var info) ? info.Timestamp : DateTimeOffset.MinValue;
        }

        internal int GetFileSize(string path)
        {
            return TryGetFileInfo(path, out var info) ? info.Size : 0;
        }

        internal bool HasCloudFiles()
        {
            EnsureLoaded();
            if (!_loaded)
                return true; // Assume cloud has files to prevent destructive sync
            return _files.Count > 0;
        }

        internal void ForgetFile(string path)
        {
            if (TryGetFileInfo(path, out var info))
                info.Persisted = false;
        }

        internal bool IsFilePersisted(string path)
        {
            return TryGetFileInfo(path, out var info) && info.Persisted;
        }

        internal void Set(string path, int size, DateTimeOffset timestamp)
        {
            _files[CacheKey(path)] = new CloudFileInfo { Size = size, Timestamp = timestamp };
        }

        internal void Remove(string path)
        {
            _files.TryRemove(CacheKey(path), out _);
        }

        private bool TryGetFileInfo(string path, out CloudFileInfo info)
        {
            EnsureLoaded();
            return _files.TryGetValue(CacheKey(path), out info);
        }

        private static string CacheKey(string path)
            => CloudSavePath.Canonicalize(path);

        private sealed class CloudFileInfo
        {
            internal CloudFileInfo() { }

            internal int Size;
            internal DateTimeOffset Timestamp;
            internal volatile bool Persisted = true;
        }
    }
}
