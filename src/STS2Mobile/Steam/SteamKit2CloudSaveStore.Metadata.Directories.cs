using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    string[] ISaveStore.GetFilesInDirectory(string directoryPath) =>
        _cache.GetFilesInDirectory(directoryPath);

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath) =>
        _cache.GetDirectoriesInDirectory(directoryPath);

    void ISaveStore.CreateDirectory(string directoryPath) { }

    void ISaveStore.DeleteDirectory(string directoryPath) { }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath) { }
}
