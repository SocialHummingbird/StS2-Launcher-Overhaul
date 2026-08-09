#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static class CancellableAtomicFile
{
    internal static Task WriteAllBytesAsync(
        string destinationPath,
        byte[] bytes,
        bool overwrite,
        CancellationToken cancellationToken
    )
        => WriteAsync(
            destinationPath,
            overwrite,
            (stagingPath, token) =>
                File.WriteAllBytesAsync(stagingPath, bytes, token),
            cancellationToken
        );

    internal static Task WriteAllTextAsync(
        string destinationPath,
        string content,
        bool overwrite,
        CancellationToken cancellationToken
    )
        => WriteAsync(
            destinationPath,
            overwrite,
            (stagingPath, token) =>
                File.WriteAllTextAsync(stagingPath, content, token),
            cancellationToken
        );

    internal static async Task WriteAsync(
        string destinationPath,
        bool overwrite,
        Func<string, CancellationToken, Task> writeStaging,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);
        ArgumentNullException.ThrowIfNull(writeStaging);
        cancellationToken.ThrowIfCancellationRequested();

        var parent = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        var stagingPath =
            $"{destinationPath}.sts2-atomic-{Guid.NewGuid():N}.tmp";
        try
        {
            await writeStaging(stagingPath, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stagingPath, destinationPath, overwrite);
        }
        finally
        {
            if (File.Exists(stagingPath))
                File.Delete(stagingPath);
        }
    }
}
