using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal readonly record struct DownloadValidationHttpEvent(string RequestId, string Scheme, string Host, int Port, string Phase, long ElapsedMs, long DispatchOrPollMs = 0);

internal sealed partial class AndroidJavaHttpMessageHandler
{
    internal static event Action<DownloadValidationHttpEvent> ValidationHttpChanged;
    private sealed record ValidationHttpRequest(Uri Uri, long StartedAt)
    {
        internal bool Polled;
    }
    private static readonly ConcurrentDictionary<string, ValidationHttpRequest> ValidationRequests = new();
    private static void ValidationHttpStart(string requestId, Uri uri, long dispatchStarted)
    {
        ValidationRequests[requestId] = new(uri, dispatchStarted);
        EmitValidationHttp(requestId, ValidationRequests[requestId], "start-returned", Environment.TickCount64 - dispatchStarted);
    }
    private static void ValidationHttpPoll(string requestId, bool ready, long pollStarted)
    {
        if (!ValidationRequests.TryGetValue(requestId, out var request)) return;
        if (!request.Polled) { request.Polled = true; EmitValidationHttp(requestId, request, "first-poll-returned", Environment.TickCount64 - pollStarted); }
        if (ready && ValidationRequests.TryRemove(requestId, out _)) EmitValidationHttp(requestId, request, "response-ready", Environment.TickCount64 - pollStarted);
    }
    private static void ValidationHttpCancel(string requestId)
    {
        if (ValidationRequests.TryRemove(requestId, out var request)) EmitValidationHttp(requestId, request, "cancelled");
    }
    private static void EmitValidationHttp(string requestId, ValidationHttpRequest request, string phase, long dispatchOrPollMs = 0)
        => ValidationHttpChanged?.Invoke(new(requestId, request.Uri?.Scheme ?? "", request.Uri?.Host ?? "", request.Uri?.Port ?? 0, phase, Environment.TickCount64 - request.StartedAt, dispatchOrPollMs));
}
