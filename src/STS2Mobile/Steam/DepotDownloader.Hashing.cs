using System;
using System.IO;
using System.Linq;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    // Computes SHA-1 of a decompressed chunk and compares it to the manifest ChunkID.
    private static bool VerifyChunkHash(byte[] buffer, int length, DepotManifest.ChunkData chunk)
    {
        if (chunk.ChunkID == null || chunk.ChunkID.Length == 0)
            return false;

        var hash = ComputeSha1(buffer.AsSpan(0, length));
        return HashesEqual(hash, chunk.ChunkID);
    }

    // Computes SHA-1 of a file on disk and compares it to the manifest hash.
    private static bool VerifyFileHash(string path, DepotManifest.FileData file)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Length != (long)file.TotalSize)
                return false;

            if (file.FileHash == null || file.FileHash.Length == 0)
                return file.TotalSize == 0;

            var hash = ComputeFileSha1(path);
            return HashesEqual(hash, file.FileHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeSha1(ReadOnlySpan<byte> data)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.SHA1.HashData(data);

        return AndroidJavaCrypto.Sha1HashData(data.ToArray());
    }

    private static byte[] ComputeFileSha1(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return System.Security.Cryptography.SHA1.HashData(fs);
        }

        return AndroidJavaCrypto.Sha1FileHashData(path);
    }

    private static bool HashesEqual(byte[]? left, byte[]? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.SequenceEqual(right);
    }
}
