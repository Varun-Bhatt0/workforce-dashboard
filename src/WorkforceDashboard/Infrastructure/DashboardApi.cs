using Microsoft.AspNetCore.Http.HttpResults;
using WorkforceDashboard.Models;
using WorkforceDashboard.Services;

namespace WorkforceDashboard.Infrastructure;

internal static class DashboardApi
{
    public static void MapDashboardApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireRateLimiting(RateLimitPolicies.Api);

        // GET /api/dashboard?scope=all|group:<name>|<unit code>&year=2024&cadre=all|executive|non-executive
        api.MapGet("/dashboard", async Task<Results<Ok<DashboardData>, ProblemHttpResult>> (
            string? scope, int? year, string? cadre, DashboardService service, CancellationToken ct) =>
        {
            if (!QueryParser.TryParse(scope, year, cadre, out var query, out var error))
                return TypedResults.Problem(error, statusCode: StatusCodes.Status400BadRequest, title: "Invalid query");

            var result = await service.GetAsync(query, ct);
            return result.Data is not null
                ? TypedResults.Ok(result.Data)
                : TypedResults.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest, title: "Invalid query");
        });

        api.MapGet("/units", async (DashboardService service, CancellationToken ct) => TypedResults.Ok(await service.GetSidebarAsync(ct)));
    }
}

internal static class QueryParser
{
    public const int MaxScopeLength = 64;

    public static bool TryParse(string? scope, int? year, string? cadre, out DashboardQuery query, out string? error)
    {
        query = new DashboardQuery();
        error = null;

        var s = string.IsNullOrWhiteSpace(scope) ? DashboardQuery.AllScope : scope.Trim();
        if (s.Length > MaxScopeLength)
        {
            error = "scope is too long.";
            return false;
        }
        if (year is < 1900 or > 2200)
        {
            error = "year is out of range.";
            return false;
        }
        if (!CadreNames.TryParse(cadre, out var c))
        {
            error = "cadre must be all, executive or non-executive.";
            return false;
        }

        query = new DashboardQuery(s, year, c);
        return true;
    }
}
