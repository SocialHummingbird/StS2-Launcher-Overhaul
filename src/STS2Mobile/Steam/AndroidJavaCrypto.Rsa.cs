using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private static readonly ConditionalWeakTable<RSA, RsaPublicKey> RsaPublicKeys = new();
    private const string RsaEncryptBase64BridgeMethod = "rsaEncryptBase64";

    public static void ImportSubjectPublicKeyInfo(
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
        RsaPublicKeys.Add(
            rsa,
            RsaPublicKey.ImportedSubjectPublicKeyInfo(source.ToArray())
        );
        if (rsa is AndroidRsa androidRsa)
            androidRsa.SetPublicKeySize(EstimateSubjectPublicKeyInfoSize(source));
        bytesRead = source.Length;
    }

    public static void ImportParameters(RSA rsa, RSAParameters parameters)
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
            RsaPublicKey.ImportedParameters(
                parameters.Modulus,
                parameters.Exponent
            )
        );
        if (rsa is AndroidRsa androidRsa)
            androidRsa.SetPublicKeySize(parameters.Modulus.Length * 8);
    }

    public static RSA CreateRsa()
    {
        if (!OperatingSystem.IsAndroid())
            return RSA.Create();

        return new AndroidRsa();
    }
}
