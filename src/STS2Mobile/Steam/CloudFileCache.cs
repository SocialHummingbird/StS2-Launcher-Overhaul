using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    // In-memory cache of cloud file metadata (size, timestamp, persistence flag).
    // Loaded lazily from Steam on first access with exponential backoff on failure.
    private sealed partial class CloudFileCache
    {
        private readonly struct CloudFileInfo
        {
            private CloudFileInfo(int size, DateTimeOffset timestamp)
            {
                Size = size;
                Timestamp = timestamp;
            }

            internal int Size { get; }
            internal DateTimeOffset Timestamp { get; }

            internal static CloudFileInfo Create(int size, DateTimeOffset timestamp)
                => new(size, timestamp);
        }

        private readonly ConcurrentDictionary<
            string,
            CloudFileInfo
        > _files = new();
        private readonly ConcurrentDictionary<string, byte> _persistedFiles = new();

        internal CloudFileCache(SteamConnection connection)
        {
            _connection = connection;
        }

        internal bool FileExists(string path)
        {
            return GetFileInfo(path).HasValue;
        }

        internal DateTimeOffset GetLastModifiedTime(string path)
        {
            return GetFileInfo(path)?.Timestamp ?? DateTimeOffset.MinValue;
        }

        internal int GetFileSize(string path)
        {
            return GetFileInfo(path)?.Size ?? 0;
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
            var key = CacheKey(path);
            if (GetFileInfoByKey(key).HasValue)
                _persistedFiles.TryRemove(key, out _);
        }

        internal bool IsFilePersisted(string path)
        {
            var key = CacheKey(path);
            return GetFileInfoByKey(key).HasValue && _persistedFiles.ContainsKey(key);
        }

        internal void Set(string path, int size, DateTimeOffset timestamp)
        {
            var key = CacheKey(path);
            SetPersistedFile(key, size, timestamp);
        }

        internal void Remove(string path)
        {
            var key = CacheKey(path);
            _files.TryRemove(key, out _);
            _persistedFiles.TryRemove(key, out _);
        }

        private CloudFileInfo? GetFileInfo(string path)
            => GetFileInfoByKey(CacheKey(path));

        private void SetPersistedFile(string key, int size, DateTimeOffset timestamp)
        {
            _files[key] = CloudFileInfo.Create(size, timestamp);
            _persistedFiles[key] = 0;
        }

        private CloudFileInfo? GetFileInfoByKey(string key)
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
