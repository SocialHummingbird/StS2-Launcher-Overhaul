using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    internal static RuntimePackManifest Inspect(
        string path,
        string expectedBranch,
        string selectedPckSha256,
        string selectedSourceAssemblySha256,
        string selectedPckPath
    )
    {
        var context = new RuntimePackManifestInspectionContext(path, expectedBranch);

        if (!File.Exists(context.ManifestPath))
        {
            return NotInstalled(context);
        }

        try
        {
            return InspectReadable(
                context,
                selectedPckSha256,
                selectedSourceAssemblySha256,
                selectedPckPath
            );
        }
        catch (Exception ex)
        {
            return Unreadable(context, ex);
        }
    }
}
