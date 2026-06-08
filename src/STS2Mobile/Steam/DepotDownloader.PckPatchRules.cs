using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string FmodProjectGodotSetting = "FmodManager=\"*res://addons/fmod/FmodManager.gd\"";
    private const string FmodExtensionListEntry = "res://addons/fmod/fmod.gdextension";

    private static readonly string[] ProjectGodotSettingsToComment =
    {
        "SentryInit=\"*res://addons/sentry/SentryInit.gd\"",
        FmodProjectGodotSetting,
    };

    private static readonly string[] ExtensionListEntriesToOverwrite =
    {
        "res://addons/sentry/sentry.gdextension",
    };

    private static readonly (string Search, string Replacement)[] ProjectBinaryReplacements =
    {
        ("autoload/SentryInit", "disabled/SentryInit"),
        ("autoload/FmodManager", "disabled/FmodManager"),
    };

    private static readonly (string Search, string Replacement)[] FmodExtensionListRestorations =
    {
        (SpacesFor(FmodExtensionListEntry), FmodExtensionListEntry),
    };

    private static readonly string[] GameSceneSettingsToComment =
    {
        "[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]",
        "[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]",
        "bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]",
        "script = ExtResource(\"3_xfu11\")",
        "[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]",
    };

    private static string SpacesFor(string value) => new(' ', value.Length);

    private static bool PatchProjectGodot(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "project.godot",
            content =>
                ApplyProjectSettingComments(content, ProjectGodotSettingsToComment)
        );

    private static bool PatchExtensionList(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "extension_list.cfg",
            content =>
                ApplyReplacementPatches(content, FmodExtensionListRestorations)
                | ApplyEntryOverwrites(content, ExtensionListEntriesToOverwrite)
        );

    private static bool PatchProjectBinary(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "project.binary",
            content =>
                ApplyReplacementPatches(content, ProjectBinaryReplacements)
        );

    private static bool PatchGameScene(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "scenes/game.tscn",
            content => ApplyProjectSettingComments(content, GameSceneSettingsToComment)
        );
}
