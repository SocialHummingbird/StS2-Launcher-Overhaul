using STS2Mobile.Patches;

namespace STS2Mobile;

internal static partial class StartupPatchOrchestrator
{
    private static readonly PatchGroup[] Groups = new PatchGroup[]
    {
        Core(),
        Gameplay(),
        Optional()
    };

    private static PatchGroup Core()
        => new(
            "core",
            true,
            new PatchStep[]
            {
                new("Platform compatibility", PlatformPatches.Apply),
                new("Controller input compatibility", ControllerInputPatches.Apply),
                new("Model DB bootstrap", ModelDbInitPatch.Apply),
                new("Launcher startup gate", LauncherPatches.Apply),
            }
        );

    private static PatchGroup Gameplay()
        => new(
            "gameplay",
            false,
            new PatchStep[]
            {
                new("Settings compatibility", SettingsPatches.Apply),
                new("Font substitution", FontSubstitutionPatches.Apply),
                new("UI scaling", UiScalePatches.Apply),
                new("Mobile layout", MobileLayoutPatches.Apply),
                new("Run history asset fallback", RunHistoryAssetPatches.Apply),
                new("Dev console Android fallback", DevConsolePatches.Apply),
                new("Event layout", EventLayoutPatches.Apply),
                new("Merchant layout", MerchantLayoutPatches.Apply),
                new("App lifecycle", AppLifecyclePatches.Apply),
                new("Touch input", TouchInputPatches.Apply),
                new("Card reward", CardRewardPatches.Apply),
                new("Early access disclaimer", EarlyAccessDisclaimerPatches.Apply),
                new("Combat background", CombatBackgroundPatches.Apply),
            }
        );

    private static PatchGroup Optional()
        => new(
            "optional",
            false,
            new PatchStep[]
            {
                new("LAN multiplayer", LanMultiplayerPatcher.Apply),
                new("Mod loader integration", ModLoaderPatches.Apply),
                new("Save diagnostics", SaveDiagnosticPatches.Apply),
            }
        );
}
