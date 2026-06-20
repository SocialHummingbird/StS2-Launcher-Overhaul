using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static RuntimePackManifest Unreadable(
        RuntimePackManifestInspectionContext context,
        Exception exception
    )
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
            $"unreadable: {exception.GetType().Name}",
            exists: true,
            readable: false,
            context.AndroidAssemblyExists,
            context.AndroidAssemblyPath,
            context.AndroidAssemblyExists ? "<not inspected>" : "<missing>"
        );
}
