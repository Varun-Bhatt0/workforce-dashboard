using WorkforceDashboard.Infrastructure;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Tests;

public class QueryParsingTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("all", null)]
    [InlineData("Executive", Cadre.Executive)]
    [InlineData("executives", Cadre.Executive)]
    [InlineData("non-executive", Cadre.NonExecutive)]
    [InlineData("Non-Executive", Cadre.NonExecutive)]
    [InlineData("non_executives", Cadre.NonExecutive)]
    public void Parses_cadre(string? input, Cadre? expected)
    {
        Assert.True(CadreNames.TryParse(input, out var cadre));
        Assert.Equal(expected, cadre);
    }

    [Fact]
    public void Rejects_unknown_cadre()
    {
        Assert.False(QueryParser.TryParse("all", null, "managers", out _, out var error));
        Assert.Contains("cadre", error);
    }

    [Fact]
    public void Rejects_absurd_year_and_overlong_scope()
    {
        Assert.False(QueryParser.TryParse("all", 99999, null, out _, out _));
        Assert.False(QueryParser.TryParse(new string('x', 65), null, null, out _, out _));
    }

    [Fact]
    public void Empty_scope_means_all_units()
    {
        Assert.True(QueryParser.TryParse("  ", 2024, "executive", out var q, out _));
        Assert.True(q.IsAll);
        Assert.Equal(2024, q.Year);
        Assert.Equal(Cadre.Executive, q.Cadre);
    }
}
