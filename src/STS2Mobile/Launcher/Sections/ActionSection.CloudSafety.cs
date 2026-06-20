using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ToggleCloudSafety()
    {
        _cloudSafetyExpanded = !_cloudSafetyExpanded;
        UpdateBranchHelpText();
    }

    private void OpenCompactCloudSafetyFromReadySummary()
    {
        if (!_compact)
            return;

        _cloudSafetyExpanded = true;
        UpdateBranchHelpText();
    }

    private string CompactCloudSafetySummary()
        => CompactPlaySyncDrawerText(
            "Save Check",
            $"Get saves first: {SteamGameBranch.CompactDisplayName(_gameBranch, 14)}"
        );

    private string CompactCloudSafetyDetailText()
        => $"Saves for: {SteamGameBranch.CompactDisplayName(_gameBranch, 18)}\n"
            + "Get Steam saves before upload. Upload can overwrite Steam.";

    private static string CompactCloudPullText()
        => CompactPlaySyncDrawerText("Get Steam Saves", "Download to Android");

    private static string CompactCloudPushToggleText(bool expanded)
        => expanded
            ? CompactPlaySyncDrawerText("Hide Upload", "Keep locked")
            : CompactPlaySyncDrawerText("Upload Locked", "Review first");

    private static string CompactCloudPushDangerText()
        => CompactPlaySyncDrawerText("Upload to Steam", "Overwrite cloud");

    private static string CompactCloudPushConfirmText()
        => CompactPlaySyncDrawerText("Confirm Upload", "Overwrite cloud");

    private static string CompactCloudPushWarningText()
        => "Steam Cloud overwrite\nConfirm only after Pull/local saves are verified.";
}
