using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlazorWorkflow.Models.NodeV2;

namespace BlazorWorkflow.Helpers
{
    /// <summary>
    /// Helper methods for generating smart default parameter mappings based on types and naming conventions
    /// </summary>
    public static class MappingHelpers
    {
        /// <summary>
        /// Generate smart output mappings based on return type semantics
        /// </summary>
        public static List<PathMapEntry> GenerateSmartOutputMappings(MethodInfo? method, List<(string? Type, string? Name)>? returnProps)
        {
            var mappings = new List<PathMapEntry>();

            if (returnProps == null || method == null)
                return mappings;

            var returnType = method.ReturnType;

            foreach (var prop in returnProps)
            {
                if (prop.Name != null)
                {
                    string targetName = TypeHelpers.ToCamelCase(prop.Name);
                    if (returnProps.Count == 1)
                    {
                        targetName = GetSmartOutputTarget(prop.Name, prop.Type, returnType);
                    }

                    mappings.Add(new PathMapEntry
                    {
                        From = prop.Name,
                        To = targetName
                    });
                }
            }

            return mappings;
        }

        public static List<PathMapEntry> GenerateDefaultInputMappings(MethodInfo? method)
        {
            var mappings = new List<PathMapEntry>();
            var parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();

            foreach (var param in parameters)
            {
                // Skip auto-injected parameters (NodeContext, IServiceProvider) as they're handled automatically
                if (!TypeHelpers.ShouldExposeParameter(param))
                    continue;

                // Check if parameter has DrawflowInputField attribute (means it should be a literal)
                var hasInputFieldAttr = param.GetCustomAttribute<BlazorWorkflow.Flow.Attributes.BlazorFlowInputFieldAttribute>() != null;

                string defaultValue;
                if (hasInputFieldAttr)
                {
                    // For input fields, use the parameter's declared default value if available,
                    // otherwise generate a placeholder literal
                    if (param.HasDefaultValue && param.DefaultValue != null)
                    {
                        defaultValue = param.DefaultValue.ToString() ?? GeneratePlaceholderLiteral(param.ParameterType, param.Name);
                    }
                    else
                    {
                        defaultValue = GeneratePlaceholderLiteral(param.ParameterType, param.Name);
                    }
                }
                else
                {
                    // For regular parameters, suggest mapping from input payload using template syntax
                    defaultValue = $"{{{{input.{param.Name}}}}}";
                }

                mappings.Add(new PathMapEntry
                {
                    From = defaultValue,
                    To = param.Name ?? string.Empty
                });
            }

            return mappings;
        }


        /// <summary>
        /// Find a smart output mapping target based on property name and return type
        /// </summary>
        private static string GetSmartOutputTarget(string outputPropName, string? outputTypeName, Type? methodReturnType)
        {
            var lowerProp = outputPropName.ToLowerInvariant();

            // For generic "result" property, apply type-based semantic naming
            if (lowerProp == "result")
            {
                // Boolean result -> "condition" (for If, While, Ternary nodes)
                 if (IsBooleanType(methodReturnType, outputTypeName))
                    return "condition";

                // Collection result -> "collection" (for ForEach, Map nodes)
                if (IsCollectionType(methodReturnType, outputTypeName))
                    return "collection";
            }

            // Boolean-named outputs -> "condition"
            if (IsBooleanOutputName(lowerProp))
                return "condition";

            // Collection-named outputs -> "collection"
            if (IsCollectionOutputName(lowerProp))
                return "collection";

            // String-named outputs -> "text"
            if (IsStringOutputName(lowerProp))
                return "text";

            // Default: camelCase the property name
            return TypeHelpers.ToCamelCase(outputPropName);
        }

        public static string GeneratePlaceholderLiteral(Type paramType, string? paramName)
        {
            // Generate helpful placeholder values based on type
            if (paramType == typeof(string))
                return $"\"\"";  // Empty string placeholder
            if (paramType == typeof(int))
                return "0";
            if (paramType == typeof(double) || paramType == typeof(float))
                return "0.0";
            if (paramType == typeof(bool))
                return "false";
            if (paramType == typeof(DateTime))
                return $"\"{DateTime.Now:yyyy-MM-dd}\"";

            // For other types, try to provide a sensible default
            return "\"\"";
        }

