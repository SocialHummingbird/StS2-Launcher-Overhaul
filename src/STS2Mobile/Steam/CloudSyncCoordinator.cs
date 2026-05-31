using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace STS2Mobile.Steam;

// Stateless cloud sync coordinator: auto sync, manual push/pull, and save backups.
internal static class CloudSyncCoordinator
{
    private enum CompareResult
    {
        CloudWins,
        LocalWins,
        Equal,
    }

    private const string ActsProperty = "acts";
    private const string CharacterStatsProperty = "character_stats";
    private const string CurrentRunPathToken = "current_run";
    private const string DiscoveredActsProperty = "discovered_acts";
    private const string DiscoveredCardsProperty = "discovered_cards";
    private const string DiscoveredEventsProperty = "discovered_events";
    private const string DiscoveredPotionsProperty = "discovered_potions";
    private const string DiscoveredRelicsProperty = "discovered_relics";
    private const string FloorsClimbedProperty = "floors_climbed";
    private const string MapPointHistoryProperty = "map_point_history";
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int AutoSyncPerPathTimeoutMs = 30_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;
    private const int MaxProgressBackups = 50;
    private const string BackupSourceCloud = "cloud";
    private const string BackupSourceCloudPrePush = "cloud-pre-push";
    private const string BackupSourceLocal = "local";
    private const string BackupSourceLocalPrePull = "local-pre-pull";
    private const int RunHistoryLimit = 100;
    private const string ProgressPathToken = "progress";
    private const string SaveExtension = ".save";
    private const string TotalLossesProperty = "total_losses";
    private const string TotalPlaytimeProperty = "total_playtime";
    private const string TotalWinsProperty = "total_wins";
    private static bool _localBackupEnabled;
    private static readonly string[] DiscoveryProperties =
    {
        DiscoveredCardsProperty,
        DiscoveredRelicsProperty,
        DiscoveredPotionsProperty,
        DiscoveredEventsProperty,
        DiscoveredActsProperty,
    };
    private static readonly string[] FallbackHistoryDirectories =
    {
        "runs",
        "run_history",
        "history",
    };
    private static readonly string[] FallbackProfileFiles =
    {
        "progress.save",
        "current_run.save",
        "current_run_mp.save",
        "prefs",
        "prefs.save",
    };
    private static readonly string[] FallbackProfilePrefixes =
    {
        "",
        "modded/",
    };

    internal static void SetLocalBackupEnabled(bool enabled)
    {
        _localBackupEnabled = enabled;
    }

    internal static async Task PushFileAsync(ISaveStore local, ICloudSaveStore cloud, string path)
    {
        if (!local.FileExists(path))
            return;

        string content = local.ReadFile(path);

        if (cloud.FileExists(path))
        {
            string cloudContent = await RunWithTimeout(
                () => cloud.ReadFileAsync(path),
                $"ReadCloudFile {path}",
                AutoSyncPerPathTimeoutMs
            );
            if (content == cloudContent)
            {
                PatchHelper.Log(CloudRuntimeMessage.PushSkippingIdentical(path));
                return;
            }
            BackupProgressContent(path, cloudContent, BackupSourceCloud);
        }

        cloud.WriteFile(path, content);
        PatchHelper.Log(CloudRuntimeMessage.PushUploaded(path));
    }

    internal static async Task PullFileAsync(ISaveStore local, ICloudSaveStore cloud, string path)
    {
        if (!cloud.FileExists(path))
            return;

        string cloudContent = await RunWithTimeout(
            () => cloud.ReadFileAsync(path),
            $"PullFile {path}",
            AutoSyncPerPathTimeoutMs
        );

        if (local.FileExists(path))
        {
            string localContent = local.ReadFile(path);
            if (localContent == cloudContent)
            {
                PatchHelper.Log(CloudRuntimeMessage.PullSkippingIdentical(path));
                return;
            }
            BackupProgressFile(local, path);
        }

        await WriteCloudContentAsync(local, cloud, path, cloudContent, AutoSyncPerPathTimeoutMs);
        PatchHelper.Log(CloudRuntimeMessage.PullDownloaded(path));
    }

