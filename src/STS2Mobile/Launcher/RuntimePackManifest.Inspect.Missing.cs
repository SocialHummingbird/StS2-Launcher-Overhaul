using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    internal static RuntimePackManifest NotInstalled(string path, string expectedBranch)
        => NotInstalled(new RuntimePackManifestInspectionContext(path, expectedBranch));

    private static RuntimePackManifest NotInstalled(RuntimePackManifestInspectionContext context)
        => new RuntimePackManifest(
            context.ManifestPath,
            context.ExpectedBranch,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            supportAssembliesDeclared: false,
            supportAssemblySha256Declared: false,
            0,
            0,
            0,
            string.Empty,
            generatedFromCleanDirectory: false,
            "not installed",
            exists: false,
            readable: false,
            androidAssemblyExists: false,
            context.AndroidAssemblyPath,
            "<missing>"
        );
}
