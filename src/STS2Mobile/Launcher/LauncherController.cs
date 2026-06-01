using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using SteamKit2;
using STS2Mobile;
using STS2Mobile.Patches;
using STS2Mobile.Steam;
using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

// Wires model events to view updates and handles the launcher UI state machine.
// All model callbacks are marshalled to the main thread before updating the view.
internal sealed class LauncherController
{
    private static readonly Color AppUpdateLogColor = new(1f, 0.85f, 0.2f);

    private const int CloudSyncOperationTimeoutMs = 180_000;
    private const string FirstLaunchConnectionFailed =
        "Connection failed. Internet required for first launch.";
    private const int LocalLoginPollDelayMs = 500;
    private const string OfflineLaunchAllowed =
        "Connection timed out. Valid ownership marker found.";
    private const string SavedCredentialsFallback =
        "No connection - saved credentials will be used";
    private const int SteamConnectionTimeoutMs = 10_000;

    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly Action<Action> _runOnMainThread;
    private bool _automaticDiagnosticsWritten;
    private volatile bool _localLoginHandoffStarted;
    private bool _updateCheckRunning;

    internal LauncherController(
        LauncherModel model,
        LauncherView view,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _runOnMainThread = runOnMainThread;
    }

    internal void Start()
    {
        _model.SessionStateChanged += state =>
            _runOnMainThread(() => UpdateUI(state));
        _model.LogReceived += message =>
            _runOnMainThread(() => _view.AppendLog(message));
        PatchHelper.LogEmitted += message =>
        {
            if (message.StartsWith("[Cloud]"))
                _runOnMainThread(() => _view.AppendLog(message));
        };
        _model.CodeNeeded += wasIncorrect =>
            _runOnMainThread(() => ShowCodePrompt(wasIncorrect));
        _model.DownloadProgressChanged += progress =>
            _runOnMainThread(() => UpdateDownloadProgress(progress));
        _model.DownloadLogReceived += message =>
            _runOnMainThread(() => _view.AppendLog(message));
        _model.DownloadCompleted += () =>
            _runOnMainThread(CompleteDownload);
        _model.DownloadFailed += message =>
            _runOnMainThread(() => FailDownload(message));
        _model.DownloadCancelled += () =>
            _runOnMainThread(CancelDownload);
        _model.UpdateCheckCompleted += hasUpdate =>
            _runOnMainThread(() => CompleteUpdateCheck(hasUpdate));
        _model.UpdateCheckFailed += message =>
            _runOnMainThread(() => FailUpdateCheck(message));

        _view.Login.LoginRequested += LoginPressed;
        _view.Code.CodeSubmitted += CodeSubmitPressed;
        _view.Download.DownloadRequested += DownloadPressed;
        _view.Actions.LaunchPressed += LaunchPressed;
        _view.Actions.RetryPressed += RetryPressed;
        _view.Actions.LocalBackupToggled += LocalBackupToggled;
        _view.Actions.CloudSyncToggled += CloudSyncToggled;
        _view.Actions.CloudPushPressed += CloudPushPressed;
        _view.Actions.CloudPullPressed += CloudPullPressed;
        _view.Actions.CheckForUpdatesPressed += RunUpdateCheck;
        _view.Actions.RedownloadPressed += RedownloadPressed;
        _view.Actions.DiagnosticsPressed += DiagnosticsPressed;
        _view.Actions.ShowLastErrorPressed += ShowLastErrorPressed;
        _view.Actions.CopyRawLogPressed += CopyRawLogPressed;
        _view.Actions.SafeLaunchPressed += SafeLaunchPressed;

        _view.Actions.SetLocalBackupChecked(LauncherPreferences.LoadAndApplyLocalBackupEnabled());
        _view.Actions.SetCloudSyncChecked(LauncherPreferences.LoadAndApplyCloudSyncEnabled());

        var result = _model.StartSession();
        HandleFastPath(result);
        StartLocalLoginHandoff();
    }

