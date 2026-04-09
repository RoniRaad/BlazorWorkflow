using System.Reflection;
using BlazorWorkflow.Flow.Attributes;
using BlazorWorkflow.Flow.BaseNodes;

namespace BlazorWorkflow.Helpers
{
    /// <summary>
    /// Registry for node types and assemblies that contain node methods.
    /// Allows library consumers to register their own custom nodes.
    /// </summary>
    public static class NodeRegistry
    {
        private static readonly HashSet<Type> _registeredTypes = new();
        private static readonly HashSet<Assembly> _registeredAssemblies = new();
        private static readonly Dictionary<string, string> _sectionDescriptions = new(StringComparer.OrdinalIgnoreCase);
        private static bool _includeDefaultNodes = true;
        private static bool _initialized = false;

        static NodeRegistry()
        {
            // Top-level section descriptions
            _sectionDescriptions["Math"] = "Arithmetic, trigonometry, and numeric operations";
            _sectionDescriptions["Math.Arithmetic"] = "Basic integer math: add, subtract, multiply, divide";
            _sectionDescriptions["Math.Floating Point"] = "Double-precision arithmetic, powers, logarithms, and rounding";
            _sectionDescriptions["Math.Trigonometry"] = "Sin, cos, and tan functions";
            _sectionDescriptions["Math.Range"] = "Clamping, interpolation, and range mapping";
            _sectionDescriptions["Math.Aggregate"] = "Sum, average, min, and max over arrays";

            _sectionDescriptions["Logic"] = "Conditions, comparisons, and boolean operations";
            _sectionDescriptions["Logic.Boolean"] = "AND, OR, XOR, NOT, and ternary gates";
            _sectionDescriptions["Logic.Comparison"] = "Equality, ordering, null checks, and tolerance checks";
            _sectionDescriptions["Logic.Flow"] = "Conditional branching for workflow execution";

            _sectionDescriptions["Collections"] = "Create, filter, and manipulate collections";
            _sectionDescriptions["Collections.Access"] = "Read elements by index, count, first, or last";
            _sectionDescriptions["Collections.Transform"] = "Map, filter, sort, reverse, and slice";
            _sectionDescriptions["Collections.Aggregate"] = "Reduce a collection to a single value";
            _sectionDescriptions["Collections.Search"] = "Find elements and check membership";
            _sectionDescriptions["Collections.Modify"] = "Add, remove, insert, concat, and generate";
            _sectionDescriptions["Collections.Iteration"] = "Loop over items with flow-control ports";

            _sectionDescriptions["Strings"] = "Search, replace, regex, encoding, and text manipulation";
            _sectionDescriptions["Convert"] = "Parse, convert between types, and access nested properties";
            _sectionDescriptions["DateTime"] = "Dates, times, formatting, and arithmetic";
            _sectionDescriptions["HTTP"] = "Make HTTP requests and handle web APIs";
            _sectionDescriptions["Events"] = "Entry points that trigger workflow execution";
            _sectionDescriptions["Loops"] = "Iterate with for, repeat, and while loops";

            _sectionDescriptions["Utility"] = "Logging, randomness, and general helpers";
            _sectionDescriptions["Utility.Logging"] = "Write messages to stdout and stderr";
            _sectionDescriptions["Utility.Random"] = "Generate random numbers and booleans";
            _sectionDescriptions["Utility.Misc"] = "Delays, GUIDs, and user prompts";

            _sectionDescriptions["Workflow"] = "Compose and run nested workflows";
            _sectionDescriptions["Examples"] = "Sample nodes to learn from";
        }

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
        /// Register a description for a node section.
        /// Descriptions appear in the Add Node modal to help users understand what each section contains.
        /// </summary>
        /// <param name="section">The section name (case-insensitive match)</param>
        /// <param name="description">A short description of the section</param>
        public static void RegisterSectionDescription(string section, string description)
        {
            if (string.IsNullOrWhiteSpace(section))
                throw new ArgumentException("Section name cannot be empty", nameof(section));

            _sectionDescriptions[section] = description;
        }

        /// <summary>
        /// Get the description for a section, or null if none is registered.
        /// </summary>
        public static string? GetSectionDescription(string section)
        {
            return _sectionDescriptions.TryGetValue(section, out var desc) ? desc : null;
        }

        /// <summary>
        /// Clear all registered types and assemblies.
        /// Useful for testing scenarios.
        /// </summary>
        public static void Clear()
        {
            _registeredTypes.Clear();
            _registeredAssemblies.Clear();
            _sectionDescriptions.Clear();
            _includeDefaultNodes = true;
            _initialized = false;
        }

        /// <summary>
        /// Returns true if the given method belongs to a registered node type
        /// and is marked with [BlazorFlowNodeMethod]. Used during deserialization
        /// to prevent arbitrary method invocation from tampered workflow JSON.
        /// </summary>
        internal static bool IsAllowedMethod(MethodInfo? method)
        {
            if (method is null || method.DeclaringType is null)
                return false;

            // Method must have the [BlazorFlowNodeMethod] attribute
            if (method.GetCustomAttribute<BlazorFlowNodeMethodAttribute>() is null)
                return false;

            // Declaring type must be in the set of registered types
            var registeredTypes = GetRegisteredTypes();
            return registeredTypes.Contains(method.DeclaringType);
        }

        /// <summary>
        /// Get all types that should be scanned for node methods.
        /// </summary>
        internal static IEnumerable<Type> GetRegisteredTypes()
        {
            var types = new HashSet<Type>();

            types.Add(typeof(CoreNodes));

            // Add default built-in node types if enabled
            if (_includeDefaultNodes)
            {
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
