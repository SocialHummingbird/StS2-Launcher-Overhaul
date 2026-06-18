namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private readonly struct SaveBatchFile
    {
        internal SaveBatchFile(string canonPath, byte[] bytes)
        {
            CanonPath = canonPath;
            Bytes = bytes;
        }

        internal string CanonPath { get; }
        internal byte[] Bytes { get; }
    }
}
