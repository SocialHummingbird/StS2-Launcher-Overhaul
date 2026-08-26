using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string FmodProjectGodotSetting = "FmodManager=\"*res://addons/fmod/FmodManager.gd\"";
    private const string FmodExtensionListEntry = "res://addons/fmod/fmod.gdextension";

    private static readonly string[] ProjectGodotSettingsToComment =
    {
        "SentryInit=\"*res://addons/sentry/SentryInit.gd\"",
    };

    private static readonly string[] X86FmodProjectGodotSettingsToComment =
    {
        FmodProjectGodotSetting,
    };

    private static readonly (string Search, string Replacement)[] Arm64FmodProjectGodotRestorations =
    {
        (";" + FmodProjectGodotSetting.Substring(1), FmodProjectGodotSetting),
    };

    private static readonly string[] ExtensionListEntriesToOverwrite =
    {
        "res://addons/sentry/sentry.gdextension",
    };

    private static readonly (string Search, string Replacement)[] ProjectBinaryReplacements =
    {
        ("autoload/SentryInit", "disabled/SentryInit"),
    };

    private static readonly (string Search, string Replacement)[] X86FmodProjectBinaryReplacements =
    {
        ("autoload/FmodManager", "disabled/FmodManager"),
    };

    private static readonly (string Search, string Replacement)[] Arm64FmodProjectBinaryRestorations =
    {
        ("disabled/FmodManager", "autoload/FmodManager"),
    };

    private static readonly (string Search, string Replacement)[] FmodExtensionListRestorations =
    {
        (SpacesFor(FmodExtensionListEntry), FmodExtensionListEntry),
    };

    private static readonly string[] GameSceneSettingsToPatch =
    {
        "[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]",
        "[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]",
        FmodDesktopBankPaths,
        "script = ExtResource(\"3_xfu11\")",
        "[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]",
    };

    private const string FmodDesktopBankPaths =
        "bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]";

    private const string FmodAndroidExternalBankPaths =
        "bank_paths = [\"/sdcard/sts2b/Master.strings.bank\", \"/sdcard/sts2b/Master.bank\", \"/sdcard/sts2b/sfx.bank\", \"/sdcard/sts2b/temp_sfx.bank\", \"/sdcard/sts2b/ambience.bank\"]";

    private const string FmodAndroidUserBankPaths =
        "bank_paths = [\"user://fmod_banks/Master.strings.bank\", \"user://fmod_banks/Master.bank\", \"user://fmod_banks/sfx.bank\", \"user://fmod_banks/temp_sfx.bank\", \"user://fmod_banks/ambience.bank\"]";

    private static readonly (string Search, string Replacement)[] GameSceneSettingRestorations =
        GameSceneSettingsToPatch.Select(setting => (";" + setting.Substring(1), setting)).ToArray();

    private static readonly (string Search, string Replacement)[] Arm64FmodBankPathReplacements =
    {
        (PadReplacement(FmodDesktopBankPaths, FmodAndroidExternalBankPaths), FmodDesktopBankPaths),
        (PadReplacement(FmodDesktopBankPaths, FmodAndroidUserBankPaths), FmodDesktopBankPaths),
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
                | (RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                    ? ApplyReplacementPatches(content, Arm64FmodProjectGodotRestorations)
                    : ApplyProjectSettingComments(content, X86FmodProjectGodotSettingsToComment))
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
                | (RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                    ? ApplyReplacementPatches(content, Arm64FmodProjectBinaryRestorations)
                    : ApplyReplacementPatches(content, X86FmodProjectBinaryReplacements))
        );

    private static bool PatchGameScene(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "scenes/game.tscn",
            content => RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? ApplyReplacementPatches(content, GameSceneSettingRestorations)
                  | ApplyReplacementPatches(content, Arm64FmodBankPathReplacements)
                : ApplyProjectSettingComments(content, GameSceneSettingsToPatch)
        );
}
