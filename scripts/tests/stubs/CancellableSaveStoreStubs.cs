using System.Threading.Tasks;

namespace MegaCrit.Sts2.Core.Saves;

internal interface ISaveStore
{
    Task<string> ReadFileAsync(string path);
    Task WriteFileAsync(string path, string content);
}
