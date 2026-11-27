using Oracle.ManagedDataAccess.Client;
using WorkforceDashboard.Models;

namespace WorkforceDashboard.Data;

/// <summary>
/// Reads units and snapshots from Oracle (schema in db/oracle/schema.sql).
/// Each call opens a pooled connection and disposes it; nothing is shared between requests.
/// </summary>
public sealed class OracleWorkforceRepository(string connectionString) : IWorkforceRepository
{
    internal const string UnitsSql = """
        SELECT unit_abb, unit_name, unit_group, sort_order
          FROM unit_mas
         WHERE active = 'Y'
         ORDER BY sort_order, unit_abb
        """;

    internal const string SnapshotsSql = """
        SELECT u.unit_abb, m.year_date, m.cadre, m.manpower_count,
               m.works, m.non_works, m.projects, m.mines,
               m.recruitments, m.separations, m.average_age,
               m.male, m.female,
               m.post_graduate, m.graduate, m.diploma, m.iti,
               m.higher_secondary, m.matriculate, m.below_matriculate
          FROM manpower_data m
          JOIN unit_mas u ON u.unit_cd = m.unit_cd
         WHERE u.active = 'Y'
        """;

    public string Name => "Oracle";

    public async Task<WorkforceDataSet> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var units = new List<Unit>();
        await using (var command = new OracleCommand(UnitsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                units.Add(new Unit(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? "Other units" : reader.GetString(2),
                    reader.IsDBNull(3) ? int.MaxValue : reader.GetInt32(3)));
            }
        }

        var snapshots = new List<WorkforceSnapshot>();
        await using (var command = new OracleCommand(SnapshotsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                int I(int ordinal) => reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
                snapshots.Add(new WorkforceSnapshot
                {
                    UnitCode = reader.GetString(0),
                    SnapshotDate = DateOnly.FromDateTime(reader.GetDateTime(1)),
                    Cadre = CadreNames.Parse(reader.GetString(2)),
                    Manpower = I(3),
                    Works = I(4),
                    NonWorks = I(5),
                    Projects = I(6),
                    Mines = I(7),
                    Recruitments = I(8),
                    Separations = I(9),
                    AverageAge = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
                    Male = I(11),
                    Female = I(12),
                    PostGraduate = I(13),
                    Graduate = I(14),
                    Diploma = I(15),
                    Iti = I(16),
                    HigherSecondary = I(17),
                    Matriculate = I(18),
                    BelowMatriculate = I(19),
                });
            }
        }

        return new WorkforceDataSet(units, snapshots, Name);
    }
}
