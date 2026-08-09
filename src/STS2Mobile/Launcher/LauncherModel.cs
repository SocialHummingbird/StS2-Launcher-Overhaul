using System;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Orchestrates launcher session state and delegates concrete workflows to focused helpers.
// Events fire from background threads; the controller marshals them to the main thread.
internal partial class LauncherModel : IDisposable
{
    internal enum FastPathOutcome
    {
        ShowLogin,
        AutoConnect,
        ReadyToLaunch,
    }

    internal readonly struct FastPathResult
    {
        private FastPathResult(
            FastPathOutcome outcome,
            LauncherLaunchReadiness readiness
        )
        {
            Outcome = outcome;
            Readiness = readiness;
        }

        internal FastPathOutcome Outcome { get; }
        internal LauncherLaunchReadiness Readiness { get; }
        internal bool ReadyToLaunch => Outcome == FastPathOutcome.ReadyToLaunch;

        internal static FastPathResult Ready(LauncherLaunchReadiness readiness)
            => new(FastPathOutcome.ReadyToLaunch, readiness);

        internal static FastPathResult AutoConnect()
            => new(FastPathOutcome.AutoConnect, readiness: null);

        internal static FastPathResult ShowLogin()
            => new(FastPathOutcome.ShowLogin, readiness: null);
    }

    private volatile bool _connectionResolved;
    private readonly string _dataDir;
    private readonly SteamCredentialStore _credentialStore;
    private readonly LauncherSteamSession _steamSession;
    private readonly string _processGameBranch;
    private int _sessionAttemptId;
    private string _failReason;
    private SessionState _sessionState = SessionState.Disconnected;

    internal string DataDir => _dataDir;

    internal LauncherModel(string dataDir)
    {
        _dataDir = dataDir;
        PatchHelper.Log($"[Launcher] Model data dir: '{_dataDir}' length={(_dataDir == null ? -1 : _dataDir.Length)} rooted={(!string.IsNullOrWhiteSpace(_dataDir) && System.IO.Path.IsPathRooted(_dataDir))}");
        _processGameBranch = LauncherPreferences.ReadGameBranch();
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
        LauncherLaunchReadinessCache.Clear("selected version redownload reset");
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
        _steamSession.Dispose();
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
