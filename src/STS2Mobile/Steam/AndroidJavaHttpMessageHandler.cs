using System;
using System.Collections.Generic;
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

        cancellationToken.ThrowIfCancellationRequested();

        var raw = (string)app.Call(
            "httpRequest",
            request.Method.Method,
            request.RequestUri?.ToString() ?? string.Empty,
            SerializeHeaders(request),
            Convert.ToBase64String(bodyBytes),
            _timeoutMs
        );

        if (string.IsNullOrEmpty(raw))
            throw new HttpRequestException("Android Java HTTP bridge returned an empty response");

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var error))
            throw new HttpRequestException(error.GetString());

        var status = root.GetProperty("status").GetInt32();
        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            RequestMessage = request,
        };

        if (root.TryGetProperty("reason", out var reason))
            response.ReasonPhrase = reason.GetString();

        var body = root.TryGetProperty("body", out var bodyElement)
            ? Convert.FromBase64String(bodyElement.GetString() ?? string.Empty)
            : Array.Empty<byte>();
        response.Content = new ByteArrayContent(body);

        if (root.TryGetProperty("headers", out var headers))
        {
            foreach (var header in headers.EnumerateObject())
            {
                var values = new List<string>();
                foreach (var value in header.Value.EnumerateArray())
                    values.Add(value.GetString() ?? string.Empty);

                if (!response.Headers.TryAddWithoutValidation(header.Name, values))
                    response.Content.Headers.TryAddWithoutValidation(header.Name, values);
            }
        }

        return response;
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
}
