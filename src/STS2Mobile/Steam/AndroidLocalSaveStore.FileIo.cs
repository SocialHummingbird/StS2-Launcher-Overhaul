using System.IO;
using System.Text;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore
{
    private string ReadTextFile(string path) => File.ReadAllText(FullPath(path));

    internal byte[] ReadBytes(string path) => File.ReadAllBytes(FullPath(path));

    private void WriteTextFile(string path, string content)
    {
        WriteBytesFile(path, Encoding.UTF8.GetBytes(content));
    }

    private void WriteBytesFile(string path, byte[] bytes)
    {
        var fullPath = FullPath(path);
        EnsureParentDirectory(fullPath);

        CancellableAtomicFile.WriteAllBytesAsync(
            fullPath,
            bytes,
            overwrite: true,
            CancellationToken.None
        ).GetAwaiter().GetResult();
        PatchHelper.Log($"[Save] Android local save write: {path} -> {fullPath} ({bytes.Length} bytes)");
        NotifyMutationCommitted(path);
    }
}
