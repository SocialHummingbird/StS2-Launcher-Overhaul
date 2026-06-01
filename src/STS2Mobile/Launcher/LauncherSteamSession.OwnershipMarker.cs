using System;
using System.IO;
using System.Text.Json.Serialization;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const string OwnershipMarkerFileName = "ownership_verified.enc";

    private string OwnershipMarkerPath
        => Path.Combine(_dataDir, OwnershipMarkerFileName);

    private bool HasOwnershipMarkerForCurrentAccount()
    {
        try
        {
            if (_credentialStore.AccountName == null || !File.Exists(OwnershipMarkerPath))
                return false;

            var marker = AndroidEncryptedJsonFile.Load<OwnershipMarker>(OwnershipMarkerPath);
            return marker != null && marker.Account == _credentialStore.AccountName;
        }
        catch
        {
            return false;
        }
    }

    private void SaveOwnershipMarker(string accountName)
    {
        try
        {
            AndroidEncryptedJsonFile.Save(
                OwnershipMarkerPath,
                new OwnershipMarker
                {
                    Account = accountName,
                    VerifiedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                }
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Ownership] Failed to save marker: {ex.Message}");
        }
    }

    private sealed class OwnershipMarker
    {
        [JsonInclude]
        public string Account { get; set; }

        [JsonInclude]
        public long VerifiedAt { get; set; }
    }
}
