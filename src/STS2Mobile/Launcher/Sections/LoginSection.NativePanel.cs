using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class LoginSection
{
    private void OnNativeLoginPressed()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        OpenNativeCredentialPanel();
    }

    private void OpenNativeCredentialPanel()
    {
        try
        {
            _nativeLoginButton.Disabled = true;
            _nativeCredentialPollsRemaining = 720;
            AndroidGodotAppBridge.ShowSteamLoginCredentialPanel();
            _nativeCredentialPollTimer.Start();
        }
        catch (Exception ex)
        {
            _nativeLoginButton.Disabled = false;
            PatchHelper.Log($"[Launcher] Could not open native Steam login panel: {ex.Message}");
            StatusRequested?.Invoke(
                "Steam sign-in panel could not open. Use Retry and try again.",
                LauncherStatusSeverity.Error
            );
        }
    }

    private void PollNativeCredentialResult()
    {
        if (--_nativeCredentialPollsRemaining <= 0)
        {
            StopNativeCredentialPolling(hidePanel: false);
            StatusRequested?.Invoke(
                "Steam sign-in timed out before credentials were submitted. Try signing in again.",
                LauncherStatusSeverity.Warning
            );
            return;
        }

        try
        {
            if (!AndroidGodotAppBridge.TryConsumeSteamLoginCredentialResult(out var username, out var password))
            {
                if (!AndroidGodotAppBridge.IsSteamLoginCredentialPanelVisible())
                    StopNativeCredentialPolling(hidePanel: false);

                return;
            }

            StopNativeCredentialPolling(hidePanel: true);
            _nativeLoginButton.Disabled = true;
            StatusRequested?.Invoke("Signing in to Steam...", LauncherStatusSeverity.Working);
            LoginRequested?.Invoke(username, password);
        }
        catch (Exception ex)
        {
            StopNativeCredentialPolling(hidePanel: true);
            _nativeLoginButton.Disabled = false;
            PatchHelper.Log($"[Launcher] Native Steam login panel result failed: {ex.Message}");
            StatusRequested?.Invoke(
                "Steam sign-in could not start. Try signing in again.",
                LauncherStatusSeverity.Error
            );
        }
    }

    private void StopNativeCredentialPolling(bool hidePanel)
    {
        _nativeCredentialPollTimer.Stop();
        _nativeLoginButton.Disabled = !OperatingSystem.IsAndroid();
        if (hidePanel && OperatingSystem.IsAndroid())
        {
            try
            {
                AndroidGodotAppBridge.HideSteamLoginCredentialPanel();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Launcher] Could not hide native Steam login panel: {ex.Message}");
            }
        }
    }
}
