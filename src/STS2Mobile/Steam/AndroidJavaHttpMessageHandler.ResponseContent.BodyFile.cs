using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static HttpContent CreateResponseContentFromBodyFile(
        JsonElement bodyFileElement,
        int status,
        string requestDescription,
        CancellationToken cancellationToken
    )
    {
        var bodyFile = GetBridgeString(bodyFileElement);
        var safeBodyFile = RequireExistingSafeBodyFile(bodyFile, requestDescription);
        ThrowIfCancelledWithBodyFileCleanup(safeBodyFile, cancellationToken);

        if (status < 400)
            return new DeleteOnDisposeFileContent(safeBodyFile);

        return CreateBufferedErrorBodyContent(safeBodyFile, requestDescription);
    }

    private static HttpContent CreateBufferedErrorBodyContent(
        string safeBodyFile,
        string requestDescription
    )
    {
        try
        {
            return new ByteArrayContent(
                ReadBodyFileLimited(
                    safeBodyFile,
                    MaxBufferedErrorBodyBytes,
                    requestDescription
                )
            );
        }
        finally
        {
            DeleteFileQuietly(safeBodyFile);
        }
    }

    private static string RequireExistingSafeBodyFile(
        string? bodyFile,
        string requestDescription
    )
    {
        if (
            TryGetSafeBodyFilePath(bodyFile, out var safeBodyFile)
            && File.Exists(safeBodyFile)
        )
        {
            return safeBodyFile;
        }

        DeleteBodyFileIfSafe(bodyFile);
        throw new HttpRequestException(
            $"Android Java HTTP bridge returned a missing body file for {requestDescription}"
        );
    }

    private static void ThrowIfCancelledWithBodyFileCleanup(
        string safeBodyFile,
        CancellationToken cancellationToken
    )
    {
        if (!cancellationToken.IsCancellationRequested)
            return;

        DeleteFileQuietly(safeBodyFile);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static byte[] ReadBodyFileLimited(string path, int maxBytes, string requestDescription)
    {
        using var input = File.OpenRead(path);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;

        while (true)
        {
            var read = input.Read(buffer, 0, buffer.Length);
            if (read == 0)
                return output.ToArray();

            total += read;
            if (total > maxBytes)
            {
                throw new HttpRequestException(
                    $"Android Java HTTP bridge error body exceeds buffered limit for {requestDescription}: {maxBytes}"
                );
            }

            output.Write(buffer, 0, read);
        }
    }
}
