namespace WorkforceDashboard.Data;

public sealed class WorkforceDataOptions
{
    public const string SectionName = "WorkforceData";

    /// <summary>"Sample" (bundled synthetic CSV) or "Oracle".</summary>
    public string Provider { get; set; } = "Sample";

    /// <summary>Folder holding units.csv and workforce.csv, relative to the app's content root.</summary>
    public string SampleDataPath { get; set; } = "Data/sample";

    /// <summary>Name of the connection string to use when Provider is Oracle.</summary>
    public string ConnectionStringName { get; set; } = "Workforce";

    /// <summary>If Oracle is unreachable, show the sample data with a notice instead of an error page.</summary>
    public bool FallBackToSample { get; set; } = true;

    /// <summary>How long Oracle results are cached. Headcount is a yearly snapshot, so minutes are plenty fresh.</summary>
    public int CacheMinutes { get; set; } = 10;

    /// <summary>After an Oracle failure, wait this long before trying it again.</summary>
    public int RetryAfterFailureSeconds { get; set; } = 120;
}
