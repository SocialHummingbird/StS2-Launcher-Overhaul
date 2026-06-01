using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static void ApplyBridgeHeaders(HttpResponseMessage response, JsonElement root)
    {
        if (!root.TryGetProperty(HeadersProperty, out var headers))
            return;

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
}
