using System;
using System.Text;

namespace STS2Mobile.Steam;

internal static class AutomaticSyncHash
{
    internal static string Compute(string content)
        => Compute(Encoding.UTF8.GetBytes(content ?? string.Empty));

    internal static string Compute(byte[] content)
        => Convert.ToHexString(
                AndroidJavaCrypto.Sha256HashData(
                    content ?? Array.Empty<byte>()
                )
            )
            .ToLowerInvariant();
}
