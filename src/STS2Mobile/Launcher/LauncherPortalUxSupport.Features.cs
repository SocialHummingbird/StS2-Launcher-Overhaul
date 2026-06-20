using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    internal static LauncherPortalUxFeature[] FeatureReports => BuildFeatureReports();

    private static LauncherPortalUxFeature[] BuildFeatureReports()
    {
        var features = new List<LauncherPortalUxFeature>();
        AddStatusFeatureReports(features);
        AddWorkflowFeatureReports(features);
        AddAuthChromeFeatureReports(features);
        AddInstallCloudFeatureReports(features);
        AddDiagnosticsFeatureReports(features);
        return features.ToArray();
    }
}
