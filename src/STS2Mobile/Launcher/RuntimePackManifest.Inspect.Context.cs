using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly struct RuntimePackManifestInspectionContext
{
    internal RuntimePackManifestInspectionContext(string manifestPath, GameIdentity expectedGameIdentity)
    {
        ManifestPath = manifestPath;
        ExpectedGameIdentity = expectedGameIdentity;
        ExpectedBranch = expectedGameIdentity?.Branch ?? SteamGameBranch.Public;
        AndroidAssemblyPath = Path.Combine(
            Path.GetDirectoryName(manifestPath) ?? string.Empty,
            RuntimePackManifest.AndroidAssemblyFileName
        );
        AndroidAssemblyExists = File.Exists(AndroidAssemblyPath);
    }

    internal RuntimePackManifestInspectionContext(string manifestPath, string expectedBranch)
        : this(manifestPath, expectedGameIdentity: null)
    {
        ExpectedBranch = SteamGameBranch.StorageIdentity(expectedBranch);
    }

    internal string ManifestPath { get; }
    internal string ExpectedBranch { get; }
    internal GameIdentity ExpectedGameIdentity { get; }
    internal string AndroidAssemblyPath { get; }
    internal bool AndroidAssemblyExists { get; }
}
