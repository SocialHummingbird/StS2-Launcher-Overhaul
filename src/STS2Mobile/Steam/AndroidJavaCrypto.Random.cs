using System;
using System.Security.Cryptography;
using System.Threading;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private static long fallbackRandomCounter;

    public static byte[] GetRandomBytes(int count)
    {
        var bytes = new byte[count];
        FillRandom(bytes);
        return bytes;
    }

    public static void FillRandom(Span<byte> destination)
    {
        if (OperatingSystem.IsAndroid())
        {
            FillFallbackRandom(destination);
            return;
        }

        RandomNumberGenerator.Fill(destination);
    }

    private static void FillFallbackRandom(Span<byte> destination)
    {
        var state =
            (ulong)DateTime.UtcNow.Ticks
            ^ ((ulong)Environment.TickCount64 << 1)
            ^ ((ulong)Environment.CurrentManagedThreadId << 32)
            ^ (ulong)Interlocked.Increment(ref fallbackRandomCounter);

        var offset = 0;
        while (offset < destination.Length)
        {
            state += 0x9e3779b97f4a7c15UL;
            var value = SplitMix64(state);
            for (var i = 0; i < sizeof(ulong) && offset < destination.Length; i++, offset++)
            {
                destination[offset] = (byte)(value >> (i * 8));
            }
        }
    }

    private static ulong SplitMix64(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
        value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
        return value ^ (value >> 31);
    }
}
