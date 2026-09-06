using System;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void RestartRequestValidation()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var json = $$"""{"version":1,"attemptId":"attempt-a","branch":"public","safe":true,"gameIdentityId":"game-a","runtimePackId":"pack-a","generation":"generation-a","createdAtUnixMs":{{now}},"state":"claimed"}""";
        var request = LauncherRestartRequest.ParseRestored(json, now);
        if (request.AttemptId != "attempt-a" || !request.Safe || request.Legacy)
            throw new Exception("Restored request lost its identity or mode.");
        foreach (var invalid in new[] { "{}", json.Replace("claimed", "consumed"), json.Replace("\"version\":1", "\"version\":2"), json.Replace("game-a", "") })
        {
            try { LauncherRestartRequest.ParseRestored(invalid, now); }
            catch (InvalidOperationException) { continue; }
            throw new Exception("Malformed request was accepted.");
        }
        try { LauncherRestartRequest.ParseRestored(json, now + 86400001); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Stale request was accepted.");
    }
}
