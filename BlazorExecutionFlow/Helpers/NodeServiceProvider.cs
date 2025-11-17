using System;

namespace BlazorExecutionFlow.Helpers
{
    /// <summary>
    /// Static registry for IServiceProvider that nodes can access during execution.
    /// Configure this at startup using ConfigureServiceProvider().
    /// </summary>
    public static class NodeServiceProvider
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Configure the service provider for node execution.
        /// Call this method once at application startup.
        /// </summary>
        /// <param name="serviceProvider">The IServiceProvider instance to use for nodes</param>
        public static void ConfigureServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the configured service provider instance.
        /// Returns null if ConfigureServiceProvider has not been called.
        /// </summary>
        internal static IServiceProvider? Instance => _serviceProvider;

        /// <summary>
        /// Clears the configured service provider.
        /// Useful for testing scenarios.
        /// </summary>
        public static void Clear()
        {
            _serviceProvider = null;
        }
    }
}
