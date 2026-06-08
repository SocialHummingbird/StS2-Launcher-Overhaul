using System.IO;
using System.Text;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore
{
    private string ReadTextFile(string path) => File.ReadAllText(FullPath(path));

    private void WriteTextFile(string path, string content)
    {
        WriteBytesFile(path, Encoding.UTF8.GetBytes(content));
    }

    private void WriteBytesFile(string path, byte[] bytes)
    {
        var fullPath = FullPath(path);
        EnsureParentDirectory(fullPath);

        File.WriteAllBytes(fullPath, bytes);
        PatchHelper.Log($"[Cloud] Android local save write: {path} -> {fullPath} ({bytes.Length} bytes)");
    }
}
