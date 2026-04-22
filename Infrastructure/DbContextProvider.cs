using Microsoft.EntityFrameworkCore;
using MatinPower.Server.Models;

namespace MatinPower.Infrastructure
{
    /// <summary>
    /// Provides DbContext instances from the DI container's pool
    /// This improves performance by reusing contexts instead of creating new ones
    /// </summary>
    public static class DbContextProvider
    {
        private static IServiceProvider? _serviceProvider;
        private static string? _connectionString;

        /// <summary>
        /// Initialize the provider with the application's service provider
        /// Call this once during application startup
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider, string connectionString)
        {
            _serviceProvider = serviceProvider;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets a new DbContext instance configured with the connection string
        /// Uses the cached connection string to avoid reading from file each time
        /// </summary>
        private static DbContextOptions<MatinPowerDbContext>? _cachedOptions;

        public static MatinPowerDbContext CreateContext()
        {
            if (_connectionString == null)
            {
                return new MatinPowerDbContext();
            }

            if (_cachedOptions == null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<MatinPowerDbContext>();
                optionsBuilder.UseSqlServer(_connectionString);
                // LazyLoadingProxies disabled - use .Include() for related data
                _cachedOptions = optionsBuilder.Options;
            }

            return new MatinPowerDbContext(_cachedOptions);
        }

        /// <summary>
        /// Gets connection string (cached)
        /// </summary>
        public static string? ConnectionString => _connectionString;
    }
}
