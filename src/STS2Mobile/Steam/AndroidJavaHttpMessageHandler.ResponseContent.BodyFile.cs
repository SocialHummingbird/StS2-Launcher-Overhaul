using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private readonly struct BodyFileContentRequest
    {
        private BodyFileContentRequest(
            string? bodyFile,
            int status,
            string requestDescription,
            CancellationToken cancellationToken
        )
        {
            BodyFile = bodyFile;
            Status = status;
            RequestDescription = requestDescription;
            CancellationToken = cancellationToken;
        }

        private string? BodyFile { get; }
        private int Status { get; }
        private string RequestDescription { get; }
        private CancellationToken CancellationToken { get; }
        private bool IsError => Status >= 400;

        private static BodyFileContentRequest Create(
            string? bodyFile,
            int status,
            string requestDescription,
            CancellationToken cancellationToken
        )
            => new(bodyFile, status, requestDescription, cancellationToken);

        private HttpContent CreateContent()
        {
            var safeBodyFile = RequireExistingSafeBodyFile(
                BodyFile,
                RequestDescription
            );
            ThrowIfCancelledWithBodyFileCleanup(safeBodyFile, CancellationToken);

            return IsError
                ? CreateBufferedErrorBodyContent(safeBodyFile, RequestDescription)
                : new DeleteOnDisposeFileContent(safeBodyFile);
        }

        private static HttpContent CreateContent(
            string? bodyFile,
            int status,
            string requestDescription,
            CancellationToken cancellationToken
        )
            => Create(
                bodyFile,
                status,
                requestDescription,
                cancellationToken
            ).CreateContent();
    }

    private static HttpContent CreateResponseContentFromBodyFile(
        string? bodyFile,
        int status,
        string requestDescription,
        CancellationToken cancellationToken
    )
        => BodyFileContentRequest.CreateContent(
            bodyFile,
            status,
            requestDescription,
            cancellationToken
        );

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
        var safeBodyFile = GetSafeBodyFilePath(bodyFile);
        if (safeBodyFile != null && File.Exists(safeBodyFile))
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
