using System;
using System.Text.RegularExpressions;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUpdateCoordinator
{
    private readonly struct LauncherVersion
    {
        private LauncherVersion(string text, int[] parts)
        {
            Text = text;
            Parts = parts;
        }

        private string Text { get; }
        private int[] Parts { get; }

        internal static bool TryParse(string version, out LauncherVersion parsed)
        {
            parsed = default;
            if (string.IsNullOrEmpty(version))
                return false;

            var match = Regex.Match(version, VersionNumberPattern);
            if (!match.Success)
                return false;

            var text = match.Value.TrimStart('v', 'V');
            parsed = new LauncherVersion(text, ParseParts(text));
            return true;
        }

        internal bool IsNewerThan(LauncherVersion other)
            => CompareTo(other) > 0;

        private int CompareTo(LauncherVersion other)
        {
            var len = Math.Max(Parts.Length, other.Parts.Length);
            for (var i = 0; i < len; i++)
            {
                var current = PartOrZero(Parts, i);
                var target = PartOrZero(other.Parts, i);
                if (current != target)
                    return current - target;
            }

            return 0;
        }

        public override string ToString()
            => Text;

        private static int[] ParseParts(string version)
        {
            var textParts = version.Split('.');
            var parts = new int[textParts.Length];
            for (var i = 0; i < textParts.Length; i++)
                parts[i] = int.TryParse(textParts[i], out var value) ? value : 0;

            return parts;
        }

        private static int PartOrZero(int[] parts, int index)
            => index < parts.Length ? parts[index] : 0;
    }
}
