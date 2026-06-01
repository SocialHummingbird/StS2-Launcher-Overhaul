using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
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
}
