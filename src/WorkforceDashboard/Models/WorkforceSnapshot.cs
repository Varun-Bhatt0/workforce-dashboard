namespace WorkforceDashboard.Models;

public enum Cadre
{
    Executive,
    NonExecutive,
}

/// <summary>
/// Headcount of one cadre in one unit on a snapshot date (1 April each year).
/// Recruitments and separations are the movements in the twelve months up to the snapshot.
/// </summary>
public sealed record WorkforceSnapshot
{
    public required string UnitCode { get; init; }
    public required DateOnly SnapshotDate { get; init; }
    public required Cadre Cadre { get; init; }
    public int Manpower { get; init; }

    // Where people work. Works + NonWorks + Projects + Mines = Manpower.
    public int Works { get; init; }
    public int NonWorks { get; init; }
    public int Projects { get; init; }
    public int Mines { get; init; }

    public int Recruitments { get; init; }
    public int Separations { get; init; }
    public double AverageAge { get; init; }

    public int Male { get; init; }
    public int Female { get; init; }

    // Highest qualification. The seven columns sum to Manpower.
    public int PostGraduate { get; init; }
    public int Graduate { get; init; }
    public int Diploma { get; init; }
    public int Iti { get; init; }
    public int HigherSecondary { get; init; }
    public int Matriculate { get; init; }
    public int BelowMatriculate { get; init; }

    public int Year => SnapshotDate.Year;
}
