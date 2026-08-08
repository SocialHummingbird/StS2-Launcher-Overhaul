using System.Threading.Tasks;

namespace MegaCrit.Sts2.Core.Saves;

internal interface ISaveStore
{
    Task<string> ReadFileAsync(string path);
    Task WriteFileAsync(string path, string content);
    Task WriteFileAsync(string path, byte[] content) => Task.CompletedTask;
    bool FileExists(string path) => true;
    void DeleteFile(string path) { }
    string GetFullPath(string path) => path;
}
