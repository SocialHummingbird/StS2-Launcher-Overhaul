using System;
using System.Text.RegularExpressions;

namespace STS2Mobile.Launcher;

internal static class LauncherGitHubIssue
{
    internal const int MaxUrlLength = 7000;
    internal const string NewIssueUrl = "https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/new";
    private const string Prefix = NewIssueUrl + "?template=bug_report.yml&title=%5BBUG%5D%20Launcher%20problem&evidence=";

    internal static string Redact(string log, string accountName, string dataDirectory)
    {
        var text = string.IsNullOrWhiteSpace(log) ? "No launcher log is available yet." : log;
        if (!string.IsNullOrWhiteSpace(dataDirectory))
        {
            text = text.Replace(dataDirectory, "[app data]", StringComparison.OrdinalIgnoreCase);
            text = text.Replace(dataDirectory.Replace('\\', '/'), "[app data]", StringComparison.OrdinalIgnoreCase);
        }
        // Drop complete credential-bearing lines, including JSON and query-string values.
        text = Regex.Replace(text,
            @"(?im)^.*(?:password|passwd|access[_ -]?token|refresh[_ -]?token|session[_ -]?(?:token|id|key)|authorization|bearer|cookie|steam[_ -]?guard(?:[_ -]?code)?|auth[_ -]?code|api[_ -]?key|secret|account[_ -]?name|username)\s*[""']?\s*[:=].*$",
            "[private authentication detail removed]");
        text = Regex.Replace(text, @"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", "[email]");
        text = Regex.Replace(text, @"\b7656119\d{10}\b", "[Steam ID]");
        text = Regex.Replace(text, @"(?i)([A-Z]:[\\/]Users[\\/]|/home/|/Users/)[^\\/\s]+", "$1[user]");
        if (!string.IsNullOrWhiteSpace(accountName))
            text = Regex.Replace(text, @"(?<![\w])" + Regex.Escape(accountName) + @"(?![\w])", "[account]", RegexOptions.IgnoreCase);
        return text;
    }

    internal static string BuildUrl(string redactedLog, bool clipboardAvailable = true)
    {
        var log = string.IsNullOrWhiteSpace(redactedLog) ? "No launcher log is available yet." : redactedLog;
        var full = Prefix + Uri.EscapeDataString(FormatEvidence(log, truncated: false, clipboardAvailable));
        if (full.Length <= MaxUrlLength)
            return full;

        // Preserve the overview and the most recent lines; budget encoded bytes, not characters.
        var count = Math.Min(log.Length, 3000);
        while (count > 0)
        {
            var head = Math.Min(500, count / 3);
            if (head > 0 && char.IsHighSurrogate(log[head - 1])) head--;
            var start = log.Length - (count - head);
            if (start < log.Length && char.IsLowSurrogate(log[start])) start++;
            var excerpt = log[..head] + "\n[... older log lines omitted ...]\n" + log[start..];
            var url = Prefix + Uri.EscapeDataString(FormatEvidence(excerpt, truncated: true, clipboardAvailable));
            if (url.Length <= MaxUrlLength)
                return url;
            count = count * 3 / 4;
        }
        return Prefix + Uri.EscapeDataString("The log is too large for this link. Use Create support report in Help for a full report.");
    }

    private static string FormatEvidence(string log, bool truncated, bool clipboardAvailable)
        => "Launcher log (automatic redaction applied; review before submitting).\n"
            + (truncated ? clipboardAvailable
                ? "Excerpt only: the full redacted log was copied to your clipboard. Paste it here if needed.\n"
                : "Excerpt only: clipboard unavailable. Use Create support report in Help for a full report.\n" : "")
            + "\n<details><summary>Launcher diagnostics</summary>\n\n<pre>"
            + System.Net.WebUtility.HtmlEncode(log)
            + "</pre>\n</details>";
}
