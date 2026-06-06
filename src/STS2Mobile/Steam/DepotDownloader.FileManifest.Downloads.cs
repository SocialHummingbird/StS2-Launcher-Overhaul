using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ManifestDownloadSelection
    {
        private readonly List<DepotManifest.FileData> _downloads = new();
        private int _verified;
        private int _corrupt;

        private ManifestDownloadSelection()
        {
        }

        private static ManifestDownloadSelection Empty()
            => new();

        private void Include(
            DepotManifest.FileData file,
            ManifestFileDownloadState decision
        )
        {
            switch (decision)
            {
                case ManifestFileDownloadState.NeedsDownload:
                    _downloads.Add(file);
                    break;

                case ManifestFileDownloadState.ExistingVerified:
                    _verified++;
                    break;

                case ManifestFileDownloadState.CorruptNeedsDownload:
                    _downloads.Add(file);
                    _corrupt++;
                    break;
            }
        }

        private List<DepotManifest.FileData> Finish(DepotDownloader owner)
        {
            if (_verified > 0)
                owner.Log($"Verified {_verified} existing files");
            if (_corrupt > 0)
                owner.Log($"Found {_corrupt} corrupt files requiring re-download");

            return _downloads;
        }

        internal static List<DepotManifest.FileData> Build(
            DepotDownloader owner,
            DepotManifest newManifest,
            Dictionary<string, DepotManifest.FileData> oldFiles,
            bool isUpdate
        )
        {
            var selection = Empty();
            foreach (var file in newManifest.Files)
            {
                selection.Include(
                    file,
                    owner.GetManifestFileDownloadState(
                        file,
                        oldFiles,
                        isUpdate
                    )
                );
            }

            return selection.Finish(owner);
        }
    }

    // Builds the list of files that need downloading. For manifest changes, uses
    // the hash diff. For all files in the target manifest, verifies the on-disk
    // copy against the expected SHA-1, catching corruption from interrupted
    // writes, disk errors, or missing files.
    private List<DepotManifest.FileData> GetFilesNeedingDownload(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        var oldFiles = BuildManifestFileMap(oldManifest);
        return ManifestDownloadSelection.Build(
            this,
            newManifest,
            oldFiles,
            isUpdate
        );
    }
}
