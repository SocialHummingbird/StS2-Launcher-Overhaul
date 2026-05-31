using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Orchestrates launcher session state and delegates concrete workflows to focused helpers.
// Events fire from background threads; the controller marshals them to the main thread.
internal partial class LauncherModel : IDisposable
{
    // Represents the current stage of the launcher's Steam connection and
    // authentication flow. Drives the launcher UI state machine.
    internal enum SessionState
    {
        Disconnected,
        Connecting,
        Authenticating,
        VerifyingOwnership,
        LoggedIn,
        Failed,
    }

    internal enum FastPathResult
    {
        ShowLogin,
        AutoConnect,
        ReadyToLaunch,
    }

    private volatile bool _connectionResolved;
    private readonly string _dataDir;
    private readonly SteamCredentialStore _credentialStore;
    private readonly LauncherSteamSession _steamSession;
    private CancellationTokenSource _downloadCts;
    private DepotDownloader _downloader;
    private int _downloadRunning;
    private string _failReason;
    private bool _inGameMode;
    private TaskCompletionSource<bool> _launchTcs;
    private SessionState _sessionState = SessionState.Disconnected;

    internal bool ConnectionResolved => _connectionResolved;
    internal bool AwaitingCode => _steamSession.AwaitingCode;

    // True when launched from GameStartupWrapper (game files present). False in
    // standalone launcher mode where a restart is needed after downloading files.
    // Setting this to true eagerly creates the launch TCS so it exists before the
    // UI is shown (preventing a race between PLAY button and WaitForLaunch).
    internal bool InGameMode
    {
        get => _inGameMode;
        set
        {
            _inGameMode = value;
            if (value && _launchTcs == null)
                _launchTcs = new TaskCompletionSource<bool>();
        }
    }
    internal string AccountName => _credentialStore.AccountName;
    internal string FailReason => _failReason;
    internal SessionState SessionState => _sessionState;

    internal event Action<SessionState> SessionStateChanged;
    internal event Action<string> LogReceived;
    internal event Action<bool> CodeNeeded;
    internal event Action<DepotDownloader.DownloadProgress> DownloadProgressChanged;
    internal event Action<string> DownloadLogReceived;
    internal event Action DownloadCompleted;
    internal event Action<string> DownloadFailed;
    internal event Action DownloadCancelled;
    internal event Action<bool> UpdateCheckCompleted;
    internal event Action<string> UpdateCheckFailed;

    internal LauncherModel(string dataDir)
    {
        _dataDir = dataDir;
        _credentialStore = new SteamCredentialStore(dataDir);
        _steamSession = new LauncherSteamSession(dataDir, _credentialStore);
    }

    // Loads saved credentials and determines the launcher path.
    internal FastPathResult StartSession()
    {
        _connectionResolved = false;
        return ResolveFastPath();
    }

    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    internal async void Connect()
    {
        SetSessionState(SessionState.Connecting);

        var error = await _steamSession.ConnectSavedCredentialsAndVerifyAsync(
            BeginOwnershipVerification
        );
        CompleteOwnershipCheck(error);
    }

    // Performs interactive login, then verifies ownership.
    internal async Task LoginAsync(string username, string password)
    {
        SetSessionState(SessionState.Authenticating);

        var error = await _steamSession.LoginAndVerifyAsync(
            username,
            password,
            RaiseLogReceived,
            RaiseCodeNeeded,
            BeginOwnershipVerification
        );
        CompleteOwnershipCheck(error);
    }

    internal void SubmitCode(string code) => _steamSession.SubmitCode(code);

    internal void MarkConnectionResolved()
    {
        _connectionResolved = true;
    }

    // Creates or reuses a SteamConnection for depot operations.
    internal async Task EnsureConnectedAsync()
    {
        if (IsLoggedIn && _steamSession.Connection != null)
            return;

        SetSessionState(SessionState.Connecting);

        var error = await _steamSession.EnsureConnectedAsync();
        if (error == null)
        {
            _connectionResolved = true;
            SetSessionState(SessionState.LoggedIn);
            return;
        }

        _connectionResolved = false;
        SetSessionState(SessionState.Failed, error);
    }

