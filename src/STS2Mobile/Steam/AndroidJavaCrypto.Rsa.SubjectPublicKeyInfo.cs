using System;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private static int EstimateSubjectPublicKeyInfoSize(ReadOnlySpan<byte> source)
    {
        // SteamKit imports RSA public keys through SPKI. The ASN.1 parser is intentionally
        // minimal: this value is only metadata for callers that inspect RSA.KeySize, while
        // the actual encryption path uses the original SPKI bytes in Android Java.
        for (var i = 0; i < source.Length - 4; i++)
        {
            if (source[i] == 0x02)
            {
                var lengthByte = source[i + 1];
                var length = 0;
                var offset = i + 2;

                if ((lengthByte & 0x80) == 0)
                {
                    length = lengthByte;
                }
                else
                {
                    var lengthBytes = lengthByte & 0x7f;
                    if (lengthBytes == 0 || lengthBytes > 4 || offset + lengthBytes > source.Length)
                        continue;

                    for (var j = 0; j < lengthBytes; j++)
                        length = (length << 8) | source[offset + j];
                    offset += lengthBytes;
                }

                if (length <= 0 || offset + length > source.Length)
                    continue;

                if (source[offset] == 0x00 && length > 1)
                    length--;

                return length * 8;
            }
        }

        return 2048;
    }
}
