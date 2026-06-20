namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string AutomationFileName = "launcher_automation_action.txt";
    private const string AutomationMarkerFileName = "last_launcher_automation.txt";

    private void TryStartAutomation()
    {
        var request = LauncherAutomationRequest.TryConsume(_model.DataDir);
        if (!request.HasValue)
            return;

        _ = RunAutomationAsync(request.Value);
    }
}
