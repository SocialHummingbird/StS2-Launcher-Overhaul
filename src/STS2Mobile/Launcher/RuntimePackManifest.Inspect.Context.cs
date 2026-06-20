using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly struct RuntimePackManifestInspectionContext
{
    internal RuntimePackManifestInspectionContext(string manifestPath, string expectedBranch)
    {
        ManifestPath = manifestPath;
        ExpectedBranch = SteamGameBranch.Normalize(expectedBranch);
        AndroidAssemblyPath = Path.Combine(
            Path.GetDirectoryName(manifestPath) ?? string.Empty,
            RuntimePackManifest.AndroidAssemblyFileName
        );
        AndroidAssemblyExists = File.Exists(AndroidAssemblyPath);
    }

    internal string ManifestPath { get; }
    internal string ExpectedBranch { get; }
    internal string AndroidAssemblyPath { get; }
    internal bool AndroidAssemblyExists { get; }
}
