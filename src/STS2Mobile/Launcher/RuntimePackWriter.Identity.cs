using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    internal static string RuntimePackId(
        GameIdentity gameIdentity,
        string patchSetVersion,
        string validationSurfaceVersion,
        string androidAssemblySha256,
        IReadOnlyDictionary<string, string> supportAssemblySha256
    )
    {
        if (gameIdentity == null)
            throw new ArgumentNullException(nameof(gameIdentity));

        var support = supportAssemblySha256 == null
            ? string.Empty
            : string.Join(
                "\n",
                supportAssemblySha256
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair => $"support={pair.Key.ToLowerInvariant()}:{pair.Value.ToLowerInvariant()}")
            );
        var canonical = string.Join(
            "\n",
            "runtime-pack-v1",
            $"gameIdentityId={gameIdentity.Id}",
            $"patchSetVersion={patchSetVersion}",
            $"validationSurfaceVersion={validationSurfaceVersion}",
            $"androidAssemblySha256={androidAssemblySha256}",
            support
        );
        var hash = Convert.ToHexString(
            AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(canonical))
        ).ToLowerInvariant();
        return $"{gameIdentity.Branch}-{ShortHash(hash)}";
    }

    private static string ShortHash(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal)
            ? "unknown"
            : value.Length <= 12
                ? value
                : value.Substring(0, 12);
}
