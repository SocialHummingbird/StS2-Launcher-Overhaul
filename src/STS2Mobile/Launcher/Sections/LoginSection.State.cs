using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class LoginSection
{
    internal void SetDisabled(bool disabled)
    {
        _loginButton.Disabled = disabled;
        _nativeLoginButton.Disabled = disabled || !OperatingSystem.IsAndroid();
        if (disabled)
            StopNativeCredentialPolling(hidePanel: true);
    }

    internal void ClearPassword()
    {
        _passwordField.Text = "";
        StopNativeCredentialPolling(hidePanel: true);
    }

    internal void SetFormVisible(bool visible, bool disabled)
    {
        Visible = visible;
        SetDisabled(disabled);
        if (visible && !disabled && OperatingSystem.IsAndroid())
            OpenNativeCredentialPanel();
    }
}
