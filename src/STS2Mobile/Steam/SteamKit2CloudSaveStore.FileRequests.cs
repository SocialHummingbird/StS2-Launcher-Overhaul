using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudFileWriteRequest
    {
        private CloudFileWriteRequest(
            string canonPath,
            byte[] bytes,
            DateTimeOffset timestamp
        )
        {
            CanonPath = canonPath;
            Bytes = bytes;
            Timestamp = timestamp;
        }

        private string CanonPath { get; }
        private byte[] Bytes { get; }
        private DateTimeOffset Timestamp { get; }

        internal static CloudFileWriteRequest From(string path, byte[] bytes)
            => new(
                CloudSavePath.Canonicalize(path),
                bytes,
                TruncatedUtcNow()
            );

        internal void Apply(SteamKit2CloudSaveStore store)
        {
            store._cache.Set(CanonPath, Bytes.Length, Timestamp);

            if (store._saveBatch.TryCollect(CanonPath, Bytes))
                return;

            store.EnqueueUpload(CanonPath, Bytes, Timestamp);
        }

        private static DateTimeOffset TruncatedUtcNow()
            => DateTimeOffset.FromUnixTimeSeconds(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
    }

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
