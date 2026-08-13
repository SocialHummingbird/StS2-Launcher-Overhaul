using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed class SteamLogonFailedException : InvalidOperationException
{
    internal SteamLogonFailedException(EResult result)
        : base($"Steam logon failed: {result}")
    {
        Result = result;
    }

    internal EResult Result { get; }

    internal bool RequiresNewAuthentication
        => Result is EResult.InvalidPassword
            or EResult.AccountNotFound
            or EResult.Revoked
            or EResult.Expired
            or EResult.PasswordUnset
            or EResult.AccountLogonDenied
            or EResult.InvalidLoginAuthCode
            or EResult.AccountLogonDeniedNoMail
            or EResult.ExpiredLoginAuthCode
            or EResult.AccountLogonDeniedVerifiedEmailRequired
            or EResult.RequirePasswordReEntry
            or EResult.AccountLoginDeniedNeedTwoFactor
            or EResult.TwoFactorCodeMismatch
            or EResult.TwoFactorActivationCodeMismatch
            or EResult.CachedCredentialInvalid;
}
