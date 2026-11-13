using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SteamKit2;

// Read only: inspect captured manifests and invoke the actual packaged path validator.
if (args.Length != 2 && !(args.Length == 3 && args[2] == "--chunk-index"))
    throw new ArgumentException("Expected snapshot.zip and tested STS2Mobile.dll paths, optionally --chunk-index.");
using var zip = ZipFile.OpenRead(args[0]);
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
var resolve = assembly.GetType("STS2Mobile.Steam.DepotDownloadStaging")
    .GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Static);
var results = new List<object>();
foreach (var entry in zip.Entries.Where(item => item.FullName.EndsWith("/2868841.manifest")))
{
    using var input = entry.Open();
    var manifest = DepotManifest.Deserialize(input);
    if (args.Length == 3)
    {
        results.Add(new
        {
            manifest = manifest.ManifestGID,
            depot = manifest.DepotID,
            files = manifest.Files.Where(file => !file.Flags.HasFlag(EDepotFileFlag.Directory)).Select(file => new
            {
                name = file.FileName.Split('\0')[0].Replace('\\', '/'),
                chunks = file.Chunks.Select(chunk => new { sha1 = Convert.ToHexString(chunk.ChunkID), chunk.UncompressedLength, chunk.CompressedLength })
            }).ToArray()
        });
        continue;
    }
    var failures = new List<object>();
    foreach (var (file, index) in manifest.Files.Select((file, index) => (file, index)))
    {
        try { resolve.Invoke(null, new object[] { Path.GetFullPath("tmp/manifest-probe-unwritten-root"), file.FileName }); }
        catch (TargetInvocationException error)
        {
            failures.Add(new
            {
                index,
                file.FileName,
                flags = file.Flags.ToString(),
                file.TotalSize,
                exception = error.InnerException.GetType().Name,
                message = error.InnerException.Message
            });
        }
    }
    results.Add(new
    {
        manifest = manifest.ManifestGID,
        depot = manifest.DepotID,
        files = manifest.Files.Count,
        rejectedNames = failures.Count,
        nulTerminatedNames = manifest.Files.Count(file => file.FileName.EndsWith('\0')),
        nulTerminatedRegularFiles = manifest.Files.Count(file => !file.Flags.HasFlag(EDepotFileFlag.Directory) && file.FileName.EndsWith('\0')),
        failures
    });
}
Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
