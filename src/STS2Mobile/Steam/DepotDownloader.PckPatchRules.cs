using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly string[] ProjectGodotSettingsToComment =
    {
        "SentryInit=\"*res://addons/sentry/SentryInit.gd\"",
        "FmodManager=\"*res://addons/fmod/FmodManager.gd\"",
    };

    private static readonly string[] ExtensionListEntriesToOverwrite =
    {
        "res://addons/fmod/fmod.gdextension",
        "res://addons/sentry/sentry.gdextension",
    };

    private static readonly (string Search, string Replacement)[] ProjectBinaryReplacements =
    {
        ("autoload/SentryInit", "disabled/SentryInit"),
        ("autoload/FmodManager", "disabled/FmodManager"),
    };

    private static readonly string[] GameSceneSettingsToComment =
    {
        "[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]",
        "[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]",
        "bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]",
        "script = ExtResource(\"3_xfu11\")",
        "[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]",
    };

    private static bool PatchProjectGodot(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "project.godot",
            PckPatchOperation.ProjectGodot
        );

    private static bool PatchExtensionList(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "extension_list.cfg",
            PckPatchOperation.ExtensionList
        );

    private static bool PatchProjectBinary(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "project.binary",
            PckPatchOperation.ProjectBinary
        );

    private static bool PatchGameScene(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "scenes/game.tscn",
            PckPatchOperation.GameScene
        );
}
