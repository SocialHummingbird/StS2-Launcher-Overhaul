using System;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed record GameIdentity
{
    internal const int SchemaVersion = 1;

    internal GameIdentity(
        string branch,
        string installGeneration,
        string pckSha256,
        string sourceAssemblySha256
    )
    {
        Branch = SteamGameBranch.StorageIdentity(branch);
        InstallGeneration = RequireSha256(installGeneration, nameof(installGeneration));
        PckSha256 = RequireSha256(pckSha256, nameof(pckSha256));
        SourceAssemblySha256 = RequireSha256(sourceAssemblySha256, nameof(sourceAssemblySha256));
    }

    internal string Branch { get; }
    internal string InstallGeneration { get; }
    internal string PckSha256 { get; }
    internal string SourceAssemblySha256 { get; }

    internal string Id
    {
        get
        {
            var canonical = string.Join(
                "\n",
                "game-identity-v1",
                $"branch={Branch}",
                $"installGeneration={InstallGeneration}",
                $"pckSha256={PckSha256}",
                $"sourceAssemblySha256={SourceAssemblySha256}"
            );
            return Convert.ToHexString(
                AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(canonical))
            ).ToLowerInvariant();
        }
    }

    private static string RequireSha256(string value, string parameterName)
    {
        if (!IsSha256(value))
            throw new ArgumentException("A complete SHA-256 value is required.", parameterName);

        return value.ToLowerInvariant();
    }

    internal static bool IsSha256(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            return false;

        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
                return false;
        }

        return true;
    }

    internal static bool TryCreate(
        string branch,
        string installGeneration,
        string pckSha256,
        string sourceAssemblySha256,
        out GameIdentity identity
    )
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(branch)
            || !IsSha256(installGeneration)
            || !IsSha256(pckSha256)
            || !IsSha256(sourceAssemblySha256))
        {
            return false;
        }

        identity = new GameIdentity(
            branch,
            installGeneration,
            pckSha256,
            sourceAssemblySha256
        );
        return true;
    }
}

internal enum GameIdentityFailureKind
{
    MissingFile,
    UnreadableFile,
    InvalidPck,
    InvalidInstallGeneration,
    IncompleteInstall,
    AmbiguousSourceAssembly,
    FileChanged,
}

internal sealed class GameIdentityException : System.IO.IOException
{
    internal GameIdentityException(
        GameIdentityFailureKind kind,
        string message,
        string path = null,
        Exception innerException = null
    ) : base(message, innerException)
    {
        Kind = kind;
        Path = path ?? string.Empty;
    }

    internal GameIdentityFailureKind Kind { get; }
    internal string Path { get; }
}
