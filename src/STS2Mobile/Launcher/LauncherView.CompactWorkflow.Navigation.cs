namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private void WireCompactWorkflowStepNavigation()
    {
        if (!_profile.Compact)
            return;

        for (var i = 0; i < _workflowStepButtons.Length; i++)
        {
            var capturedStep = (CompactWorkflowStep)i;
            _workflowStepButtons[i].Pressed += () => ScrollCompactWorkflowStep(capturedStep);
        }
    }

    private void WireCompactCurrentTaskNavigation()
    {
        if (!_profile.Compact)
            return;

        _compactCurrentTaskButton.Pressed += () => ScrollCompactPrimaryTo(_compactCurrentTaskTarget);
    }

    private void ScrollCompactWorkflowStep(CompactWorkflowStep step)
    {
        if (!_profile.Compact)
            return;

        var target = step switch
        {
            CompactWorkflowStep.SignIn => Login.Visible
                ? Login
                : (FirstRunGuide.Visible ? FirstRunGuide : _compactCurrentTaskTarget),
            CompactWorkflowStep.Code => Code.Visible ? Code : _compactCurrentTaskTarget,
            CompactWorkflowStep.Files => Download.Visible ? Download : _compactCurrentTaskTarget,
            CompactWorkflowStep.Play => _compactCurrentTaskTarget,
            _ => _compactCurrentTaskTarget,
        };
        ScrollCompactPrimaryTo(target);
    }
}
