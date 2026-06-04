using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static HttpResponseMessage CreateBridgeResponse(
        HttpRequestMessage request,
        JsonElement root,
        int status
    )
    {
        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            RequestMessage = request,
        };

        if (TryGetBridgeString(root, ReasonProperty, out var reason))
            response.ReasonPhrase = reason;

        return response;
    }
}
