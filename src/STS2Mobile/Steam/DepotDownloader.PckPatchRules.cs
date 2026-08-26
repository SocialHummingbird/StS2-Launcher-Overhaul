using System.IO;
using System.Linq;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly string[] ProjectGodotSettingsToComment =
    {
        "SentryInit=\"*res://addons/sentry/SentryInit.gd\"",
    };

    private static readonly string[] X86FmodProjectGodotSettingsToComment =
    {
        ManagedFmodPckForms.ProjectGodotSetting,
    };

    private static readonly (string Search, string Replacement)[] Arm64FmodProjectGodotRestorations =
    {
        (
            ManagedFmodPckForms.DisabledTextEntry(
                ManagedFmodPckForms.ProjectGodotSetting
            ),
            ManagedFmodPckForms.ProjectGodotSetting
        ),
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
        (
            ManagedFmodPckForms.ProjectBinaryAutoload,
            ManagedFmodPckForms.DisabledProjectBinaryAutoload
        ),
    };

    private static readonly (string Search, string Replacement)[] Arm64FmodProjectBinaryRestorations =
    {
        (
            ManagedFmodPckForms.DisabledProjectBinaryAutoload,
            ManagedFmodPckForms.ProjectBinaryAutoload
        ),
    };

    private static readonly (string Search, string Replacement)[] FmodExtensionListRestorations =
    {
        (
            SpacesFor(ManagedFmodPckForms.ExtensionListEntry),
            ManagedFmodPckForms.ExtensionListEntry
        ),
    };

    private static readonly string[] GameSceneSettingsToPatch =
        ManagedFmodPckForms.GameSceneEntries;

    private static readonly (string Search, string Replacement)[] GameSceneSettingRestorations =
        GameSceneSettingsToPatch.Select(setting => (
            ManagedFmodPckForms.DisabledTextEntry(setting),
            setting
        )).ToArray();

    private static readonly (string Search, string Replacement)[] Arm64FmodBankPathReplacements =
    {
        (
            PadReplacement(
                ManagedFmodPckForms.DesktopBankPaths,
                ManagedFmodPckForms.AndroidExternalBankPaths
            ),
            ManagedFmodPckForms.DesktopBankPaths
        ),
        (
            PadReplacement(
                ManagedFmodPckForms.DesktopBankPaths,
                ManagedFmodPckForms.AndroidUserBankPaths
            ),
            ManagedFmodPckForms.DesktopBankPaths
        ),
    };

    private static string SpacesFor(string value) => new(' ', value.Length);

    private static bool PatchProjectGodot(
        FileStream fs,
        long offset,
        long size,
        bool enableArm64V2FmodManagers
    )
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "project.godot",
            content =>
                ApplyProjectSettingComments(content, ProjectGodotSettingsToComment)
                | (enableArm64V2FmodManagers
                    ? ApplyReplacementPatches(
                        content,
                        Arm64FmodProjectGodotRestorations
                    )
                    : ApplyProjectSettingComments(
                        content,
                        X86FmodProjectGodotSettingsToComment
                    ))
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

    private static bool PatchProjectBinary(
        FileStream fs,
        long offset,
        long size,
        bool enableArm64V2FmodManagers
    )
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "project.binary",
            content =>
                ApplyReplacementPatches(content, ProjectBinaryReplacements)
                | (enableArm64V2FmodManagers
                    ? ApplyReplacementPatches(
                        content,
                        Arm64FmodProjectBinaryRestorations
                    )
                    : ApplyReplacementPatches(
                        content,
                        X86FmodProjectBinaryReplacements
                    ))
        );

    private static bool PatchGameScene(
        FileStream fs,
        long offset,
        long size,
        bool targetArm64
    )
        => ApplyPckEntryPatch(
            fs,
            offset,
            size,
            "scenes/game.tscn",
            content => targetArm64
                ? ApplyReplacementPatches(content, GameSceneSettingRestorations)
                  | ApplyReplacementPatches(content, Arm64FmodBankPathReplacements)
                : ApplyProjectSettingComments(content, GameSceneSettingsToPatch)
        );
}
