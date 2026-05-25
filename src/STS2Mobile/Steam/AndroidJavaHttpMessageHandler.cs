using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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
    private static readonly object AppLock = new();
    private static GodotObject _godotApp;

    public static void Prime()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        _ = GetGodotApp();
    }

    public static NetHttpClient CreateClient(HttpClientPurpose purpose)
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
        var requestDescription = $"{request.Method} {SanitizeUriForLog(request.RequestUri)}";

        cancellationToken.ThrowIfCancellationRequested();

        string? raw;
        try
        {
            raw = (string)app.Call(
                "httpRequest",
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

        if (root.TryGetProperty("error", out var error))
        {
            var errorMessage = SanitizeBridgeErrorForLog(GetBridgeString(error), request.RequestUri);
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? $"Android Java HTTP bridge reported an unknown error for {requestDescription}"
                    : $"Android Java HTTP bridge error for {requestDescription}: {errorMessage}"
            );
        }

        if (!root.TryGetProperty("status", out var statusElement) || !statusElement.TryGetInt32(out var status))
            throw new HttpRequestException($"Android Java HTTP bridge returned a response without a valid status for {requestDescription}");
        if (status < 100 || status > 599)
            throw new HttpRequestException($"Android Java HTTP bridge returned an invalid HTTP status for {requestDescription}: {status}");

        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            RequestMessage = request,
        };

        try
        {
            if (root.TryGetProperty("reason", out var reason))
                response.ReasonPhrase = GetBridgeString(reason);

            if (root.TryGetProperty("bodyFile", out var bodyFileElement))
            {
                var bodyFile = GetBridgeString(bodyFileElement);
                if (!TryGetSafeBodyFilePath(bodyFile, out var safeBodyFile) || !File.Exists(safeBodyFile))
                {
                    DeleteBodyFileIfSafe(bodyFile);
                    throw new HttpRequestException($"Android Java HTTP bridge returned a missing body file for {requestDescription}");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    DeleteQuietly(safeBodyFile);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (status >= 400)
                {
                    try
                    {
                        response.Content = new ByteArrayContent(
                            ReadBodyFileLimited(
                                safeBodyFile,
                                MaxBufferedErrorBodyBytes,
                                requestDescription
                            )
                        );
                    }
                    finally
                    {
                        DeleteQuietly(safeBodyFile);
                    }
                }
                else
                {
                    response.Content = new DeleteOnDisposeFileContent(safeBodyFile);
                }
            }
            else
            {
                byte[] body;
                try
                {
                    body = root.TryGetProperty("body", out var bodyElement)
                        ? Convert.FromBase64String(GetBridgeString(bodyElement) ?? string.Empty)
                        : Array.Empty<byte>();
                }
                catch (FormatException ex)
                {
                    throw new HttpRequestException(
                        $"Android Java HTTP bridge returned malformed base64 response body for {requestDescription}",
                        ex
                    );
                }

                response.Content = new ByteArrayContent(body);
            }

            if (root.TryGetProperty("headers", out var headers))
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

    private static GodotObject GetGodotApp()
    {
        lock (AppLock)
        {
            if (_godotApp != null)
                return _godotApp;

            try
            {
                var jcw = Engine.GetSingleton("JavaClassWrapper");
                var wrapper = (GodotObject)jcw.Call("wrap", "com.game.sts2launcher.GodotApp");
                _godotApp = (GodotObject)wrapper.Call("getInstance");
                return _godotApp;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Auth] Java HTTP bridge unavailable: {ex.Message}");
                return null;
            }
        }
    }

    private sealed class DeleteOnDisposeFileContent : StreamContent
    {
        private readonly string _path;
        private readonly long _length;

        public DeleteOnDisposeFileContent(string path)
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
                System.IO.FileAccess.Read,
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

            DeleteQuietly(_path);
        }
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
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

    private static string? GetBridgeString(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    private static string SanitizeUriForLog(Uri? uri)
    {
        if (uri == null)
            return "<no-uri>";

        if (uri.IsAbsoluteUri)
        {
            var path = uri.GetLeftPart(UriPartial.Path);
            var absoluteFragmentIndex = path.IndexOf('#');
            return absoluteFragmentIndex >= 0 ? path[..absoluteFragmentIndex] : path;
        }

        var raw = uri.ToString();
        var queryIndex = raw.IndexOf('?');
        var fragmentIndex = raw.IndexOf('#');
        var cutIndex = queryIndex >= 0 && fragmentIndex >= 0
            ? Math.Min(queryIndex, fragmentIndex)
            : queryIndex >= 0
                ? queryIndex
                : fragmentIndex;

        return cutIndex >= 0 ? raw[..cutIndex] : raw;
    }

    private static string? SanitizeBridgeErrorForLog(string? message, Uri? requestUri)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        if (requestUri != null)
        {
            var rawUri = requestUri.ToString();
            if (!string.IsNullOrEmpty(rawUri))
                message = message.Replace(rawUri, SanitizeUriForLog(requestUri), StringComparison.Ordinal);
        }

        return RedactUrlSuffixes(message);
    }

    private static string RedactUrlSuffixes(string message)
    {
        var builder = new System.Text.StringBuilder(message.Length);
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
            index++;
            while (index < message.Length)
            {
                var suffix = message[index];
                if (char.IsWhiteSpace(suffix) || suffix == '"' || suffix == '\'' || suffix == ')')
                    break;

                index++;
            }

            if (index < message.Length)
            {
                builder.Append(message[index]);
                index++;
            }
        }

        return builder.ToString();
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

    private static void DeleteBodyFileFromRawResponse(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("bodyFile", out var bodyFileElement))
            {
                var bodyFile = bodyFileElement.GetString();
                DeleteBodyFileIfSafe(bodyFile);
            }
        }
        catch { }
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
            DeleteQuietly(safePath);
    }
}
