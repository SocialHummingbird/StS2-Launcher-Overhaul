using System;
using System.IO;
using System.Linq;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class PatchCompatibilityValidator
{
    private static SymbolCheck[] CheckSymbols(string assemblyPath, out string readFailure)
    {
        readFailure = string.Empty;
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(assemblyPath);
        }
        catch (Exception ex)
        {
            readFailure = $"source sts2.dll could not be read: {ex.GetType().Name}";
            return Array.Empty<SymbolCheck>();
        }

        return RequiredCriticalSymbols
            .Select(symbol => new SymbolCheck(
                symbol.Category,
                symbol.Kind,
                symbol.Symbol,
                ContainsAscii(bytes, symbol.Symbol)
            ))
            .ToArray();
    }

    private static bool ContainsAscii(byte[] haystack, string needle)
    {
        if (haystack == null || haystack.Length == 0 || string.IsNullOrWhiteSpace(needle))
            return false;

        var pattern = Encoding.UTF8.GetBytes(needle);
        if (pattern.Length > haystack.Length)
            return false;

        for (var i = 0; i <= haystack.Length - pattern.Length; i++)
        {
            var matched = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (haystack[i + j] == pattern[j])
                    continue;

                matched = false;
                break;
            }

            if (matched)
                return true;
        }

        return false;
    }
}
