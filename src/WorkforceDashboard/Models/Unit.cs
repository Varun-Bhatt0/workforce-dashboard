namespace WorkforceDashboard.Models;

/// <summary>A plant, works or office that reports headcount.</summary>
public sealed record Unit(string Code, string Name, string Group, int SortOrder);
