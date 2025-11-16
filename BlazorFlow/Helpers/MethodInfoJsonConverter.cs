using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class MethodInfoJsonConverter : JsonConverter<MethodInfo>
{
    public override MethodInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Read the whole JSON object into a JsonDocument for convenience
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var typeName = root.GetProperty("declaringType").GetString();
        var methodName = root.GetProperty("methodName").GetString();

        if (typeName is null || methodName is null)
            return null;

        var type = Type.GetType(typeName, throwOnError: false);
        if (type == null)
            return null;

        Type[] parameterTypes = Array.Empty<Type>();

        if (root.TryGetProperty("parameterTypes", out var paramArrayElem) && paramArrayElem.ValueKind == JsonValueKind.Array)
        {
            parameterTypes = paramArrayElem
                .EnumerateArray()
                .Select(e =>
                {
                    var tName = e.GetString();
                    return tName is null ? null : Type.GetType(tName, throwOnError: false);
                })
                .Where(t => t != null)!
                .ToArray()!;
        }

        // You can adjust BindingFlags depending on your needs
        var method = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        return method;
    }

    public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WriteString("declaringType", value.DeclaringType?.AssemblyQualifiedName);
        writer.WriteString("methodName", value.Name);

        writer.WritePropertyName("parameterTypes");
        writer.WriteStartArray();
        foreach (var param in value.GetParameters())
        {
            writer.WriteStringValue(param.ParameterType.AssemblyQualifiedName);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
