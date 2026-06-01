using System;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private sealed class RsaPublicKey
    {
        private RsaPublicKey(byte[] subjectPublicKeyInfo)
        {
            SubjectPublicKeyInfo = subjectPublicKeyInfo;
        }

        private RsaPublicKey(byte[] modulus, byte[] exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
        }

        private byte[]? SubjectPublicKeyInfo { get; }
        private byte[]? Modulus { get; }
        private byte[]? Exponent { get; }

        internal static RsaPublicKey FromParameters(byte[] modulus, byte[] exponent)
            => new(modulus, exponent);

        internal static RsaPublicKey FromSubjectPublicKeyInfo(byte[] subjectPublicKeyInfo)
            => new(subjectPublicKeyInfo);

        internal string EncodedSubjectPublicKeyInfo()
            => Encoded(SubjectPublicKeyInfo);

        internal string EncodedModulus()
            => Encoded(Modulus);

        internal string EncodedExponent()
            => Encoded(Exponent);

        private static string Encoded(byte[]? value)
            => value == null ? string.Empty : Convert.ToBase64String(value);
    }
}
