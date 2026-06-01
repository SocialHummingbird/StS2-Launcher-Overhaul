using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static bool ApplyPckEntryPatch(
        FileStream fs,
        long offset,
        long size,
        string label,
        Func<byte[], bool> applyPatch
    )
    {
        const long maxPatchablePckEntryBytes = 8L * 1024L * 1024L;

        if (offset < 0 || size < 0 || size > maxPatchablePckEntryBytes || offset + size > fs.Length)
        {
            PatchHelper.Log(
                $"PCK patching skipped for {label}: offset={offset}, size={size}, fileSize={fs.Length}"
            );
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

    private static bool ApplyReplacementPatches(
        byte[] content,
        IEnumerable<PckTextReplacement> replacements
    )
    {
        var patched = false;
        foreach (var replacement in replacements)
            patched |= replacement.Apply(content);

        return patched;
    }
}
