using WorkforceDashboard.Data;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Services;

/// <summary>Turns raw snapshots into the numbers and series shown on the dashboard. Pure functions; no I/O.</summary>
public static class WorkforceAnalytics
{
    public const string CadreKey = "cadre";
    public const string FunctionKey = "function";
    public const string GenderKey = "gender";
    public const string QualificationKey = "qualification";
    public const string TotalKey = "total";
    public const string FlowsKey = "flows";

    private static readonly string[] FunctionLabels = ["Works", "Non-works", "Projects", "Mines"];
    private static readonly string[] GenderLabels = ["Male", "Female"];
    private static readonly string[] QualificationLabels = ["Post-graduate", "Graduate", "Diploma", "ITI", "Other"];

    public static DashboardResult Build(WorkforceDataSet data, DashboardQuery query)
    {
        var scope = ResolveScope(data.Units, query);
        if (scope is null)
            return DashboardResult.Invalid($"Unknown unit or group \"{query.Scope}\".");

        var inScope = data.Snapshots
            .Where(s => scope.UnitCodes.Contains(s.UnitCode) && (query.Cadre is null || s.Cadre == query.Cadre))
            .ToList();

        var years = data.Snapshots.Select(s => s.Year).Distinct().Order().ToList();
        if (years.Count == 0)
            return DashboardResult.Invalid("The data source contains no snapshots.");

        var year = query.Year ?? years[^1];
        if (!years.Contains(year))
            return DashboardResult.Invalid($"No data for {year}. Available years: {string.Join(", ", years)}.");

        var current = inScope.Where(s => s.Year == year).ToList();
        var previous = inScope.Where(s => s.Year == year - 1).ToList();

        var dashboard = new DashboardData
        {
            Scope = scope.Key,
            ScopeLabel = scope.Label,
            ScopeDetail = scope.Detail,
            Year = year,
            Cadre = CadreNames.Display(query.Cadre),
            Years = years,
            Kpis = ComputeKpis(current, previous.Count > 0 ? previous : null),
            Breakdowns = new Dictionary<string, Breakdown>
            {
                [CadreKey] = new("By cadre", [CadreNames.Executive, CadreNames.NonExecutive], CadreValues(current)),
                [FunctionKey] = new("By function", FunctionLabels, FunctionValues(current)),
                [GenderKey] = new("By gender", GenderLabels, GenderValues(current)),
                [QualificationKey] = new("By qualification", QualificationLabels, QualificationValues(current)),
            },
            UnitComparison = scope.UnitCodes.Count > 1 ? UnitComparison(data.Units, scope.UnitCodes, current) : new("By unit", [], []),
            Trends = BuildTrends(inScope, years),
            DataSource = data.IsFallback ? $"{data.Source} (database unavailable)" : data.Source,
            IsSample = data.IsFallback || data.Source == CsvWorkforceRepository.SourceName,
            Notice = data.IsFallback
                ? "The database could not be reached, so the dashboard is showing the bundled sample data."
                : null,
        };
        return DashboardResult.Ok(dashboard);
    }

    internal static Kpis ComputeKpis(IReadOnlyCollection<WorkforceSnapshot> current, IReadOnlyCollection<WorkforceSnapshot>? previous)
    {
        var manpower = current.Sum(s => s.Manpower);
        var works = current.Sum(s => s.Works);
        var female = current.Sum(s => s.Female);
        var recruitments = current.Sum(s => s.Recruitments);
        var separations = current.Sum(s => s.Separations);

        int? change = null;
        double? changePercent = null;
        if (previous is not null)
        {
            var before = previous.Sum(s => s.Manpower);
            change = manpower - before;
            changePercent = before > 0 ? Round1(100.0 * change.Value / before) : null;
        }

        // Attrition: separations during the year over the average headcount across the year.
        var openingHeadcount = manpower - recruitments + separations;
        var averageHeadcount = (openingHeadcount + manpower) / 2.0;

        return new Kpis(
            Manpower: manpower,
            ChangeFromPreviousYear: change,
            ChangePercent: changePercent,
            WorksShare: Percent(works, manpower),
            AverageAge: WeightedAverageAge(current),
            FemaleShare: Percent(female, manpower),
            Recruitments: recruitments,
            Separations: separations,
            AttritionRate: averageHeadcount > 0 ? Round1(100.0 * separations / averageHeadcount) : 0);
    }

    /// <summary>Average age across rows, weighted by headcount (a plain mean of the row averages would be wrong).</summary>
    internal static double WeightedAverageAge(IEnumerable<WorkforceSnapshot> rows)
    {
        long people = 0;
        double ageTimesPeople = 0;
        foreach (var r in rows)
        {
            people += r.Manpower;
            ageTimesPeople += r.AverageAge * r.Manpower;
        }
        return people > 0 ? Round1(ageTimesPeople / people) : 0;
    }

