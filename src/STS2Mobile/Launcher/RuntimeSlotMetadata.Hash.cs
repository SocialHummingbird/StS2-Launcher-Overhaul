using System.Text;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimeSlotMetadata
{
    private static string StableHash16(string value)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            foreach (var b in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash.ToString("x16");
        }
    }
}
