using System;
using System.Collections.Generic;
using System.Globalization;

namespace STS2Mobile.Patches;

internal static partial class PlatformPatches
{
    private static string ResolveThreeLetterLanguageCode()
    {
        try
        {
            var locale = Godot.OS.GetLocale(); // e.g. "de_DE_u_mu_celsius" or "de_DE"
            foreach (var cultureName in LocaleCandidates(locale))
            {
                if (TryResolveThreeLetterCulture(cultureName, out var threeLetter))
                {
                    PatchHelper.Log(
                        $"Locale fix: resolved '{locale}' -> '{cultureName}' -> '{threeLetter}'"
                    );
                    return threeLetter;
                }
            }

            PatchHelper.Log("Locale fix: no locale candidates resolved, fallback to 'eng'");
            return "eng";
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Locale fix: fallback to 'eng' due to: {ex.Message}");
            return "eng";
        }
    }

    private static bool TryResolveThreeLetterCulture(string locale, out string threeLetter)
    {
        threeLetter = null;
        var trimmed = locale?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        try
        {
            var culture = new CultureInfo(trimmed);
            threeLetter = culture.ThreeLetterISOLanguageName;
            return !string.IsNullOrWhiteSpace(threeLetter);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static IEnumerable<string> LocaleCandidates(string locale)
    {
        var raw = (locale ?? string.Empty).Trim().Replace('_', '-');

        if (string.IsNullOrWhiteSpace(raw))
        {
            yield return "eng";
            yield break;
        }

        var sanitized = StripExtension(raw, "-u-");
        sanitized = StripExtension(sanitized, "-x-");

        yield return sanitized;

        var firstToken = sanitized.Split('-')[0];
        if (!string.IsNullOrWhiteSpace(firstToken) && !string.Equals(firstToken, sanitized))
            yield return firstToken;

        var tokens = sanitized.Split('-');
        if (tokens.Length > 1)
            yield return tokens[0];

        yield return "eng";
    }

    private static string StripExtension(string locale, string extensionMarker)
    {
        var idx = locale.IndexOf(extensionMarker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? locale.Substring(0, idx) : locale;
    }
}
