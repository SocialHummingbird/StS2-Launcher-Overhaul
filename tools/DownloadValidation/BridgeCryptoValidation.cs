using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Opt-in Android fixture: no Steam session, credentials, or Cloud operations.
internal static class BridgeCryptoValidation
{
    internal static async Task RunAsync(string output, Action<string> status)
    {
        var events = Path.Combine(output, "crypto-fixture-events.jsonl");
        void Event(string phase) => File.AppendAllText(events, JsonSerializer.Serialize(new
        {
            phase,
            utc = DateTimeOffset.UtcNow,
            monotonicMs = Environment.TickCount64,
            processId = Environment.ProcessId,
            cloudOperations = 0,
        }) + "\n");
        var first = await Task.Run(EncryptKnownBlock).ConfigureAwait(false);
        RequireKnownBlock(first);
        Event("foreground-worker-crypto-verified");
        status("Offline crypto fixture armed: background the app before its ten-second timer fires.");
        using var timer = new Timer(_ =>
        {
            Event("timer-callback-start");
            // Intentionally leave this exception unhandled, as in SteamKit's real
            // ScheduledFunction.Tick. Only this fixture arms this diagnostic timer.
            RequireKnownBlock(EncryptKnownBlock());
            Event("timer-crypto-verified");
        }, null, TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        Event("armed");
        await Task.Delay(TimeSpan.FromSeconds(220)).ConfigureAwait(false);
        Event("observation-completed-without-fatal-timeout");
    }

    private static byte[] EncryptKnownBlock()
    {
        using var aes = AndroidJavaCrypto.CreateAes();
        aes.Key = new byte[16];
        var output = new byte[16];
        if (AndroidJavaCrypto.AesEncryptEcb(aes, new byte[16], output, PaddingMode.None) != 16)
            throw new IOException("Unexpected AES output length.");
        return output;
    }

    private static void RequireKnownBlock(byte[] bytes)
    {
        if (!Convert.ToHexString(bytes).Equals("66E94BD4EF8A2C3B884CFA59CA342B2E", StringComparison.Ordinal))
            throw new IOException("Known AES block did not match.");
    }
}
