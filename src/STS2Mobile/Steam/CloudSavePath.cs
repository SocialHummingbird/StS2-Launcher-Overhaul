namespace STS2Mobile.Steam;

internal static class CloudSavePath
{
    internal static string Canonicalize(string path)
        => path.Replace("user://", "").Replace("\\", "/");

    internal static string Relative(string? path)
        => Canonicalize(path ?? string.Empty).TrimStart('/');
}
