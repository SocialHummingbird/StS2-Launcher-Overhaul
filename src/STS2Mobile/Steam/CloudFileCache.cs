using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
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

        internal CloudFileCache(SteamConnection connection)
        {
            _connection = connection;
        }

        internal bool FileExists(string path)
        {
            EnsureLoaded();
            if (!_loaded)
                return true; // Unknown after enumeration failure; let direct reads ask Steam.

            return GetFileInfo(path).HasValue;
        }

        internal bool IsLoaded()
        {
            EnsureLoaded();
            return _loaded;
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

        private (int Size, DateTimeOffset Timestamp)? GetFileInfo(string path)
            => GetFileInfoByKey(CacheKey(path));

        private void SetPersistedFile(string key, int size, DateTimeOffset timestamp)
        {
            _files[key] = (Size: size, Timestamp: timestamp);
            _persistedFiles[key] = 0;
        }

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
