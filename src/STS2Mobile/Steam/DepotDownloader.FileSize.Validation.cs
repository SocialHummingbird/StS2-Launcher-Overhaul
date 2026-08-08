using System;
using System.Collections.Generic;
using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static void ValidateDownloadFileSizes(IEnumerable<DepotManifest.FileData> files)
    {
        foreach (var file in files)
        {
            if (file.TotalSize > (ulong)MaxDepotFileBytes)
            {
                throw new IOException(
                    $"Depot file is unexpectedly large for {file.FileName}: "
                        + $"{file.TotalSize} bytes"
                );
            }
        }
    }

    private static long ComputeTotalDownloadBytes(IEnumerable<DepotManifest.FileData> files)
    {
        long total = 0;
        foreach (var file in files)
        {
            try
            {
                total = checked(total + (long)file.TotalSize);
            }
            catch (OverflowException ex)
            {
                throw new IOException(
                    $"Depot download size is too large while adding {file.FileName}: "
                        + $"{file.TotalSize} bytes",
                    ex
                );
            }
        }

        return total;
    }
}
