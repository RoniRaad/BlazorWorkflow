using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.Attributes;

namespace BlazorExecutionFlow.Helpers
{
    public static class TypeHelpers
    {
        /// <summary>
        /// Checks if a parameter has the BlazorFlowDictionaryMapping attribute,
        /// indicating it should use the dictionary mapping UI.
        /// </summary>
        public static bool HasDictionaryMappingAttribute(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<BlazorFlowDictionaryMappingAttribute>() != null;
        }


        /// <summary>
        /// Gets the return properties for a method's return type.
        /// For primitive types, strings, and collections, returns a single "result" property.
        /// For special types (DateTime, Guid, etc.), returns curated useful properties.
        /// For complex objects, returns all public instance properties.
        /// </summary>
        public static List<(string? Type, string? Name)>? GetReturnProperties(MethodInfo methodInfo)
        {
            var methodReturnType = methodInfo?.ReturnType;

            // Handle void and non-generic Task
            if (methodReturnType == typeof(void) || methodReturnType == typeof(Task))
            {
                return null;
            }

            // Unwrap Task<T> to get the actual return type
            methodReturnType = UnwrapTaskType(methodReturnType);

            // Treat single-value types (primitives, strings, collections, enums, nullables) as a single "result"
            if (ShouldTreatAsSingleValue(methodReturnType))
            {
                return [new() { Name = "result", Type = FormatTypeName(methodReturnType) }];
            }

            // For special types with curated properties (DateTime, Guid, etc.)
            var curatedProperties = GetCuratedProperties(methodReturnType);
            if (curatedProperties != null)
            {
                return curatedProperties;
            }

            // For complex objects, return all public instance properties
            var returnProperties = methodReturnType?.GetProperties(BindingFlags.Instance | BindingFlags.Public) ?? [];
            return [.. returnProperties.Select<PropertyInfo, (string? Type, string? Name)>(x => new() { Name = x.Name, Type = FormatTypeName(x.PropertyType) })];
        }

        /// <summary>
        /// Determines if a type should be treated as a single value rather than exposing its properties.
        /// This includes primitives, strings, collections (arrays, IEnumerable, etc.), enums, nullable types, Guid, and JSON types.
        /// </summary>
        public static bool ShouldTreatAsSingleValue(Type? type)
        {
            if (type == null)
                return false;

            // Primitives and strings are always single values
            if (IsPrimitiveOrString(type))
                return true;

            // object type should be treated as a single value (used by Ternary, comparison nodes, etc.)
            // Otherwise it would try to expose object's properties like GetType(), ToString(), etc.
            if (type == typeof(object))
                return true;

            // Enums should be treated as single values
            if (type.IsEnum)
                return true;

            // Nullable types (int?, bool?, etc.) should be treated as single values
            // to avoid exposing HasValue and Value properties
            if (IsNullableType(type))
                return true;

            // Guid is a value type with no useful properties to expose, treat as single value
            if (type == typeof(Guid))
                return true;

            // JSON types (JsonNode, JsonObject, JsonArray, JsonValue, JsonElement) are data containers
            // and should be treated as single values to avoid exposing infrastructure properties
            if (IsJsonType(type))
                return true;

            // Collections (arrays, lists, etc.) should be treated as single values
            // to avoid exposing infrastructure properties like Length, Count, Capacity
            if (IsCollectionType(type))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a type is a primitive type or string.
        /// </summary>
        public static bool IsPrimitiveOrString(Type? type)
        {
            return type == typeof(string) || type?.IsPrimitive == true;
        }

        /// <summary>
        /// Checks if a type is a collection type (array or implements IEnumerable).
        /// Excludes string since string implements IEnumerable but is typically treated as a primitive.
        /// </summary>
        public static bool IsCollectionType(Type? type)
        {
            if (type == null || type == typeof(string))
                return false;

            // Check if it's an array
            if (type.IsArray)
                return true;

            // Check if it implements IEnumerable (but not string)
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        /// <summary>
        /// Checks if a type is a nullable value type (Nullable&lt;T&gt;).
        /// </summary>
        public static bool IsNullableType(Type? type)
        {
            if (type == null)
                return false;

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Checks if a type is a JSON type (JsonNode, JsonObject, JsonArray, JsonValue, or JsonElement).
        /// These are data container types that should be treated as single values.
        /// </summary>
        public static bool IsJsonType(Type? type)
        {
            if (type == null)
                return false;

            // Check for JsonElement (struct)
            if (type == typeof(JsonElement))
                return true;

            // Check for JsonNode and its derived types (JsonObject, JsonArray, JsonValue)
            if (typeof(JsonNode).IsAssignableFrom(type))
                return true;

            return false;
        }

        /// <summary>
        /// Returns curated properties for special types like DateTime, Guid, TimeSpan, etc.
        /// These are hand-picked useful properties rather than all available properties.
        /// Returns null if the type doesn't have curated properties defined.
        /// </summary>
        public static List<(string? Type, string? Name)>? GetCuratedProperties(Type? type)
        {
            if (type == null)
                return null;

            // DateTime - provide useful date/time components (only actual properties)
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                var typeName = FormatTypeName(type);
                return
                [
                    new() { Name = "Year", Type = typeName },
                    new() { Name = "Month", Type = typeName },
                    new() { Name = "Day", Type = typeName },
                    new() { Name = "Hour", Type = typeName },
                    new() { Name = "Minute", Type = typeName },
                    new() { Name = "Second", Type = typeName },
                    new() { Name = "Millisecond", Type = typeName },
                    new() { Name = "DayOfWeek", Type = typeName },
                    new() { Name = "DayOfYear", Type = typeName },
                    new() { Name = "Date", Type = typeName },            // Date part only (DateTime)
                    new() { Name = "TimeOfDay", Type = typeName },       // Time part only (TimeSpan)
                    new() { Name = "Ticks", Type = typeName }            // For precise calculations
                ];
            }

            // TimeSpan - provide useful duration components (only actual properties)
            if (type == typeof(TimeSpan))
            {
                var typeName = FormatTypeName(type);
                return
                [
                    new() { Name = "TotalDays", Type = typeName },
                    new() { Name = "TotalHours", Type = typeName },
                    new() { Name = "TotalMinutes", Type = typeName },
                    new() { Name = "TotalSeconds", Type = typeName },
                    new() { Name = "TotalMilliseconds", Type = typeName },
                    new() { Name = "Days", Type = typeName },
                    new() { Name = "Hours", Type = typeName },
                    new() { Name = "Minutes", Type = typeName },
                    new() { Name = "Seconds", Type = typeName },
                    new() { Name = "Milliseconds", Type = typeName },
                    new() { Name = "Ticks", Type = typeName }
                ];
            }

            // No curated properties defined for this type
            return null;
        }

        /// <summary>
        /// Unwraps Task{T} to get the inner type T.
        /// Returns the original type if it's not a generic Task.
        /// </summary>
        public static Type? UnwrapTaskType(Type? type)
        {
            if (type == null)
                return null;

            // Check if it's Task<T> (BaseType == Task and has generic arguments)
            if (type.BaseType == typeof(Task) && type.GenericTypeArguments.Length > 0)
            {
                return type.GenericTypeArguments.First();
            }

            // Check if it's a generic type definition Task<> directly
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return type.GenericTypeArguments.First();
            }

            return type;
        }

        /// <summary>
        /// Checks if a parameter type is auto-injected (IServiceProvider, NodeContext, or JSON payload types)
        /// </summary>
        public static bool IsAutoInjectedParameter(Type parameterType)
        {
            return parameterType == typeof(IServiceProvider) ||
                   parameterType.Name == "NodeContext" ||
                   IsJsonType(parameterType);
        }

        /// <summary>
        /// Checks if a parameter is auto-injected (IServiceProvider, NodeContext, or JSON payload types)
        /// </summary>
        public static bool IsAutoInjectedParameter(ParameterInfo parameter)
        {
            return IsAutoInjectedParameter(parameter.ParameterType);
        }

        public static bool ShouldExposeParameter(ParameterInfo parameter)
        {
            var isDictionaryMapped = HasDictionaryMappingAttribute(parameter);

            return !IsAutoInjectedParameter(parameter) && !isDictionaryMapped;
        }

        /// <summary>
        /// Filters out auto-injected parameters (IServiceProvider, NodeContext, JSON payload types) from a parameter list
        /// </summary>
        public static IEnumerable<ParameterInfo> FilterAutoInjectedParameters(this ParameterInfo[] parameters)
        {
            return parameters.Where(p => !IsAutoInjectedParameter(p));
        }

        /// <summary>
        /// Converts a property or parameter name to camelCase
        /// </summary>
        public static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 1)
                return name;

