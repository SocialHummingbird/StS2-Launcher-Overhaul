namespace STS2Mobile.Steam;

internal static class ManagedFmodPckForms
{
    internal const string ProjectBinaryPath = "project.binary";
    internal const string ProjectGodotPath = "project.godot";
    internal const string ExtensionListPath = ".godot/extension_list.cfg";
    internal const string GameScenePath = "scenes/game.tscn";

    internal const string ProjectGodotSetting =
        "FmodManager=\"*res://addons/fmod/FmodManager.gd\"";
    internal const string ProjectBinaryAutoload = "autoload/FmodManager";
    internal const string DisabledProjectBinaryAutoload = "disabled/FmodManager";
    internal const string ExtensionListEntry = "res://addons/fmod/fmod.gdextension";

    internal const string DesktopBankPaths =
        "bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]";

    internal const string AndroidExternalBankPaths =
        "bank_paths = [\"/sdcard/sts2b/Master.strings.bank\", \"/sdcard/sts2b/Master.bank\", \"/sdcard/sts2b/sfx.bank\", \"/sdcard/sts2b/temp_sfx.bank\", \"/sdcard/sts2b/ambience.bank\"]";

    internal const string AndroidUserBankPaths =
        "bank_paths = [\"user://fmod_banks/Master.strings.bank\", \"user://fmod_banks/Master.bank\", \"user://fmod_banks/sfx.bank\", \"user://fmod_banks/temp_sfx.bank\", \"user://fmod_banks/ambience.bank\"]";

    internal static readonly string[] GameSceneEntries =
    {
        "[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]",
        "[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]",
        DesktopBankPaths,
        "script = ExtResource(\"3_xfu11\")",
        "[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]",
    };

    internal static string DisabledTextEntry(string entry)
        => ";" + entry.Substring(1);
}
