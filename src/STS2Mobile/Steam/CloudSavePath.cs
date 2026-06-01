namespace STS2Mobile.Steam;

internal static class CloudSavePath
{
    internal static string Canonicalize(string path)
        => path.Replace("user://", "").Replace("\\", "/");
}
