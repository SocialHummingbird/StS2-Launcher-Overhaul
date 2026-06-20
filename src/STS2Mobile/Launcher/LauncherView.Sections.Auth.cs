namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void ClearLoginPasswordAndDisable()
    {
        Login.SetDisabled(true);
        Login.ClearPassword();
    }

    internal void SetLoginFormVisible(bool visible, bool disabled)
    {
        if (visible)
        {
            SetFirstRunGuideVisible(false);
            HideCompactCompletedAuthSections(showCode: false);
        }

        Login.SetFormVisible(visible, disabled);
        if (visible)
        {
            SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
            SetCompactCurrentTask("Sign in", Login, "Steam account");
            ScrollCompactPrimaryTo(Login);
        }
    }

    internal void ShowCodePrompt(bool wasIncorrect)
    {
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: true);
        SetCompactWorkflowStep(CompactWorkflowStep.Code);
        SetCompactCurrentTask("Verify", Code, "Steam Guard code");
        Code.Show(wasIncorrect);
        ScrollCompactPrimaryTo(Code);
    }

    private void SetFirstRunGuideVisible(bool visible)
        => FirstRunGuide.Visible = !_profile.Compact || visible;

    private void HideCompactCompletedAuthSections(bool showCode)
    {
        if (!_profile.Compact)
            return;

        Login.SetFormVisible(false, disabled: true);
        Code.Visible = showCode;
    }
}
