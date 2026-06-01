using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed partial class DownloadStateStore
    {
        private readonly DepotDownloader _owner;
        private readonly string _stateDir;

        private DownloadStateStore(DepotDownloader owner, string stateDir)
        {
            _owner = owner;
            _stateDir = stateDir;
        }

        private ulong LoadManifestId(uint depotId)
        {
            var path = Path.Combine(_stateDir, $"{depotId}.id");
            if (!File.Exists(path))
                return 0;

            try
            {
                var raw = File.ReadAllText(path).Trim();
                if (ulong.TryParse(raw, out var id))
                    return id;

                Log($"Ignoring malformed cached manifest id for depot {depotId}: {raw}");
                MoveBadStateFile(path);
                return 0;
            }
            catch (Exception ex)
            {
                Log($"Could not read cached manifest id for depot {depotId}: {ex.Message}");
                return 0;
            }
        }

        private DepotManifest? LoadManifest(uint depotId)
        {
            var path = Path.Combine(_stateDir, $"{depotId}.manifest");
            if (!File.Exists(path))
                return null;

            try
            {
                using var fs = File.OpenRead(path);
                return DepotManifest.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Log($"Cached manifest for depot {depotId} could not be loaded: {ex.Message}");
                MoveBadStateFile(path);
                return null;
            }
        }

        private void SaveManifest(uint depotId, DepotManifest manifest, ulong manifestId)
        {
            var manifestPath = Path.Combine(_stateDir, $"{depotId}.manifest");
            var manifestTempPath = manifestPath + ".tmp";
            using (var fs = File.Create(manifestTempPath))
            {
                manifest.Serialize(fs);
            }
            CommitStateFile(manifestTempPath, manifestPath);

            var idPath = Path.Combine(_stateDir, $"{depotId}.id");
            var idTempPath = idPath + ".tmp";
            File.WriteAllText(idTempPath, manifestId.ToString());
            CommitStateFile(idTempPath, idPath);

            DeleteQuietly(manifestPath + ".bad");
            DeleteQuietly(idPath + ".bad");
        }

        private void Prepare()
        {
            Directory.CreateDirectory(_stateDir);
            CleanupTempFiles();
        }

        private void Log(string message)
            => _owner.Log(message);
    }
}
