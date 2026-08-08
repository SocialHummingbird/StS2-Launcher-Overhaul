using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static FileReadResult ReadFileText(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
                return FileReadResult.Missing();

            return FileReadResult.Read(System.IO.File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            return FileReadResult.Failed(ex.Message);
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;

        return text.Substring(0, maxChars) + "\n<truncated>";
    }
}
