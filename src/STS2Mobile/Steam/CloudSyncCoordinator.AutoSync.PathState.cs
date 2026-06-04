using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly partial struct AutoSyncContext
    {
        private enum PathPresence
        {
            Missing,
            LocalOnly,
            CloudOnly,
            Both,
        }

        private readonly struct PathState
        {
            private PathState(string? localContent, bool cloudExists)
            {
                LocalContent = localContent;
                CloudExists = cloudExists;
            }

            private string? LocalContent { get; }
            private bool CloudExists { get; }

            private PathPresence Presence => (LocalContent != null, CloudExists) switch
            {
                (true, true) => PathPresence.Both,
                (true, false) => PathPresence.LocalOnly,
                (false, true) => PathPresence.CloudOnly,
                _ => PathPresence.Missing,
            };

            internal static PathState From(string? localContent, bool cloudExists)
                => new(localContent, cloudExists);

            internal async Task RunAsync(AutoSyncContext sync)
            {
                switch (Presence)
                {
                    case PathPresence.Both:
                        await SyncExistingFileAsync(sync, RequireLocalContent());
                        return;

                    case PathPresence.CloudOnly:
                        await sync.PullCloudOnlyFileAsync();
                        return;

                    case PathPresence.LocalOnly:
                        sync.PushLocalOnlyFile(RequireLocalContent());
                        return;
                }
            }

            private string RequireLocalContent()
            {
                if (LocalContent == null)
                    throw new InvalidOperationException(
                        "Auto sync path state has no local content"
                    );

                return LocalContent;
            }
        }
    }
}
