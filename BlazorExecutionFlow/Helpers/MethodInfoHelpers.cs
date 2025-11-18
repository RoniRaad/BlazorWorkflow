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
                : ParseParameterTypes(parts[2]);

            // Try to get the type with the full assembly-qualified name first
            Type? type = Type.GetType(typeName);

            // If that fails (version mismatch), try to get it without version information
            if (type == null)
            {
                type = TryGetTypeWithoutVersion(typeName);
            }

            if (type == null)
                throw new InvalidOperationException($"Type not found: {typeName}");

            Type[] paramTypes = parameterTypeNames
                .Select(tn =>
                {
                    var t = Type.GetType(tn);
                    if (t == null)
                    {
                        t = TryGetTypeWithoutVersion(tn);
                    }
                    return t;
                })
                .Where(tn => tn != null)
                .ToArray()!;

            var method = type.GetMethod(methodName, paramTypes);

            // If exact match fails, try to find method by name and parameter count
            if (method == null)
            {
                method = TryFindMethodBySignature(type, methodName, paramTypes);
            }

            if (method == null)
            {
                // Provide detailed error information
                var availableMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == methodName)
                    .ToList();

                var errorMsg = $"Method '{methodName}' not found on type '{type.FullName}'.\n";

                if (availableMethods.Any())
                {
                    errorMsg += $"Found {availableMethods.Count} method(s) with name '{methodName}' but parameter types don't match.\n";
                    errorMsg += "Expected parameter types:\n";
                    foreach (var pt in paramTypes)
                    {
                        errorMsg += $"  - {pt.FullName}\n";
                    }
                    errorMsg += "Available method signatures:\n";
                    foreach (var m in availableMethods)
                    {
                        var paramStr = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.FullName));
                        errorMsg += $"  - {m.Name}({paramStr})\n";
                    }
                }
                else
                {
                    errorMsg += $"No methods with name '{methodName}' found on type '{type.FullName}'.";
                }

                throw new InvalidOperationException(errorMsg);
            }

            return method;
        }

        /// <summary>
        /// Parses parameter type names from a comma-separated string, accounting for commas
        /// within assembly qualified names (which contain brackets and nested commas).
        /// </summary>
        private static string[] ParseParameterTypes(string parameterTypesString)
        {
            if (string.IsNullOrEmpty(parameterTypesString))
                return Array.Empty<string>();

            var result = new List<string>();
            var currentParam = new System.Text.StringBuilder();
            int bracketDepth = 0;

            for (int i = 0; i < parameterTypesString.Length; i++)
            {
                char c = parameterTypesString[i];

                if (c == '[')
                {
                    bracketDepth++;
                    currentParam.Append(c);
                }
                else if (c == ']')
                {
                    bracketDepth--;
                    currentParam.Append(c);
                }
                else if (c == ',' && bracketDepth == 0)
                {
                    // This comma is a parameter separator, not part of an assembly qualified name
                    var param = currentParam.ToString().Trim();
                    if (!string.IsNullOrEmpty(param))
                    {
                        result.Add(param);
                    }
                    currentParam.Clear();
                }
                else
                {
                    currentParam.Append(c);
                }
            }

            // Add the last parameter
            var lastParam = currentParam.ToString().Trim();
            if (!string.IsNullOrEmpty(lastParam))
            {
                result.Add(lastParam);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Tries to find a method by matching name and parameter types more loosely.
        /// This helps when exact type matching fails due to assembly version differences.
        /// </summary>
        private static MethodInfo? TryFindMethodBySignature(Type type, string methodName, Type[] expectedParamTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName)
                .ToList();

            foreach (var method in methods)
            {
                var methodParams = method.GetParameters();

                // Check parameter count matches
                if (methodParams.Length != expectedParamTypes.Length)
                    continue;

                // Check if parameter types are compatible
                bool allMatch = true;
                for (int i = 0; i < methodParams.Length; i++)
                {
                    var methodParamType = methodParams[i].ParameterType;
                    var expectedParamType = expectedParamTypes[i];

                    // Try exact match first
                    if (methodParamType == expectedParamType)
                        continue;

                    // Try matching by full name (ignoring assembly version)
                    if (methodParamType.FullName == expectedParamType.FullName)
                        continue;

                    // Try matching generic types
                    if (methodParamType.IsGenericType && expectedParamType.IsGenericType)
                    {
                        var methodGenericDef = methodParamType.GetGenericTypeDefinition();
                        var expectedGenericDef = expectedParamType.GetGenericTypeDefinition();

                        if (methodGenericDef.FullName == expectedGenericDef.FullName)
                        {
                            // Check generic arguments match too
                            var methodGenericArgs = methodParamType.GetGenericArguments();
                            var expectedGenericArgs = expectedParamType.GetGenericArguments();

                            if (methodGenericArgs.Length == expectedGenericArgs.Length)
                            {
                                bool argsMatch = true;
                                for (int j = 0; j < methodGenericArgs.Length; j++)
                                {
                                    if (methodGenericArgs[j].FullName != expectedGenericArgs[j].FullName)
                                    {
                                        argsMatch = false;
                                        break;
                                    }
                                }
                                if (argsMatch)
                                    continue;
                            }
                        }
                    }

                    // Types don't match
                    allMatch = false;
                    break;
                }

                if (allMatch)
                    return method;
            }

            return null;
        }

        /// <summary>
        /// Attempts to load a type by removing version information from the assembly qualified name.
        /// This allows workflows saved with different versions to be loaded.
        /// </summary>
        private static Type? TryGetTypeWithoutVersion(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
                return null;

            // Parse the assembly qualified name
            // Format: "Namespace.TypeName, AssemblyName, Version=..., Culture=..., PublicKeyToken=..."
            var parts = assemblyQualifiedName.Split(',');
            if (parts.Length < 2)
                return null;

            string typeName = parts[0].Trim();
            string assemblyName = parts[1].Trim();

            // Try with just type name and assembly name (no version)
            string simpleTypeName = $"{typeName}, {assemblyName}";
            var type = Type.GetType(simpleTypeName);

            if (type != null)
                return type;

            // If that didn't work, search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Check if this is the right assembly (by name, ignoring version)
                if (assembly.GetName().Name == assemblyName)
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }

            return null;
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
