namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct CdnOperationName
    {
        private CdnOperationName(string value)
        {
            Value = value;
        }

        internal static CdnOperationName ChunkAuthRetry { get; } = new("Chunk");
        internal static CdnOperationName ChunkDownload { get; } = new("Chunk download");
        internal static CdnOperationName ManifestAuthRetry { get; } = new("Manifest");
        internal static CdnOperationName ManifestDownload { get; } =
            new("Manifest download");

        private string Value { get; }

        public override string ToString()
            => Value;
    }
}
