using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static void ApplyBridgeHeaders(HttpResponseMessage response, JsonElement root)
    {
        if (!TryGetBridgeObject(root, HeadersProperty, out var headers))
            return;

        foreach (var header in headers.EnumerateObject())
        {
            if (!TryGetBridgeArray(header.Value, out var valuesElement))
                continue;

            var values = ReadBridgeHeaderValues(valuesElement);

            if (!response.Headers.TryAddWithoutValidation(header.Name, values))
                response.Content.Headers.TryAddWithoutValidation(header.Name, values);
        }
    }

    private static List<string> ReadBridgeHeaderValues(JsonElement valuesElement)
    {
        var values = new List<string>();
        foreach (var value in valuesElement.EnumerateArray())
            values.Add(GetBridgeString(value) ?? string.Empty);

        return values;
    }
}
