using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static class WorkshopModConsent
{
    internal static bool IsAccepted()
    {
        try
        {
            return File.Exists(AppPaths.AppPrivateWorkshopConsentMarkerPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to inspect mod consent marker: {ex.Message}");
            return false;
        }
    }

    internal static void Accept(string reason)
    {
        try
        {
            AppPaths.EnsureWorkshopDirectories();
            File.WriteAllText(
                AppPaths.AppPrivateWorkshopConsentMarkerPath,
                $"acceptedUtc={DateTimeOffset.UtcNow:O}\nreason={reason ?? string.Empty}\n"
            );
            PatchHelper.Log("[Workshop] Workshop mod consent marker written");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to write mod consent marker: {ex.Message}");
        }
    }

    internal static void Clear()
    {
        try
        {
            if (File.Exists(AppPaths.AppPrivateWorkshopConsentMarkerPath))
                File.Delete(AppPaths.AppPrivateWorkshopConsentMarkerPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to clear mod consent marker: {ex.Message}");
        }
    }
}
