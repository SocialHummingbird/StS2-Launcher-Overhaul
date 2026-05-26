using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Wires model events to view updates and handles the launcher UI state machine.
// All model callbacks are marshalled to the main thread before updating the view.
public class LauncherController
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly Action<Action> _runOnMainThread;
    private volatile bool _checkingForUpdates;
    private volatile bool _localLoginHandoffStarted;
    private bool _autoDiagnosticsWritten;

    public LauncherController(
        LauncherModel model,
        LauncherView view,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _runOnMainThread = runOnMainThread;
    }

    public void Start()
    {
        _model.SessionStateChanged += s => _runOnMainThread(() => UpdateUI(s));
        _model.LogReceived += msg => _runOnMainThread(() => _view.AppendLog(msg));
        PatchHelper.LogEmitted += msg =>
        {
            if (msg.StartsWith("[Cloud]"))
                _runOnMainThread(() => _view.AppendLog(msg));
        };
        _model.CodeNeeded += wasIncorrect =>
            _runOnMainThread(() =>
            {
                _view.Login.Visible = false;
                _view.Code.Show(wasIncorrect);
            });
        _model.DownloadProgressChanged += p =>
            _runOnMainThread(() =>
            {
                _view.Download.SetProgress(
                    p.Percentage,
                    $"{LauncherModel.FormatSize(p.DownloadedBytes)} / {LauncherModel.FormatSize(p.TotalBytes)} ({p.Percentage:F1}%)"
                );
                _view.AppendLog(p.CurrentFile);
            });
        _model.DownloadLogReceived += msg => _runOnMainThread(() => _view.AppendLog(msg));
        _model.DownloadCompleted += () =>
            _runOnMainThread(() =>
            {
                _view.SetStatus("Download complete! Start game when ready.");
                _view.Download.Visible = false;
                if (LauncherModel.GameFilesReady())
                {
                    var text = _model.InGameMode ? "PLAY" : "START GAME";
                    _view.Actions.ShowLaunch(text, showCloudSync: true, showUpdate: false);
                }
                else
                    _view.Actions.ShowRetry();
            });
        _model.DownloadFailed += msg =>
            _runOnMainThread(() =>
            {
                if (msg == null)
                {
                    _view.Download.Reset();
                    return;
                }
                _view.SetStatus($"Download failed: {msg}");
                _view.Download.Reset("RETRY DOWNLOAD");
            });
        _model.DownloadCancelled += () =>
            _runOnMainThread(() =>
            {
                _view.SetStatus("Download cancelled");
                _view.Download.SetButtonDisabled(false);
            });
        _model.UpdateCheckCompleted += hasUpdate =>
            _runOnMainThread(() =>
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
            });
        _model.UpdateCheckFailed += msg =>
            _runOnMainThread(() =>
            {
                _view.Actions.SetUpdateButtonText("CHECK FAILED");
                _view.Actions.SetUpdateButtonDisabled(false);
                _view.AppendLog($"Update check failed: {msg}");
            });

        _view.Login.LoginRequested += OnLoginPressed;
        _view.Code.CodeSubmitted += OnCodeSubmitPressed;
        _view.Download.DownloadRequested += OnDownloadPressed;
        _view.Actions.LaunchPressed += OnLaunchPressed;
        _view.Actions.RetryPressed += OnRetryPressed;
        _view.Actions.LocalBackupToggled += OnLocalBackupToggled;
        _view.Actions.CloudSyncToggled += OnCloudSyncToggled;
        _view.Actions.CloudPushPressed += OnCloudPushPressed;
        _view.Actions.CloudPullPressed += OnCloudPullPressed;
        _view.Actions.CheckForUpdatesPressed += OnCheckForUpdatesPressed;
        _view.Actions.RedownloadPressed += OnRedownloadPressed;
        _view.Actions.DiagnosticsPressed += OnDiagnosticsPressed;
        _view.Actions.SafeLaunchPressed += OnSafeLaunchPressed;

        var localBackupPref = LauncherModel.LoadLocalBackupPref();
        _view.Actions.SetLocalBackupChecked(localBackupPref);
        CloudSyncCoordinator.LocalBackupEnabled = localBackupPref;
        if (localBackupPref)
            AppPaths.EnsureExternalDirectories();
        _view.Actions.SetCloudSyncChecked(LauncherModel.LoadCloudSyncPref());

        var result = _model.StartSession();
        HandleFastPath(result);
        StartLocalLoginHandoffWatcher();
    }

    private void StartLocalLoginHandoffWatcher()
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
            var credentials = TryConsumeLocalLoginCredentials();
            if (credentials != null)
            {
                PatchHelper.Log("[Launcher] Consumed local Steam credential file");
                _runOnMainThread(() =>
                {
                    _view.Login.Visible = false;
                    _view.Login.SetDisabled(true);
                    _view.SetStatus("Authenticating...");
                });

                await _model.LoginAsync(credentials.Value.Username, credentials.Value.Password);
                return;
            }

            await Task.Delay(500);
        }
    }

    private static (string Username, string Password)? TryConsumeLocalLoginCredentials()
    {
        var path = GetLocalLoginCredentialsPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var lines = File.ReadAllLines(path);
            File.Delete(path);

            if (lines.Length < 2)
                throw new InvalidDataException("expected two base64 lines");

            var username = Encoding.UTF8.GetString(Convert.FromBase64String(lines[0].Trim())).Trim();
            var password = Encoding.UTF8.GetString(Convert.FromBase64String(lines[1].Trim()));

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                throw new InvalidDataException("username or password was empty");

            return (username, password);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Ignored local Steam credential file: {ex.Message}");
            return null;
        }
    }

    private static string GetLocalLoginCredentialsPath()
    {
        try
        {
            var godotApp = LauncherModel.GetGodotApp();
            var dir = (string)godotApp?.Call("getExternalFilesDirPath");
            return string.IsNullOrWhiteSpace(dir)
                ? null
                : Path.Combine(dir, "steam_login_credentials.txt");
        }
        catch
        {
            return null;
        }
    }

    private void HandleFastPath(FastPathResult result)
    {
        switch (result)
        {
            case FastPathResult.ReadyToLaunch:
                _view.SetStatus($"Welcome back, {_model.AccountName}");
                ShowPreviousLaunchWarningIfNeeded();
                var text = _model.InGameMode ? "PLAY" : "START GAME";
                _view.Actions.ShowLaunch(text, showCloudSync: true, showUpdate: true);
                break;

            case FastPathResult.AutoConnect:
                _model.Connect();
                StartConnectionTimeout();
                break;

            case FastPathResult.ShowLogin:
                _view.SetStatus("Enter your Steam credentials");
                _view.Login.Visible = true;
                _view.Login.SetDisabled(false);
                break;
        }
    }

    private async void StartConnectionTimeout()
    {
        await Task.Delay(10000);

        if (_model.ConnectionResolved)
            return;

        var state = _model.SessionState;
        if (
            state
            is SessionState.Connecting
                or SessionState.Authenticating
                or SessionState.VerifyingOwnership
        )
        {
            if (_model.HasOwnershipMarker() && LauncherModel.GameFilesReady())
            {
                _runOnMainThread(() =>
                {
                    _view.SetStatus("No connection — saved credentials will be used");
                    _view.AppendLog("Connection timed out. Valid ownership marker found.");
                    var text = _model.InGameMode ? "PLAY" : "START GAME";
                    _view.Actions.ShowLaunch(text, showCloudSync: true, showUpdate: false);
                });
            }
            else
            {
                _runOnMainThread(() =>
                {
                    _view.SetStatus("Connection failed. Internet required for first launch.");
                    _view.Actions.ShowRetry();
                });
            }
        }
    }

    // Updates visible sections and status text based on session state transitions.
    private void UpdateUI(SessionState state)
    {
        if (
            _model.AwaitingCode
            && state
                is SessionState.Connecting
                    or SessionState.WaitingForCredentials
                    or SessionState.Authenticating
        )
            return;

        if (_checkingForUpdates)
            return;

        // After successful login, ignore session disconnects — cloud ops use
        // their own token-based connections, so the launcher session dropping is expected.
        if (state == SessionState.Disconnected && _model.ConnectionResolved)
            return;

        _view.HideAllSections();

        switch (state)
        {
            case SessionState.Connecting:
                _view.SetStatus("Connecting to Steam...");
                break;

            case SessionState.WaitingForCredentials:
                _view.SetStatus("Enter your Steam credentials");
                _view.Login.Visible = true;
                _view.Login.SetDisabled(false);
                break;

            case SessionState.Authenticating:
                _view.SetStatus("Authenticating...");
                break;

            case SessionState.VerifyingOwnership:
                _view.SetStatus("Verifying game ownership...");
                break;

            case SessionState.LoggedIn:
                _model.ConnectionResolved = true;
                _view.SetStatus($"Logged in as {_model.AccountName}");
                if (LauncherModel.GameFilesReady())
                {
                    ShowPreviousLaunchWarningIfNeeded();
                    var text = _model.InGameMode ? "PLAY" : "START GAME";
                    _view.Actions.ShowLaunch(text, showCloudSync: true, showUpdate: true);
                }
                else
                {
                    _view.Download.Visible = true;
                    _view.Download.SetButtonDisabled(false);
                }
                break;

            case SessionState.Failed:
                _model.ConnectionResolved = true;
                _view.SetStatus($"Error: {_model.FailReason}");
                _view.Login.Visible = true;
                _view.Login.SetDisabled(false);
                break;

            case SessionState.Disconnected:
                _view.SetStatus("Enter your Steam credentials");
                _view.Login.Visible = true;
                _view.Login.SetDisabled(false);
                break;
        }
    }

    private async void OnLoginPressed(string username, string password)
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

    private void ShowPreviousLaunchWarningIfNeeded()
    {
        if (!LauncherModel.PreviousGameLaunchIncomplete(out var phase))
            return;

        var suffix = string.IsNullOrWhiteSpace(phase) ? "" : $" Last phase: {phase}.";
        _view.SetStatus("Previous game launch did not finish.");
        _view.AppendLog("Previous game launch did not finish." + suffix);
        _view.AppendLog("The launcher is staying available so you are not trapped on a black screen.");
        WriteAutomaticDiagnosticsSnapshot();
    }

    private void WriteAutomaticDiagnosticsSnapshot()
    {
        if (_autoDiagnosticsWritten)
            return;

        _autoDiagnosticsWritten = true;
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

    private void OnCodeSubmitPressed(string code)
    {
        _view.SetStatus("Verifying code...");
        _model.SubmitCode(code);
    }

    private async void OnDownloadPressed()
    {
        _view.Download.ShowProgress("Connecting to Steam...");
        await _model.StartDownloadAsync();
    }

    private async void OnCheckForUpdatesPressed()
    {
        _checkingForUpdates = true;
        _view.Actions.SetUpdateButtonDisabled(true);
        _view.Actions.SetUpdateButtonText("Checking...");

        try
        {
            // Check for launcher (APK) updates from GitHub in parallel with game file updates.
            var appUpdateTask = CheckAppUpdateAsync();
            await _model.CheckForUpdatesAsync();
            await appUpdateTask;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Check for updates failed: {ex}");
            _view.AppendLog($"Update check failed: {ex.Message}");
            _view.Actions.SetUpdateButtonText("CHECK FAILED");
        }
        finally
        {
            _checkingForUpdates = false;
            _view.Actions.SetUpdateButtonDisabled(false);
        }
    }

    private static readonly Color YellowLog = new(1f, 0.85f, 0.2f);

    private async Task CheckAppUpdateAsync()
    {
        try
        {
            var result = await AppUpdateChecker.CheckAsync();
            if (!result.HasUpdate)
            {
                _runOnMainThread(() => _view.AppendLog("Launcher is up to date"));
            }
            else
            {
                _runOnMainThread(() =>
                {
                    _view.AppendColoredLog(
                        $"Launcher update available: v{result.LatestVersion} — "
                        + $"download at {AppUpdateChecker.RepoReleasesPage}",
                        YellowLog
                    );
                    _view.SetStatus(
                        $"Launcher update available! Visit GitHub to download v{result.LatestVersion}"
                    );
                });
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] App update check failed: {ex.Message}");
        }
    }

    private void OnLocalBackupToggled(bool pressed)
    {
        if (pressed && !AppPaths.HasStoragePermission())
            AppPaths.RequestStoragePermission();

        if (pressed)
            AppPaths.EnsureExternalDirectories();

        LauncherModel.SaveLocalBackupPref(pressed);
        CloudSyncCoordinator.LocalBackupEnabled = pressed;
    }

    private void OnCloudSyncToggled(bool pressed)
    {
        LauncherModel.SaveCloudSyncPref(pressed);
        LauncherPatches.CloudSyncEnabled = pressed;
    }

    private void OnCloudPushPressed()
    {
        ShowConfirmation(
            "Push local saves to cloud?\nThis will overwrite your cloud saves.",
            () => _ = ExecuteCloudSyncOperationAsync(
                () =>
                    CloudSyncCoordinator.ManualPushAllAsync(
                        LauncherPatches.SavedAccountName,
                        LauncherPatches.SavedRefreshToken
                    ),
                "Push",
                "Pushing local saves to cloud...",
                "Push complete."
            )
        );
    }

    private void OnCloudPullPressed()
    {
        ShowConfirmation(
            "Pull cloud saves to local?\nThis will overwrite your local saves.",
            () => _ = ExecuteCloudSyncOperationAsync(
                () =>
                    CloudSyncCoordinator.ManualPullAllAsync(
                        LauncherPatches.SavedAccountName,
                        LauncherPatches.SavedRefreshToken
                    ),
                "Pull",
                "Pulling cloud saves to local...",
                "Pull complete."
            )
        );
    }

    private const int CloudSyncOperationTimeoutMs = 180_000;

    private async Task ExecuteCloudSyncOperationAsync(
        Func<Task> operation,
        string operationName,
        string startMessage,
        string completeMessage
    )
    {
        _runOnMainThread(() =>
        {
            _view.Actions.SetPushPullDisabled(true);
            _view.AppendLog(startMessage);
        });

        try
        {
            var timeout = Task.Delay(CloudSyncOperationTimeoutMs);
            var operationTask = operation();
            if (await Task.WhenAny(operationTask, timeout) != operationTask)
                throw new TimeoutException($"{operationName} timed out after {CloudSyncOperationTimeoutMs}ms");

            await operationTask;
            _runOnMainThread(() =>
            {
                _view.AppendLog(completeMessage);
                _view.Actions.SetPushPullDisabled(false);
            });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] {operationName} sync failed: {ex.Message}");
            _runOnMainThread(() =>
            {
                _view.AppendLog($"{operationName} failed: {ex.Message}");
                _view.Actions.SetPushPullDisabled(false);
            });
        }
    }

    private void ShowConfirmation(string message, Action onConfirmed)
    {
        _view.ShowConfirmation(message, onConfirmed);
    }

    private void OnRetryPressed()
    {
        var result = _model.Retry();
        HandleFastPath(result);
    }

    private void OnRedownloadPressed()
    {
        ShowConfirmation(
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

    private void OnDiagnosticsPressed()
    {
        try
        {
            var path = _model.WriteDiagnosticsReport();
            _view.SetStatus("Diagnostics exported.");
            _view.AppendLog($"Diagnostics exported: {path}");
            if (OperatingSystem.IsAndroid())
            {
                var shared = (bool)(LauncherModel.GetGodotApp()?.Call("shareTextFile", path) ?? false);
                _view.AppendLog(shared ? "Android share sheet opened." : "Could not open Android share sheet.");
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Diagnostics export failed: {ex}");
            _view.SetStatus($"Diagnostics export failed: {ex.Message}");
        }
    }

    private void OnLaunchPressed() => _model.Launch();

    private void OnSafeLaunchPressed()
    {
        _view.AppendLog("Safe launch requested: default renderer, no shader warmup, local saves only for one run.");
        _model.LaunchSafe();
    }
}
