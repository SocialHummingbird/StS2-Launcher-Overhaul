using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal readonly record struct CloudPushEligibilityPresentation(
    bool IsEligible,
    string GuidanceText,
    string ReviewButtonDetail
)
{
    internal static CloudPushEligibilityPresentation Create(
        CloudPushEligibilityResult result
    )
    {
        if (result.IsEligible)
        {
            return new CloudPushEligibilityPresentation(
                true,
                "Upload available.\nAndroid local saves are present. Review the overwrite warning before uploading.",
                "Local saves found"
            );
        }

        var blockerCount = result.BlockingReasons.Count;
        var lines = new List<string>
        {
            $"Upload unavailable: {CountLabel(blockerCount, "safety check")} {(blockerCount == 1 ? "needs" : "need")} attention.",
            "Why upload is unavailable:"
        };
        for (var index = 0; index < blockerCount; index++)
        {
            lines.Add($"{index + 1}. {result.BlockingReasons[index].Reason}");
        }

        lines.Add("How to unlock upload:");
        for (var index = 0; index < result.RequiredNextActions.Count; index++)
        {
            lines.Add($"{index + 1}. {result.RequiredNextActions[index].Description}");
        }

        return new CloudPushEligibilityPresentation(
            false,
            string.Join("\n", lines),
            $"{CountLabel(blockerCount, "check")} to fix"
        );
    }

    private static string CountLabel(int count, string singular)
        => count == 1
            ? $"1 {singular}"
            : $"{count} {singular}s";
}
