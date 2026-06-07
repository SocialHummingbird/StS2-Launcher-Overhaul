using System;
using System.Net.Http;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private const int AsyncHttpPollDelayMs = 50;
    private const int AsyncHttpCompletionGraceMs = 5000;
    private const string HttpRequestAsyncCancelBridgeMethod = "httpRequestAsyncCancel";
    private const string HttpRequestAsyncPollBridgeMethod = "httpRequestAsyncPoll";
    private const string HttpRequestAsyncStartBridgeMethod = "httpRequestAsyncStart";

    private string? CallHttpRequestBridge(
        HttpRequestMessage request,
        byte[] bodyBytes,
        BridgeRequestContext requestContext,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var requestId = StartHttpRequestBridge(
                request,
                bodyBytes
            );
            return WaitForHttpRequestBridge(
                requestId,
                requestContext,
                cancellationToken
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

    private string StartHttpRequestBridge(
        HttpRequestMessage request,
        byte[] bodyBytes
    )
        => AndroidBridgeDispatcher.Run(
            () =>
            {
                if (
                    !AndroidGodotAppBridge.TryGetInstance(
                        out var app,
                        "[Auth] Java HTTP bridge unavailable"
                    )
                )
                    throw new InvalidOperationException("GodotApp Java bridge is unavailable");

                return (string)app.Call(
                    HttpRequestAsyncStartBridgeMethod,
                    request.Method.Method,
                    request.RequestUri?.ToString() ?? string.Empty,
                    SerializeHeaders(request),
                    Convert.ToBase64String(bodyBytes),
                    _timeoutMs
                );
            }
        );

    private string WaitForHttpRequestBridge(
        string requestId,
        BridgeRequestContext requestContext,
        CancellationToken cancellationToken
    )
    {
        var deadline = Environment.TickCount64 + _timeoutMs + AsyncHttpCompletionGraceMs;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = PollHttpRequestBridge(requestId);
                if (!string.IsNullOrEmpty(response))
                    return response;

                if (Environment.TickCount64 > deadline)
                {
                    CancelHttpRequestBridge(requestId);
                    throw new HttpRequestException(
                        $"Android Java HTTP bridge timed out waiting for async response for {requestContext.Description}"
                    );
                }

                Thread.Sleep(AsyncHttpPollDelayMs);
            }
        }
        catch (OperationCanceledException)
        {
            CancelHttpRequestBridge(requestId);
            throw;
        }
        catch
        {
            CancelHttpRequestBridge(requestId);
            throw;
        }
    }

    private static string PollHttpRequestBridge(string requestId)
        => AndroidBridgeDispatcher.Run(
            () =>
            {
                if (
                    !AndroidGodotAppBridge.TryGetInstance(
                        out var app,
                        "[Auth] Java HTTP bridge unavailable"
                    )
                )
                    throw new InvalidOperationException("GodotApp Java bridge is unavailable");

                return (string)app.Call(
                    HttpRequestAsyncPollBridgeMethod,
                    requestId
                );
            }
        );

    private static void CancelHttpRequestBridge(string requestId)
    {
        try
        {
            AndroidBridgeDispatcher.Run(
                () =>
                {
                    if (
                        AndroidGodotAppBridge.TryGetInstance(
                            out var app,
                            "[Auth] Java HTTP bridge unavailable"
                        )
                    )
                        app.Call(HttpRequestAsyncCancelBridgeMethod, requestId);

                    return true;
                }
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Auth] Java HTTP bridge cancel failed: {ex.Message}");
        }
    }
}
