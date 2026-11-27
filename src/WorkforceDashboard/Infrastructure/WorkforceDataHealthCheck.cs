using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WorkforceDashboard.Data;

namespace WorkforceDashboard.Infrastructure;

internal sealed class WorkforceDataHealthCheck(IWorkforceRepository repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await repository.LoadAsync(cancellationToken);
            var details = new Dictionary<string, object> { ["source"] = data.Source, ["units"] = data.Units.Count, ["snapshots"] = data.Snapshots.Count };
            if (data.IsFallback)
                return HealthCheckResult.Degraded("Primary data source unavailable; serving sample data.", data: details);
            return data.Snapshots.Count > 0
                ? HealthCheckResult.Healthy(data: details)
                : HealthCheckResult.Unhealthy("No workforce snapshots found.", data: details);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Workforce data could not be loaded.", ex);
        }
    }
}

internal static class HealthResponse
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.ToDictionary(e => e.Key, e => new
            {
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data,
            }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
