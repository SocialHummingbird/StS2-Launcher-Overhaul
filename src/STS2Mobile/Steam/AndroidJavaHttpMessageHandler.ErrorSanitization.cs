using System;
using System.Text;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static string? SanitizeBridgeError(string? message, Uri? requestUri)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        if (requestUri != null)
        {
            var rawUri = requestUri.ToString();
            if (!string.IsNullOrEmpty(rawUri))
                message = message.Replace(rawUri, SanitizeUri(requestUri), StringComparison.Ordinal);
        }

        return RedactUrlSuffixes(message);
    }

    private static string RedactUrlSuffixes(string message)
    {
        var builder = new StringBuilder(message.Length);
        var index = 0;

        while (index < message.Length)
        {
            var current = message[index];
            if ((current != '?' && current != '#') || !IsUrlLikeSuffixMarker(message, index))
            {
                builder.Append(current);
                index++;
                continue;
            }

            builder.Append(current).Append("<redacted>");
            index = SkipUrlSuffix(message, index + 1);

            if (index < message.Length)
            {
                builder.Append(message[index]);
                index++;
            }
        }

        return builder.ToString();
    }

    private static int SkipUrlSuffix(string message, int index)
    {
        while (index < message.Length)
        {
            var suffix = message[index];
            if (char.IsWhiteSpace(suffix) || suffix == '"' || suffix == '\'' || suffix == ')')
                break;

            index++;
        }

        return index;
    }

    private static bool IsUrlLikeSuffixMarker(string message, int markerIndex)
    {
        var tokenStart = markerIndex - 1;
        while (tokenStart >= 0)
        {
            var value = message[tokenStart];
            if (char.IsWhiteSpace(value) || value == '"' || value == '\'' || value == '(' || value == ')')
                break;

            tokenStart--;
        }

        var tokenPrefix = message.Substring(tokenStart + 1, markerIndex - tokenStart - 1);
        return tokenPrefix.StartsWith("/", StringComparison.Ordinal)
            || tokenPrefix.Contains("://", StringComparison.Ordinal)
            || tokenPrefix.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || tokenPrefix.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
