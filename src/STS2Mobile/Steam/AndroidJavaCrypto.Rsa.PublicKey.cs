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

        private byte[] SubjectPublicKeyInfo { get; }
        private byte[] Modulus { get; }
        private byte[] Exponent { get; }
    }
}
