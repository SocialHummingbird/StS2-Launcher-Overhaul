namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    internal const string Model =
        "Task-led launcher with five stable destinations: Home, Saves, Versions, Mods, and Help.";

    internal const string HomeDestinationDescription =
        "Home keeps readiness, Start Game, Safe Start, and retry actions together without exposing save upload controls.";

    internal const string SavesDestinationDescription =
        "Saves keeps Pull before Push and preserves the locked reveal, arm, and explicit overwrite confirmation gates.";

    internal const string VersionsDestinationDescription =
        "Versions owns branch selection, update, repair, refresh, and cached-version cleanup controls.";

    internal const string SupportDestinationDescription =
        "Mods owns play mode and Workshop controls; Help owns recovery, diagnostics, error, report, and review-before-sharing log controls.";

    internal const string CurrentImplementation =
        "Phone layouts use persistent bottom navigation, while wide and foldable layouts use top navigation around a constrained content surface. "
        + "Each destination has a stable container; navigation changes visibility and resets that page to its origin without deferred task re-anchoring or ready-path reparenting. "
        + "Android launcher startup supports sensor rotation, display safe-area insets, bottom-system-bar spacing, and a short composition refresh for affected Qualcomm/Godot partial-damage paths. "
        + "The existing launcher controller events, Steam branch behavior, mod safety, and Steam Cloud Push gates remain unchanged.";

    internal const string ValidationBoundary =
        "Deterministic desktop fixtures and static contracts are validated. Exact final-build cover, inner-display, rotation, and real-game handoff evidence still requires an unlocked ARM64 device; Steam Cloud Push is outside this UI validation.";
}
