namespace STS2Mobile.Launcher.Sections;

internal static partial class CompactButtonDetailLabels
{
    private static bool TrySplitText(string text, out string title, out string detail)
    {
        title = text ?? "";
        detail = "";
        var separator = title.IndexOf('\n');
        if (separator < 0)
            return false;

        detail = title[(separator + 1)..].Trim();
        title = title[..separator].Trim();
        return title.Length > 0 && detail.Length > 0;
    }
}
