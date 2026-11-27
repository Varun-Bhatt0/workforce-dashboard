using WorkforceDashboard.Models;
using WorkforceDashboard.Services;

namespace WorkforceDashboard.Tests;

public class WorkforceAnalyticsTests
{
    private static DashboardData Build(DashboardQuery query)
    {
        var result = WorkforceAnalytics.Build(TestData.Small(), query);
        Assert.Null(result.Error);
        return result.Data!;
    }

    [Fact]
    public void Defaults_to_all_units_and_the_latest_year()
    {
        var d = Build(new DashboardQuery());
        Assert.Equal("all", d.Scope);
        Assert.Equal(2024, d.Year);
        Assert.Equal(500, d.Kpis.Manpower);
        Assert.Equal([2023, 2024], d.Years);
    }

    [Fact]
    public void Change_is_measured_against_the_previous_year()
    {
        var d = Build(new DashboardQuery());
        Assert.Equal(0, d.Kpis.ChangeFromPreviousYear);
        Assert.Equal(0.0, d.Kpis.ChangePercent);

        var first = Build(new DashboardQuery(Year: 2023));
        Assert.Null(first.Kpis.ChangeFromPreviousYear);
    }

    [Fact]
    public void Average_age_is_weighted_by_headcount()
    {
        // Plant A 2024: 110 people at 40 and 290 at 50 → (4400 + 14500) / 400 = 47.25
        var d = Build(new DashboardQuery("AAA"));
        Assert.Equal(47.3, d.Kpis.AverageAge);
    }

    [Fact]
    public void Attrition_uses_the_average_of_opening_and_closing_headcount()
    {
        // Plant A 2024: closing 400, recruitments 25, separations 25 → opening 400, average 400; 25 / 400 = 6.25%
        var d = Build(new DashboardQuery("AAA"));
        Assert.Equal(25, d.Kpis.Recruitments);
        Assert.Equal(25, d.Kpis.Separations);
        Assert.Equal(6.3, d.Kpis.AttritionRate);
    }

    [Fact]
    public void Shares_are_percentages_of_headcount()
    {
        var d = Build(new DashboardQuery("AAA"));
        Assert.Equal(5.0, d.Kpis.FemaleShare); // 20 of 400
        Assert.Equal(86.3, d.Kpis.WorksShare); // 345 of 400
    }

    [Fact]
    public void Cadre_filter_limits_every_number()
    {
        var d = Build(new DashboardQuery(Cadre: Cadre.Executive));
        Assert.Equal(150, d.Kpis.Manpower);
        Assert.Equal("Executive", d.Cadre);
        Assert.Equal([150, 0], d.Breakdowns[WorkforceAnalytics.CadreKey].Values);
    }

    [Fact]
    public void Group_scope_includes_only_its_units_and_compares_them()
    {
        var d = Build(new DashboardQuery("group:plants"));
        Assert.Equal("Plants", d.ScopeLabel);
        Assert.Equal(440, d.Kpis.Manpower);
        Assert.Equal(["Plant A", "Plant B"], d.UnitComparison.Labels);
        Assert.Equal([400, 40], d.UnitComparison.Values);
    }

    [Fact]
    public void Single_unit_has_no_unit_comparison()
    {
        var d = Build(new DashboardQuery("hq"));
        Assert.Equal("Head Office", d.ScopeLabel);
        Assert.Empty(d.UnitComparison.Labels);
    }

    [Fact]
    public void Unit_comparison_is_sorted_largest_first()
    {
        var d = Build(new DashboardQuery());
        Assert.Equal(d.UnitComparison.Values.OrderByDescending(v => v), d.UnitComparison.Values);
    }

    [Fact]
    public void Trend_series_add_up_to_the_total_each_year()
    {
        var d = Build(new DashboardQuery());
        var total = d.Trends[WorkforceAnalytics.TotalKey].Series[0].Values;
        foreach (var key in new[] { WorkforceAnalytics.CadreKey, WorkforceAnalytics.FunctionKey, WorkforceAnalytics.GenderKey, WorkforceAnalytics.QualificationKey })
        {
            var series = d.Trends[key].Series;
            for (var i = 0; i < total.Count; i++)
                Assert.Equal(total[i], series.Sum(s => s.Values[i]));
        }
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("group:nothing")]
    public void Unknown_scope_is_an_error(string scope)
    {
        var result = WorkforceAnalytics.Build(TestData.Small(), new DashboardQuery(scope));
        Assert.Null(result.Data);
        Assert.Contains(scope, result.Error);
    }

    [Fact]
    public void Missing_year_is_an_error_that_lists_the_available_years()
    {
        var result = WorkforceAnalytics.Build(TestData.Small(), new DashboardQuery(Year: 2019));
        Assert.Contains("2023, 2024", result.Error);
    }

    [Fact]
    public void Fallback_data_is_flagged_on_the_dashboard()
    {
        var data = TestData.Small() with { IsFallback = true };
        var d = WorkforceAnalytics.Build(data, new DashboardQuery()).Data!;
        Assert.NotNull(d.Notice);
        Assert.True(d.IsSample);
    }
}
