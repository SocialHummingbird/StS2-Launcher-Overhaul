using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore
{
    private string FullPath(string path)
    {
        var canonical = CloudSavePath.Relative(path);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, canonical));
        if (!IsInsideBasePath(fullPath))
            throw new IOException($"Save path escapes app data directory: {path}");

        return fullPath;
    }

    private void EnsureParentDirectory(string fullPath)
    {
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
    }

    private bool IsInsideBasePath(string fullPath)
    {
        return string.Equals(fullPath, _basePath, StringComparison.Ordinal)
            || fullPath.StartsWith(_basePathWithSeparator, StringComparison.Ordinal);
    }
}
