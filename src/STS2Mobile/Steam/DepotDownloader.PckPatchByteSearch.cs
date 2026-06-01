using System;
using System.Text;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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
}
