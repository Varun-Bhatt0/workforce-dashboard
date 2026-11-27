using WorkforceDashboard.Models;

namespace WorkforceDashboard.Data;

/// <summary>Source of units and headcount snapshots. The dataset is small (units × years × cadres), so it is read whole.</summary>
public interface IWorkforceRepository
{
    string Name { get; }

    Task<WorkforceDataSet> LoadAsync(CancellationToken cancellationToken = default);
}

public sealed record WorkforceDataSet(IReadOnlyList<Unit> Units, IReadOnlyList<WorkforceSnapshot> Snapshots, string Source)
{
    public bool IsFallback { get; init; }
}
