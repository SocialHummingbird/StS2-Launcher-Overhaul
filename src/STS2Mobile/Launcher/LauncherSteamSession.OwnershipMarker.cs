using System;
using System.IO;
using System.Text.Json.Serialization;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private sealed class OwnershipMarkerStore
    {
        private readonly string _accountName;
        private readonly string _markerPath;

        private OwnershipMarkerStore(string dataDir, string accountName)
        {
            _markerPath = Path.Combine(dataDir, "ownership_verified.enc");
            _accountName = accountName;
        }

        private bool HasMarker()
        {
            try
            {
                if (!File.Exists(_markerPath))
                    return false;

                var marker = AndroidEncryptedJsonFile.Load<OwnershipMarker>(_markerPath);
                return marker != null && marker.Account == _accountName;
            }
            catch
            {
                return false;
            }
        }

        private void Save()
        {
            try
            {
                AndroidEncryptedJsonFile.Save(
                    _markerPath,
                    new OwnershipMarker
                    {
                        Account = _accountName,
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
            public string Account { get; private set; }

            [JsonInclude]
            public long VerifiedAt { get; private set; }
        }
    }
}
