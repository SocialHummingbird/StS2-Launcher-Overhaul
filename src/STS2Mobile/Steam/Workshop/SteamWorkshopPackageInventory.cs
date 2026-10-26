using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace STS2Mobile.Steam.Workshop;

internal static class SteamWorkshopPackageInventory
{
    internal static List<SteamWorkshopInstalledFile> Capture(string directory)
    {
        var root = Path.GetFullPath(directory);
        var pending = new Stack<string>();
        var files = new List<SteamWorkshopInstalledFile>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException($"Workshop package contains a linked directory: {current}");
            foreach (var child in Directory.EnumerateDirectories(current))
                pending.Push(child);
            foreach (var path in Directory.EnumerateFiles(current))
            {
                var before = new FileInfo(path);
                if ((before.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Workshop package contains a linked file: {path}");
                var length = before.Length;
                var modified = before.LastWriteTimeUtc;
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (relative.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                    throw new IOException("Workshop package contains an unsupported file name");
                using var stream = File.OpenRead(path);
                var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
                var after = new FileInfo(path);
                if (after.Length != length || after.LastWriteTimeUtc != modified)
                    throw new IOException($"Workshop package changed while reading: {relative}");
                files.Add(new SteamWorkshopInstalledFile { Path = relative, Length = length, Sha256 = hash });
            }
        }

        files = files.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.Path, StringComparer.Ordinal).ToList();
        if (files.Select(file => file.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
            throw new IOException("Workshop package contains file names that differ only by case");
        return files;
    }

    internal static string ContentSha256(IEnumerable<SteamWorkshopInstalledFile> files)
    {
        var canonical = string.Join("\n", files.Select(file =>
            $"{file.Path}\n{file.Length.ToString(CultureInfo.InvariantCulture)}\n{file.Sha256}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
