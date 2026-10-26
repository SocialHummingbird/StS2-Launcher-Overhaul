using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2Launcher.ModFrameworkFixtures;

[ModInitializer(nameof(Initialize))]
public static class CryptoInitializer
{
    public static void Initialize()
    {
        var input = Encoding.ASCII.GetBytes("abc");
        using (var md5 = MD5.Create())
            AssertHash("MD5", md5.ComputeHash(input), "900150983CD24FB0D6963F7D28E17F72");
        using (var sha1 = SHA1.Create())
            AssertHash("SHA1", sha1.ComputeHash(input), "A9993E364706816ABA3E25717850C26C9CD0D89D");
        AssertHash("SHA256", SHA256.HashData(input), "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
        var initializerThread = System.Environment.CurrentManagedThreadId;
        Task.Run(() =>
        {
            if (System.Environment.CurrentManagedThreadId == initializerThread)
                throw new InvalidOperationException("Stream SHA256 contract must run on a background thread.");
            using var stream = new MemoryStream(input, writable: false);
            AssertHash("background Stream SHA256", SHA256.HashData(stream),
                "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
        }).GetAwaiter().GetResult();
        GD.Print("RUNTIME_CRYPTO_CONTRACT_PASS");
    }

    private static void AssertHash(string algorithm, byte[] actualBytes, string expected)
    {
        var actual = Convert.ToHexString(actualBytes);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"{algorithm} digest mismatch: expected {expected}, actual {actual}.");
    }
}
