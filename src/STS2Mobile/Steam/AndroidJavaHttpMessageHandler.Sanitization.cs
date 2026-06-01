using System;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static string SanitizeUri(Uri? uri)
    {
        if (uri == null)
            return "<no-uri>";

        if (uri.IsAbsoluteUri)
            return StripFragment(uri.GetLeftPart(UriPartial.Path));

        return StripUriSuffix(uri.ToString());
    }

    private static string StripUriSuffix(string raw)
    {
        var queryIndex = raw.IndexOf('?');
        var fragmentIndex = raw.IndexOf('#');
        var cutIndex = queryIndex >= 0 && fragmentIndex >= 0
            ? Math.Min(queryIndex, fragmentIndex)
            : queryIndex >= 0
                ? queryIndex
                : fragmentIndex;

        return cutIndex >= 0 ? raw[..cutIndex] : raw;
    }

    private static string StripFragment(string path)
    {
        var fragmentIndex = path.IndexOf('#');
        return fragmentIndex >= 0 ? path[..fragmentIndex] : path;
    }
}
