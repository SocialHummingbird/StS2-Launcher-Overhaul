using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using SteamKit2;
using NetHttpClient = System.Net.Http.HttpClient;

namespace STS2Mobile.Steam;

// SteamKit's default HttpClient uses System.Net.Security.SslStream. In the
// Godot Android Mono runtime, creating that AndroidCrypto SSL stream aborts the
// whole process. Route SteamKit WebAPI/CDN HTTPS requests through Android's Java
// HttpsURLConnection instead.
internal sealed class AndroidJavaHttpMessageHandler : HttpMessageHandler
{
    private const int DefaultTimeoutMs = 30_000;
    private const int MaxBufferedErrorBodyBytes = 1024 * 1024;
    private const string BodyProperty = "body";
    private const string BodyFileProperty = "bodyFile";
    private const string ErrorProperty = "error";
    private const string HeadersProperty = "headers";
    private const string HttpRequestBridgeMethod = "httpRequest";
    private const string ReasonProperty = "reason";
    private const string StatusProperty = "status";
    private static readonly object AppLock = new();
    private static GodotObject _godotApp;

    internal static void Prime()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        _ = GetGodotApp();
    }

    internal static NetHttpClient CreateClient(HttpClientPurpose purpose)
    {
        if (!OperatingSystem.IsAndroid())
            return new NetHttpClient();

        var timeout = purpose == HttpClientPurpose.CDN
            ? TimeSpan.FromMinutes(2)
            : TimeSpan.FromMilliseconds(DefaultTimeoutMs);

        return new NetHttpClient(new AndroidJavaHttpMessageHandler((int)timeout.TotalMilliseconds))
        {
            Timeout = timeout,
        };
    }

    private readonly int _timeoutMs;

    private AndroidJavaHttpMessageHandler(int timeoutMs)
    {
        _timeoutMs = timeoutMs;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var app = GetGodotApp();
        if (app == null)
            throw new HttpRequestException("GodotApp Java bridge is unavailable");

        var bodyBytes = request.Content == null
            ? Array.Empty<byte>()
            : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var requestDescription = $"{request.Method} {SanitizeUri(request.RequestUri)}";

        cancellationToken.ThrowIfCancellationRequested();

        string? raw;
        try
        {
            raw = (string)app.Call(
                HttpRequestBridgeMethod,
                request.Method.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                SerializeHeaders(request),
                Convert.ToBase64String(bodyBytes),
                _timeoutMs
            );
        }
        catch (InvalidCastException ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned a non-string response for {requestDescription}",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge call failed before returning a response for {requestDescription}",
                ex
            );
        }

        if (cancellationToken.IsCancellationRequested)
        {
            DeleteBodyFileFromRawResponse(raw);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (string.IsNullOrEmpty(raw))
            throw new HttpRequestException($"Android Java HTTP bridge returned an empty response for {requestDescription}");

        using var doc = ParseBridgeResponse(raw, requestDescription);
        var root = doc.RootElement;

        if (root.TryGetProperty(ErrorProperty, out var error))
        {
            var errorMessage = SanitizeBridgeError(
                GetBridgeString(error),
                request.RequestUri
            );
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? $"Android Java HTTP bridge reported an unknown error for {requestDescription}"
                    : $"Android Java HTTP bridge error for {requestDescription}: {errorMessage}"
            );
        }

        if (!root.TryGetProperty(StatusProperty, out var statusElement) || !statusElement.TryGetInt32(out var status))
            throw new HttpRequestException($"Android Java HTTP bridge returned a response without a valid status for {requestDescription}");
        if (status < 100 || status > 599)
            throw new HttpRequestException($"Android Java HTTP bridge returned an invalid HTTP status for {requestDescription}: {status}");

        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            RequestMessage = request,
        };

        try
        {
            if (root.TryGetProperty(ReasonProperty, out var reason))
                response.ReasonPhrase = GetBridgeString(reason);

            response.Content = CreateResponseContent(
                root,
                status,
                requestDescription,
                cancellationToken
            );

            if (root.TryGetProperty(HeadersProperty, out var headers))
            {
                foreach (var header in headers.EnumerateObject())
                {
                    if (header.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    var values = new List<string>();
                    foreach (var value in header.Value.EnumerateArray())
                        values.Add(GetBridgeString(value) ?? string.Empty);

                    if (!response.Headers.TryAddWithoutValidation(header.Name, values))
                        response.Content.Headers.TryAddWithoutValidation(header.Name, values);
                }
            }

            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static GodotObject GetGodotApp()
    {
        lock (AppLock)
        {
            if (_godotApp != null)
                return _godotApp;

            try
            {
                if (!AndroidGodotAppBridge.TryGetInstance(out _godotApp))
                    return null;

                return _godotApp;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Auth] Java HTTP bridge unavailable: {ex.Message}");
                return null;
            }
        }
    }

    private static JsonDocument ParseBridgeResponse(string raw, string requestDescription)
    {
        try
        {
            return JsonDocument.Parse(raw);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned malformed JSON for {requestDescription}",
                ex
            );
        }
    }

    private static string? GetBridgeString(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    private static HttpContent CreateResponseContent(
        JsonElement root,
        int status,
        string requestDescription,
        CancellationToken cancellationToken
    )
    {
        if (root.TryGetProperty(BodyFileProperty, out var bodyFileElement))
            return CreateResponseContentFromBodyFile(bodyFileElement, status, requestDescription, cancellationToken);

        return CreateResponseContentFromBase64Body(root, requestDescription);
    }

    private static HttpContent CreateResponseContentFromBodyFile(
        JsonElement bodyFileElement,
        int status,
        string requestDescription,
        CancellationToken cancellationToken
    )
    {
        var bodyFile = GetBridgeString(bodyFileElement);
        if (
            !TryGetSafeBodyFilePath(bodyFile, out var safeBodyFile)
            || !File.Exists(safeBodyFile)
        )
        {
            DeleteBodyFileIfSafe(bodyFile);
            throw new HttpRequestException($"Android Java HTTP bridge returned a missing body file for {requestDescription}");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            DeleteFileQuietly(safeBodyFile);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (status < 400)
            return new DeleteOnDisposeFileContent(safeBodyFile);

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

    private static HttpContent CreateResponseContentFromBase64Body(JsonElement root, string requestDescription)
    {
        try
        {
            var body = root.TryGetProperty(BodyProperty, out var bodyElement)
                ? Convert.FromBase64String(GetBridgeString(bodyElement) ?? string.Empty)
                : Array.Empty<byte>();
            return new ByteArrayContent(body);
        }
        catch (FormatException ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned malformed base64 response body for {requestDescription}",
                ex
            );
        }
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

    private static void DeleteBodyFileFromRawResponse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty(BodyFileProperty, out var bodyFileElement))
                DeleteBodyFileIfSafe(bodyFileElement.GetString());
        }
        catch
        {
        }
    }

    private static bool TryGetSafeBodyFilePath(string? path, out string safePath)
    {
        safePath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var normalized = fullPath.Replace('\\', '/');
            var fileName = Path.GetFileName(fullPath);
            if (
                !fileName.StartsWith("sts2_cdn_", StringComparison.Ordinal)
                || !fileName.EndsWith(".bin", StringComparison.Ordinal)
            )
            {
                return false;
            }

            if (
                !normalized.Contains("/cache/sts2_cdn_", StringComparison.Ordinal)
                && !normalized.Contains("/files/tmp/sts2_cdn_", StringComparison.Ordinal)
            )
            {
                return false;
            }

            safePath = fullPath;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteBodyFileIfSafe(string? path)
    {
        if (TryGetSafeBodyFilePath(path, out var safePath))
            DeleteFileQuietly(safePath);
    }

    private static void DeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static string SanitizeUri(Uri? uri)
    {
        if (uri == null)
            return "<no-uri>";

        if (uri.IsAbsoluteUri)
            return StripFragment(uri.GetLeftPart(UriPartial.Path));

        return StripUriSuffix(uri.ToString());
    }

    private static string StripUriSuffix(string raw)
    {
        var queryIndex = raw.IndexOf('?');
        var fragmentIndex = raw.IndexOf('#');
        var cutIndex = queryIndex >= 0 && fragmentIndex >= 0
            ? Math.Min(queryIndex, fragmentIndex)
            : queryIndex >= 0
                ? queryIndex
                : fragmentIndex;

        return cutIndex >= 0 ? raw[..cutIndex] : raw;
    }

    private static string StripFragment(string path)
    {
        var fragmentIndex = path.IndexOf('#');
        return fragmentIndex >= 0 ? path[..fragmentIndex] : path;
    }

    private static string? SanitizeBridgeError(string? message, Uri? requestUri)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        if (requestUri != null)
        {
            var rawUri = requestUri.ToString();
            if (!string.IsNullOrEmpty(rawUri))
                message = message.Replace(rawUri, SanitizeUri(requestUri), StringComparison.Ordinal);
        }

        return RedactUrlSuffixes(message);
    }

    private static string RedactUrlSuffixes(string message)
    {
        var builder = new StringBuilder(message.Length);
        var index = 0;

        while (index < message.Length)
        {
            var current = message[index];
            if ((current != '?' && current != '#') || !IsUrlLikeSuffixMarker(message, index))
            {
                builder.Append(current);
                index++;
                continue;
            }

            builder.Append(current).Append("<redacted>");
            index = SkipUrlSuffix(message, index + 1);

            if (index < message.Length)
            {
                builder.Append(message[index]);
                index++;
            }
        }

        return builder.ToString();
    }

    private static int SkipUrlSuffix(string message, int index)
    {
        while (index < message.Length)
        {
            var suffix = message[index];
            if (char.IsWhiteSpace(suffix) || suffix == '"' || suffix == '\'' || suffix == ')')
                break;

            index++;
        }

        return index;
    }

    private static bool IsUrlLikeSuffixMarker(string message, int markerIndex)
    {
        var tokenStart = markerIndex - 1;
        while (tokenStart >= 0)
        {
            var value = message[tokenStart];
            if (char.IsWhiteSpace(value) || value == '"' || value == '\'' || value == '(' || value == ')')
                break;

            tokenStart--;
        }

        var tokenPrefix = message.Substring(tokenStart + 1, markerIndex - tokenStart - 1);
        return tokenPrefix.StartsWith("/", StringComparison.Ordinal)
            || tokenPrefix.Contains("://", StringComparison.Ordinal)
            || tokenPrefix.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || tokenPrefix.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string SerializeHeaders(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in request.Headers)
            headers[header.Key] = new List<string>(header.Value);

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
                headers[header.Key] = new List<string>(header.Value);
        }

        return JsonSerializer.Serialize(headers);
    }

    private sealed class DeleteOnDisposeFileContent : StreamContent
    {
        private readonly string _path;
        private readonly long _length;

        private DeleteOnDisposeFileContent(string path)
            : this(path, OpenResponseFile(path)) { }

        private DeleteOnDisposeFileContent(string path, FileStream stream)
            : base(stream)
        {
            _path = path;
            _length = new FileInfo(path).Length;
            Headers.ContentLength = _length;
        }

        private static FileStream OpenResponseFile(string path)
        {
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024
            );
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _length;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DeleteFileQuietly(_path);
        }
    }
}