            return $"{char.ToLower(name[0])}{name[1..]}";
        }

        /// <summary>
        /// Adds spaces between PascalCase words.
        /// Example: "ContainsString" -> "Contains String", "ForEachString" -> "For Each String"
        /// </summary>
        public static string AddSpacesToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new System.Text.StringBuilder(text.Length * 2);
            result.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                // Add space before uppercase letters (except if previous char is also uppercase and next is lowercase)
                if (char.IsUpper(text[i]))
                {
                    // Don't add space if:
                    // 1. Previous character is uppercase AND
                    // 2. Current is not the last character AND
                    // 3. Next character is uppercase (handling acronyms like "XMLParser")
                    if (i > 0 && char.IsUpper(text[i - 1]) &&
                        i < text.Length - 1 && char.IsUpper(text[i + 1]))
                    {
                        result.Append(text[i]);
                    }
                    else
                    {
                        result.Append(' ');
                        result.Append(text[i]);
                    }
                }
                else
                {
                    result.Append(text[i]);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Formats a type name to display properly, handling generics like List&lt;int&gt; instead of List`1
        /// </summary>
        public static string FormatTypeName(Type? type)
        {
            if (type == null)
                return "unknown";

            // Handle generic types
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var genericTypeName = genericTypeDef.Name;

                // Remove the `1, `2, etc. suffix
                var backtickIndex = genericTypeName.IndexOf('`');
                if (backtickIndex > 0)
                {
                    genericTypeName = genericTypeName.Substring(0, backtickIndex);
                }

                // Get the generic arguments
                var genericArgs = type.GetGenericArguments();
                var argNames = string.Join(", ", genericArgs.Select(FormatTypeName));

                return $"{genericTypeName}<{argNames}>";
            }

            // Handle arrays
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{FormatTypeName(elementType)}[]";
            }

            // Handle nullable types
            if (Nullable.GetUnderlyingType(type) != null)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                return $"{FormatTypeName(underlyingType)}?";
            }

            // Use friendly names for common types
            return type.Name switch
            {
                "Int32" => "int",
                "Int64" => "long",
                "Int16" => "short",
                "Byte" => "byte",
                "Double" => "double",
                "Single" => "float",
                "Decimal" => "decimal",
                "Boolean" => "bool",
                "String" => "string",
                "Object" => "object",
                _ => type.Name
            };
        }
    }
}
