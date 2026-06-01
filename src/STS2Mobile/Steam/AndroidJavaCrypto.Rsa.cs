using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private static readonly ConditionalWeakTable<RSA, RsaPublicKey> RsaPublicKeys = new();
    private const string RsaEncryptBase64BridgeMethod = "rsaEncryptBase64";

    internal static void ImportSubjectPublicKeyInfo(
        AsymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> source,
        out int bytesRead
    )
    {
        if (!OperatingSystem.IsAndroid())
        {
            algorithm.ImportSubjectPublicKeyInfo(source, out bytesRead);
            return;
        }

        var rsa = (RSA)algorithm;
        RsaPublicKeys.Remove(rsa);
        RsaPublicKeys.Add(rsa, RsaPublicKey.FromSubjectPublicKeyInfo(source.ToArray()));
        if (rsa is AndroidRsa androidRsa)
            androidRsa.SetPublicKeySize(EstimateSubjectPublicKeyInfoSize(source));
        bytesRead = source.Length;
    }

    internal static void ImportParameters(RSA rsa, RSAParameters parameters)
    {
        if (!OperatingSystem.IsAndroid())
        {
            rsa.ImportParameters(parameters);
            return;
        }

        if (parameters.Modulus == null || parameters.Exponent == null)
            throw new ArgumentException("RSA public parameters require modulus and exponent");

        RsaPublicKeys.Remove(rsa);
        RsaPublicKeys.Add(
            rsa,
            RsaPublicKey.FromParameters(parameters.Modulus, parameters.Exponent)
        );
        if (rsa is AndroidRsa androidRsa)
            androidRsa.SetPublicKeySize(parameters.Modulus.Length * 8);
    }

    internal static RSA CreateRsa()
    {
        if (!OperatingSystem.IsAndroid())
            return RSA.Create();

        return new AndroidRsa();
    }
}
