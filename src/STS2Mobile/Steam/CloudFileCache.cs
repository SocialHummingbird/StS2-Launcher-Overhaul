using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    // In-memory cache of cloud file metadata (size, timestamp, persistence flag).
    // Loaded lazily from Steam on first access with exponential backoff on failure.
    private sealed partial class CloudFileCache
    {
        private readonly ConcurrentDictionary<
            string,
            (int Size, DateTimeOffset Timestamp)
        > _files = new();
        private readonly ConcurrentDictionary<string, byte> _persistedFiles = new();

        public CloudFileCache(SteamConnection connection)
        {
            _connection = connection;
        }

        public bool FileExists(string path)
        {
            return GetFileInfo(path).HasValue;
        }

        public DateTimeOffset GetLastModifiedTime(string path)
        {
            return GetFileInfo(path)?.Timestamp ?? DateTimeOffset.MinValue;
        }

        public int GetFileSize(string path)
        {
            return GetFileInfo(path)?.Size ?? 0;
        }

        public bool HasCloudFiles()
        {
            EnsureLoaded();
            if (!_loaded)
                return true; // Assume cloud has files to prevent destructive sync
            return _files.Count > 0;
        }

        public void ForgetFile(string path)
        {
            var key = CacheKey(path);
            if (GetFileInfoByKey(key).HasValue)
                _persistedFiles.TryRemove(key, out _);
        }

        public bool IsFilePersisted(string path)
        {
            var key = CacheKey(path);
            return GetFileInfoByKey(key).HasValue && _persistedFiles.ContainsKey(key);
        }

        public void Set(string path, int size, DateTimeOffset timestamp)
        {
            var key = CacheKey(path);
            _files[key] = (size, timestamp);
            _persistedFiles[key] = 0;
        }

        public void Remove(string path)
        {
            var key = CacheKey(path);
            _files.TryRemove(key, out _);
            _persistedFiles.TryRemove(key, out _);
        }

        private (int Size, DateTimeOffset Timestamp)? GetFileInfo(string path)
            => GetFileInfoByKey(CacheKey(path));

        private (int Size, DateTimeOffset Timestamp)? GetFileInfoByKey(string key)
        {
            EnsureLoaded();
            return _files.TryGetValue(key, out var info)
                ? info
                : null;
        }

        private static string CacheKey(string path)
            => CloudSavePath.Canonicalize(path);
    }
}
