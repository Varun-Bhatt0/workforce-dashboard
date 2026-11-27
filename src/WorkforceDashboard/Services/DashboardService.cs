using WorkforceDashboard.Data;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Services;

public sealed class DashboardService(IWorkforceRepository repository)
{
    public async Task<DashboardResult> GetAsync(DashboardQuery query, CancellationToken cancellationToken = default)
    {
        var data = await repository.LoadAsync(cancellationToken);
        return WorkforceAnalytics.Build(data, query);
    }

    public async Task<SidebarModel> GetSidebarAsync(CancellationToken cancellationToken = default)
    {
        var data = await repository.LoadAsync(cancellationToken);
        var groups = data.Units
            .GroupBy(u => u.Group)
            .OrderBy(g => g.Min(u => u.SortOrder))
            .Select(g => new SidebarGroup(g.Key, DashboardQuery.GroupPrefix + g.Key, g.OrderBy(u => u.SortOrder).ToList()))
            .ToList();
        return new SidebarModel(groups);
    }
}

public sealed record SidebarModel(IReadOnlyList<SidebarGroup> Groups);

public sealed record SidebarGroup(string Name, string Scope, IReadOnlyList<Unit> Units);
