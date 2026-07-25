using System.Collections.Generic;
using System.Threading;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private static readonly string[] FallbackRootFiles =
        {
            "profile.save",
            "modded/profile.save",
        };

        private static void AddFallbackProfilePaths(
            List<string> paths,
            ISaveStore store,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            paths.AddRange(FallbackRootFiles);
            foreach (var profile in FallbackProfiles())
                profile.AddTo(paths, store, cancellationToken);
            AddEnumeratedSavePaths(paths, store, cancellationToken);
        }
    }
}
