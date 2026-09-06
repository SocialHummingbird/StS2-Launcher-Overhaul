using System;
using System.Linq;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void GitHubIssueDraft()
    {
        var raw = "Build: 429\nAccountName: alice\nPassword=hide-me\n{\"refresh_token\":\"secret-token\"}\n"
            + "Authorization: Bearer private-token\nSteamGuard=12345\nalice@example.com 76561198012345678\n"
            + "C:\\Users\\alice\\game\\error.log\n/data/user/0/launcher/files/log.txt\nFailure: A&B + #? 😀 </pre>";
        var clean = LauncherGitHubIssue.Redact(raw, "alice", "/data/user/0/launcher/files");
        foreach (var privateValue in new[] { "alice", "hide-me", "secret-token", "private-token", "12345", "76561198012345678", "/data/user/0/launcher/files" })
            if (clean.Contains(privateValue, StringComparison.Ordinal))
                throw new InvalidOperationException("Private data survived redaction: " + privateValue);
        var url = LauncherGitHubIssue.BuildUrl(clean);
        if (!url.StartsWith(LauncherGitHubIssue.NewIssueUrl + "?template=bug_report.yml&", StringComparison.Ordinal)
            || !url.Contains("&evidence=", StringComparison.Ordinal))
            throw new InvalidOperationException("Draft did not target the existing bug form's evidence field.");
        var evidence = Uri.UnescapeDataString(url.Split("&evidence=")[1]);
        if (!evidence.Contains("Failure: A&amp;B + #?", StringComparison.Ordinal)
            || !evidence.Contains("&lt;/pre&gt;", StringComparison.Ordinal))
            throw new InvalidOperationException("Log punctuation or markup was not preserved safely.");
        var hugeLog = "Build: 429\n" + string.Concat(Enumerable.Repeat("古いログ 😀 &?#\n", 10000)) + "MOST RECENT FAILURE";
        url = LauncherGitHubIssue.BuildUrl(hugeLog);
        evidence = Uri.UnescapeDataString(url.Split("&evidence=")[1]);
        if (url.Length > LauncherGitHubIssue.MaxUrlLength || !evidence.Contains("MOST RECENT FAILURE", StringComparison.Ordinal)
            || !evidence.Contains("Excerpt only", StringComparison.Ordinal) || !evidence.Contains("Build: 429", StringComparison.Ordinal))
            throw new InvalidOperationException("Large log lost its overview, tail, truncation notice, or URL budget.");
        if (!Uri.UnescapeDataString(LauncherGitHubIssue.BuildUrl("")).Contains("No launcher log is available yet.", StringComparison.Ordinal))
            throw new InvalidOperationException("Missing logs did not have a useful fallback.");
        if (!LauncherGitHubIssue.Redact("Account: a\nFatal crash", "a", "").Contains("Fatal crash", StringComparison.Ordinal))
            throw new InvalidOperationException("Short account names corrupted unrelated log messages.");
        evidence = Uri.UnescapeDataString(LauncherGitHubIssue.BuildUrl(hugeLog, clipboardAvailable: false));
        if (!evidence.Contains("clipboard unavailable", StringComparison.Ordinal)
            || evidence.Contains("was copied", StringComparison.Ordinal))
            throw new InvalidOperationException("Draft incorrectly claimed a failed clipboard copy succeeded.");
    }
}
