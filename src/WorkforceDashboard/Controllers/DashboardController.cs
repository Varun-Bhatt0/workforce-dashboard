using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkforceDashboard.Infrastructure;
using WorkforceDashboard.Models;
using WorkforceDashboard.Services;

namespace WorkforceDashboard.Controllers;

public sealed class DashboardController(DashboardService service) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(string? scope, int? year, string? cadre, CancellationToken ct)
    {
        // A bad link shouldn't show an error page: fall back to the default view and say so.
        string? warning = null;
        if (!QueryParser.TryParse(scope, year, cadre, out var query, out var error))
        {
            warning = error;
            query = new DashboardQuery();
        }

        var result = await service.GetAsync(query, ct);
        if (result.Data is null)
        {
            warning = result.Error;
            result = await service.GetAsync(new DashboardQuery(), ct);
        }

        var model = new DashboardPageModel(result.Data!, await service.GetSidebarAsync(ct), warning);
        return View(model);
    }

    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorPageModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}

public sealed record DashboardPageModel(DashboardData Data, SidebarModel Sidebar, string? Warning);

public sealed record ErrorPageModel(string RequestId);
