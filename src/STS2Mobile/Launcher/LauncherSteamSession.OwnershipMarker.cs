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
            if (!_credentialStore.HasAccount() || !File.Exists(OwnershipMarkerPath))
                return false;

            var marker = AndroidEncryptedJsonFile.Load<OwnershipMarker>(OwnershipMarkerPath);
            return marker != null && _credentialStore.IsAccount(marker.Account);
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
                new OwnershipMarker(
                    accountName,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                )
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Ownership] Failed to save marker: {ex.Message}");
        }
    }

    private sealed class OwnershipMarker
    {
        public OwnershipMarker()
        {
        }

        internal OwnershipMarker(string account, long verifiedAt)
        {
            Account = account;
            VerifiedAt = verifiedAt;
        }

        [JsonInclude]
        public string Account { get; private set; }

        [JsonInclude]
        public long VerifiedAt { get; private set; }
    }
}
