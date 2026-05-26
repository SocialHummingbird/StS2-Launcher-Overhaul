using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Orchestrates the launcher flow: credential loading, authentication, ownership
// verification, game file downloads, and update checks. Delegates persistence to
// SteamCredentialStore and ownership to OwnershipVerifier. Events fire from
// background threads; the controller marshals them to the main thread.
public class LauncherModel : IDisposable
{
    private readonly string _dataDir;
    private readonly SteamCredentialStore _credentialStore;

    private SteamConnection _connection;
    private SteamAuth _auth;
    private DepotDownloader _downloader;
    private CancellationTokenSource _downloadCts;
    private TaskCompletionSource<bool> _launchTcs;
    private TaskCompletionSource<string> _codeTcs;
    private SessionState _state = SessionState.Disconnected;
    private string _failReason;
    private int _downloadRunning;

    public volatile bool OfflineMode;
    public volatile bool ConnectionResolved;
    public volatile bool AwaitingCode;

    // True when launched from GameStartupWrapper (game files present). False in
    // standalone launcher mode where a restart is needed after downloading files.
    // Setting this to true eagerly creates the launch TCS so it exists before the
    // UI is shown (preventing a race between PLAY button and WaitForLaunch).
    private bool _inGameMode;
    public bool InGameMode
    {
        get => _inGameMode;
        set
        {
            _inGameMode = value;
            if (value && _launchTcs == null)
                _launchTcs = new TaskCompletionSource<bool>();
        }
    }
    public string AccountName => _credentialStore.AccountName;
    public string SavedAccountName => _credentialStore.AccountName;
    public string SavedRefreshToken => _credentialStore.RefreshToken;
    public string FailReason => _failReason;
    public SessionState SessionState => _state;

    public event Action<SessionState> SessionStateChanged;
    public event Action<string> LogReceived;
    public event Action<bool> CodeNeeded;
    public event Action<DownloadProgress> DownloadProgressChanged;
    public event Action<string> DownloadLogReceived;
    public event Action DownloadCompleted;
    public event Action<string> DownloadFailed;
    public event Action DownloadCancelled;
    public event Action<bool> UpdateCheckCompleted;
    public event Action<string> UpdateCheckFailed;

    public LauncherModel(string dataDir)
    {
        _dataDir = dataDir;
        _credentialStore = new SteamCredentialStore(dataDir);
    }

    public Task WaitForLaunch()
    {
        _launchTcs ??= new TaskCompletionSource<bool>();
        return _launchTcs.Task;
    }

    // Loads saved credentials and determines the launcher path. Sets
    // LauncherPatches statics so cloud push/pull works on all code paths.
    public FastPathResult StartSession()
    {
        OfflineMode = false;
        ConnectionResolved = false;
        _credentialStore.Load();

        if (_credentialStore.HasCredentials)
        {
            LauncherPatches.SavedAccountName = _credentialStore.AccountName;
            LauncherPatches.SavedRefreshToken = _credentialStore.RefreshToken;
        }

        var verifier = CreateOwnershipVerifier();
        var hasMarker = verifier?.HasMarker() ?? false;
        PatchHelper.Log(
            $"[Launcher] Fast path: creds={_credentialStore.HasCredentials}, marker={hasMarker}"
        );

        if (_credentialStore.HasCredentials && hasMarker && GameFilesReady())
            return FastPathResult.ReadyToLaunch;

        if (_credentialStore.HasCredentials)
            return FastPathResult.AutoConnect;

        return FastPathResult.ShowLogin;
    }

    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    public async void Connect()
    {
        SetState(SessionState.Connecting);

        try
        {
            _connection = new SteamConnection(
                _credentialStore.AccountName,
                _credentialStore.RefreshToken
            );
            await VerifyOwnershipAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Connection failed: {ex.Message}");
            SetState(
                SessionState.Failed,
                "Could not connect to Steam. Check your internet connection."
            );
        }
    }