        #region Type Checking Methods

        private static bool IsBooleanType(Type? returnType, string? typeName)
        {
            // Unwrap Task<T> if needed
            returnType = TypeHelpers.UnwrapTaskType(returnType);

            if (returnType == null)
                return false;

            // Check if it's a bool
            if (returnType == typeof(bool) || returnType == typeof(bool?))
                return true;

            // Check by type name as fallback
            if (!string.IsNullOrEmpty(typeName))
            {
                var lowerType = typeName.ToLowerInvariant();
                return lowerType.Contains("boolean") || lowerType == "bool";
            }

            return false;
        }

        private static bool IsCollectionType(Type? returnType, string? typeName)
        {
            // Unwrap Task<T> if needed
            returnType = TypeHelpers.UnwrapTaskType(returnType);

            if (returnType == null)
                return false;

            // Check if it's an array
            if (returnType.IsArray)
                return true;

            // Check if it's a generic collection (List<T>, IEnumerable<T>, etc.)
            if (returnType.IsGenericType)
            {
                var genericDef = returnType.GetGenericTypeDefinition();
                return genericDef == typeof(List<>) ||
                       genericDef == typeof(IEnumerable<>) ||
                       genericDef == typeof(ICollection<>) ||
                       genericDef == typeof(IList<>);
            }

            // Check by type name as fallback
            if (!string.IsNullOrEmpty(typeName))
            {
                var lowerType = typeName.ToLowerInvariant();
                return lowerType.Contains("list") ||
                       lowerType.Contains("array") ||
                       lowerType.Contains("collection") ||
                       lowerType.Contains("enumerable");
            }

            return false;
        }

        private static bool IsStringType(Type? returnType, string? typeName)
        {
            // Unwrap Task<T> if needed
            returnType = TypeHelpers.UnwrapTaskType(returnType);

            if (returnType == null)
                return false;

            // Check if it's a string
            if (returnType == typeof(string))
                return true;

            // Check by type name as fallback
            if (!string.IsNullOrEmpty(typeName))
            {
                var lowerType = typeName.ToLowerInvariant();
                return lowerType == "string";
            }

            return false;
        }

        private static bool IsIntegerType(Type? returnType, string? typeName)
        {
            // Unwrap Task<T> if needed
            returnType = TypeHelpers.UnwrapTaskType(returnType);

            if (returnType == null)
                return false;

            // Check if it's an integer type
            if (returnType == typeof(int) || returnType == typeof(int?) ||
                returnType == typeof(long) || returnType == typeof(long?) ||
                returnType == typeof(short) || returnType == typeof(short?) ||
                returnType == typeof(byte) || returnType == typeof(byte?))
                return true;

            // Check by type name as fallback
            if (!string.IsNullOrEmpty(typeName))
            {
                var lowerType = typeName.ToLowerInvariant();
                return lowerType == "int32" || lowerType == "int64" ||
                       lowerType == "int16" || lowerType == "byte" ||
                       lowerType == "integer" || lowerType == "int";
            }

            return false;
        }

        #endregion

        #region Name Pattern Matching

        private static bool IsBooleanOutputName(string name)
        {
            return name == "success" || name == "valid" || name == "isvalid" ||
                   name == "enabled" || name == "active" || name == "flag" ||
                   name == "condition" || name.StartsWith("is") || name.StartsWith("has");
        }

        private static bool IsCollectionOutputName(string name)
        {
            return name == "results" || name == "items" || name == "collection" ||
                   name == "list" || name == "array" || name == "data";
        }

        private static bool IsStringOutputName(string name)
        {
            return name == "text" || name == "message" || name == "content" ||
                   name == "str" || name == "description";
        }

        private static bool IsIntegerOutputName(string name)
        {
            return name == "count" || name == "number" || name == "length" ||
                   name == "size" || name == "total" || name == "index";
        }

        #endregion
    }
}
