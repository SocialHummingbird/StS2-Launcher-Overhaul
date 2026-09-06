using System;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed record LauncherRestartRequest(
    string AttemptId, string Branch, bool Safe, string GameIdentityId,
    string RuntimePackId, string Generation, long CreatedAtUnixMs, bool Legacy = false)
{
    internal static LauncherRestartRequest Create(string attemptId, bool safe, LauncherLaunchReadiness readiness)
        => new(attemptId, readiness.Branch, safe, readiness.GameIdentityId,
            readiness.RuntimeSlot?.RuntimePack?.PackId ?? "", readiness.RuntimeSlot?.InstallGeneration ?? "",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    internal string Serialize() => JsonSerializer.Serialize(new {
        version = 1, attemptId = AttemptId, branch = Branch, safe = Safe,
        gameIdentityId = GameIdentityId, runtimePackId = RuntimePackId, generation = Generation,
        createdAtUnixMs = CreatedAtUnixMs, legacy = Legacy, state = "pending"
    });

    internal static LauncherRestartRequest ConsumeRestored()
        => ParseRestored(AndroidGodotAppBridge.ConsumeLaunchRestartRequest(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    internal static LauncherRestartRequest ParseRestored(string json, long now)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            string Required(string key)
            {
                var value = root.GetProperty(key).GetString();
                if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Missing restart request {key}.");
                return value;
            }
            if (root.GetProperty("version").GetInt32() != 1 || Required("state") != "claimed")
                throw new InvalidOperationException("Restart request version or consumption state is invalid.");
            var created = root.GetProperty("createdAtUnixMs").GetInt64();
            if (created <= 0 || created > now + 60000 || now - created > 86400000)
                throw new InvalidOperationException("Restart request expired; press Play to start a new attempt.");
            var legacy = root.TryGetProperty("legacy", out var legacyValue) && legacyValue.GetBoolean();
            return new LauncherRestartRequest(Required("attemptId"), Required("branch"), root.GetProperty("safe").GetBoolean(),
                legacy ? "" : Required("gameIdentityId"), legacy ? "" : Required("runtimePackId"),
                legacy ? "" : Required("generation"), created, legacy);
        }
        catch (Exception error) when (error is not InvalidOperationException)
        {
            throw new InvalidOperationException("No valid unconsumed Android restart request is available.", error);
        }
    }

    internal void ValidateReadiness(LauncherLaunchReadiness readiness)
    {
        if (readiness?.Ready != true || !string.Equals(Branch, readiness.Branch, StringComparison.Ordinal))
            throw new InvalidOperationException("Restored restart request no longer matches the ready selected branch.");
        if (!readiness.HasCurrentLaunchAuthorization(out var problem))
            throw new InvalidOperationException(problem);
        // Legacy flags carry no identity: they must pass today's complete authorization gate.
        if (!Legacy && (!string.Equals(GameIdentityId, readiness.GameIdentityId, StringComparison.Ordinal)
            || !string.Equals(RuntimePackId, readiness.RuntimeSlot?.RuntimePack?.PackId, StringComparison.Ordinal)
            || !string.Equals(Generation, readiness.RuntimeSlot?.InstallGeneration, StringComparison.Ordinal)))
            throw new InvalidOperationException("Installed game or runtime pack changed since this restart was requested.");
    }
}

internal readonly record struct LauncherRestartAcceptance(bool Accepted, string AttemptId, string Error)
{
    internal static LauncherRestartAcceptance Parse(string json, string expectedAttemptId)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var id = root.GetProperty("attemptId").GetString() ?? "";
            var accepted = root.GetProperty("accepted").GetBoolean() && id == expectedAttemptId;
            return new(accepted, id, accepted ? "" : root.GetProperty("error").GetString() ?? "Android rejected the restart request.");
        }
        catch (Exception) { return new(false, expectedAttemptId, "Android returned no valid restart acknowledgement."); }
    }
}
