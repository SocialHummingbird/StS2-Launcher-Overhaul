using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudFileCache
    {
        private void LoadFileList()
        {
            uint startIndex = 0;
            const uint pageSize = 500;
            var sampleNames = new List<string>();

            while (true)
            {
                var result = _connection
                    .SendCloud<CCloud_EnumerateUserFiles_Request, CCloud_EnumerateUserFiles_Response>(
                        "EnumerateUserFiles",
                        new CCloud_EnumerateUserFiles_Request
                        {
                            appid = SteamCloudApp.AppId,
                            start_index = startIndex,
                            count = pageSize,
                        }
                    )
                    .GetAwaiter()
                    .GetResult();

                if (result.files == null || result.files.Count == 0)
                    break;

                foreach (var file in result.files)
                {
                    var key = CacheKey(file.filename);
                    if (sampleNames.Count < 25)
                        sampleNames.Add(key);
                    SetPersistedFile(
                        key,
                        (int)file.file_size,
                        DateTimeOffset.FromUnixTimeSeconds((long)file.timestamp)
                    );
                }

                startIndex += (uint)result.files.Count;
                if (result.files.Count < pageSize)
                    break;
            }

            PatchHelper.Log(FilesEnumerated(_files.Count));
            if (sampleNames.Count > 0)
                PatchHelper.Log("[Cloud] Enumerated cloud file sample: " + string.Join(", ", sampleNames));
        }

        private static string FilesEnumerated(int count) =>
            CloudFileCacheMessage($"Enumerated {count} cloud files");
    }
}
