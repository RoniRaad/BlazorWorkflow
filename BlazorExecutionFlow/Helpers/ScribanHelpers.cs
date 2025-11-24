using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Models.NodeV2;
using Scriban;
using Scriban.Runtime;

namespace BlazorExecutionFlow.Helpers
{
    public static class ScribanHelpers
    {
        public static object? GetScribanObject(string? value, JsonObject inputPayload, GraphExecutionContext? executionContext, Type parameterType)
        {
            if (value is null)
            {
                return parameterType.IsValueType
                        ? Activator.CreateInstance(parameterType)
                        : null;
            }

            // Check if value is a simple path reference (no Scriban template expressions like {{ }})
            // If so, get the JsonNode directly to preserve type information (especially for arrays/objects)
            if (!value.Contains("{{") && !value.Contains("}}"))
            {
                var jsonValue = inputPayload.GetByPath(value);
                if (jsonValue != null)
                {
                    // Deserialize directly to the target type without template rendering
                    // This preserves arrays, objects, and other complex types
                    return jsonValue.CoerceToType(parameterType);
                }
            }

            // Fall back to template rendering for complex expressions
            if (executionContext is not null)
            {
                inputPayload.SetByPath("workflow.parameters", executionContext.Parameters);
            }

            var modelDict = inputPayload.ToPlainObject()!;

            var scriptObject = new ScriptObject();
            scriptObject.Import(modelDict);

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            if (parameterType == typeof(string) &&
                value is not null &&
                !value.StartsWith("\"") &&
                !value.EndsWith("\"") &&
                !value.Contains("{{"))
            {
                value = $"\"{value}\"";
            }

            var template = Template.Parse(value);
            var result = template.Render(context);

            if (result == string.Empty)
            {
                if (parameterType == typeof(string))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // Fix arrays/objects rendered by Scriban without proper JSON quoting
                // e.g., [a,b,c] should be ["a","b","c"]
                result = EnsureValidJson(result);

                var parsedResult = ParseLiteral(result);
                return parsedResult.CoerceToType(parameterType);
            }
        }


        public static string EnsureValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var trimmed = input.Trim();

            // Check if it looks like an array or object
            if (!trimmed.StartsWith("[") && !trimmed.StartsWith("{"))
                return input;

            // Try to parse as-is - if it's already valid JSON, return it
            try
            {
                JsonSerializer.Deserialize<JsonNode>(trimmed);
                return input; // Already valid JSON
            }
            catch
            {
                // Not valid JSON, try to fix it
            }

            // Fix arrays with unquoted string elements: [a,b,c] -> ["a","b","c"]
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    return FixArrayQuoting(trimmed);
                }
                catch
                {
                    // If we can't fix it, return original
                    return input;
                }
            }

            return input;
        }

        public static JsonNode? ParseLiteral(string input)
        {
            var json = input;
            var trimmed = input.TrimStart();

            if (trimmed.Length > 0 &&
                !trimmed.StartsWith("{") &&
                !trimmed.StartsWith("[") &&
                !trimmed.StartsWith("\"") &&
                !char.IsDigit(trimmed[0]) &&
                !trimmed.StartsWith("-") &&  // Allow negative numbers
                !trimmed.StartsWith("+") &&  // Allow explicit positive numbers
                !"tfn".Contains(char.ToLowerInvariant(trimmed[0])))
            {
                json = JsonSerializer.Serialize(input);
            }

            return JsonSerializer.Deserialize<JsonNode>(json);
        }

        public static string FixArrayQuoting(string arrayString)
        {
            // Remove brackets
            var content = arrayString.Substring(1, arrayString.Length - 2).Trim();

            if (string.IsNullOrEmpty(content))
                return "[]";

            // Split by comma, but respect nested structures
            var elements = SplitArrayElements(content);

            // Process each element
            var fixedElements = elements.Select(element =>
            {
                var trimmedElement = element.Trim();

                // Already quoted string
                if (trimmedElement.StartsWith("\"") && trimmedElement.EndsWith("\""))
                    return trimmedElement;

                // Nested array or object
                if (trimmedElement.StartsWith("[") || trimmedElement.StartsWith("{"))
                    return EnsureValidJson(trimmedElement);

                // Boolean literals
                if (trimmedElement == "true" || trimmedElement == "false")
                    return trimmedElement;

                // Null literal
                if (trimmedElement == "null")
                    return trimmedElement;

                // Number (int or decimal)
                if (double.TryParse(trimmedElement, out _))
                    return trimmedElement;

                // Otherwise, treat as unquoted string and quote it
                return JsonSerializer.Serialize(trimmedElement);
            });

            return $"[{string.Join(",", fixedElements)}]";
        }
        public static List<string> SplitArrayElements(string content)
        {
            var elements = new List<string>();
            var currentElement = new StringBuilder();
            var depth = 0;
            var inString = false;
            var escapeNext = false;

            foreach (var ch in content)
            {
                if (escapeNext)
                {
                    currentElement.Append(ch);
                    escapeNext = false;
                    continue;
                }

                if (ch == '\\')
                {
                    currentElement.Append(ch);
                    escapeNext = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    currentElement.Append(ch);
                    continue;
                }

                if (!inString)
                {
                    if (ch == '[' || ch == '{')
                    {
                        depth++;
                        currentElement.Append(ch);
                        continue;
                    }

                    if (ch == ']' || ch == '}')
                    {
                        depth--;
                        currentElement.Append(ch);
                        continue;
                    }

                    if (ch == ',' && depth == 0)
                    {
                        // End of current element
                        elements.Add(currentElement.ToString());
                        currentElement.Clear();
                        continue;
                    }
                }

                currentElement.Append(ch);
            }

            // Add the last element
            if (currentElement.Length > 0)
            {
                elements.Add(currentElement.ToString());
            }

            return elements;
        }
    }
}
