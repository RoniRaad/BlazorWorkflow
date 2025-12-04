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
    }

}
