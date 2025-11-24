using System.Reflection;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Flow.BaseNodes;

namespace BlazorExecutionFlow.Helpers
{
    /// <summary>
    /// Registry for node types and assemblies that contain node methods.
    /// Allows library consumers to register their own custom nodes.
    /// </summary>
    public static class NodeRegistry
    {
        private static readonly HashSet<Type> _registeredTypes = new();
        private static readonly HashSet<Assembly> _registeredAssemblies = new();
        private static bool _includeDefaultNodes = true;
        private static bool _initialized = false;

        /// <summary>
        /// Register a specific type that contains static methods marked with [BlazorFlowNodeMethod].
        /// </summary>
        /// <param name="type">The type containing node methods</param>
        public static void RegisterNodeType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _registeredTypes.Add(type);
            _initialized = true;
        }

        /// <summary>
        /// Register a specific type that contains static methods marked with [BlazorFlowNodeMethod].
        /// </summary>
        /// <typeparam name="T">The type containing node methods</typeparam>
        public static void RegisterNodeType<T>() where T : class
        {
            RegisterNodeType(typeof(T));
        }

        /// <summary>
        /// Register an entire assembly to scan for types with node methods.
        /// All types in the assembly with methods marked with [BlazorFlowNodeMethod] will be discovered.
        /// </summary>
        /// <param name="assembly">The assembly to scan</param>
        public static void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            _registeredAssemblies.Add(assembly);
            _initialized = true;
        }

        /// <summary>
        /// Register the calling assembly to scan for node methods.
        /// Convenient helper that automatically registers the assembly of the caller.
        /// </summary>
        public static void RegisterCallingAssembly()
        {
            // Get the assembly of the caller (2 frames up: this method -> caller)
            var callingAssembly = Assembly.GetCallingAssembly();
            RegisterAssembly(callingAssembly);
        }

        /// <summary>
        /// Control whether to include the default BaseNodeCollection nodes.
        /// Default is true. Set to false if you want only your custom nodes.
        /// </summary>
        /// <param name="include">True to include default nodes, false to exclude</param>
        public static void IncludeDefaultNodes(bool include)
        {
            _includeDefaultNodes = include;
            _initialized = true;
        }

        /// <summary>
        /// Clear all registered types and assemblies.
        /// Useful for testing scenarios.
        /// </summary>
        public static void Clear()
        {
            _registeredTypes.Clear();
            _registeredAssemblies.Clear();
            _includeDefaultNodes = true;
            _initialized = false;
        }

        /// <summary>
        /// Get all types that should be scanned for node methods.
        /// </summary>
        internal static IEnumerable<Type> GetRegisteredTypes()
        {
            var types = new HashSet<Type>();

            // Add default built-in node types if enabled
            if (_includeDefaultNodes)
            {
                types.Add(typeof(CoreNodes));
                types.Add(typeof(HttpNodes));
                types.Add(typeof(BaseNodeCollection));
                types.Add(typeof(CollectionNodes));
                types.Add(typeof(AdvancedIterationNodes));
            }

            // Add explicitly registered types
            foreach (var type in _registeredTypes)
            {
                types.Add(type);
            }

            // Scan registered assemblies for types with node methods
            foreach (var assembly in _registeredAssemblies)
            {
                try
                {
                    var assemblyTypes = assembly.GetTypes()
                        .Where(t => t.IsClass &&
                                    t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                        .Any(m => m.GetCustomAttribute<BlazorFlowNodeMethodAttribute>() != null));

                    foreach (var type in assemblyTypes)
                    {
                        types.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // If we can't load some types, just use the ones we can
                    var loadedTypes = ex.Types.Where(t => t != null &&
                                                          t.IsClass &&
                                                          t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                              .Any(m => m.GetCustomAttribute<BlazorFlowNodeMethodAttribute>() != null));
                    foreach (var type in loadedTypes)
                    {
                        types.Add(type!);
                    }
                }
            }

            // If nothing was registered, always include default nodes
            if (!_initialized && types.Count == 0)
            {
                types.Add(typeof(BaseNodeCollection));
            }

            return types;
        }
    }
}
