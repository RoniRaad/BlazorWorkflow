using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlazorExecutionFlow.Helpers
{
    public static class JsonHelpers
    {
        /// <summary>
        /// Walks the JsonNode tree and, for any string value that contains valid JSON,
        /// replaces that string with its parsed JSON representation.
        ///
        /// This mutates the input node.
        /// </summary>
        public static void ExpandEmbeddedJson(this JsonNode? node)
        {
            if (node is null)
                return;

            switch (node)
            {
                case JsonObject obj:
                    // Copy to a list so we can safely modify while iterating
                    foreach (var kvp in obj.ToList())
                    {
                        var key = kvp.Key;
                        var child = kvp.Value;

                        if (TryConvertStringNodeToJson(child, out var converted))
                        {
                            obj[key] = converted;
                            ExpandEmbeddedJson(converted);
                        }
                        else
                        {
                            ExpandEmbeddedJson(child);
                        }
                    }
                    break;

                case JsonArray arr:
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var child = arr[i];

                        if (TryConvertStringNodeToJson(child, out var converted))
                        {
                            arr[i] = converted;
                            ExpandEmbeddedJson(converted);
                        }
                        else
                        {
                            ExpandEmbeddedJson(child);
                        }
                    }
                    break;

                default:
                    // JsonValue or other – handled in TryConvertStringNodeToJson if needed
                    break;
            }
        }

        /// <summary>
        /// If the node represents a string (either as a plain string or as a JsonElement with ValueKind = String)
        /// and that string contains valid JSON, parse it into a JsonNode.
        /// </summary>
        private static bool TryConvertStringNodeToJson(JsonNode? node, out JsonNode? parsed)
        {
            parsed = null;

            if (node is not JsonValue value)
                return false;

            string? str = null;

            // Case 1: Underlying type is actually string
            if (value.TryGetValue<string>(out var directString))
            {
                str = directString;
            }
            else if (value.TryGetValue<JsonElement>(out var element) &&
                     element.ValueKind == JsonValueKind.String)
            {
                // Case 2: Underlying type is JsonElement (common when created via JsonNode.Parse)
                str = element.GetString();
            }

            if (string.IsNullOrWhiteSpace(str))
                return false;

            if (!IsValidJson(str))
            {
                return false;
            }

            try
            {
                parsed = JsonNode.Parse(str);
                return parsed is not null;
            }
            catch (JsonException)
            {
                // Not valid JSON
                return false;
            }
        }

        public static bool IsProbablyJson(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;

            var span = s.AsSpan().TrimStart();
            if (span.IsEmpty) return false;

            char c = span[0];

            // Valid JSON can start with:
            // object, array, string, number, true, false, null
            return c == '{' || c == '[' || c == '"' ||
                   c == '-' || (c >= '0' && c <= '9') ||
                   c == 't' || c == 'f' || c == 'n';
        }

        public static bool IsValidJson(string? s)
        {
            if (!IsProbablyJson(s)) return false;

            try
            {
                using var _ = JsonDocument.Parse(s);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static bool IsValidJson(ReadOnlySpan<byte> utf8Json,
            JsonReaderOptions readerOptions = default)
        {
            if (utf8Json.IsEmpty)
                return false;

            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);

            if (!JsonDocument.TryParseValue(ref reader, out var doc))
                return false;

            doc.Dispose();

            var remaining = utf8Json.Slice((int)reader.BytesConsumed);
            for (int i = 0; i < remaining.Length; i++)
            {
                byte b = remaining[i];
                if (b != (byte)' ' && b != (byte)'\t' && b != (byte)'\r' && b != (byte)'\n')
                    return false;
            }

            return true;
        }
    }
}
