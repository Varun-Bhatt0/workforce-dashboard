using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorkforceDashboard.Data;

namespace WorkforceDashboard.Tests;

public class CachingFallbackRepositoryTests
{
    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeRepository(string name, Func<WorkforceDataSet> load) : IWorkforceRepository
    {
        public int Calls { get; private set; }
        public string Name => name;
        public Task<WorkforceDataSet> LoadAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(load());
        }
    }

    private static CachingFallbackRepository Create(IWorkforceRepository primary, IWorkforceRepository? fallback, ManualTime time) =>
        new(primary, fallback, new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new WorkforceDataOptions { CacheMinutes = 10, RetryAfterFailureSeconds = 120 }),
            time, NullLogger<CachingFallbackRepository>.Instance);

    [Fact]
    public async Task Caches_the_primary_result()
    {
        var primary = new FakeRepository("Oracle", TestData.Small);
        var repo = Create(primary, null, new ManualTime());

        await repo.LoadAsync();
        await repo.LoadAsync();

        Assert.Equal(1, primary.Calls);
    }

    [Fact]
    public async Task Falls_back_and_waits_before_retrying_the_primary()
    {
        var time = new ManualTime();
        var primary = new FakeRepository("Oracle", () => throw new InvalidOperationException("ORA-12541: no listener"));
        var fallback = new FakeRepository("Sample data", TestData.Small);
        var repo = Create(primary, fallback, time);

        var first = await repo.LoadAsync();
        Assert.True(first.IsFallback);

        await repo.LoadAsync();
        Assert.Equal(1, primary.Calls); // still inside the back-off window

        time.Now = time.Now.AddSeconds(121);
        await repo.LoadAsync();
        Assert.Equal(2, primary.Calls);
    }

    [Fact]
    public async Task Without_a_fallback_the_error_surfaces()
    {
        var primary = new FakeRepository("Oracle", () => throw new InvalidOperationException("down"));
        var repo = Create(primary, null, new ManualTime());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.LoadAsync());
    }
}