    // Performs interactive login via SteamAuth, saves credentials on success,
    // then verifies ownership.
    public async Task LoginAsync(string username, string password)
    {
        SetState(SessionState.Authenticating);

        try
        {
            _auth = new SteamAuth();
            _auth.LogMessage += msg => LogReceived?.Invoke(msg);
            _auth.CodeProvider = async (wasIncorrect) =>
            {
                AwaitingCode = true;
                CodeNeeded?.Invoke(wasIncorrect);
                _codeTcs = new TaskCompletionSource<string>();
                var code = await WaitForSubmittedCodeAsync(_codeTcs.Task);

                if (_auth.NeedsReconnectForAuth)
                    await _auth.ReconnectForAuthAsync();

                AwaitingCode = false;
                return code;
            };

            _auth.Connect();
            var result = await _auth.LoginWithCredentialsAsync(
                username,
                password,
                _credentialStore.GuardData
            );

            _credentialStore.Save(result.AccountName, result.RefreshToken, result.GuardData);
            LauncherPatches.SavedAccountName = result.AccountName;
            LauncherPatches.SavedRefreshToken = result.RefreshToken;

            _auth.Dispose();
            _auth = null;

            _connection = new SteamConnection(result.AccountName, result.RefreshToken);
            await VerifyOwnershipAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Login failed: {ex.Message}");
            SetState(SessionState.Failed, ex.Message);
            _auth?.Dispose();
            _auth = null;
        }
    }

    public void SubmitCode(string code) => _codeTcs?.TrySetResult(code);

    private async Task<string> WaitForSubmittedCodeAsync(Task<string> uiCodeTask)
    {
        while (true)
        {
            if (uiCodeTask.IsCompleted)
                return await uiCodeTask;

            var localCode = TryConsumeLocalGuardCode();
            if (!string.IsNullOrWhiteSpace(localCode))
                return localCode;

            await Task.Delay(500);
        }
    }

    private static string TryConsumeLocalGuardCode()
    {
        if (!OperatingSystem.IsAndroid())
            return null;

        var path = GetLocalGuardCodePath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        string code;
        try
        {
            code = File.ReadAllText(path).Trim().ToUpperInvariant();
            File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Auth] Failed to consume local Steam Guard code file: {ex.Message}");
            return null;
        }

        if (!IsGuardCodeShape(code))
        {
            PatchHelper.Log("[Auth] Ignored local Steam Guard code file with invalid shape");
            return null;
        }

