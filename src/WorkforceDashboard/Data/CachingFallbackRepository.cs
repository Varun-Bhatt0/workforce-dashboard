using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace WorkforceDashboard.Data;

/// <summary>
/// Wraps the primary source (Oracle) with a short cache and, if configured, falls back to the
/// sample data when the primary source fails. After a failure the primary is not retried until
/// <see cref="WorkforceDataOptions.RetryAfterFailureSeconds"/> has passed, so a database outage
/// doesn't make every page load wait for a connection timeout.
/// </summary>
public sealed class CachingFallbackRepository(
    IWorkforceRepository primary,
    IWorkforceRepository? fallback,
    IMemoryCache cache,
    IOptions<WorkforceDataOptions> options,
    TimeProvider time,
    ILogger<CachingFallbackRepository> logger) : IWorkforceRepository
{
    private const string CacheKey = "workforce-data";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _primaryBlockedUntil = DateTimeOffset.MinValue;

    public string Name => primary.Name;

    public async Task<WorkforceDataSet> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out WorkforceDataSet? cached) && cached is not null)
            return cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            var o = options.Value;
            if (time.GetUtcNow() >= _primaryBlockedUntil)
            {
                try
                {
                    var data = await primary.LoadAsync(cancellationToken);
                    cache.Set(CacheKey, data, TimeSpan.FromMinutes(Math.Max(1, o.CacheMinutes)));
                    return data;
                }
                catch (Exception ex) when (fallback is not null && ex is not OperationCanceledException)
                {
                    _primaryBlockedUntil = time.GetUtcNow().AddSeconds(Math.Max(5, o.RetryAfterFailureSeconds));
                    logger.LogError(ex, "Could not load workforce data from {Source}; showing sample data until {RetryAt:u}",
                        primary.Name, _primaryBlockedUntil);
                }
            }

            var sample = await fallback!.LoadAsync(cancellationToken);
            return sample with { IsFallback = true };
        }
        finally
        {
            _gate.Release();
        }
    }
}
