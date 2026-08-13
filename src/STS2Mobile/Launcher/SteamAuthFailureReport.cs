using System;
using System.Net.Http;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Launcher;

internal readonly struct SteamAuthFailureReport
{
    private SteamAuthFailureReport(
        string category,
        string userMessage,
        string technicalMessage
    )
    {
        Category = category;
        UserMessage = userMessage;
        TechnicalMessage = technicalMessage;
    }

    internal string Category { get; }
    internal string UserMessage { get; }
    internal string TechnicalMessage { get; }

    internal static SteamAuthFailureReport From(Exception ex)
    {
        var root = ex.GetBaseException();
        var message = root.Message ?? string.Empty;

        if (root is TimeoutException or TaskCanceledException)
        {
            return Create(
                "timeout",
                "Steam sign-in timed out. Check the connection, keep the launcher open, then try again.",
                root
            );
        }

        if (root is HttpRequestException)
        {
            return Create(
                "network",
                "Steam sign-in could not reach Steam services. Check the connection or try again later.",
                root
            );
        }

        if (root is AuthenticationException || ContainsAny(message, "InvalidPassword", "Invalid password", "AccountLoginDenied", "AccountLogonDenied"))
        {
            return Create(
                "credentials",
                "Steam rejected the sign-in. Re-enter the Steam username, password, and the newest Steam Guard code if prompted.",
                root
            );
        }

        if (ContainsAny(message, "TwoFactor", "Steam Guard", "code", "AuthSession"))
        {
            return Create(
                "steam-guard",
                "Steam Guard did not complete. Use the newest Steam Guard code and submit it once.",
                root
            );
        }

        if (ContainsAny(message, "connect", "disconnect", "NoConnection", "ServiceUnavailable", "Busy", "RateLimit"))
        {
            return Create(
                "steam-connection",
                "Steam connection failed during sign-in. Wait a moment, then try signing in again.",
                root
            );
        }

        return Create(
            "unknown",
            "Steam sign-in failed. Try signing in again; if it repeats, attach a support report.",
            root
        );
    }

    private static SteamAuthFailureReport Create(
        string category,
        string userMessage,
        Exception root
    )
        => new(
            category,
            userMessage,
            $"{root.GetType().Name}: {root.Message}"
        );

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