    // Uses content comparison only because timestamps are unreliable on mobile.
    // Progress/run files use SaveProgressComparer; non-progress files default to
    // cloud wins; history files sync bidirectionally.
    internal static async Task AutoSyncFileAsync(ISaveStore local, ICloudSaveStore cloud, string path)
    {
        try
        {
            bool cloudExists = cloud.FileExists(path);
            bool localExists = local.FileExists(path);

            if (cloudExists && localExists)
            {
                string localContent = local.ReadFile(path);
                string cloudContent = await RunWithTimeout(
                    () => cloud.ReadFileAsync(path),
                    $"ReadCloudFile {path}",
                    AutoSyncPerPathTimeoutMs
                );

                if (IsCorrupt(localContent))
                {
                    PatchHelper.Log(CloudRuntimeMessage.SyncLocalCorruptPulling(path));
                    BackupProgressFile(local, path);
                    await WriteCloudContentAsync(local, cloud, path, cloudContent, AutoSyncPerPathTimeoutMs);
                    return;
                }

                if (localContent == cloudContent)
                {
                    PatchHelper.Log(CloudRuntimeMessage.SyncIdenticalSkipping(path));
                    return;
                }

                var result = CompareSaveProgress(path, localContent, cloudContent);

                if (result == CompareResult.CloudWins)
                {
                    PatchHelper.Log(CloudRuntimeMessage.SyncCloudWins(path));
                    BackupProgressFile(local, path);
                    await WriteCloudContentAsync(local, cloud, path, cloudContent, AutoSyncPerPathTimeoutMs);
                }
                else if (result == CompareResult.LocalWins)
                {
                    PatchHelper.Log(CloudRuntimeMessage.SyncLocalWinsUploading(path));
                    BackupProgressContent(path, cloudContent, BackupSourceCloud);
                    cloud.WriteFile(path, localContent);
                }
                else
                {
                    // Cloud wins on equal progress or non-progress files to preserve PC as primary.
                    PatchHelper.Log(CloudRuntimeMessage.SyncContentsDifferCloudWins(path));
                    BackupProgressFile(local, path);
                    await WriteCloudContentAsync(local, cloud, path, cloudContent, AutoSyncPerPathTimeoutMs);
                }
            }
            else if (cloudExists)
            {
                await PullFileAsync(local, cloud, path);
            }
            else if (localExists)
            {
                await PushFileAsync(local, cloud, path);
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.SyncFailed(path, ex));
        }
    }

