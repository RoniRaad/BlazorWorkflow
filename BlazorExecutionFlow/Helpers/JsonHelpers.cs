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
            if (string.IsNullOrWhiteSpace(s))
                return false;

            ReadOnlySpan<char> span = s.AsSpan();

            // Trim leading/trailing whitespace without allocations
            int start = 0;
            int end = span.Length - 1;

            while (start <= end && char.IsWhiteSpace(span[start])) start++;
            while (end >= start && char.IsWhiteSpace(span[end])) end--;

            if (start > end)
                return false;

            span = span.Slice(start, end - start + 1);
            if (span.IsEmpty)
                return false;

            char first = span[0];

            // Valid JSON can start with:
            // object, array, string, number, true, false, null
            switch (first)
            {
                case '{':
                    return LooksLikeObject(span);

                case '[':
                    return LooksLikeArray(span);

                case '"':
                    return LooksLikeStringLiteral(span);

                case 't':
                case 'f':
                case 'n':
                    return IsJsonLiteral(span); // true / false / null

                default:
                    if (first == '-' || (first >= '0' && first <= '9'))
                        return LooksLikeNumber(span);

                    return false;
            }
        }

        private static bool LooksLikeObject(ReadOnlySpan<char> span)
        {
            // Must start with '{' and end with '}'
            if (span.Length < 2 || span[0] != '{' || span[^1] != '}')
                return false;

            // {} is valid
            if (span.Length == 2)
                return true;

            // After '{' we expect either '}' or a quoted property name
            int i = 1;
            SkipWhitespace(span, ref i);

            if (i >= span.Length - 1)
                return false;

            if (span[i] != '"' && span[i] != '}')
                return false; // keys must be quoted in strict JSON

            return HasBalancedJsonStructure(span);
        }

        private static bool LooksLikeArray(ReadOnlySpan<char> span)
        {
            // Must start with '[' and end with ']'
            if (span.Length < 2 || span[0] != '[' || span[^1] != ']')
                return false;

            // [] is valid
            if (span.Length == 2)
                return true;

            int i = 1;
            SkipWhitespace(span, ref i);

            if (i >= span.Length - 1)
                return false;

            // After '[' we expect either ']' (empty array) or a valid value start
            char c = span[i];
            if (c != ']' && !IsValidValueStartChar(c))
                return false;

            return HasBalancedJsonStructure(span);
        }

        private static bool LooksLikeStringLiteral(ReadOnlySpan<char> span)
        {
            // JSON string literal must start and end with an unescaped double quote
            if (span.Length < 2 || span[0] != '"' || span[^1] != '"')
                return false;

            bool escaped = false;

            for (int i = 1; i < span.Length; i++)
            {
                char c = span[i];

                if (c < 0x20)
                    return false; // control characters are not allowed in JSON strings

                if (escaped)
                {
                    // After a backslash, any char is fine per this heuristic
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"' && i != span.Length - 1)
                {
                    // Found an unescaped quote before the final quote
                    return false;
                }
            }

            // Last char is '"' (checked above) and we are not in an escape state
            return !escaped;
        }

        private static bool IsJsonLiteral(ReadOnlySpan<char> span)
        {
            // true / false / null
            return span.SequenceEqual("true".AsSpan())
                || span.SequenceEqual("false".AsSpan())
                || span.SequenceEqual("null".AsSpan());
        }

        private static bool LooksLikeNumber(ReadOnlySpan<char> span)
        {
            // Very small, allocation-free JSON number validator.
            // Covers: -?0 | -?[1-9][0-9]* (integer part)
            // Optional fraction: .[0-9]+
            // Optional exponent: [eE][+-]?[0-9]+

            int i = 0;
            int len = span.Length;

            if (span[i] == '-')
            {
                i++;
                if (i >= len)
                    return false; // must have digits after '-'
            }

            if (span[i] == '0')
            {
                i++;
                // Leading 0 must not be followed by another digit
                if (i < len && span[i] >= '0' && span[i] <= '9')
                    return false;
            }
            else if (span[i] >= '1' && span[i] <= '9')
            {
                i++;
                while (i < len && span[i] >= '0' && span[i] <= '9')
                    i++;
            }
            else
            {
                return false; // not a digit
            }

            // Fraction
            if (i < len && span[i] == '.')
            {
                i++;
                if (i >= len || span[i] < '0' || span[i] > '9')
                    return false; // must have at least one digit after '.'

                while (i < len && span[i] >= '0' && span[i] <= '9')
                    i++;
            }

            // Exponent
            if (i < len && (span[i] == 'e' || span[i] == 'E'))
            {
                i++;
                if (i >= len)
                    return false;

                if (span[i] == '+' || span[i] == '-')
                {
                    i++;
                    if (i >= len)
                        return false;
                }

                if (span[i] < '0' || span[i] > '9')
                    return false;

                while (i < len && span[i] >= '0' && span[i] <= '9')
                    i++;
            }

            // Must consume entire span
            return i == len;
        }

        private static bool HasBalancedJsonStructure(ReadOnlySpan<char> span)
        {
            // Lightweight structural check:
            // - Matching {} and []
            // - Proper quote toggling with escape handling
            // Does NOT fully validate JSON tokens.

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];

                if (c < 0x20)
                    return false; // control characters are not allowed in JSON text

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                // Not inside a string
                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        depth++;
                        break;

                    case '}':
                    case ']':
                        depth--;
                        if (depth < 0)
                            return false;
                        break;

                    default:
                        break;
                }
            }

            return depth == 0 && !inString && !escaped;
        }

        private static void SkipWhitespace(ReadOnlySpan<char> span, ref int index)
        {
            while (index < span.Length && char.IsWhiteSpace(span[index]))
                index++;
        }

        private static bool IsValidValueStartChar(char c)
        {
            // Any valid JSON value can start with:
            // { [ " - digit t f n
            return c == '{'
                || c == '['
                || c == '"'
                || c == '-'
                || (c >= '0' && c <= '9')
                || c == 't'
                || c == 'f'
                || c == 'n';
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
    }
}
