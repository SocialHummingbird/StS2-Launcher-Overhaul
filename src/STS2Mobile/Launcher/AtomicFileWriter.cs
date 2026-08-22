using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed class AtomicFileWriter
{
    private readonly Action<string, string> _beforeReplace;

    internal AtomicFileWriter(Action<string, string> beforeReplace = null)
    {
        _beforeReplace = beforeReplace;
    }

    internal void WriteAllBytes(string targetPath, ReadOnlySpan<byte> bytes)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("An atomic-write target path is required.", nameof(targetPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(targetPath));
        if (string.IsNullOrWhiteSpace(directory))
            throw new IOException($"Cannot resolve the directory for atomic-write target: {targetPath}.");

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp"
        );

        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.WriteThrough
            ))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            _beforeReplace?.Invoke(temporaryPath, targetPath);
            File.Move(temporaryPath, targetPath, overwrite: true);
            temporaryPath = null;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryPath))
            {
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch
                {
                    // A process interruption can leave a temporary file. Readers
                    // deliberately inspect only the final target filename.
                }
            }
        }
    }
}
