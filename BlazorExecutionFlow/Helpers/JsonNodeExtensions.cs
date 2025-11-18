using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlazorExecutionFlow.Helpers
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

            JsonNode? valueNode = value switch
            {
                null => null,
                JsonNode n => n.DeepClone(),              // 👈 important change
                JsonElement e => JsonNode.Parse(e.GetRawText()),
                _ => JsonSerializer.SerializeToNode(value)
            };

            var segments = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return false;

            JsonNode current = root;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                string segment = segments[i];

                if (int.TryParse(segment, out int index))
                {
                    if (current is not JsonArray arr)
                        return false;

                    while (arr.Count <= index)
                        arr.Add(null);

                    if (arr[index] is null)
                    {
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
                    if (current is not JsonObject obj)
                        return false;

                    if (!obj.TryGetPropertyValue(segment, out JsonNode? child) || child is null)
                    {
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

        public static object? CoerceToType(this JsonNode? node, Type targetType, JsonSerializerOptions? options = null)
        {
            if (node is null)
            {
                // null -> default(T)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            try
            {
                // Uses System.Text.Json to deserialize node into the given Type
                return JsonSerializer.Deserialize(node, targetType, options);
            }
            catch
            {
                throw new InvalidCastException($"Unable to cast {node.GetValueKind()} to {targetType}"); 
            }
        }

        public static object? ToPlainObject(this JsonNode? node)
        {
            return node switch
            {
                null => null,
                JsonValue value => UnwrapJsonValue(value),

                JsonObject obj => obj.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ToPlainObject(kvp.Value)
                ),

                JsonArray arr => arr.Select(ToPlainObject).ToList(),

                _ => null
            };
        }

        /// <summary>
        /// Unwraps a JsonValue to its underlying CLR type (bool, int, string, etc.)
        /// instead of returning a JsonElement.
        /// </summary>
        private static object? UnwrapJsonValue(JsonValue value)
        {
            var element = value.GetValue<JsonElement>();

            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
                JsonValueKind.Number when element.TryGetInt64(out var longVal) => longVal,
                JsonValueKind.Number when element.TryGetDouble(out var doubleVal) => doubleVal,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Null => null,
                _ => element  // Fallback to JsonElement for unknown types
            };
        }
    }
}
