using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

// In-memory cache of cloud file metadata (size, timestamp, persistence flag).
// Loaded lazily from Steam on first access with exponential backoff on failure.
internal sealed class CloudFileCache
{
    private const int MaxLoadRetries = 5;

    private readonly SteamConnection _connection;
    private readonly ConcurrentDictionary<string, CloudFileInfo> _files = new();
    private volatile bool _loaded;
    private readonly object _loadLock = new();
    private int _loadRetries;
    private DateTimeOffset _nextLoadRetryTime = DateTimeOffset.MinValue;

    internal CloudFileCache(SteamConnection connection)
    {
        _connection = connection;
    }

    internal static string CanonicalizePath(string path)
    {
        return path.Replace("user://", "").Replace("\\", "/");
    }

    internal bool FileExists(string path)
    {
        EnsureLoaded();
        return _files.ContainsKey(CanonicalizePath(path));
    }

    internal DateTimeOffset GetLastModifiedTime(string path)
    {
        EnsureLoaded();
        return _files.TryGetValue(CanonicalizePath(path), out var info)
            ? info.Timestamp
            : DateTimeOffset.MinValue;
    }

    internal int GetFileSize(string path)
    {
        EnsureLoaded();
        return _files.TryGetValue(CanonicalizePath(path), out var info) ? info.Size : 0;
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
        if (_files.TryGetValue(CanonicalizePath(path), out var info))
            info.Persisted = false;
    }

    internal bool IsFilePersisted(string path)
    {
        return _files.TryGetValue(CanonicalizePath(path), out var info) && info.Persisted;
    }

    internal void Set(string path, int size, DateTimeOffset timestamp)
    {
        _files[CanonicalizePath(path)] = new CloudFileInfo { Size = size, Timestamp = timestamp };
    }

    internal void Remove(string path)
    {
        _files.TryRemove(CanonicalizePath(path), out _);
    }

    internal string[] GetFilesInDirectory(string directoryPath)
    {
        directoryPath = CanonicalizePath(directoryPath);
        EnsureLoaded();
        var result = new List<string>();

        foreach (var remainder in EnumerateDirectoryEntries(_files.Keys, directoryPath))
        {
            if (!remainder.Contains('/') && !remainder.Contains('\\'))
                result.Add(remainder);
        }

        return result.ToArray();
    }

    internal string[] GetDirectoriesInDirectory(string directoryPath)
    {
        directoryPath = CanonicalizePath(directoryPath);
        EnsureLoaded();
        var dirs = new HashSet<string>();

        foreach (var remainder in EnumerateDirectoryEntries(_files.Keys, directoryPath))
        {
            var slashIndex = remainder.IndexOf('/');
            if (slashIndex >= 0)
                dirs.Add(remainder.Substring(0, slashIndex));
        }

        return [.. dirs];
    }

    internal void Refresh()
    {
        _files.Clear();
        _loaded = false;
        _loadRetries = 0;
        _nextLoadRetryTime = DateTimeOffset.MinValue;
        EnsureLoaded();
    }

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
        PatchHelper.Log(CloudRuntimeMessage.FileEnumerationFailed(_loadRetries, MaxLoadRetries, ex));

        if (_loadRetries >= MaxLoadRetries)
            PatchHelper.Log(CloudRuntimeMessage.FileEnumerationMaxRetriesReached);
    }

    private void LoadFileList()
    {
        uint startIndex = 0;
        const uint pageSize = 500;

        while (true)
        {
            var result = _connection
                .SendCloud<CCloud_EnumerateUserFiles_Request, CCloud_EnumerateUserFiles_Response>(
                    "EnumerateUserFiles",
                    new CCloud_EnumerateUserFiles_Request
                    {
                        appid = SteamCloudApp.AppId,
                        start_index = startIndex,
                        count = pageSize,
                    }
                )
                .GetAwaiter()
                .GetResult();

            if (result.files == null || result.files.Count == 0)
                break;

            foreach (var file in result.files)
            {
                _files[file.filename] = new CloudFileInfo
                {
                    Size = (int)file.file_size,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)file.timestamp),
                };
            }

            startIndex += (uint)result.files.Count;
            if (result.files.Count < pageSize)
                break;
        }

        PatchHelper.Log(CloudRuntimeMessage.FilesEnumerated(_files.Count));
    }

    private static IEnumerable<string> EnumerateDirectoryEntries(
        IEnumerable<string> paths,
        string directoryPath
    )
    {
        var normalizedDirectory = CanonicalizePath(directoryPath);
        var prefix = normalizedDirectory.Length > 0 ? normalizedDirectory + "/" : "";

        foreach (var key in paths)
        {
            var normalizedKey = CanonicalizePath(key);
            if (!normalizedKey.StartsWith(prefix) || normalizedKey.Length <= prefix.Length)
                continue;

            yield return normalizedKey.Substring(prefix.Length);
        }
    }

    private sealed class CloudFileInfo
    {
        private int Size;
        private DateTimeOffset Timestamp;
        private volatile bool Persisted = true;
    }
}