    private static double Percent(int part, int whole) => whole > 0 ? Round1(100.0 * part / whole) : 0;

    /// <summary>One decimal place, halves rounded up (Math.Round's default would show 6.25% as 6.2%).</summary>
    internal static double Round1(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static int[] CadreValues(IEnumerable<WorkforceSnapshot> rows)
    {
        var list = rows as IReadOnlyCollection<WorkforceSnapshot> ?? rows.ToList();
        return [list.Where(r => r.Cadre == Cadre.Executive).Sum(r => r.Manpower),
                list.Where(r => r.Cadre == Cadre.NonExecutive).Sum(r => r.Manpower)];
    }

    private static int[] FunctionValues(IReadOnlyCollection<WorkforceSnapshot> rows) =>
        [rows.Sum(r => r.Works), rows.Sum(r => r.NonWorks), rows.Sum(r => r.Projects), rows.Sum(r => r.Mines)];

    private static int[] GenderValues(IReadOnlyCollection<WorkforceSnapshot> rows) =>
        [rows.Sum(r => r.Male), rows.Sum(r => r.Female)];

    private static int[] QualificationValues(IReadOnlyCollection<WorkforceSnapshot> rows) =>
        [rows.Sum(r => r.PostGraduate), rows.Sum(r => r.Graduate), rows.Sum(r => r.Diploma), rows.Sum(r => r.Iti),
         rows.Sum(r => r.HigherSecondary + r.Matriculate + r.BelowMatriculate)];

    private static Breakdown UnitComparison(IReadOnlyList<Unit> units, IReadOnlySet<string> codes, IReadOnlyCollection<WorkforceSnapshot> rows)
    {
        var totals = units
            .Where(u => codes.Contains(u.Code))
            .Select(u => (Unit: u, Manpower: rows.Where(r => r.UnitCode == u.Code).Sum(r => r.Manpower)))
            .OrderByDescending(x => x.Manpower)
            .ThenBy(x => x.Unit.SortOrder)
            .ToList();
        return new("By unit", totals.Select(t => t.Unit.Name).ToList(), totals.Select(t => t.Manpower).ToList());
    }

    private static Dictionary<string, Trend> BuildTrends(IReadOnlyCollection<WorkforceSnapshot> rows, IReadOnlyList<int> years)
    {
        var byYear = years.ToDictionary(y => y, y => (IReadOnlyCollection<WorkforceSnapshot>)rows.Where(r => r.Year == y).ToList());

        Trend Make(string title, IReadOnlyList<string> names, Func<IReadOnlyCollection<WorkforceSnapshot>, int[]> values)
        {
            var perYear = years.Select(y => values(byYear[y])).ToList();
            var series = names.Select((name, i) => new Series(name, perYear.Select(v => v[i]).ToList())).ToList();
            return new Trend(title, years, series);
        }

        return new Dictionary<string, Trend>
        {
            [TotalKey] = Make("Total headcount", ["Headcount"], r => [r.Sum(x => x.Manpower)]),
            [CadreKey] = Make("Headcount by cadre", [CadreNames.Executive, CadreNames.NonExecutive], CadreValues),
            [FunctionKey] = Make("Headcount by function", FunctionLabels, FunctionValues),
            [GenderKey] = Make("Headcount by gender", GenderLabels, GenderValues),
            [QualificationKey] = Make("Headcount by qualification", QualificationLabels, QualificationValues),
            [FlowsKey] = Make("Recruitments and separations", ["Recruitments", "Separations"],
                r => [r.Sum(x => x.Recruitments), r.Sum(x => x.Separations)]),
        };
    }

    internal sealed record ResolvedScope(string Key, string Label, string Detail, IReadOnlySet<string> UnitCodes);

    internal static ResolvedScope? ResolveScope(IReadOnlyList<Unit> units, DashboardQuery query)
    {
        if (query.IsAll)
            return new(DashboardQuery.AllScope, "All units", $"{units.Count} units", units.Select(u => u.Code).ToHashSet());

        if (query.IsGroup)
        {
            var members = units.Where(u => string.Equals(u.Group, query.GroupName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (members.Count == 0)
                return null;
            var name = members[0].Group;
            return new(DashboardQuery.GroupPrefix + name, name, $"{members.Count} units", members.Select(u => u.Code).ToHashSet());
        }

        var unit = units.FirstOrDefault(u => string.Equals(u.Code, query.Scope, StringComparison.OrdinalIgnoreCase));
        return unit is null ? null : new(unit.Code, unit.Name, unit.Group, new HashSet<string> { unit.Code });
    }
}

public sealed record DashboardResult(DashboardData? Data, string? Error)
{
    public static DashboardResult Ok(DashboardData data) => new(data, null);
    public static DashboardResult Invalid(string error) => new(null, error);
}
