using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlazorExecutionFlow.Helpers
{
    public static class TypeHelpers
    {
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
                return [new() { Name = "result", Type = methodReturnType?.Name }];
            }

            // For special types with curated properties (DateTime, Guid, etc.)
            var curatedProperties = GetCuratedProperties(methodReturnType);
            if (curatedProperties != null)
            {
                return curatedProperties;
            }

            // For complex objects, return all public instance properties
            var returnProperties = methodReturnType?.GetProperties(BindingFlags.Instance | BindingFlags.Public) ?? [];
            return [.. returnProperties.Select<PropertyInfo, (string? Type, string? Name)>(x => new() { Name = x.Name, Type = methodReturnType?.Name })];
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
                return
                [
                    new() { Name = "Year", Type = type.Name },
                    new() { Name = "Month", Type = type.Name },
                    new() { Name = "Day", Type = type.Name },
                    new() { Name = "Hour", Type = type.Name },
                    new() { Name = "Minute", Type = type.Name },
                    new() { Name = "Second", Type = type.Name },
                    new() { Name = "Millisecond", Type = type.Name },
                    new() { Name = "DayOfWeek", Type = type.Name },
                    new() { Name = "DayOfYear", Type = type.Name },
                    new() { Name = "Date", Type = type.Name },            // Date part only (DateTime)
                    new() { Name = "TimeOfDay", Type = type.Name },       // Time part only (TimeSpan)
                    new() { Name = "Ticks", Type = type.Name }            // For precise calculations
                ];
            }

            // TimeSpan - provide useful duration components (only actual properties)
            if (type == typeof(TimeSpan))
            {
                return
                [
                    new() { Name = "TotalDays", Type = type.Name },
                    new() { Name = "TotalHours", Type = type.Name },
                    new() { Name = "TotalMinutes", Type = type.Name },
                    new() { Name = "TotalSeconds", Type = type.Name },
                    new() { Name = "TotalMilliseconds", Type = type.Name },
                    new() { Name = "Days", Type = type.Name },
                    new() { Name = "Hours", Type = type.Name },
                    new() { Name = "Minutes", Type = type.Name },
                    new() { Name = "Seconds", Type = type.Name },
                    new() { Name = "Milliseconds", Type = type.Name },
                    new() { Name = "Ticks", Type = type.Name }
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
    }
}
