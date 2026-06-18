using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private void DeleteCloudFile(string path)
    {
        SendCloudBlocking<
            CCloud_ClientDeleteFile_Request,
            CCloud_ClientDeleteFile_Response
        >(
            "ClientDeleteFile",
            CreateDeleteFileRequest(path)
        );
    }
}
