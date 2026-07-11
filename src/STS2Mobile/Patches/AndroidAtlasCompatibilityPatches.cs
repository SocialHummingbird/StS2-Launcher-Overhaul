using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;

namespace STS2Mobile.Patches;

// The Steam PCK contains desktop-compressed atlas pages. GPUs without BPTC/S3TC
// support expand those pages to RGBA8, so loading every atlas at startup can
// add hundreds of MiB before the first main-menu frame settles.
internal static class AndroidAtlasCompatibilityPatches
{
    private const string AtlasSpritePrefix = "res://images/atlases/";
    private const string AtlasSpriteSeparator = ".sprites/";
    private const string AtlasSpriteSuffix = ".tres";

    private static readonly HashSet<string> LoggedFallbackAtlases = new(StringComparer.Ordinal);
    private static readonly object LogLock = new();

    internal static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(AtlasManager),
            nameof(AtlasManager.LoadAllAtlases),
            prefix: PatchHelper.Method(
                typeof(AndroidAtlasCompatibilityPatches),
                nameof(LoadAllAtlasesPrefix)
            )
        );
        PatchHelper.Patch(
            harmony,
            typeof(AtlasResourceLoader),
            nameof(AtlasResourceLoader._Exists),
            prefix: PatchHelper.Method(
                typeof(AndroidAtlasCompatibilityPatches),
                nameof(AtlasResourceExistsPrefix)
            )
        );
        PatchHelper.Patch(
            harmony,
            typeof(AtlasResourceLoader),
            nameof(AtlasResourceLoader._Load),
            prefix: PatchHelper.Method(
                typeof(AndroidAtlasCompatibilityPatches),
                nameof(AtlasResourceLoadPrefix)
            )
        );
    }

    private static bool LoadAllAtlasesPrefix()
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        PatchHelper.Log(
            "[AndroidAtlasCompat] Skipping eager AtlasManager.LoadAllAtlases; "
            + "non-essential atlases will load on demand"
        );
        return false;
    }

    private static bool AtlasResourceExistsPrefix(string path, ref bool __result)
    {
        if (!OperatingSystem.IsAndroid() || !TryResolveFallback(path, out var fallback))
            return true;

        __result = true;
        LogFallback(path, fallback);
        return false;
    }

    private static bool AtlasResourceLoadPrefix(string path, ref Variant __result)
    {
        if (!OperatingSystem.IsAndroid() || !TryResolveFallback(path, out var fallback))
            return true;

        var texture = ResourceLoader.Load<Texture2D>(
            fallback,
            null,
            ResourceLoader.CacheMode.Reuse
        );
        if (texture == null)
        {
            PatchHelper.Log(
                $"[AndroidAtlasCompat] Individual fallback load failed for {path}: {fallback}; "
                + "falling back to the source atlas loader"
            );
            return true;
        }

        __result = Variant.From<Texture2D>(texture);
        LogFallback(path, fallback);
        return false;
    }

    private static bool TryResolveFallback(string path, out string fallback)
    {
        fallback = null;
        if (!TryParseSpritePath(path, out var atlasName, out var spriteName))
            return false;
        if (AtlasManager.IsAtlasLoaded(atlasName))
            return false;

        foreach (var candidate in FallbackCandidates(atlasName, spriteName))
        {
            if (!ResourceLoader.Exists(candidate))
                continue;

            fallback = candidate;
            return true;
        }

        // The reporter PCK has an individual import for every card atlas sprite.
        // Keep a final guard for future game builds so one new card cannot pull
        // all three desktop-compressed card pages into memory on Android.
        if (atlasName == "card_atlas")
        {
            const string missingCard = "res://images/packed/card_portraits/beta.png";
            if (ResourceLoader.Exists(missingCard))
            {
                fallback = missingCard;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseSpritePath(
        string path,
        out string atlasName,
        out string spriteName
    )
    {
        atlasName = null;
        spriteName = null;
        if (string.IsNullOrEmpty(path)
            || !path.StartsWith(AtlasSpritePrefix, StringComparison.Ordinal)
            || !path.EndsWith(AtlasSpriteSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        int separator = path.IndexOf(
            AtlasSpriteSeparator,
            AtlasSpritePrefix.Length,
            StringComparison.Ordinal
        );
        if (separator <= AtlasSpritePrefix.Length)
            return false;

        int spriteStart = separator + AtlasSpriteSeparator.Length;
        int spriteLength = path.Length - spriteStart - AtlasSpriteSuffix.Length;
        if (spriteLength <= 0)
            return false;

        atlasName = path.Substring(
            AtlasSpritePrefix.Length,
            separator - AtlasSpritePrefix.Length
        );
        spriteName = path.Substring(spriteStart, spriteLength);
        return true;
    }

    private static IEnumerable<string> FallbackCandidates(
        string atlasName,
        string spriteName
    )
    {
        switch (atlasName)
        {
            case "relic_atlas":
            case "relic_outline_atlas":
                yield return $"res://images/relics/{spriteName}.png";
                yield return $"res://images/relics/beta/{spriteName}.png";
                break;
            case "power_atlas":
                yield return $"res://images/powers/{spriteName}.png";
                yield return $"res://images/powers/beta/{spriteName}.png";
                break;
            case "card_atlas":
                yield return $"res://images/packed/card_portraits/{spriteName}.png";
                int separator = spriteName.LastIndexOf('/');
                if (separator > 0)
                {
                    yield return "res://images/packed/card_portraits/"
                        + spriteName.Substring(0, separator)
                        + "/beta/"
                        + spriteName.Substring(separator + 1)
                        + ".png";
                }
                break;
            case "potion_atlas":
            case "potion_outline_atlas":
                yield return $"res://images/potions/{spriteName}.png";
                break;
        }
    }

    private static void LogFallback(string path, string fallback)
    {
        if (!TryParseSpritePath(path, out var atlasName, out _))
            return;

        lock (LogLock)
        {
            if (!LoggedFallbackAtlases.Add(atlasName))
                return;
        }

        PatchHelper.Log(
            $"[AndroidAtlasCompat] Using individual texture fallback for {atlasName}: "
            + $"{path} -> {fallback}"
        );
    }
}
