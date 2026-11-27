using WorkforceDashboard.Data;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Tests;

internal static class TestData
{
    public static readonly IReadOnlyList<Unit> Units =
    [
        new("AAA", "Plant A", "Plants", 1),
        new("BBB", "Plant B", "Plants", 2),
        new("HQ", "Head Office", "Offices", 3),
    ];

    public static WorkforceSnapshot Row(string unit, int year, Cadre cadre, int manpower, double age = 40,
        int recruitments = 0, int separations = 0, int female = 0, int works = -1) => new()
    {
        UnitCode = unit,
        SnapshotDate = new DateOnly(year, 4, 1),
        Cadre = cadre,
        Manpower = manpower,
        Works = works < 0 ? manpower : works,
        NonWorks = works < 0 ? 0 : manpower - works,
        AverageAge = age,
        Recruitments = recruitments,
        Separations = separations,
        Male = manpower - female,
        Female = female,
        Graduate = manpower,
    };

    public static WorkforceDataSet Small() => new(Units,
    [
        Row("AAA", 2023, Cadre.Executive, 100, age: 40),
        Row("AAA", 2023, Cadre.NonExecutive, 300, age: 50),
        Row("BBB", 2023, Cadre.Executive, 50),
        Row("HQ", 2023, Cadre.NonExecutive, 50),
        Row("AAA", 2024, Cadre.Executive, 110, age: 40, recruitments: 20, separations: 10, female: 11, works: 55),
        Row("AAA", 2024, Cadre.NonExecutive, 290, age: 50, recruitments: 5, separations: 15, female: 9, works: 290),
        Row("BBB", 2024, Cadre.Executive, 40, age: 30),
        Row("HQ", 2024, Cadre.NonExecutive, 60, age: 45),
    ], "Test");

    public static string SampleFolder =>
        Path.Combine(AppContext.BaseDirectory, "Data", "sample");
}
