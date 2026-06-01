using System;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Orchestrates launcher session state and delegates concrete workflows to focused helpers.
// Events fire from background threads; the controller marshals them to the main thread.
internal partial class LauncherModel : IDisposable
{
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
    private int _sessionAttemptId;
    private string _failReason;
    private SessionState _sessionState = SessionState.Disconnected;

    internal LauncherModel(string dataDir)
    {
        _dataDir = dataDir;
        _credentialStore = new SteamCredentialStore(dataDir);
        _steamSession = new LauncherSteamSession(dataDir, _credentialStore);
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

    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        CancelDownload();
        ResetDownload();
        _steamSession.Dispose(preserveLaunchConnection: PreserveLaunchConnection);
    }

    private static void Raise(Action callback, string name)
        => RunCallback(name, () => callback?.Invoke());

    private static void Raise<T>(Action<T> callback, T value, string name)
        => RunCallback(name, () => callback?.Invoke(value));

    private static void RunCallback(string name, Action invoke)
    {
        try
        {
            invoke();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] {name} callback failed: {ex.Message}");
        }
    }
}
