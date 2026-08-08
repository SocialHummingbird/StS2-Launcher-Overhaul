using System;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    DateTimeOffset ISaveStore.GetLastModifiedTime(string path)
        => _cache.GetLastModifiedTime(path);

    int ISaveStore.GetFileSize(string path)
        => _cache.GetFileSize(path);

    void ISaveStore.SetLastModifiedTime(string path, DateTimeOffset time)
        => throw UnsupportedCloudMetadataOperation(nameof(ISaveStore.SetLastModifiedTime));

    string ISaveStore.GetFullPath(string filename)
        => throw UnsupportedCloudMetadataOperation(nameof(ISaveStore.GetFullPath));

    bool ICloudSaveStore.HasCloudFiles()
        => _cache.HasCloudFiles();

    void ICloudSaveStore.ForgetFile(string path)
        => _cache.ForgetFile(path);

    bool ICloudSaveStore.IsFilePersisted(string path)
        => _cache.IsFilePersisted(path);

    private static NotSupportedException UnsupportedCloudMetadataOperation(string operation)
        => new($"Steam cloud save store does not support {operation}");
}
