using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    internal static RuntimePackManifest Inspect(
        string path,
        GameIdentity expectedGameIdentity
    )
    {
        if (expectedGameIdentity == null)
            throw new ArgumentNullException(nameof(expectedGameIdentity));

        var context = new RuntimePackManifestInspectionContext(path, expectedGameIdentity);

        if (!File.Exists(context.ManifestPath))
        {
            return NotInstalled(context);
        }

        try
        {
            return InspectReadable(context);
        }
        catch (Exception ex)
        {
            return Unreadable(context, ex);
        }
    }
}
