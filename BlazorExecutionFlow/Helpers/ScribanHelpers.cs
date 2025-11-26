using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Models.NodeV2;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

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
                inputPayload.Merge(executionContext.SharedContext);
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

        public static void GetWorkflowInputMappings(PathMapEntry entry, out string? parameterName)
        {
            parameterName = null;
            if (!entry.From.StartsWith("workflow.parameters"))
            {
                return;
            }
            parameterName = entry.From["workflow.parameters".Length..].TrimStart('.');
        }

        public static WorkflowOutputMap? GetWorkflowOutputMappings(PathMapEntry entry)
        {
            if (!entry.To.StartsWith("workflow.output"))
            {
                return null;
            }

            var outputPath = entry.To["workflow.output".Length..].TrimStart('.');
            return new WorkflowOutputMap
            {
                OutputPath = outputPath,
                FromParameter = entry.From,
            };
        }

        /// <summary>
        /// Returns all distinct variable paths accessed under 'workflow.input'
        /// from the given Scriban template.
        ///
        /// Example:
        ///  template: "{{ workflow.input.name }} {{ workflow.input.address.city }}"
        ///  result: [ "input.name", "input.address.city" ]
        /// </summary>
        public static IReadOnlyCollection<string> GetWorkflowInputVariables(string scribanTemplate)
        {
            if (scribanTemplate == null) throw new ArgumentNullException(nameof(scribanTemplate));

            var parsed = Template.Parse(scribanTemplate);
            if (parsed.HasErrors)
            {
                var errors = string.Join(Environment.NewLine, parsed.Messages.Select(m => m.ToString()));
                throw new ArgumentException($"Template has errors:{Environment.NewLine}{errors}");
            }

            var visitor = new WorkflowInputVisitor();
            visitor.Visit(parsed.Page);

            visitor.Paths.RemoveWhere(p => string.Equals(p, "parameters", StringComparison.OrdinalIgnoreCase));

            return visitor.Paths;
        }

        private sealed class WorkflowInputVisitor : ScriptVisitor
        {
            public HashSet<string> Paths { get; } =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public override void Visit(ScriptMemberExpression node)
            {
                AddPathIfWorkflowInput(node);
                base.Visit(node);
            }

            public override void Visit(ScriptIndexerExpression node)
            {
                AddPathIfWorkflowInput(node);
                base.Visit(node);
            }

            private void AddPathIfWorkflowInput(ScriptNode node)
            {
                var path = TryBuildPath(node);
                if (path != null)
                {
                    // path is "parameters.foo.bar" (starting at input)
                    Paths.Add(path);
                }
            }

            /// <summary>
            /// Builds a dotted path for a chain like workflow.parameters.foo.bar or workflow.parameters.items[0].name
            /// Returns "parameters.foo.bar" / "parameters.items[0].name" if it starts with workflow.input,
            /// otherwise null.
            /// </summary>
            private static string? TryBuildPath(ScriptNode node)
            {
                var segments = new List<string>();
                ScriptNode? current = node;

                while (current != null)
                {
                    switch (current)
                    {
                        case ScriptMemberExpression member:
                            {
                                // Member is usually a ScriptVariable (e.g. "name", "address", "city")
                                if (member.Member is ScriptVariable varMember)
                                {
                                    segments.Add(varMember.Name);
                                }

                                current = member.Target;
                                break;
                            }

                        case ScriptIndexerExpression indexer:
                            {
                                // Handle things like workflow.input.items[0] or ["key"]
                                if (indexer.Index is ScriptLiteral litIndex)
                                {
                                    // For numeric indexes we get [0], for strings ["key"]
                                    segments.Add($"[{litIndex.Value}]");
                                }
                                else
                                {
                                    // Non-literal index (e.g. [i]) – just mark it generically
                                    segments.Add("[*]");
                                }

                                current = indexer.Target;
                                break;
                            }

                        case ScriptVariable scriptVar:
                            {
                                // Covers ScriptVariableGlobal, ScriptVariableLocal, etc.
                                segments.Add(scriptVar.Name);
                                current = null;
                                break;
                            }

                        default:
                            current = null;
                            break;
                    }
                }

                // Built from leaf to root, so reverse
                segments.Reverse();

                // Expect ["workflow", "parameters", ...]
                if (segments.Count >= 2 &&
                    string.Equals(segments[0], "workflow", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(segments[1], "parameters", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove leading "workflow" and keep "parameters" + rest.
                    // If you want to drop "parameters" too, change Skip(1) to Skip(2).
                    var pathSegments = segments.Skip(1); // "parameters.foo.bar"
                    return string.Join(".", pathSegments);
                }

                return null;
            }
        }

        public class WorkflowOutputMap
        {
            public string OutputPath { get; set; } = string.Empty;
            public string FromParameter { get; set; } = string.Empty;
        }
    }
}
