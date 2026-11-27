namespace WorkforceDashboard.Models;

/// <summary>Everything the dashboard page needs for one selection. Serialised as the API response.</summary>
public sealed record DashboardData
{
    public required string Scope { get; init; }
    public required string ScopeLabel { get; init; }
    public required string ScopeDetail { get; init; }
    public required int Year { get; init; }
    public required string Cadre { get; init; }
    public required IReadOnlyList<int> Years { get; init; }
    public required Kpis Kpis { get; init; }

    /// <summary>Breakdowns for the selected year: cadre, function, gender, qualification.</summary>
    public required IReadOnlyDictionary<string, Breakdown> Breakdowns { get; init; }

    /// <summary>Headcount per unit for the selected year (empty when a single unit is selected).</summary>
    public required Breakdown UnitComparison { get; init; }

    /// <summary>Year-by-year series for each breakdown, plus "total" and "flows" (recruitments vs separations).</summary>
    public required IReadOnlyDictionary<string, Trend> Trends { get; init; }

    /// <summary>Where the numbers came from, for the page footer.</summary>
    public required string DataSource { get; init; }

    /// <summary>True when the bundled synthetic dataset is being shown.</summary>
    public bool IsSample { get; init; }

    public string? Notice { get; init; }
}

public sealed record Kpis(
    int Manpower,
    int? ChangeFromPreviousYear,
    double? ChangePercent,
    double WorksShare,
    double AverageAge,
    double FemaleShare,
    int Recruitments,
    int Separations,
    double AttritionRate);

public sealed record Breakdown(string Title, IReadOnlyList<string> Labels, IReadOnlyList<int> Values);

public sealed record Trend(string Title, IReadOnlyList<int> Years, IReadOnlyList<Series> Series);

public sealed record Series(string Name, IReadOnlyList<int> Values);
