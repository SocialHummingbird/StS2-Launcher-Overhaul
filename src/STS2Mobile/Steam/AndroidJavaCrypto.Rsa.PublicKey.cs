namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private sealed class RsaPublicKey
    {
        public RsaPublicKey(byte[] subjectPublicKeyInfo)
        {
            SubjectPublicKeyInfo = subjectPublicKeyInfo;
        }

        public RsaPublicKey(byte[] modulus, byte[] exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
        }

        public byte[] SubjectPublicKeyInfo { get; }
        public byte[] Modulus { get; }
        public byte[] Exponent { get; }
    }
}