    // Updates visible sections and status text based on session state transitions.
    private void UpdateUI(SessionState state)
    {
        if (ShouldSuppressSessionUpdate(state))
            return;

        ShowSessionState(state);
    }

    private async void LoginPressed(string username, string password)
        => await LoginAsync(username, password);

    private void CodeSubmitPressed(string code)
    {
        _view.SetStatus("Verifying code...");
        _model.SubmitCode(code);
    }

    private async void DownloadPressed()
        => await DownloadAsync();

    private void LocalBackupToggled(bool pressed)
        => LauncherPreferences.SaveLocalBackupEnabled(pressed);

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(CloudSyncRequest.Push);

    private void CloudPullPressed()
        => RequestCloudSync(CloudSyncRequest.Pull);

    private void RetryPressed()
    {
        var result = _model.Retry();
        HandleFastPath(result);
    }

    private void RedownloadPressed()
    {
        _view.ShowConfirmation(
            "Redownload game files?\nThis keeps your Steam login but deletes downloaded game files.",
            () =>
            {
                _model.ResetGameFilesForRedownload();
                _view.Actions.HideAll();
                _view.Download.Visible = true;
                _view.Download.Reset("DOWNLOAD GAME FILES");
                _view.SetStatus("Game files deleted. Download again to rebuild them.");
                _view.AppendLog("Game files were deleted for a clean redownload.");
            }
        );
    }

    private void DiagnosticsPressed()
    {
        try
        {
            var path = _model.WriteDiagnosticsReport();
            _view.SetStatus("Diagnostics exported.");
            _view.AppendLog($"Diagnostics exported: {path}");
            if (OperatingSystem.IsAndroid())
            {
                var shared = AndroidGodotAppBridge.ShareTextFile(path);
                _view.AppendLog(
                    shared ? "Android share sheet opened." : "Could not open Android share sheet."
                );
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Diagnostics export failed: {ex}");
            _view.SetStatus($"Diagnostics export failed: {ex.Message}");
        }
    }

    private void ShowLastErrorPressed()
    {
        try
        {
            var summary = _model.BuildDiagnosticsSummaryForDisplay();
            _view.SetStatus("Last error summary printed in console.");
            _view.AppendLog(summary);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Error summary failed: {ex}");
            _view.SetStatus($"Error summary failed: {ex.Message}");
        }
    }

    private void CopyRawLogPressed()
    {
        try
        {
            var rawLog = _model.BuildRawErrorLogForClipboard();
            DisplayServer.ClipboardSet(rawLog);
            _view.SetStatus("Raw error log copied.");
            _view.AppendLog($"Raw error log copied to clipboard ({rawLog.Length:N0} chars).");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Raw error log copy failed: {ex}");
            _view.SetStatus($"Raw error log copy failed: {ex.Message}");
        }
    }

    private void LaunchPressed()
        => _model.Launch();

    private void SafeLaunchPressed()
    {
        _view.AppendLog(
            "Safe launch requested: default renderer, no shader warmup, local saves only for one run."
        );
        _model.LaunchSafe();
    }

    private async Task LoginAsync(string username, string password)
    {
        try
        {
            _view.Login.SetDisabled(true);
            _view.Login.ClearPassword();
            await _model.LoginAsync(username, password);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Login handler failed: {ex.Message}");
            _view.SetStatus($"Login failed: {ex.Message}");
            _view.Login.Visible = true;
            _view.Login.SetDisabled(false);
        }
    }

    private async Task DownloadAsync()
    {
        _view.Download.ShowProgress("Connecting to Steam...");
        await _model.StartDownloadAsync();
    }

    private void RequestCloudSync(CloudSyncRequest request)
    {
        _view.ShowConfirmation(
            request.ConfirmationMessage,
            () => ExecuteCloudSync(request)
        );
    }

    private async void ExecuteCloudSync(CloudSyncRequest request)
        => await ExecuteCloudSyncAsync(request);

    private async Task ExecuteCloudSyncAsync(CloudSyncRequest request)
    {
        _runOnMainThread(() =>
        {
            _view.Actions.SetPushPullDisabled(true);
            _view.AppendLog(request.StartMessage);
        });

        try
        {
            var timeout = Task.Delay(CloudSyncOperationTimeoutMs);
            if (!LauncherCloudSaveState.TryGetSavedCredentials(
                    out var accountName,
                    out var refreshToken
                ))
                throw new InvalidOperationException("No saved Steam credentials");

            var operationTask = request.Run(accountName, refreshToken);
            if (await Task.WhenAny(operationTask, timeout) != operationTask)
                throw new TimeoutException(
                    $"{request.OperationName} timed out after {CloudSyncOperationTimeoutMs}ms"
                );

            await operationTask;
            _runOnMainThread(() =>
            {
                _view.AppendLog(request.CompleteMessage);
                _view.Actions.SetPushPullDisabled(false);
            });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] {request.OperationName} sync failed: {ex.Message}");
            _runOnMainThread(() =>
            {
                _view.AppendLog($"{request.OperationName} failed: {ex.Message}");
                _view.Actions.SetPushPullDisabled(false);
            });
        }
    }