        PatchHelper.Log("[Auth] Consumed local Steam Guard code file");
        return code;
    }

    private static bool IsGuardCodeShape(string code)
    {
        if (code == null || code.Length != 5)
            return false;

        foreach (var ch in code)
        {
            if (!char.IsLetterOrDigit(ch))
                return false;
        }

        return true;
    }

    private static string GetLocalGuardCodePath()
    {
        try
        {
            var godotApp = GetGodotApp();
            var dir = (string)godotApp?.Call("getExternalFilesDirPath");
            return string.IsNullOrWhiteSpace(dir)
                ? null
                : Path.Combine(dir, "steam_guard_code.txt");
        }
        catch
        {
            return null;
        }
    }

    // Creates or reuses a SteamConnection for depot operations.
    public async Task EnsureConnectedAsync()
    {
        if (_state == SessionState.LoggedIn && _connection != null)
            return;

        if (!_credentialStore.HasCredentials)
        {
            SetState(SessionState.Failed, "No saved credentials");
            return;
        }

        _connection ??= new SteamConnection(
            _credentialStore.AccountName,
            _credentialStore.RefreshToken
        );

        SetState(SessionState.Connecting);
        try
        {
            var tokenResult = await _connection.Apps.PICSGetAccessTokens(2868840, null);
            if (tokenResult.AppTokens != null && tokenResult.AppTokens.TryGetValue(2868840, out var token))
                _connection.AppAccessToken = token;
            else if (tokenResult.AppTokensDenied != null && tokenResult.AppTokensDenied.Contains(2868840))
                throw new InvalidOperationException(
                    "Steam denied app access token; ownership/session may be invalid"
                );

            ConnectionResolved = true;
            OfflineMode = false;
            SetState(SessionState.LoggedIn);
        }
        catch (Exception ex)
        {
            SetState(SessionState.Failed, $"Connection failed: {ex.Message}");
        }
    }

    public async Task StartDownloadAsync()
    {
        if (Interlocked.Exchange(ref _downloadRunning, 1) == 1)
        {
            InvokeDownloadFailed("Download already running");
            return;
        }

        try
        {
            await EnsureConnectedAsync();
            if (_state != SessionState.LoggedIn || _connection == null)
            {
                InvokeDownloadFailed(null);
                return;
            }

            _downloader?.Dispose();
            _downloadCts?.Dispose();
            _downloader = new DepotDownloader(_connection, _dataDir);
            _downloader.LogMessage += InvokeDownloadLogReceived;
            _downloader.ProgressChanged += InvokeDownloadProgressChanged;

            _downloadCts = new CancellationTokenSource();

            try
            {
                await _downloader.DownloadAsync(_downloadCts.Token).ConfigureAwait(false);
                InvokeDownloadCompleted();
            }
            catch (OperationCanceledException)
            {
                InvokeDownloadCancelled();
            }
            catch (Exception ex)
            {
                InvokeDownloadFailed(ex.Message);
                PatchHelper.Log($"[Launcher] Download error: {ex}");
            }
            finally
            {
                _downloader?.Dispose();
                _downloader = null;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _downloadRunning, 0);
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            await EnsureConnectedAsync();
            if (_state != SessionState.LoggedIn || _connection == null)
            {
                UpdateCheckFailed?.Invoke("Not connected");
                return;
            }

            using var downloader = new DepotDownloader(_connection, _dataDir);
            downloader.LogMessage += InvokeDownloadLogReceived;

            bool hasUpdate = await downloader.CheckForUpdatesAsync().ConfigureAwait(false);

            InvokeUpdateCheckCompleted(hasUpdate);
        }
        catch (Exception ex)
        {
            InvokeUpdateCheckFailed(ex.Message);
        }
    }

    public FastPathResult Retry()
    {
        var downloadActive = Interlocked.CompareExchange(ref _downloadRunning, 0, 0) == 1;

        try
        {
            _downloadCts?.Cancel();
        }
        catch (ObjectDisposedException) { }

        if (downloadActive)
        {
            PatchHelper.Log("[Launcher] Retry requested while download is active; cancellation requested");
        }
        else
        {
            _downloader?.Dispose();
            _downloadCts?.Dispose();
            _downloadCts = null;
            _downloader = null;
            _connection?.Dispose();
            _connection = null;
        }

        _auth?.Dispose();
        _auth = null;
        return StartSession();
    }

    public void Launch()
    {
        if (_credentialStore.HasCredentials)
        {
            LauncherPatches.SavedAccountName = _credentialStore.AccountName;
            LauncherPatches.SavedRefreshToken = _credentialStore.RefreshToken;
        }

        if (_launchTcs != null)
            _launchTcs.TrySetResult(true);
        else
        {
            PatchHelper.Log("[Launcher] Restarting app to launch game files");
            var godotApp = GetGodotApp();
            if (GameFilesReady())
                godotApp?.Call("launchGameOnRestart");
            else
                godotApp?.Call("restartApp");
        }
    }

    public void LaunchSafe()
    {
        SaveManualSafeLaunchMarker();

        if (_credentialStore.HasCredentials)
        {
            LauncherPatches.SavedAccountName = _credentialStore.AccountName;
            LauncherPatches.SavedRefreshToken = _credentialStore.RefreshToken;
        }

        if (OperatingSystem.IsAndroid() && GameFilesReady())
        {
            PatchHelper.Log("[Launcher] Restarting app for safe game launch");
            GetGodotApp()?.Call("launchGameSafelyOnRestart");
            return;
        }

        if (_launchTcs != null)
            _launchTcs.TrySetResult(true);
        else
        {
            PatchHelper.Log("[Launcher] Restarting app for safe game launch");
            var godotApp = GetGodotApp();
            if (GameFilesReady())
                godotApp?.Call("launchGameSafelyOnRestart");
            else
                godotApp?.Call("restartApp");
        }
    }

    public void ResetGameFilesForRedownload()
    {
        _downloadCts?.Cancel();
        _downloader?.Dispose();
        _downloader = null;
        _downloadCts?.Dispose();
        _downloadCts = null;

        DeleteDirectoryQuietly(Path.Combine(_dataDir, "game"));
        DeleteDirectoryQuietly(Path.Combine(_dataDir, "download_state"));
        DeleteFileQuietly(Path.Combine(_dataDir, "last_game_start_incomplete"));
        PatchHelper.Log("[Launcher] Deleted downloaded game files and download state");
    }

    public string WriteDiagnosticsReport()
    {
        var report = BuildDiagnosticsReport();
        var fileName = $"sts2-launcher-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var externalPath = TryGetExternalDiagnosticsPath(fileName);
        var targetPath = externalPath ?? Path.Combine(_dataDir, fileName);

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        File.WriteAllText(targetPath, report);
        PatchHelper.Log($"[Launcher] Diagnostics written to {targetPath}");
        return targetPath;
    }

    private string BuildDiagnosticsReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("STS2 Launcher diagnostics");
        sb.AppendLine($"Generated UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Data dir: {_dataDir}");
        sb.AppendLine($"Account: {_credentialStore.AccountName ?? "<none>"}");
        sb.AppendLine($"Has saved credentials: {_credentialStore.HasCredentials}");
        sb.AppendLine($"Session state: {_state}");
        sb.AppendLine($"Fail reason: {_failReason ?? "<none>"}");
        sb.AppendLine($"Game files ready: {GameFilesReady()}");
        sb.AppendLine($"Cloud sync pref: {LoadCloudSyncPref()}");
        sb.AppendLine($"Local backup pref: {LoadLocalBackupPref()}");

        if (PreviousGameLaunchIncomplete(out var phase))
            sb.AppendLine($"Previous launch incomplete phase: {phase ?? "<unknown>"}");
        else
            sb.AppendLine("Previous launch incomplete phase: <none>");

        AppendFileInfo(sb, "Startup marker", Path.Combine(_dataDir, "last_game_start_incomplete"));
        AppendFileInfo(sb, "Manual safe launch marker", ManualSafeLaunchPath);
        AppendFileInfo(sb, "Game PCK", Path.Combine(_dataDir, "game", "SlayTheSpire2.pck"));
        AppendDirectoryListing(sb, "Game directory", Path.Combine(_dataDir, "game"), maxDepth: 2);
        AppendDirectoryListing(sb, "Download state", Path.Combine(_dataDir, "download_state"), maxDepth: 1);
        AppendDirectoryListing(sb, "Mono publish root", Path.Combine(_dataDir, ".godot", "mono", "publish"), maxDepth: 2);

        try
        {
            var godotApp = GetGodotApp();
            sb.AppendLine($"Android app version: {(string)godotApp?.Call("getVersionName") ?? "<unknown>"}");
            sb.AppendLine($"External files dir: {(string)godotApp?.Call("getExternalFilesDirPath") ?? "<none>"}");
            sb.AppendLine($"Usable data bytes: {(long)(godotApp?.Call("getUsableSpaceBytes", _dataDir) ?? -1L)}");
            sb.AppendLine();
            sb.AppendLine("Android logcat tail:");
            sb.AppendLine((string)godotApp?.Call("getLogcatTail", 500) ?? "<unavailable>");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Android bridge diagnostics failed: {ex.Message}");
        }

        return sb.ToString();
    }

    private static string TryGetExternalDiagnosticsPath(string fileName)
    {
        try
        {
            var godotApp = GetGodotApp();
            var externalDir = (string)godotApp?.Call("getExternalFilesDirPath");
            if (string.IsNullOrWhiteSpace(externalDir))
                return null;

            return Path.Combine(externalDir, "diagnostics", fileName);
        }
        catch
        {
            return null;
        }
    }

    private static void AppendFileInfo(StringBuilder sb, string label, string path)
    {
        try
        {
            var file = new FileInfo(path);
            sb.AppendLine($"{label}: {path}");
            sb.AppendLine($"  exists={file.Exists}");
            if (file.Exists)
            {
                sb.AppendLine($"  bytes={file.Length}");
                sb.AppendLine($"  modifiedUtc={file.LastWriteTimeUtc:O}");
                if (file.Length <= 4096)
                    sb.AppendLine($"  contents={File.ReadAllText(path).Replace('\n', ' ').Replace('\r', ' ')}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{label}: failed to inspect {path}: {ex.Message}");
        }
    }

    private static void AppendDirectoryListing(StringBuilder sb, string label, string path, int maxDepth)
    {
        sb.AppendLine($"{label}: {path}");
        try
        {
            if (!Directory.Exists(path))
            {
                sb.AppendLine("  exists=false");
                return;
            }

            AppendDirectoryListingRecursive(sb, path, depth: 0, maxDepth);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  failed={ex.Message}");
        }
    }

    private static void AppendDirectoryListingRecursive(
        StringBuilder sb,
        string path,
        int depth,
        int maxDepth
    )
    {
        if (depth > maxDepth)
            return;

        var indent = new string(' ', 2 + depth * 2);
        foreach (var dir in Directory.GetDirectories(path).OrderBy(p => p).Take(40))
        {
            sb.AppendLine($"{indent}[dir] {Path.GetFileName(dir)}");
            AppendDirectoryListingRecursive(sb, dir, depth + 1, maxDepth);
        }

        foreach (var filePath in Directory.GetFiles(path).OrderBy(p => p).Take(80))
        {
            var file = new FileInfo(filePath);
            sb.AppendLine($"{indent}{file.Name} bytes={file.Length} modifiedUtc={file.LastWriteTimeUtc:O}");
        }
    }

    private static void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete directory {path}: {ex.Message}");
        }
    }

    private static void DeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete file {path}: {ex.Message}");
        }
    }

    public bool HasOwnershipMarker() => CreateOwnershipVerifier()?.HasMarker() ?? false;

    public void Dispose()
    {
        try
        {
            _downloadCts?.Cancel();
        }
        catch (ObjectDisposedException) { }
        _downloader?.Dispose();
        _downloadCts?.Dispose();
        _auth?.Dispose();
        if (_launchTcs == null)
            _connection?.Dispose();
    }

    private async Task VerifyOwnershipAsync()
    {
        SetState(SessionState.VerifyingOwnership);

        var verifier = CreateOwnershipVerifier();
        bool owns = await verifier.VerifyAsync(_connection);

        if (owns)
        {
            PatchHelper.Log("[Launcher] Ownership verified");
            ConnectionResolved = true;
            SetState(SessionState.LoggedIn);
        }
        else
        {
            PatchHelper.Log("[Launcher] Ownership denied");
            SetState(
                SessionState.Failed,
                "You don't own Slay the Spire 2. Purchase on Steam to play."
            );
        }
    }

    private OwnershipVerifier CreateOwnershipVerifier()
    {
        var account = _credentialStore.AccountName;
        return account != null ? new OwnershipVerifier(_dataDir, account) : null;
    }

    private void SetState(SessionState state, string failReason = null)
    {
        _state = state;
        _failReason = failReason;
        try
        {
            SessionStateChanged?.Invoke(state);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] SessionStateChanged callback failed: {ex.Message}");
        }
    }

    private void InvokeDownloadCompleted()
    {
        try
        {
            DownloadCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] DownloadCompleted callback failed: {ex.Message}");
        }
    }

    private void InvokeDownloadCancelled()
    {
        try
        {
            DownloadCancelled?.Invoke();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] DownloadCancelled callback failed: {ex.Message}");
        }
    }

    private void InvokeDownloadFailed(string message)
    {
        try
        {
            DownloadFailed?.Invoke(message);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] DownloadFailed callback failed: {ex.Message}");
        }
    }

    private void InvokeDownloadLogReceived(string message)
    {
        try
        {
            DownloadLogReceived?.Invoke(message);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] DownloadLogReceived callback failed: {ex.Message}");
        }
    }

    private void InvokeDownloadProgressChanged(DownloadProgress progress)
    {
        try
        {
            DownloadProgressChanged?.Invoke(progress);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] DownloadProgressChanged callback failed: {ex.Message}");
        }
    }

    private void InvokeUpdateCheckCompleted(bool hasUpdate)
    {
        try
        {
            UpdateCheckCompleted?.Invoke(hasUpdate);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] UpdateCheckCompleted callback failed: {ex.Message}");
        }
    }

    private void InvokeUpdateCheckFailed(string message)
    {
        try
        {
            UpdateCheckFailed?.Invoke(message);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] UpdateCheckFailed callback failed: {ex.Message}");
        }
    }

    public static bool GameFilesReady()
    {
        var pckPath = Path.Combine(OS.GetDataDir(), "game", "SlayTheSpire2.pck");
        try
        {
            using var fs = File.OpenRead(pckPath);
            using var reader = new BinaryReader(fs);
            if (fs.Length < 96)
                return false;

            if (reader.ReadUInt32() != 0x43504447)
                return false;

            reader.ReadUInt32(); // format version
            reader.ReadUInt32(); // major
            reader.ReadUInt32(); // minor
            reader.ReadUInt32(); // patch
            reader.ReadUInt32(); // flags
            reader.ReadInt64(); // file base
            var dirBase = reader.ReadInt64();
            if (dirBase <= 0 || dirBase + 4 > fs.Length)
                return false;

            fs.Position = dirBase;
            return reader.ReadUInt32() > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool PreviousGameLaunchIncomplete(out string phase)
    {
        phase = null;
        var markerPath = Path.Combine(OS.GetDataDir(), "last_game_start_incomplete");
        try
        {
            if (!File.Exists(markerPath))
                return false;

            var lines = File.ReadAllLines(markerPath);
            phase = lines.Length >= 2 ? lines[1].Trim() : null;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    private static string LocalBackupPrefPath =>
        Path.Combine(OS.GetDataDir(), "local_backup_enabled");

    public static bool LoadLocalBackupPref()
    {
        try
        {
            if (File.Exists(LocalBackupPrefPath))
                return File.ReadAllText(LocalBackupPrefPath).Trim() == "true";
        }
        catch { }
        return false;
    }

    public static void SaveLocalBackupPref(bool enabled)
    {
        try
        {
            File.WriteAllText(LocalBackupPrefPath, enabled ? "true" : "false");
        }
        catch { }
    }

    private static string CloudSyncPrefPath => Path.Combine(OS.GetDataDir(), "cloud_sync_enabled");

    public static bool LoadCloudSyncPref()
    {
        try
        {
            if (File.Exists(CloudSyncPrefPath))
                return File.ReadAllText(CloudSyncPrefPath).Trim() == "true";
        }
        catch { }
        return true;
    }

    public static void SaveCloudSyncPref(bool enabled)
    {
        try
        {
            File.WriteAllText(CloudSyncPrefPath, enabled ? "true" : "false");
        }
        catch { }
    }

    public static string ManualSafeLaunchPath =>
        Path.Combine(OS.GetDataDir(), "manual_safe_launch");

    private static void SaveManualSafeLaunchMarker()
    {
        try
        {
            File.WriteAllText(ManualSafeLaunchPath, $"{DateTime.UtcNow:O}\n");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual safe launch marker: {ex.Message}");
        }
    }

    public static GodotObject GetGodotApp()
    {
        try
        {
            var jcw = Engine.GetSingleton("JavaClassWrapper");
            var wrapper = (GodotObject)jcw.Call("wrap", "com.game.sts2launcher.GodotApp");
            return (GodotObject)wrapper.Call("getInstance");
        }
        catch
        {
            return null;
        }
    }
}
