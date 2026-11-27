namespace WorkforceDashboard.Models;

public static class CadreNames
{
    public const string Executive = "Executive";
    public const string NonExecutive = "Non-Executive";
    public const string All = "All";

    public static string Display(Cadre? cadre) => cadre switch
    {
        Models.Cadre.Executive => Executive,
        Models.Cadre.NonExecutive => NonExecutive,
        _ => All,
    };

    /// <summary>Parses the stored value ("Executive" / "Non-Executive"); also accepts the query-string forms.</summary>
    public static Cadre Parse(string value) => TryParse(value, out var cadre)
        ? cadre ?? throw new FormatException("\"All\" is not a stored cadre.")
        : throw new FormatException($"Unknown cadre \"{value}\".");

    /// <summary>"all"/empty → null (both cadres). Returns false for anything unrecognised.</summary>
    public static bool TryParse(string? value, out Cadre? cadre)
    {
        cadre = null;
        var v = (value ?? string.Empty).Trim().Replace("-", "").Replace("_", "").Replace(" ", "").ToLowerInvariant();
        switch (v)
        {
            case "":
            case "all":
                return true;
            case "executive":
            case "executives":
            case "exec":
                cadre = Cadre.Executive;
                return true;
            case "nonexecutive":
            case "nonexecutives":
            case "nonexec":
                cadre = Cadre.NonExecutive;
                return true;
            default:
                return false;
        }
    }

    public static string QueryValue(Cadre? cadre) => cadre switch
    {
        Models.Cadre.Executive => "executive",
        Models.Cadre.NonExecutive => "non-executive",
        _ => "all",
    };
}