    internal bool HasOwnershipMarker()
        => HasOwnershipMarker(_dataDir, _credentialStore.AccountName);

    internal async Task StartDownloadAsync()
    {
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            RaiseDownloadFailed(null);
            return;
        }

        if (Interlocked.Exchange(ref _downloadRunning, 1) != 0)
        {
            RaiseDownloadFailed("Download already running");
            return;
        }

        try
        {
            BeginDownload(connection);
            await RunDownloadAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _downloadRunning, 0);
        }
    }

    internal async Task CheckForUpdatesAsync()
    {
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            RaiseUpdateCheckFailed("Not connected");
            return;
        }

        try
        {
            using var downloader = new DepotDownloader(connection, _dataDir);
            downloader.LogMessage += RaiseDownloadLogReceived;
            bool hasUpdate = await downloader.CheckForUpdatesAsync().ConfigureAwait(false);
            RaiseUpdateCheckCompleted(hasUpdate);
        }
        catch (Exception ex)
        {
            RaiseUpdateCheckFailed(ex.Message);
        }
    }

    internal FastPathResult Retry()
    {
        var downloadActive = DownloadIsRunning;
        CancelDownloadForRetry();
        _steamSession.Retry(downloadActive);
        return StartSession();
    }

    internal void ResetGameFilesForRedownload()
    {
        CancelDownloadForRetry();
        ResetDownload();
        LauncherGameFiles.DeleteDownloadedState(_dataDir);
    }

    internal Task WaitForLaunch()
    {
        _launchTcs ??= new TaskCompletionSource<bool>();
        return _launchTcs.Task;
    }

    internal void Launch()
    {
        LauncherCloudSaveState.SaveCredentials(
            _credentialStore.AccountName,
            _credentialStore.RefreshToken
        );

        if (TrySignalInProcessLaunch())
            return;

        RestartForLaunch(safe: false);
    }

    internal void LaunchSafe()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        LauncherCloudSaveState.SaveCredentials(
            _credentialStore.AccountName,
            _credentialStore.RefreshToken
        );

        if (TrySafeAndroidRestart())
            return;

        if (TrySignalInProcessLaunch())
            return;

        RestartForLaunch(safe: true);
    }

    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        CancelDownload();
        ResetDownload();
        _steamSession.Dispose(preserveLaunchConnection: _launchTcs != null);
    }

    private async Task<SteamConnection> GetDepotConnectionAsync()
    {
        await EnsureConnectedAsync();
        return IsLoggedIn ? _steamSession.Connection : null;
    }

    private void BeginOwnershipVerification()
    {
        SetSessionState(SessionState.VerifyingOwnership);
    }

    private void CompleteOwnershipCheck(string error)
    {
        if (error == null)
        {
            _connectionResolved = true;
            SetSessionState(SessionState.LoggedIn);
            return;
        }

        _connectionResolved = false;
        SetSessionState(SessionState.Failed, error);
    }

    private void SetSessionState(SessionState state, string failReason = null)
    {
        _sessionState = state;
        _failReason = failReason;
        RaiseSessionStateChanged(state);
    }

    private bool IsLoggedIn => _sessionState == SessionState.LoggedIn;

    private bool DownloadIsRunning => Interlocked.CompareExchange(ref _downloadRunning, 0, 0) == 1;

    private async Task RunDownloadAsync()
    {
        try
        {
            await _downloader.DownloadAsync(_downloadCts.Token).ConfigureAwait(false);
            RaiseDownloadCompleted();
        }
        catch (OperationCanceledException)
        {
            RaiseDownloadCancelled();
        }
        catch (Exception ex)
        {
            RaiseDownloadFailed(ex.Message);
            PatchHelper.Log($"[Launcher] Download error: {ex}");
        }
        finally
        {
            ResetDownload();
        }
    }

    private void BeginDownload(SteamConnection connection)
    {
        ResetDownload();
        _downloader = new DepotDownloader(connection, _dataDir);
        _downloader.LogMessage += RaiseDownloadLogReceived;
        _downloader.ProgressChanged += RaiseDownloadProgressChanged;
        _downloadCts = new CancellationTokenSource();
    }

    private void CancelDownloadForRetry()
    {
        CancelDownload();

        if (DownloadIsRunning)
            PatchHelper.Log(
                "[Launcher] Retry requested while download is active; cancellation requested"
            );
        else
            ResetDownload();
    }

    private void CancelDownload()
    {
        try
        {
            _downloadCts?.Cancel();
        }
        catch (ObjectDisposedException) { }
    }

    private void ResetDownload()
    {
        _downloader?.Dispose();
        _downloadCts?.Dispose();
        _downloader = null;
        _downloadCts = null;
    }

    private bool TrySignalInProcessLaunch()
    {
        if (_launchTcs == null)
            return false;

        _launchTcs.TrySetResult(true);
        return true;
    }

    private bool TrySafeAndroidRestart()
    {
        if (!OperatingSystem.IsAndroid() || !LauncherGameFiles.Ready(_dataDir))
            return false;

        PatchHelper.Log(RestartMessage(safe: true));
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
        return true;
    }

    private void RestartForLaunch(bool safe)
    {
        PatchHelper.Log(RestartMessage(safe));

        if (!LauncherGameFiles.Ready(_dataDir))
        {
            AndroidGodotAppBridge.RestartApp();
            return;
        }

        if (safe)
            AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
        else
            AndroidGodotAppBridge.LaunchGameOnRestart();
    }

    private static string RestartMessage(bool safe)
        => safe
            ? "[Launcher] Restarting app for safe game launch"
            : "[Launcher] Restarting app to launch game files";

    private void RaiseSessionStateChanged(SessionState state)
        => Raise(SessionStateChanged, state, nameof(SessionStateChanged));

    private void RaiseLogReceived(string message)
        => Raise(LogReceived, message, nameof(LogReceived));

    private void RaiseCodeNeeded(bool wasIncorrect)
        => Raise(CodeNeeded, wasIncorrect, nameof(CodeNeeded));

    private void RaiseDownloadCompleted()
        => Raise(DownloadCompleted, nameof(DownloadCompleted));

    private void RaiseDownloadCancelled()
        => Raise(DownloadCancelled, nameof(DownloadCancelled));

    private void RaiseDownloadFailed(string message)
        => Raise(DownloadFailed, message, nameof(DownloadFailed));

    private void RaiseDownloadLogReceived(string message)
        => Raise(DownloadLogReceived, message, nameof(DownloadLogReceived));

    private void RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress progress)
        => Raise(DownloadProgressChanged, progress, nameof(DownloadProgressChanged));

    private void RaiseUpdateCheckCompleted(bool hasUpdate)
        => Raise(UpdateCheckCompleted, hasUpdate, nameof(UpdateCheckCompleted));

    private void RaiseUpdateCheckFailed(string message)
        => Raise(UpdateCheckFailed, message, nameof(UpdateCheckFailed));

    private static void Raise(Action callback, string name)
    {
        try
        {
            callback?.Invoke();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] {name} callback failed: {ex.Message}");
        }
    }

    private static void Raise<T>(Action<T> callback, T value, string name)
    {
        try
        {
            callback?.Invoke(value);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] {name} callback failed: {ex.Message}");
        }
    }

    private FastPathResult ResolveFastPath()
    {
        _credentialStore.Load();
        if (_credentialStore.HasCredentials)
            LauncherCloudSaveState.SaveCredentials(
                _credentialStore.AccountName,
                _credentialStore.RefreshToken
            );

        var hasCredentials = _credentialStore.HasCredentials;
        var hasOwnershipMarker = HasOwnershipMarker(_dataDir, _credentialStore.AccountName);
        var gameFilesReady = LauncherGameFiles.Ready(_dataDir);
        PatchHelper.Log(
            $"[Launcher] Fast path: creds={hasCredentials}, marker={hasOwnershipMarker}"
        );

        if (hasCredentials && hasOwnershipMarker && gameFilesReady)
            return FastPathResult.ReadyToLaunch;

        if (hasCredentials)
            return FastPathResult.AutoConnect;

        return FastPathResult.ShowLogin;
    }

    private static bool HasOwnershipMarker(string dataDir, string accountName)
        => LauncherSteamSession.HasOwnershipMarker(dataDir, accountName);
}
