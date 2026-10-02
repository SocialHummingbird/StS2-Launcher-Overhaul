using System.Collections;
using System.Linq;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    // The Android NInputManager.Init replacement must set the hotkey maps this
    // game build actually reads, on both the desktop and publicized assemblies.
    private static void KeyboardMapTargetsMatchGameInputManager()
    {
        using var publicized = new PublicizedGameAssembly();
        foreach (var path in new[] { ReferenceGameAssemblyPath(), publicized.AssemblyPath })
        {
            var fields = RunInGameAssemblyContext(path, (game, launcher) =>
            {
                var targets = (IEnumerable)InvokeLauncherStatic(
                    launcher,
                    "STS2Mobile.Patches.ControllerInputPatches",
                    "ResolveKeyboardMapTargets",
                    game.GetType("MegaCrit.Sts2.Core.Nodes.CommonUi.NInputManager", throwOnError: true)!,
                    game.GetType("MegaCrit.Sts2.Core.Saves.SettingsSave", throwOnError: true)!
                )!;
                return string.Join(",", targets.Cast<object>()
                    .Select(target => (string)target.GetType().GetProperty("FieldName")!.GetValue(target)!));
            });

            Equal("_mKbInputMap,_fKbInputMap", fields,
                "Both current hotkey maps must be initialized, not the removed _keyboardInputMap.");
        }
    }
}
