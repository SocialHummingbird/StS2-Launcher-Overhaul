namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static readonly string[] CompactWorkflowStepNames =
    {
        "Sign in",
        "Verify",
        "Files",
        "Play",
    };

    private static readonly string[] CompactWorkflowStepNumbers =
    {
        "1",
        "2",
        "3",
        "4",
    };

    private static readonly string[] CompactWorkflowStepDetails =
    {
        "Account",
        "Steam Guard",
        "Game files",
        "Saves safe",
    };

    private static readonly string[] CompactWorkflowStepTooltips =
    {
        "Open sign-in",
        "Open Steam Guard",
        "Open game files",
        "Open play and saves",
    };

    private enum CompactWorkflowStep
    {
        SignIn = 0,
        Code = 1,
        Files = 2,
        Play = 3,
    }
}
