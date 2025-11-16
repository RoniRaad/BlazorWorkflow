using System.Reflection;

namespace BlazorExecutionFlow.Helpers
{
    public static class MethodInfoHelpers
    {
        public static MethodInfo FromSerializableString(string serialized)
        {
            var parts = serialized.Split('|');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid method string format");

            string typeName = parts[0];
            string methodName = parts[1];
            string[] parameterTypeNames = string.IsNullOrEmpty(parts[2])
                ? new string[0]
                : parts[2].Split(',');

            Type type = Type.GetType(typeName);
            if (type == null) throw new InvalidOperationException("Type not found");

            Type[] paramTypes = parameterTypeNames
                .Select(tn => Type.GetType(tn))
                .Where(tn => tn != null)
                .ToArray()!;

            var method = type.GetMethod(methodName, paramTypes);
            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found on type '{typeName}'");

            return method;
        }

        public static string ToSerializableString(MethodInfo method)
        {
            string typeName = method.DeclaringType.AssemblyQualifiedName;
            string methodName = method.Name;
            string parameterTypes = string.Join(",", method.GetParameters()
                .Select(p => p.ParameterType.AssemblyQualifiedName));

            return $"{typeName}|{methodName}|{parameterTypes}";
        }
    }
}
