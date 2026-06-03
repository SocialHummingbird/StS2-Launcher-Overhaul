using System;
using System.Net.Http;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private string? CallHttpRequestBridge(
        HttpRequestMessage request,
        byte[] bodyBytes,
        BridgeRequestContext requestContext
    )
    {
        try
        {
            return AndroidBridgeDispatcher.Run(
                () =>
                {
                    if (!TryGetGodotApp(out var app))
                        throw new InvalidOperationException("GodotApp Java bridge is unavailable");

                    return (string)app.Call(
                        HttpRequestBridgeMethod,
                        request.Method.Method,
                        request.RequestUri?.ToString() ?? string.Empty,
                        SerializeHeaders(request),
                        Convert.ToBase64String(bodyBytes),
                        _timeoutMs
                    );
                }
            );
        }
        catch (InvalidCastException ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned a non-string response for {requestContext.Description}",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge call failed before returning a response for {requestContext.Description}",
                ex
            );
        }
    }
}
