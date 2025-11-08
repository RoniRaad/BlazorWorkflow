using System.Text.Json;
using System.Text.Json.Nodes;

namespace DrawflowWrapper.Helpers
{
    public static class JsonNodeExtensions
    {
        public static JsonNode? GetByPath(this JsonNode node, string path, char separator = '.')
        {
            var current = node;
            foreach (var segment in path.Split(separator))
            {
                if (current is JsonObject obj)
                {
                    if (!obj.TryGetPropertyValue(segment, out var child))
                        return null;

                    current = child!;
                }
                else if (current is JsonArray arr && int.TryParse(segment, out var index))
                {
                    if (index < 0 || index >= arr.Count)
                        return null;

                    current = arr[index]!;
                }
                else
                {
                    return null;
                }
            }
            return current;
        }

        /// <summary>
        /// Sets a value in a JsonNode by dotted path, creating intermediate objects/arrays as needed.
        /// Supports object properties and numeric array indices.
        /// Example: "input.name.firstname"
        /// </summary>
        public static bool SetByPath(this JsonNode root, string path, object? value, char separator = '.')
        {
            if (root is null)
                throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be empty.", nameof(path));

            // Convert value -> JsonNode
            JsonNode? valueNode = value switch
            {
                null => null,
                JsonNode n => n,
                JsonElement e => JsonNode.Parse(e.GetRawText()),
                _ => JsonSerializer.SerializeToNode(value)
            };

            var segments = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return false;

            JsonNode current = root;

            // Walk all but last segment
            for (int i = 0; i < segments.Length - 1; i++)
            {
                string segment = segments[i];

                // Array index
                if (int.TryParse(segment, out int index))
                {
                    if (current is not JsonArray arr)
                        return false; // or throw, depending on how strict you want

                    // Grow array if needed
                    while (arr.Count <= index)
                        arr.Add(null);

                    if (arr[index] is null)
                    {
                        // Default to JsonObject for nested container
                        var newObj = new JsonObject();
                        arr[index] = newObj;
                        current = newObj;
                    }
                    else
                    {
                        current = arr[index]!;
                    }
                }
                else
                {
                    // Object property
                    if (current is not JsonObject obj)
                        return false;

                    if (!obj.TryGetPropertyValue(segment, out JsonNode? child) || child is null)
                    {
                        // Default to JsonObject for nested container
                        var newObj = new JsonObject();
                        obj[segment] = newObj;
                        current = newObj;
                    }
                    else
                    {
                        current = child;
                    }
                }
            }

            // Final segment: actually set the value
            string last = segments[^1];

            if (int.TryParse(last, out int lastIndex))
            {
                if (current is not JsonArray arr)
                    return false;

                while (arr.Count <= lastIndex)
                    arr.Add(null);

                arr[lastIndex] = valueNode;
                return true;
            }
            else
            {
                if (current is not JsonObject obj)
                    return false;

                obj[last] = valueNode;
                return true;
            }
        }
    }
}
