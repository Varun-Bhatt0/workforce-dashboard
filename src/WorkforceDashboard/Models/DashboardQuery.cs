namespace WorkforceDashboard.Models;

/// <summary>
/// What the user selected. <see cref="Scope"/> is "all", a group key ("group:Integrated plants")
/// or a unit code. A null year means the latest year in the data.
/// </summary>
public sealed record DashboardQuery(string Scope = DashboardQuery.AllScope, int? Year = null, Cadre? Cadre = null)
{
    public const string AllScope = "all";
    public const string GroupPrefix = "group:";

    public bool IsAll => string.Equals(Scope, AllScope, StringComparison.OrdinalIgnoreCase);
    public bool IsGroup => Scope.StartsWith(GroupPrefix, StringComparison.OrdinalIgnoreCase);
    public string GroupName => IsGroup ? Scope[GroupPrefix.Length..] : string.Empty;
}
