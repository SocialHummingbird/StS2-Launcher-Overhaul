namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private sealed class RsaPublicKey
    {
        internal RsaPublicKey(byte[] subjectPublicKeyInfo)
        {
            SubjectPublicKeyInfo = subjectPublicKeyInfo;
        }

        internal RsaPublicKey(byte[] modulus, byte[] exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
        }

        internal byte[] SubjectPublicKeyInfo { get; }
        internal byte[] Modulus { get; }
        internal byte[] Exponent { get; }
    }
}
