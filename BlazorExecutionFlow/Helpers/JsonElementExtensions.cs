using System.Text.Json;

namespace BlazorExecutionFlow.Helpers
{
    public static class JsonElementExtensions
    {
        public static object ToValue(this JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l)) return l;
                    if (element.TryGetDouble(out double d)) return d;
                    return element.GetDecimal();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Object:
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                case JsonValueKind.Array:
                    return JsonSerializer.Deserialize<List<object>>(element.GetRawText());
                case JsonValueKind.Null:
                default:
                    return null;
            }
        }
    }
}
