namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static string FormatBytes(long bytes)
    {
        const long kilobyte = 1024L;
        const long megabyte = kilobyte * 1024L;
        const long gigabyte = megabyte * 1024L;

        if (bytes >= gigabyte)
            return $"{bytes / (double)gigabyte:F1} GB";

        if (bytes >= megabyte)
            return $"{bytes / (double)megabyte:F1} MB";

        if (bytes >= kilobyte)
            return $"{bytes / (double)kilobyte:F1} KB";

        return $"{bytes} B";
    }
}
