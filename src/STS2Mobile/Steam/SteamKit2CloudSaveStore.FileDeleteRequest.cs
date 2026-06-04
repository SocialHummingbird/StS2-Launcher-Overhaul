namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudFileDeleteRequest
    {
        private CloudFileDeleteRequest(string canonPath)
        {
            CanonPath = canonPath;
        }

        private string CanonPath { get; }

        internal static CloudFileDeleteRequest From(string path)
            => new(CloudSavePath.Canonicalize(path));

        internal void Apply(SteamKit2CloudSaveStore store)
        {
            store._cache.Remove(CanonPath);
            store.EnqueueDelete(CanonPath);
        }
    }
}
