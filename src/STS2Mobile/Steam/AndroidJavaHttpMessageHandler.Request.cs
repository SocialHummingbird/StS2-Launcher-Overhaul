using System;
using System.Net.Http;
using Godot;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private string? CallHttpRequestBridge(
        GodotObject app,
        HttpRequestMessage request,
        byte[] bodyBytes,
        string requestDescription
    )
    {
        try
        {
            return (string)app.Call(
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
    }
}
