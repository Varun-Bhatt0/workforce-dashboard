using WorkforceDashboard.Data;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Tests;

/// <summary>The bundled dataset is generated; these checks make sure it stays internally consistent.</summary>
public class SampleDataTests
{
    private static readonly Lazy<WorkforceDataSet> Data = new(() => new CsvWorkforceRepository(TestData.SampleFolder).LoadAsync().GetAwaiter().GetResult());

    [Fact]
    public void Loads_units_and_snapshots()
    {
        Assert.Equal(14, Data.Value.Units.Count);
        Assert.Equal(14 * 2 * 6, Data.Value.Snapshots.Count);
    }

    [Fact]
    public void Every_snapshot_belongs_to_a_known_unit()
    {
        var codes = Data.Value.Units.Select(u => u.Code).ToHashSet();
        Assert.All(Data.Value.Snapshots, s => Assert.Contains(s.UnitCode, codes));
    }

    [Fact]
    public void Breakdowns_add_up_to_headcount_on_every_row()
    {
        Assert.All(Data.Value.Snapshots, s =>
        {
            Assert.Equal(s.Manpower, s.Works + s.NonWorks + s.Projects + s.Mines);
            Assert.Equal(s.Manpower, s.Male + s.Female);
            Assert.Equal(s.Manpower, s.PostGraduate + s.Graduate + s.Diploma + s.Iti + s.HigherSecondary + s.Matriculate + s.BelowMatriculate);
        });
    }

    [Fact]
    public void Headcount_moves_by_recruitments_minus_separations()
    {
        foreach (var series in Data.Value.Snapshots.GroupBy(s => (s.UnitCode, s.Cadre)))
        {
            var rows = series.OrderBy(s => s.Year).ToList();
            for (var i = 1; i < rows.Count; i++)
                Assert.Equal(rows[i - 1].Manpower + rows[i].Recruitments - rows[i].Separations, rows[i].Manpower);
        }
    }

    [Fact]
    public void Every_unit_has_both_cadres_every_year()
    {
        var years = Data.Value.Snapshots.Select(s => s.Year).Distinct().ToList();
        foreach (var unit in Data.Value.Units)
            foreach (var year in years)
                foreach (var cadre in Enum.GetValues<Cadre>())
                    Assert.Single(Data.Value.Snapshots, s => s.UnitCode == unit.Code && s.Year == year && s.Cadre == cadre);
    }

    [Fact]
    public void Rejects_a_row_with_the_wrong_number_of_columns()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(folder.FullName, "units.csv"), "unit_code,unit_name,unit_group,sort_order\nA,Plant,Plants\n");
            var ex = Assert.Throws<InvalidDataException>(() => CsvWorkforceRepository.ReadCsv(Path.Combine(folder.FullName, "units.csv")).ToList());
            Assert.Contains("line 2", ex.Message);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
