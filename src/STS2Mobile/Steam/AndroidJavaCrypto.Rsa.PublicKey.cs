using System;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private sealed class RsaPublicKey
    {
        internal readonly struct BridgeArguments
        {
            private BridgeArguments(
                string subjectPublicKeyInfo,
                string modulus,
                string exponent
            )
            {
                SubjectPublicKeyInfo = subjectPublicKeyInfo;
                Modulus = modulus;
                Exponent = exponent;
            }

            internal string SubjectPublicKeyInfo { get; }
            internal string Modulus { get; }
            internal string Exponent { get; }

            internal static BridgeArguments From(RsaPublicKey key)
                => new(
                    Encoded(key.SubjectPublicKeyInfo),
                    Encoded(key.Modulus),
                    Encoded(key.Exponent)
                );
        }

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

        private static RsaPublicKey FromParameters(byte[] modulus, byte[] exponent)
            => new(modulus, exponent);

        private static RsaPublicKey FromSubjectPublicKeyInfo(byte[] subjectPublicKeyInfo)
            => new(subjectPublicKeyInfo);

        internal static RsaPublicKey ImportedParameters(
            byte[] modulus,
            byte[] exponent
        )
            => FromParameters(modulus, exponent);

        internal static RsaPublicKey ImportedSubjectPublicKeyInfo(
            byte[] subjectPublicKeyInfo
        )
            => FromSubjectPublicKeyInfo(subjectPublicKeyInfo);

        internal BridgeArguments ToBridgeArguments()
            => BridgeArguments.From(this);

        private static string Encoded(byte[]? value)
            => value == null ? string.Empty : Convert.ToBase64String(value);
    }
}
