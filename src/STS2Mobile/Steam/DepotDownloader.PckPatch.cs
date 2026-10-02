using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using STS2Mobile.Launcher;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private void PatchGamePck(string pckPath)
    {
        RequireUpdatingBeforeInstalledMutation();
        PatchGamePck(pckPath, targetArm64: RuntimeInformation.ProcessArchitecture == Architecture.Arm64, enableArm64V2FmodManagers: false);
    }

    internal static void RepairGamePckForArm64V2(BranchInstallUpdate update, string pckPath)
    {
        if (update == null)
            throw new ArgumentNullException(nameof(update));
        update.RequireUpdatingBeforeInstalledMutation();
        PatchGamePck(pckPath, targetArm64: true, enableArm64V2FmodManagers: true);
    }

    private static void PatchGamePck(string pckPath, bool targetArm64, bool enableArm64V2FmodManagers)
    {
        const uint maxPckPathBytes = 4096;
        if (!File.Exists(pckPath))
            throw new FileNotFoundException("Cannot prepare the Android PCK because the downloaded PCK is missing.", pckPath);
        try
        {
            using var fs = new FileStream(pckPath, FileMode.Open, FileAccess.ReadWrite);
            using var reader = new BinaryReader(fs);
            uint magic = reader.ReadUInt32();
            if (magic != 0x43504447) // "GDPC"
                throw new InvalidDataException($"Downloaded PCK has invalid magic: {pckPath}");
            reader.ReadUInt32(); // format version
            reader.ReadUInt32(); // major
            reader.ReadUInt32(); // minor
            reader.ReadUInt32(); // patch
            uint flags = reader.ReadUInt32();
            long fileBase = reader.ReadInt64();
            long dirBase = reader.ReadInt64();
            fs.Seek(16 * 4, SeekOrigin.Current); // 16 reserved uint32s
            bool relativeOffsets = (flags & 0x02) != 0;
            fs.Position = dirBase;
            uint fileCount = reader.ReadUInt32();
            bool patched = false;
            for (uint i = 0; i < fileCount; i++)
            {
                uint pathLen = reader.ReadUInt32();
                if (pathLen == 0 || pathLen > maxPckPathBytes)
                {
                    throw new InvalidDataException($"Downloaded PCK contains invalid path length {pathLen}: {pckPath}");
                }

                byte[] pathBytes = reader.ReadBytes((int)pathLen);
                string path = Encoding.UTF8.GetString(pathBytes).TrimEnd('\0');
                long offset = reader.ReadInt64();
                long size = reader.ReadInt64();
                reader.ReadBytes(16); // MD5
                reader.ReadUInt32(); // flags
                long absOffset = relativeOffsets ? fileBase + offset : offset;
                patched |= PatchPckEntry(path, fs, absOffset, size, targetArm64, enableArm64V2FmodManagers);
            }

            fs.Flush(flushToDisk: true);
            if (patched)
                PatchHelper.Log("Patched game PCK: removed Android-incompatible plugin references");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PCK patching failed: {ex.Message}");
            throw new IOException($"Failed to prepare downloaded PCK for Android: {ex.Message}", ex);
        }
    }

    private static bool IsPckPath(string path, string expected)
    {
        return path == expected || path == $"res://{expected}";
    }

    private static bool PatchPckEntry(string path, FileStream fs, long offset, long size, bool targetArm64, bool enableArm64V2FmodManagers)
    {
        if (IsPckPath(path, "project.binary"))
        {
            return PatchProjectBinary(fs, offset, size, enableArm64V2FmodManagers);
        }

        if (IsPckPath(path, "project.godot"))
        {
            return PatchProjectGodot(fs, offset, size, enableArm64V2FmodManagers);
        }

        if (IsPckPath(path, ".godot/extension_list.cfg"))
            return PatchExtensionList(fs, offset, size);
        if (IsPckPath(path, "scenes/game.tscn"))
            return PatchGameScene(fs, offset, size, targetArm64);
        return false;
    }

    private static bool ApplyPckEntryPatch(FileStream fs, long offset, long size, string label, Func<byte[], bool> applyPatch)
    {
        const long maxPatchablePckEntryBytes = 8L * 1024L * 1024L;
        if (offset < 0 || size < 0 || size > maxPatchablePckEntryBytes || offset + size > fs.Length)
        {
            PatchHelper.Log($"PCK patching skipped for {label}: offset={offset}, size={size}, fileSize={fs.Length}");
            return false;
        }

        long savedPos = fs.Position;
        fs.Position = offset;
        var content = new byte[(int)size];
        fs.ReadExactly(content, 0, (int)size);
        if (!applyPatch(content))
        {
            fs.Position = savedPos;
            return false;
        }

        fs.Position = offset;
        fs.Write(content, 0, content.Length);
        fs.Position = savedPos;
        return true;
    }

    private static bool ApplyProjectSettingComments(byte[] content, IEnumerable<string> settings)
    {
        var patched = false;
        foreach (var setting in settings)
            patched |= CommentOutProjectSetting(content, setting);
        return patched;
    }

    private static bool ApplyEntryOverwrites(byte[] content, IEnumerable<string> entries)
    {
        var patched = false;
        foreach (var entry in entries)
            patched |= OverwriteEntryBytes(content, entry);
        return patched;
    }

    private static bool ApplyReplacementPatches(byte[] content, IEnumerable<(string Search, string Replacement)> replacements)
    {
        var patched = false;
        foreach (var replacement in replacements)
            patched |= ReplaceEntryBytes(content, replacement.Search, replacement.Replacement);
        return patched;
    }

    private static bool OverwriteEntryBytes(byte[] content, string entry)
    {
        var search = Encode(entry);
        int idx = FindBytes(content, search);
        if (idx < 0)
            return false;
        Fill(content, idx, search.Length, (byte)' ');
        return true;
    }

    private static bool ReplaceEntryBytes(byte[] content, string searchText, string replacementText)
    {
        var search = Encode(searchText);
        var replacement = Encode(replacementText);
        if (search.Length != replacement.Length)
            throw new InvalidOperationException($"PCK replacement length mismatch for {searchText}");
        var patched = false;
        int idx;
        while ((idx = FindBytes(content, search)) >= 0)
        {
            Array.Copy(replacement, 0, content, idx, replacement.Length);
            patched = true;
        }

        return patched;
    }

    private static string PadReplacement(string searchText, string replacementText)
    {
        var searchLength = Encode(searchText).Length;
        var replacementLength = Encode(replacementText).Length;
        if (replacementLength > searchLength)
            throw new InvalidOperationException($"PCK replacement too long for {replacementText}");
        return replacementText.PadRight(searchLength);
    }

    private static bool CommentOutProjectSetting(byte[] content, string setting)
    {
        var search = Encode(setting);
        int idx = FindBytes(content, search);
        if (idx < 0)
            return false;
        content[idx] = (byte)';';
        return true;
    }

    private static byte[] Encode(string value) => Encoding.UTF8.GetBytes(value);
    private static void Fill(byte[] content, int offset, int length, byte value)
    {
        for (var i = 0; i < length; i++)
            content[offset + i] = value;
    }

    private static int FindBytes(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return i;
        }

        return -1;
    }

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
        (ManagedFmodPckForms.DisabledTextEntry(ManagedFmodPckForms.ProjectGodotSetting), ManagedFmodPckForms.ProjectGodotSetting),
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
        (ManagedFmodPckForms.ProjectBinaryAutoload, ManagedFmodPckForms.DisabledProjectBinaryAutoload),
    };
    private static readonly (string Search, string Replacement)[] Arm64FmodProjectBinaryRestorations =
    {
        (ManagedFmodPckForms.DisabledProjectBinaryAutoload, ManagedFmodPckForms.ProjectBinaryAutoload),
    };
    private static readonly (string Search, string Replacement)[] FmodExtensionListRestorations =
    {
        (SpacesFor(ManagedFmodPckForms.ExtensionListEntry), ManagedFmodPckForms.ExtensionListEntry),
    };
    private static readonly string[] GameSceneSettingsToPatch = ManagedFmodPckForms.GameSceneEntries;
    private static readonly (string Search, string Replacement)[] GameSceneSettingRestorations = GameSceneSettingsToPatch.Select(setting => (ManagedFmodPckForms.DisabledTextEntry(setting), setting)).ToArray();
    private static readonly (string Search, string Replacement)[] Arm64FmodBankPathReplacements =
    {
        (PadReplacement(ManagedFmodPckForms.DesktopBankPaths, ManagedFmodPckForms.AndroidExternalBankPaths), ManagedFmodPckForms.DesktopBankPaths),
        (PadReplacement(ManagedFmodPckForms.DesktopBankPaths, ManagedFmodPckForms.AndroidUserBankPaths), ManagedFmodPckForms.DesktopBankPaths),
    };
    private static string SpacesFor(string value) => new(' ', value.Length);
    private static bool PatchProjectGodot(FileStream fs, long offset, long size, bool enableArm64V2FmodManagers) => ApplyPckEntryPatch(fs, offset, size, "project.godot", content => ApplyProjectSettingComments(content, ProjectGodotSettingsToComment) | (enableArm64V2FmodManagers ? ApplyReplacementPatches(content, Arm64FmodProjectGodotRestorations) : ApplyProjectSettingComments(content, X86FmodProjectGodotSettingsToComment)));
    private static bool PatchExtensionList(FileStream fs, long offset, long size) => ApplyPckEntryPatch(fs, offset, size, "extension_list.cfg", content => ApplyReplacementPatches(content, FmodExtensionListRestorations) | ApplyEntryOverwrites(content, ExtensionListEntriesToOverwrite));
    private static bool PatchProjectBinary(FileStream fs, long offset, long size, bool enableArm64V2FmodManagers) => ApplyPckEntryPatch(fs, offset, size, "project.binary", content => ApplyReplacementPatches(content, ProjectBinaryReplacements) | (enableArm64V2FmodManagers ? ApplyReplacementPatches(content, Arm64FmodProjectBinaryRestorations) : ApplyReplacementPatches(content, X86FmodProjectBinaryReplacements)));
    private static bool PatchGameScene(FileStream fs, long offset, long size, bool targetArm64) => ApplyPckEntryPatch(fs, offset, size, "scenes/game.tscn", content => targetArm64 ? ApplyReplacementPatches(content, GameSceneSettingRestorations) | ApplyReplacementPatches(content, Arm64FmodBankPathReplacements) : ApplyProjectSettingComments(content, GameSceneSettingsToPatch));
}
