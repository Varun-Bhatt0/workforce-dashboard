using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WorkforceDashboard.Data;

namespace WorkforceDashboard.Infrastructure;

internal static class WorkforceRepositoryFactory
{
    public static IWorkforceRepository Create(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var options = services.GetRequiredService<IOptions<WorkforceDataOptions>>();
        var o = options.Value;
        var sample = new CsvWorkforceRepository(Path.Combine(environment.ContentRootPath, o.SampleDataPath));

        if (!string.Equals(o.Provider, "Oracle", StringComparison.OrdinalIgnoreCase))
            return sample;

        var connectionString = configuration.GetConnectionString(o.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"WorkforceData:Provider is Oracle but ConnectionStrings:{o.ConnectionStringName} is not set. " +
                "Set it with user secrets or an environment variable (see README).");

        return new CachingFallbackRepository(
            new OracleWorkforceRepository(connectionString),
            o.FallBackToSample ? sample : null,
            services.GetRequiredService<IMemoryCache>(),
            options,
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<ILogger<CachingFallbackRepository>>());
    }
}