    private async void RunUpdateCheck()
    {
        _updateCheckRunning = true;
        StartUpdateCheckPresentation();

        try
        {
            await CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Check for updates failed: {ex}");
            FailUpdateCheckPresentation(ex);
        }
        finally
        {
            _updateCheckRunning = false;
            FinishUpdateCheckPresentation();
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        // Check for launcher (APK) updates from GitHub in parallel with game file updates.
        var appUpdateTask = CheckForAppUpdatesAsync();
        await _model.CheckForUpdatesAsync();
        await appUpdateTask;
    }

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var latestVersion = await AppUpdateChecker.CheckAsync();
            if (latestVersion == null)
            {
                _runOnMainThread(() => _view.AppendLog("Launcher is up to date"));
                return;
            }

            _runOnMainThread(() =>
            {
                _view.AppendColoredLog(
                    $"Launcher update available: v{latestVersion} - "
                        + $"download at {AppUpdateChecker.RepoReleasesPage}",
                    AppUpdateLogColor
                );
                _view.SetStatus(
                    $"Launcher update available! Visit GitHub to download v{latestVersion}"
                );
            });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] App update check failed: {ex.Message}");
        }
    }

    private void StartUpdateCheckPresentation()
    {
        _view.Actions.SetUpdateButtonDisabled(true);
        _view.Actions.SetUpdateButtonText("Checking...");
    }

    private void FailUpdateCheckPresentation(Exception ex)
    {
        _view.AppendLog($"Update check failed: {ex.Message}");
        _view.Actions.SetUpdateButtonText("CHECK FAILED");
    }

    private void FinishUpdateCheckPresentation()
    {
        _view.Actions.SetUpdateButtonDisabled(false);
    }

    private void StartLocalLoginHandoff()
    {
        if (_localLoginHandoffStarted || !OperatingSystem.IsAndroid())
            return;

        _localLoginHandoffStarted = true;
        _ = Task.Run(WatchLocalLoginHandoffAsync);
    }

    private async Task WatchLocalLoginHandoffAsync()
    {
        while (!_model.ConnectionResolved)
        {
            var credentials = LocalSteamCredentialInbox.TryConsume();
            if (credentials != null)
            {
                PatchHelper.Log("[Launcher] Consumed local Steam credential file");
                _runOnMainThread(ShowAuthenticating);

                await _model.LoginAsync(credentials.Value.Username, credentials.Value.Password);
                return;
            }

            await Task.Delay(LocalLoginPollDelayMs);
        }
    }

    private void ShowAuthenticating()
    {
        _view.Login.Visible = false;
        _view.Login.SetDisabled(true);
        _view.SetStatus("Authenticating...");
    }

    private void ShowCodePrompt(bool wasIncorrect)
    {
        _view.Login.Visible = false;
        _view.Code.Show(wasIncorrect);
    }

    private void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
    {
        _view.Download.SetProgress(
            progress.Percentage,
            $"{LauncherGameFiles.FormatSize(progress.DownloadedBytes)} / {LauncherGameFiles.FormatSize(progress.TotalBytes)} ({progress.Percentage:F1}%)"
        );
        _view.AppendLog(progress.CurrentFile);
    }

    private void CompleteDownload()
    {
        _view.SetStatus("Download complete! Start game when ready.");
        _view.Download.Visible = false;
        if (LauncherGameFiles.Ready())
        {
            _view.Actions.ShowLaunch(
                _model.InGameMode ? "PLAY" : "START GAME",
                showCloudSync: true,
                showUpdate: false
            );
        }
        else
        {
            _view.Actions.ShowRetry();
        }
    }

    private void FailDownload(string message)
    {
        if (message == null)
        {
            _view.Download.Reset();
            return;
        }

        _view.SetStatus($"Download failed: {message}");
        _view.Download.Reset("RETRY DOWNLOAD");
    }

    private void CancelDownload()
    {
        _view.SetStatus("Download cancelled");
        _view.Download.SetButtonDisabled(false);
    }

    private void CompleteUpdateCheck(bool hasUpdate)
    {
        if (hasUpdate)
        {
            _view.Actions.HideAll();
            _view.Download.Visible = true;
            _view.Download.Reset("UPDATE GAME FILES");
            _view.SetStatus("Update available!");
        }
        else
        {
            _view.Actions.SetUpdateButtonText("UP TO DATE");
        }
    }

    private void FailUpdateCheck(string message)
    {
        _view.Actions.SetUpdateButtonText("CHECK FAILED");
        _view.Actions.SetUpdateButtonDisabled(false);
        _view.AppendLog($"Update check failed: {message}");
    }

    private void HandleFastPath(LauncherModel.FastPathResult result)
    {
        switch (result)
        {
            case LauncherModel.FastPathResult.ReadyToLaunch:
                ShowReadyToLaunch(
                    $"Welcome back, {_model.AccountName}",
                    showUpdate: true
                );
                break;

            case LauncherModel.FastPathResult.AutoConnect:
                _model.Connect();
                StartConnectionTimeout();
                break;

            case LauncherModel.FastPathResult.ShowLogin:
                ShowLogin();
                break;
        }
    }

    private bool ShouldSuppressSessionUpdate(SessionState state)
    {
        if (
            _model.AwaitingCode
            && state
                is SessionState.Connecting
                    or SessionState.Authenticating
        )
            return true;

        if (_updateCheckRunning)
            return true;

        return state == SessionState.Disconnected && _model.ConnectionResolved;
    }

    private void ShowSessionState(SessionState state)
    {
        _view.HideAllSections();

        var status = state switch
        {
            SessionState.Connecting => "Connecting to Steam...",
            SessionState.Authenticating => "Authenticating...",
            SessionState.VerifyingOwnership => "Verifying game ownership...",
            _ => null,
        };
        if (status != null)
        {
            _view.SetStatus(status);
            return;
        }

        if (state is SessionState.Disconnected)
        {
            ShowLogin();
            return;
        }

        switch (state)
        {
            case SessionState.LoggedIn:
                ShowLoggedIn();
                break;

            case SessionState.Failed:
                ShowFailed();
                break;
        }
    }

    private void ShowLogin()
    {
        _view.SetStatus("Enter your Steam credentials");
        _view.Login.Visible = true;
        _view.Login.SetDisabled(false);
    }

    private void ShowLoggedIn()
    {
        _model.MarkConnectionResolved();
        if (LauncherGameFiles.Ready())
        {
            ShowReadyToLaunch($"Logged in as {_model.AccountName}", showUpdate: true);
            return;
        }

        _view.SetStatus($"Logged in as {_model.AccountName}");
        _view.Download.Visible = true;
        _view.Download.SetButtonDisabled(false);
    }

    private void ShowFailed()
    {
        _model.MarkConnectionResolved();
        _view.SetStatus($"Error: {_model.FailReason}");
        _view.Login.Visible = true;
        _view.Login.SetDisabled(false);
    }

    private void ShowReadyToLaunch(string status, bool showUpdate)
    {
        _view.SetStatus(status);
        ShowPreviousLaunchWarningIfNeeded();
        _view.Actions.ShowLaunch(
            _model.InGameMode ? "PLAY" : "START GAME",
            showCloudSync: true,
            showUpdate
        );
    }

    private void ShowPreviousLaunchWarningIfNeeded()
    {
        if (!LauncherLaunchMarkers.PreviousGameLaunchIncomplete(out var phase))
            return;

        const string status = "Previous game launch did not finish.";
        var phaseSuffix = string.IsNullOrWhiteSpace(phase) ? "" : $" Last phase: {phase}.";

        _view.SetStatus(status);
        _view.AppendLog(status + phaseSuffix);
        _view.AppendLog(
            "The launcher is staying available so you are not trapped on a black screen."
        );
        _view.AppendLog(
            "Tap SHOW LAST ERROR to print the failure summary here, or EXPORT DIAGNOSTICS to share the full report."
        );
        WriteAutomaticDiagnosticsOnce();
    }

    private void WriteAutomaticDiagnosticsOnce()
    {
        if (_automaticDiagnosticsWritten)
            return;

        _automaticDiagnosticsWritten = true;
        try
        {
            var path = _model.WriteDiagnosticsReport();
            _view.AppendLog($"Automatic diagnostics snapshot: {path}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Automatic diagnostics snapshot failed: {ex.Message}");
        }
    }

    private async void StartConnectionTimeout()
    {
        await Task.Delay(SteamConnectionTimeoutMs);

        if (ShouldIgnoreConnectionTimeout())
            return;

        if (CanUseOfflineLaunch())
        {
            _runOnMainThread(ShowOfflineLaunch);
            return;
        }

        _runOnMainThread(ShowFirstLaunchFailure);
    }

    private bool ShouldIgnoreConnectionTimeout()
    {
        if (_model.ConnectionResolved)
            return true;

        return _model.SessionState
            is not (
                SessionState.Connecting
                or SessionState.Authenticating
                or SessionState.VerifyingOwnership
            );
    }

    private bool CanUseOfflineLaunch()
        => _model.HasOwnershipMarker() && LauncherGameFiles.Ready();

    private void ShowOfflineLaunch()
    {
        _view.SetStatus(SavedCredentialsFallback);
        _view.AppendLog(OfflineLaunchAllowed);
        _view.Actions.ShowLaunch(
            _model.InGameMode ? "PLAY" : "START GAME",
            showCloudSync: true,
            showUpdate: false
        );
    }

    private void ShowFirstLaunchFailure()
    {
        _view.SetStatus(FirstLaunchConnectionFailed);
        _view.Actions.ShowRetry();
    }

    private static class AppUpdateChecker
    {
        private const string LatestReleaseApiUrl =
            "https://api.github.com/repos/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest";
        private const string NameProperty = "name";
        private const string TagNameProperty = "tag_name";
        private const string UserAgent = "StS2-Launcher";
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        private const string RepoReleasesPage =
            "https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest";

        private static async Task<string> CheckAsync()
        {
            var currentVersion = GetInstalledVersion();
            if (currentVersion == null)
                return null;

            using var http = OperatingSystem.IsAndroid()
                ? AndroidJavaHttpMessageHandler.CreateClient(HttpClientPurpose.CDN)
                : new System.Net.Http.HttpClient { Timeout = Timeout };
            http.Timeout = Timeout;
            http.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            var response = await http.GetStringAsync(LatestReleaseApiUrl).ConfigureAwait(false);
            var release = ParseRelease(response);
            if (release.Name == null)
                return null;

            var latestVersion = NormalizeVersion(release.Tag ?? release.Name);
            var installedVersion = NormalizeVersion(currentVersion);

            if (latestVersion == null || installedVersion == null)
                return null;

            if (CompareVersions(latestVersion, installedVersion) <= 0)
                return null;

            return latestVersion;
        }

        private static string GetInstalledVersion()
        {
            try
            {
                if (!AndroidGodotAppBridge.TryGetInstance(out var godotApp))
                    return null;

                return (string)godotApp.Call("getVersionName");
            }
            catch
            {
                return null;
            }
        }

        private static GitHubRelease ParseRelease(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var releaseName = root.TryGetProperty(NameProperty, out var nameProp)
                ? nameProp.GetString()
                : null;
            var releaseTag = root.TryGetProperty(TagNameProperty, out var tagProp)
                ? tagProp.GetString()
                : null;

            return new GitHubRelease(releaseName, releaseTag);
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return null;

            var match = Regex.Match(version, @"\d+(?:\.\d+)*(?:\.\d+)*");
            return match.Success ? match.Value.TrimStart('v', 'V') : null;
        }

        private static int CompareVersions(string a, string b)
        {
            var aParts = a.Split('.');
            var bParts = b.Split('.');
            var len = Math.Max(aParts.Length, bParts.Length);

            for (int i = 0; i < len; i++)
            {
                int aVal = i < aParts.Length && int.TryParse(aParts[i], out var av) ? av : 0;
                int bVal = i < bParts.Length && int.TryParse(bParts[i], out var bv) ? bv : 0;
                if (aVal != bVal)
                    return aVal - bVal;
            }

            return 0;
        }

        private readonly record struct GitHubRelease(string Name, string Tag);
    }

    private readonly record struct CloudSyncRequest(
        string ConfirmationMessage,
        Func<string, string, Task> Run,
        string OperationName,
        string StartMessage,
        string CompleteMessage
    )
    {
        private static CloudSyncRequest Push => new(
            "Push local saves to cloud?\nThis will overwrite your cloud saves.",
            CloudSyncCoordinator.ManualPushAllAsync,
            "Push",
            "Pushing local saves to cloud...",
            "Push complete."
        );

        private static CloudSyncRequest Pull => new(
            "Pull cloud saves to local?\nThis will overwrite your local saves.",
            CloudSyncCoordinator.ManualPullAllAsync,
            "Pull",
            "Pulling cloud saves to local...",
            "Pull complete."
        );
    }

    private static class LocalSteamCredentialInbox
    {
        private const string FileName = "steam_login_credentials.txt";

        private static (string Username, string Password)? TryConsume()
        {
            var path = GetPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            if (!TryReadAndDelete(path, out var lines))
                return null;

            try
            {
                return Decode(lines);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Launcher] Ignored local Steam credential file: {ex.Message}");
                return null;
            }
        }

        private static string GetPath()
        {
            try
            {
                var dir = AndroidGodotAppBridge.GetExternalFilesDirPath();
                return string.IsNullOrWhiteSpace(dir)
                    ? null
                    : Path.Combine(dir, FileName);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryReadAndDelete(string path, out string[] lines)
        {
            lines = null;
            try
            {
                lines = File.ReadAllLines(path);
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Launcher] Ignored local Steam credential file: {ex.Message}");
                return false;
            }
        }

        private static (string Username, string Password) Decode(string[] lines)
        {
            if (lines.Length < 2)
                throw new InvalidDataException("expected two base64 lines");

            var username = Encoding.UTF8.GetString(Convert.FromBase64String(lines[0].Trim())).Trim();
            var password = Encoding.UTF8.GetString(Convert.FromBase64String(lines[1].Trim()));

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                throw new InvalidDataException("username or password was empty");

            return (username, password);
        }
    }
}
