using System;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private static string RuntimePackId(GameRuntimeSlot slot, string patchSetVersion)
    {
        var pck = ShortHash(slot.PckSha256);
        var asm = ShortHash(slot.SourceAssemblySha256);
        return $"{slot.Branch}-{pck}-{asm}-{patchSetVersion}";
    }

    private static string ShortHash(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal)
            ? "unknown"
            : value.Length <= 12
                ? value
                : value.Substring(0, 12);
}
