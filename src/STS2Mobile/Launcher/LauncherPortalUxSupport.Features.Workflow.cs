using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    private static void AddWorkflowFeatureReports(List<LauncherPortalUxFeature> features)
    {
        features.Add(new LauncherPortalUxFeature("Launcher stable destination navigation supported", StableDestinationNavigationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Home destination supported", HomeDestinationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Saves destination supported", SavesDestinationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Versions destination supported", VersionsDestinationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Mods destination supported", ModsDestinationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Help destination supported", HelpDestinationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher phone bottom navigation supported", PhoneBottomNavigationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher wide/foldable top navigation supported", WideFoldableTopNavigationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher destination-owned action groups supported", DestinationOwnedActionGroupsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher deterministic destination scroll reset supported", DeterministicDestinationScrollResetSupported));
        features.Add(new LauncherPortalUxFeature("Launcher dynamic task re-anchor removed", DynamicTaskReanchorRemoved));
        features.Add(new LauncherPortalUxFeature("Launcher touch-safe destination controls supported", TouchSafeDestinationControlsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Android display safe-area insets supported", AndroidSafeAreaInsetsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Android bottom-navigation safe-area spacer supported", AndroidBottomNavigationSafeAreaSpacerSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Android composition refresh supported", AndroidCompositionRefreshSupported));
    }
}
