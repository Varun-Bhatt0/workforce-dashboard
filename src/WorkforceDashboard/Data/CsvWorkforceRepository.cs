using System.Globalization;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Data;

/// <summary>Reads the bundled synthetic dataset (Data/sample/*.csv). Parsed once, then served from memory.</summary>
public sealed class CsvWorkforceRepository : IWorkforceRepository
{
    private readonly string _folder;
    private readonly Lazy<Task<WorkforceDataSet>> _data;

    public CsvWorkforceRepository(string folder)
    {
        _folder = folder;
        _data = new Lazy<Task<WorkforceDataSet>>(() => Task.Run(Read));
    }

    public const string SourceName = "Sample data";

    public string Name => SourceName;

    public Task<WorkforceDataSet> LoadAsync(CancellationToken cancellationToken = default) =>
        _data.Value.WaitAsync(cancellationToken);

    private WorkforceDataSet Read()
    {
        var units = ReadCsv(Path.Combine(_folder, "units.csv"))
            .Select(r => new Unit(r["unit_code"], r["unit_name"], r["unit_group"], Int(r, "sort_order")))
            .OrderBy(u => u.SortOrder)
            .ToList();

        var snapshots = ReadCsv(Path.Combine(_folder, "workforce.csv"))
            .Select(r => new WorkforceSnapshot
            {
                UnitCode = r["unit_code"],
                SnapshotDate = DateOnly.ParseExact(r["snapshot_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Cadre = CadreNames.Parse(r["cadre"]),
                Manpower = Int(r, "manpower"),
                Works = Int(r, "works"),
                NonWorks = Int(r, "non_works"),
                Projects = Int(r, "projects"),
                Mines = Int(r, "mines"),
                Recruitments = Int(r, "recruitments"),
                Separations = Int(r, "separations"),
                AverageAge = double.Parse(r["average_age"], CultureInfo.InvariantCulture),
                Male = Int(r, "male"),
                Female = Int(r, "female"),
                PostGraduate = Int(r, "post_graduate"),
                Graduate = Int(r, "graduate"),
                Diploma = Int(r, "diploma"),
                Iti = Int(r, "iti"),
                HigherSecondary = Int(r, "higher_secondary"),
                Matriculate = Int(r, "matriculate"),
                BelowMatriculate = Int(r, "below_matriculate"),
            })
            .ToList();

        return new WorkforceDataSet(units, snapshots, Name);
    }

    private static int Int(IReadOnlyDictionary<string, string> row, string column) =>
        int.Parse(row[column], NumberStyles.Integer, CultureInfo.InvariantCulture);

    /// <summary>Minimal CSV reader: comma separated, header row, no quoted commas (the sample files never need them).</summary>
    internal static IEnumerable<IReadOnlyDictionary<string, string>> ReadCsv(string path)
    {
        using var reader = new StreamReader(path);
        var header = reader.ReadLine()?.Split(',') ?? throw new InvalidDataException($"{path} is empty.");
        string? line;
        var lineNo = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNo++;
            if (line.Length == 0)
                continue;
            var cells = line.Split(',');
            if (cells.Length != header.Length)
                throw new InvalidDataException($"{Path.GetFileName(path)} line {lineNo}: expected {header.Length} columns, found {cells.Length}.");
            var row = new Dictionary<string, string>(header.Length, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Length; i++)
                row[header[i].Trim()] = cells[i].Trim();
            yield return row;
        }
    }
}
