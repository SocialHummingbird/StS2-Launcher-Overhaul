namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string AutomationFileName = "launcher_automation_action.txt";
    private const string AutomationMarkerFileName = "last_launcher_automation.txt";

    private bool TryStartAutomation()
    {
        var request = LauncherAutomationRequest.TryConsume(_model.DataDir);
        if (!request.HasValue)
            return false;

        _ = RunAutomationAsync(request.Value);
        return true;
    }
}