    internal static async Task ManualPushAllAsync(string accountName, string refreshToken)
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);

        var paths = GetSaveFilePaths(sync.Local.GetFilesInDirectory, sync.Local.DirectoryExists);
        PatchHelper.Log(CloudRuntimeMessage.PushStarting(paths.Count));

        var backedUp = await BackupCloudBeforeManualPushAsync(sync.Cloud, paths);
        if (backedUp > 0)
            PatchHelper.Log(CloudRuntimeMessage.PushBackedUpCloudFiles(backedUp));

        sync.Cloud.BeginSaveBatch();
        int count = 0;
        foreach (var path in paths)
        {
            try
            {
                if (!sync.Local.FileExists(path))
                    continue;

                string content = sync.Local.ReadFile(path);
                PatchHelper.Log(CloudRuntimeMessage.PushQueuing(path, content.Length));
                if (ManualSyncBudgetExceeded(sync.Deadline, CloudRuntimeMessage.ManualPushBudgetExceeded))
                    break;

                sync.Cloud.WriteFile(path, content);
                count++;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(CloudRuntimeMessage.PushFailed(path, ex));
            }
        }
        sync.Cloud.EndSaveBatch();

        PatchHelper.Log(CloudRuntimeMessage.PushComplete(count));
    }

    internal static async Task ManualPullAllAsync(string accountName, string refreshToken)
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);

        var paths = GetSaveFilePaths(sync.Cloud.GetFilesInDirectory, sync.Cloud.DirectoryExists);
        PatchHelper.Log(CloudRuntimeMessage.PullStarting(paths.Count));

        var backedUp = BackupLocalBeforeManualPull(sync.Local, paths);
        if (backedUp > 0)
            PatchHelper.Log(CloudRuntimeMessage.PullBackedUpLocalFiles(backedUp));

        int downloaded = 0;
        int skipped = 0;
        foreach (var path in paths)
        {
            try
            {
                if (!sync.Cloud.FileExists(path))
                {
                    skipped++;
                    continue;
                }
                PatchHelper.Log(CloudRuntimeMessage.PullDownloading(path));
                string content = await RunWithTimeout(
                    () => sync.Cloud.ReadFileAsync(path),
                    $"ManualPull download {path}",
                    ManualSyncPerPathTimeoutMs
                );
                await WriteCloudContentAsync(sync.Local, sync.Cloud, path, content, ManualSyncPerPathTimeoutMs);
                PatchHelper.Log(CloudRuntimeMessage.PullWrote(path, content.Length));
                downloaded++;
            }
            catch (TimeoutException)
            {
                PatchHelper.Log(CloudRuntimeMessage.PullPathTimedOut(path));
            }
            catch (Exception ex)
            {
                PatchHelper.Log(CloudRuntimeMessage.PullFailed(path, ex));
            }

            if (ManualSyncBudgetExceeded(sync.Deadline, CloudRuntimeMessage.ManualPullBudgetExceeded))
                break;
        }

        PatchHelper.Log(CloudRuntimeMessage.PullComplete(downloaded, skipped));
    }

    private readonly record struct ManualSyncContext(
        ISaveStore Local,
        ICloudSaveStore Cloud,
        DateTime Deadline
    );

    private static ManualSyncContext CreateManualSyncContext(
        string accountName,
        string refreshToken
    )
        => new(
            CreateLocalStore(),
            CreateCloudStore(accountName, refreshToken),
            DateTime.UtcNow.AddMilliseconds(ManualSyncOverallTimeoutMs)
        );

    private static bool ManualSyncBudgetExceeded(DateTime deadline, string message)
    {
        if (DateTime.UtcNow <= deadline)
            return false;

        PatchHelper.Log(message);
        return true;
    }

    private static List<string> GetSaveFilePaths(
        Func<string, string[]> getFiles,
        Func<string, bool> dirExists
    )
    {
        var paths = new List<string>();
        CollectProfilePathsSafe(paths, getFiles, dirExists);
        return paths;
    }

    private static void CollectProfilePathsSafe(
        List<string> paths,
        Func<string, string[]> getFiles,
        Func<string, bool> dirExists
    )
    {
        if (OperatingSystem.IsAndroid())
        {
            AddFallbackProfilePaths(paths, getFiles, dirExists);
            return;
        }

        try
        {
            CollectProfilePaths(paths, getFiles, dirExists);
        }
        catch (TypeInitializationException ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.SavePathManagerFallback(ex));
            AddFallbackProfilePaths(paths, getFiles, dirExists);
        }
        catch (Exception ex) when (OperatingSystem.IsAndroid())
        {
            PatchHelper.Log(CloudRuntimeMessage.SavePathManagerFallback(ex));
            AddFallbackProfilePaths(paths, getFiles, dirExists);
        }
    }

    private static void CollectProfilePaths(
        List<string> paths,
        Func<string, string[]> getFiles,
        Func<string, bool> dirExists
    )
    {
        var wasModded = UserDataPathProvider.IsRunningModded;
        try
        {
            foreach (bool modded in new[] { false, true })
            {
                UserDataPathProvider.IsRunningModded = modded;
                for (int i = 1; i <= 3; i++)
                {
                    paths.Add(ProgressSaveManager.GetProgressPathForProfile(i));
                    paths.Add(RunSaveManager.GetRunSavePath(i, "current_run.save"));
                    paths.Add(RunSaveManager.GetRunSavePath(i, "current_run_mp.save"));
                    paths.Add(PrefsSaveManager.GetPrefsPath(i));
                    AddHistoryFiles(paths, getFiles, dirExists, i);
                }
            }
        }
        finally
        {
            UserDataPathProvider.IsRunningModded = wasModded;
        }
    }

    private static void AddHistoryFiles(
        List<string> paths,
        Func<string, string[]> getFiles,
        Func<string, bool> dirExists,
        int profileId
    )
    {
        var historyDir = RunHistorySaveManager.GetHistoryPath(profileId);
        if (!dirExists(historyDir))
            return;

        foreach (var file in SelectManagedRunHistoryFiles(getFiles(historyDir)))
            paths.Add($"{historyDir}/{file}");
    }

    private static void AddFallbackProfilePaths(
        List<string> paths,
        Func<string, string[]> getFiles,
        Func<string, bool> dirExists
    )
    {
        foreach (var prefix in FallbackProfilePrefixes)
        {
            for (int i = 1; i <= 3; i++)
            {
                var profile = $"{prefix}profile{i}";
                foreach (var file in FallbackProfileFiles)
                    paths.Add($"{profile}/{file}");

                foreach (var historyName in FallbackHistoryDirectories)
                {
                    var historyDir = $"{profile}/{historyName}";
                    if (!dirExists(historyDir))
                        continue;

                    foreach (var file in SelectFallbackRunHistoryFiles(getFiles(historyDir)))
                        paths.Add($"{historyDir}/{file}");
                }
            }
        }
    }

    private static IEnumerable<string> SelectManagedRunHistoryFiles(IEnumerable<string> files)
    {
        return files
            .Where(f => f.EndsWith(".run") && !f.EndsWith(".backup") && !f.EndsWith(".tmp"))
            .OrderByDescending(f => f)
            .Take(RunHistoryLimit);
    }

    private static IEnumerable<string> SelectFallbackRunHistoryFiles(IEnumerable<string> files)
    {
        return files.Where(f => f.EndsWith(".run")).Take(RunHistoryLimit);
    }

    private static CompareResult CompareSaveProgress(string path, string localContent, string cloudContent)
    {
        try
        {
            var canonPath = CloudFileCache.CanonicalizePath(path).ToLowerInvariant();
            if (canonPath.Contains(ProgressPathToken) && canonPath.EndsWith(SaveExtension))
                return CompareProgress(localContent, cloudContent);

            if (canonPath.Contains(CurrentRunPathToken) && canonPath.EndsWith(SaveExtension))
                return CompareCurrentRun(localContent, cloudContent);

            // History files have unique filenames (no conflict); prefs have no progress concept.
            return CompareResult.Equal;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.ProgressComparisonFailed(path, ex));
            return CompareResult.Equal;
        }
    }

    private static CompareResult CompareProgress(string local, string cloud)
    {
        using var localDoc = JsonDocument.Parse(local);
        using var cloudDoc = JsonDocument.Parse(cloud);
        var localRoot = localDoc.RootElement;
        var cloudRoot = cloudDoc.RootElement;

        int localFloors = GetInt(localRoot, FloorsClimbedProperty);
        int cloudFloors = GetInt(cloudRoot, FloorsClimbedProperty);
        if (localFloors != cloudFloors)
            return CompareNumeric(localFloors, cloudFloors);

        int localGames = SumCharacterGames(localRoot);
        int cloudGames = SumCharacterGames(cloudRoot);
        if (localGames != cloudGames)
            return CompareNumeric(localGames, cloudGames);

        int localDiscovered = CountDiscovered(localRoot);
        int cloudDiscovered = CountDiscovered(cloudRoot);
        if (localDiscovered != cloudDiscovered)
            return CompareNumeric(localDiscovered, cloudDiscovered);

        int localPlaytime = GetInt(localRoot, TotalPlaytimeProperty);
        int cloudPlaytime = GetInt(cloudRoot, TotalPlaytimeProperty);
        if (localPlaytime != cloudPlaytime)
            return CompareNumeric(localPlaytime, cloudPlaytime);

        return CompareResult.Equal;
    }

    private static CompareResult CompareCurrentRun(string local, string cloud)
    {
        using var localDoc = JsonDocument.Parse(local);
        using var cloudDoc = JsonDocument.Parse(cloud);

        int localFloors = CountRunFloors(localDoc.RootElement);
        int cloudFloors = CountRunFloors(cloudDoc.RootElement);

        if (localFloors != cloudFloors)
            return CompareNumeric(localFloors, cloudFloors);

        return CompareResult.Equal;
    }

    private static CompareResult CompareNumeric(int localValue, int cloudValue)
    {
        return localValue > cloudValue ? CompareResult.LocalWins : CompareResult.CloudWins;
    }

    private static int CountRunFloors(JsonElement root)
    {
        int count = 0;
        if (
            root.TryGetProperty(MapPointHistoryProperty, out var history)
            && history.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var act in history.EnumerateArray())
            {
                if (act.ValueKind == JsonValueKind.Array)
                    count += act.GetArrayLength();
            }
        }
        else if (root.TryGetProperty(ActsProperty, out var acts) && acts.ValueKind == JsonValueKind.Array)
        {
            // Alternate save format
            count = acts.GetArrayLength();
        }

        return count;
    }

    private static int SumCharacterGames(JsonElement root)
    {
        int total = 0;
        if (
            root.TryGetProperty(CharacterStatsProperty, out var stats)
            && stats.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var character in stats.EnumerateArray())
            {
                total += GetInt(character, TotalWinsProperty);
                total += GetInt(character, TotalLossesProperty);
            }
        }
        return total;
    }

    private static int CountDiscovered(JsonElement root)
    {
        int count = 0;
        foreach (var property in DiscoveryProperties)
            count += GetArrayLength(root, property);
        return count;
    }

    private static int GetInt(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result)
            ? result
            : 0;
    }

    private static int GetArrayLength(JsonElement element, string property)
    {
        return
            element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;
    }

    // Save files are JSON; a non-JSON opener indicates corruption, e.g. unencrypted write.
    private static bool IsCorrupt(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;
        return content[0] != '{' && content[0] != '[';
    }

    private static async Task<int> BackupCloudBeforeManualPushAsync(
        ICloudSaveStore cloudStore,
        IEnumerable<string> paths
    )
    {
        var backedUp = 0;
        foreach (var path in paths)
        {
            try
            {
                if (!IsImportantSave(path) || !cloudStore.FileExists(path))
                    continue;

                PatchHelper.Log(CloudRuntimeMessage.PushBackingUpCloud(path));
                var content = await RunWithTimeout(
                    () => cloudStore.ReadFileAsync(path),
                    $"ManualPush backup read {path}",
                    ManualSyncPerPathTimeoutMs
                );
                BackupSaveContent(path, content, BackupSourceCloudPrePush);
                backedUp++;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(CloudRuntimeMessage.PushCloudBackupFailed(path, ex));
            }
        }

        return backedUp;
    }

    private static int BackupLocalBeforeManualPull(ISaveStore localStore, IEnumerable<string> paths)
    {
        var backedUp = 0;
        foreach (var path in paths)
        {
            try
            {
                if (!localStore.FileExists(path))
                    continue;

                BackupSaveContent(path, localStore.ReadFile(path), BackupSourceLocalPrePull);
                backedUp++;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(CloudRuntimeMessage.PullLocalBackupFailed(path, ex));
            }
        }

        return backedUp;
    }

    private static bool IsImportantSave(string path)
    {
        var lower = NormalizeSavePath(path).ToLowerInvariant();
        return lower.Contains("progress.save")
            || lower.Contains("current_run")
            || lower.Contains("prefs");
    }

    private static void BackupSaveContent(string path, string content, string source)
    {
        try
        {
            if (ShouldSkipBackup(content))
                return;

            var canonPath = NormalizeSavePath(path);
            var backupPath = BuildBackupPath(
                GetProfileDir(canonPath),
                Path.GetFileName(canonPath),
                source
            );

            File.WriteAllText(backupPath, content);
            PatchHelper.Log(CloudRuntimeMessage.SaveBackedUp(source, path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.BackupFailed(source, path, ex));
        }
    }

    private static void BackupProgressFile(ISaveStore local, string path)
    {
        if (!IsProgressSave(path))
            return;

        if (!local.FileExists(path))
            return;

        BackupProgressContent(path, local.ReadFile(path), BackupSourceLocal);
    }

    private static void BackupProgressContent(string path, string content, string source)
    {
        try
        {
            var canonPath = NormalizeSavePath(path).ToLowerInvariant();
            if (!IsProgressSave(canonPath))
                return;

            if (ShouldSkipBackup(content))
                return;

            var backupPath = BuildBackupPath(GetProfileDir(canonPath), "progress.save", source);
            var backupDir = Path.GetDirectoryName(backupPath) ?? AppPaths.ExternalSaveBackupsDir;

            File.WriteAllText(backupPath, content);
            PatchHelper.Log(CloudRuntimeMessage.SaveBackedUpTo(source, path, backupPath));

            PruneProgressBackups(backupDir);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.BackupFailed(source, path, ex));
        }
    }

    private static bool HasBackupAccess()
        => _localBackupEnabled && AppPaths.HasStoragePermission();

    private static bool ShouldSkipBackup(string content)
        => string.IsNullOrEmpty(content) || !HasBackupAccess();

    private static bool IsProgressSave(string path)
    {
        var canonPath = NormalizeSavePath(path).ToLowerInvariant();
        return canonPath.Contains("progress") && canonPath.EndsWith(".save");
    }

    private static string NormalizeSavePath(string path)
    {
        return CloudFileCache.CanonicalizePath(path);
    }

    private static string GetProfileDir(string canonPath)
    {
        var parts = canonPath.Split('/');
        foreach (var part in parts)
        {
            if (part.StartsWith("profile"))
                return part;
        }

        return "default";
    }

    private static string BuildBackupPath(string profileDir, string fileName, string source)
    {
        var backupDir = Path.Combine(AppPaths.ExternalSaveBackupsDir, profileDir);
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Path.Combine(backupDir, $"{fileName}.{timestamp}.{source}.bak");
    }

    private static void PruneProgressBackups(string backupDir)
    {
        var backups = Directory
            .GetFiles(backupDir, "progress.save.*.bak")
            .OrderByDescending(f => f)
            .Skip(MaxProgressBackups)
            .ToArray();

        foreach (var old in backups)
        {
            try
            {
                File.Delete(old);
            }
            catch
            {
            }
        }
    }

    private static ISaveStore CreateLocalStore()
    {
        return OperatingSystem.IsAndroid()
            ? new AndroidLocalSaveStore()
            : new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath(null));
    }

    private static ICloudSaveStore CreateCloudStore(string accountName, string refreshToken)
    {
        return SteamKit2CloudSaveStore.GetOrCreate(accountName, refreshToken);
    }

    private static async Task WriteCloudContentAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        string path,
        string content,
        int timeoutMs
    )
    {
        var cloudTime = cloud.GetLastModifiedTime(path);
        await RunWithTimeout(
            () => local.WriteFileAsync(path, content),
            $"WriteLocalFile {path}",
            timeoutMs
        );
        local.SetLastModifiedTime(path, cloudTime);
    }

    private static async Task<T> RunWithTimeout<T>(
        Func<Task<T>> operationFactory,
        string operation,
        int timeoutMs
    )
    {
        var task = operationFactory();
        await WaitForTimeoutAsync(task, operation, timeoutMs).ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }

    private static async Task RunWithTimeout(
        Func<Task> operationFactory,
        string operation,
        int timeoutMs
    )
    {
        var task = operationFactory();
        await WaitForTimeoutAsync(task, operation, timeoutMs).ConfigureAwait(false);
        await task.ConfigureAwait(false);
    }

    private static async Task WaitForTimeoutAsync(Task task, string operation, int timeoutMs)
    {
        var timeout = Task.Delay(timeoutMs);
        var winner = await Task.WhenAny(task, timeout).ConfigureAwait(false);
        if (winner == task)
            return;

        _ = task.ContinueWith(
            t =>
                PatchHelper.Log(CloudRuntimeMessage.LateCompletionAfterTimeout(operation, t.Exception)),
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously
        );
        throw new TimeoutException($"{operation} timed out after {timeoutMs}ms");
    }
}
