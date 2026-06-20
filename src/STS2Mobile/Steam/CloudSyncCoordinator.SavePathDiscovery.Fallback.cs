using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private static readonly string[] FallbackRootFiles =
        {
        };

        private static void AddFallbackProfilePaths(
            List<string> paths,
            ISaveStore store
        )
        {
            paths.AddRange(FallbackRootFiles);
            foreach (var profile in FallbackProfiles())
                profile.AddTo(paths, store);
            AddEnumeratedSavePaths(paths, store);
        }
    }
}
