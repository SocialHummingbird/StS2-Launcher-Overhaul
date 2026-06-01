using STS2Mobile.Steam;
using System;
using System.IO;
using System.Threading.Tasks;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed class LauncherSteamSession : IDisposable
{
    private const string GuardCodeFileName = "steam_guard_code.txt";
    private const int GuardCodePollDelayMs = 500;

    private readonly string _dataDir;
    private readonly SteamCredentialStore _credentialStore;
    private TaskCompletionSource<string> _codeTcs;
    private SteamConnection _connection;
    private SteamAuth _auth;
    private volatile bool _awaitingCode;

    internal LauncherSteamSession(string dataDir, SteamCredentialStore credentialStore)
    {
        _dataDir = dataDir;
        _credentialStore = credentialStore;
    }

    internal SteamConnection Connection => _connection;
    internal bool AwaitingCode => _awaitingCode;

    internal static bool HasOwnershipMarker(string dataDir, string accountName)
        => accountName != null && new OwnershipMarkerStore(dataDir, accountName).HasMarker();

    internal async Task<string> ConnectSavedCredentialsAndVerifyAsync(Action verifyingOwnership)
    {
        try
        {
            _connection = new SteamConnection(
                _credentialStore.AccountName,
                _credentialStore.RefreshToken
            );
            return await VerifyAsync(_connection, verifyingOwnership);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Connection failed: {ex.Message}");
            return "Could not connect to Steam. Check your internet connection.";
        }
    }

    internal async Task<string> LoginAndVerifyAsync(
        string username,
        string password,
        Action<string> logMessage,
        Action<bool> codeNeeded,
        Action verifyingOwnership
    )
    {
        try
        {
            ResetAuth();
            _auth = new SteamAuth();
            _auth.LogMessage += msg => logMessage?.Invoke(msg);
            _auth.CodeProvider = wasIncorrect =>
                RequestSteamGuardCodeAsync(
                    wasIncorrect,
                    codeNeeded,
                    () => _auth.NeedsReconnectForAuth,
                    _auth.ReconnectForAuthAsync
                );
            _auth.Connect();

            var result = await _auth.LoginWithCredentialsAsync(
                username,
                password,
                _credentialStore.GuardData
            );
            _credentialStore.Save(result.AccountName, result.RefreshToken, result.GuardData);
            LauncherCloudSaveState.SaveCredentials(result.AccountName, result.RefreshToken);
            _connection = new SteamConnection(result.AccountName, result.RefreshToken);
            ResetAuth();
            return await VerifyAsync(_connection, verifyingOwnership);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Login failed: {ex.Message}");
            ResetAuth();
            return ex.Message;
        }
    }

    internal void SubmitCode(string code) => _codeTcs?.TrySetResult(code);

    internal async Task<string> EnsureConnectedAsync()
    {
        if (!_credentialStore.HasCredentials)
            return "No saved credentials";

        var connection = _connection
            ?? new SteamConnection(_credentialStore.AccountName, _credentialStore.RefreshToken);

        try
        {
            await ApplyAppAccessTokenAsync(connection);
            _connection = connection;
            return null;
        }
        catch (Exception ex)
        {
            _connection = connection;
            return $"Connection failed: {ex.Message}";
        }
    }

    internal void Retry(bool downloadActive)
    {
        if (!downloadActive)
        {
            _connection?.Dispose();
            _connection = null;
        }

        ResetAuth();
    }

    internal void Dispose(bool preserveLaunchConnection)
    {
        ResetAuth();
        if (!preserveLaunchConnection)
            _connection?.Dispose();
    }

    void IDisposable.Dispose() => Dispose(preserveLaunchConnection: false);

    private void ResetAuth()
    {
        _auth?.Dispose();
        _auth = null;
    }

    private async Task<string> RequestSteamGuardCodeAsync(
        bool wasIncorrect,
        Action<bool> codeNeeded,
        Func<bool> needsReconnect,
        Func<Task> reconnectAsync
    )
    {
        _awaitingCode = true;
        codeNeeded?.Invoke(wasIncorrect);
        _codeTcs = new TaskCompletionSource<string>();

        try
        {
            var code = await WaitForSteamGuardCodeAsync(_codeTcs.Task);
            if (needsReconnect())
                await reconnectAsync();
            return code;
        }
        finally
        {
            _awaitingCode = false;
            _codeTcs = null;
        }
    }

    private static async Task<string> WaitForSteamGuardCodeAsync(Task<string> uiCodeTask)
    {
        while (true)
        {
            if (uiCodeTask.IsCompleted)
                return await uiCodeTask;

            var localCode = TryConsumeLocalSteamGuardCode();
            if (!string.IsNullOrWhiteSpace(localCode))
                return localCode;

            await Task.Delay(GuardCodePollDelayMs);
        }
    }

    private static string TryConsumeLocalSteamGuardCode()
    {
        if (!OperatingSystem.IsAndroid())
            return null;

        if (!TryConsumeSteamGuardInbox(out var code))
            return null;

        if (!IsValidSteamGuardCode(code))
        {
            PatchHelper.Log("[Auth] Ignored local Steam Guard code file with invalid shape");
            return null;
        }

        PatchHelper.Log("[Auth] Consumed local Steam Guard code file");
        return code;
    }

    private static bool TryConsumeSteamGuardInbox(out string code)
    {
        code = null;
        var path = GetGuardCodePath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        return TryReadAndDeleteGuardCode(path, out code);
    }

    private static string GetGuardCodePath()
    {
        try
        {
            var dir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            return string.IsNullOrWhiteSpace(dir)
                ? null
                : Path.Combine(dir, GuardCodeFileName);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryReadAndDeleteGuardCode(string path, out string code)
    {
        code = null;
        try
        {
            code = File.ReadAllText(path).Trim().ToUpperInvariant();
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Auth] Failed to consume local Steam Guard code file: {ex.Message}");
            return false;
        }
    }

    private static bool IsValidSteamGuardCode(string code)
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

    private async Task<string> VerifyAsync(SteamConnection connection, Action verifyingOwnership)
    {
        verifyingOwnership?.Invoke();
        if (_credentialStore.AccountName == null)
            throw new InvalidOperationException("Cannot verify ownership without an account");

        var ownershipMarker = new OwnershipMarkerStore(_dataDir, _credentialStore.AccountName);
        var owns = await VerifyOwnershipAsync(connection, ownershipMarker);
        PatchHelper.Log(owns ? "[Launcher] Ownership verified" : "[Launcher] Ownership denied");

        return owns ? null : "You don't own Slay the Spire 2. Purchase on Steam to play.";
    }

    private static async Task<bool> VerifyOwnershipAsync(
        SteamConnection connection,
        OwnershipMarkerStore ownershipMarker
    )
    {
        var result = await connection.Apps.PICSGetAccessTokens(SteamCloudApp.AppId, null);
        bool owns = result.AppTokens.ContainsKey(SteamCloudApp.AppId);

        if (owns)
        {
            result.AppTokens.TryGetValue(SteamCloudApp.AppId, out var token);
            connection.AppAccessToken = token;
            ownershipMarker.Save();
        }

        return owns;
    }

    private static async Task ApplyAppAccessTokenAsync(SteamConnection connection)
    {
        var tokenResult = await connection.Apps.PICSGetAccessTokens(SteamCloudApp.AppId, null);
        if (
            tokenResult.AppTokens != null
            && tokenResult.AppTokens.TryGetValue(SteamCloudApp.AppId, out var token)
        )
        {
            connection.AppAccessToken = token;
            return;
        }

        if (
            tokenResult.AppTokensDenied != null
            && tokenResult.AppTokensDenied.Contains(SteamCloudApp.AppId)
        )
            throw new InvalidOperationException(
                "Steam denied app access token; ownership/session may be invalid"
            );
    }

    private sealed class OwnershipMarkerStore
    {
        private readonly string _accountName;
        private readonly string _markerPath;

        private OwnershipMarkerStore(string dataDir, string accountName)
        {
            _markerPath = Path.Combine(dataDir, "ownership_verified.enc");
            _accountName = accountName;
        }

        private bool HasMarker()
        {
            try
            {
                if (!File.Exists(_markerPath))
                    return false;

                var marker = AndroidEncryptedJsonFile.Load<OwnershipMarker>(_markerPath);
                return marker != null && marker.Account == _accountName;
            }
            catch
            {
                return false;
            }
        }

        private void Save()
        {
            try
            {
                AndroidEncryptedJsonFile.Save(
                    _markerPath,
                    new OwnershipMarker
                    {
                        Account = _accountName,
                        VerifiedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    }
                );
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Ownership] Failed to save marker: {ex.Message}");
            }
        }

        private sealed class OwnershipMarker
        {
            public string Account { get; set; }
            public long VerifiedAt { get; set; }
        }
    }
}
