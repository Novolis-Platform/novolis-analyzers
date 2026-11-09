namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Deterministic readability-complexity breakdown for one executable body.
/// Weights match the Phase 1 heuristic in the readability-complexity concept spec.
/// </summary>
public readonly record struct ReadabilityScore(
    int Declarations,
    int LocalReassignments,
    int Decisions,
    int SwitchPoints,
    int IterationPoints,
    int CatchPoints,
    int ThrowPoints,
    int ExternalMutationPoints,
    int ConsumedResults)
{
    /// <summary>Weighted sum of all scored constructs.</summary>
    public int Total =>
        Declarations
        + LocalReassignments
        + Decisions
        + SwitchPoints
        + IterationPoints
        + CatchPoints
        + ThrowPoints
        + ExternalMutationPoints
        + ConsumedResults;

    /// <summary>
    /// Human-readable primary contributors for diagnostics.
    /// </summary>
    public string FormatPrimaryCauses()
    {
        var parts = new List<string>(8);
        Add(parts, Declarations, "local declaration", "local declarations");
        Add(parts, LocalReassignments, "local reassignment", "local reassignments");
        Add(parts, Decisions, "decision", "decisions");
        Add(parts, SwitchPoints, "switch point", "switch points");
        Add(parts, IterationPoints, "iteration cost", "iteration cost");
        Add(parts, CatchPoints, "catch cost", "catch cost");
        Add(parts, ThrowPoints, "throw", "throws");
        Add(parts, ExternalMutationPoints, "external mutation cost", "external mutation cost");
        Add(parts, ConsumedResults, "consumed result", "consumed results");

        if (parts.Count == 0)
            return "no scored constructs";

        if (parts.Count == 1)
            return parts[0];

        if (parts.Count == 2)
            return parts[0] + " and " + parts[1];

        var head = new string[parts.Count - 1];
        for (var i = 0; i < head.Length; i++)
            head[i] = parts[i];

        return string.Join(", ", head) + ", and " + parts[parts.Count - 1];
    }

    /// <summary>Compact machine-readable breakdown of each score component.</summary>
    public string FormatBreakdown() =>
        $"decl={Declarations}; reassign={LocalReassignments}; decisions={Decisions}; switches={SwitchPoints}; " +
        $"loops={IterationPoints}; catch={CatchPoints}; throw={ThrowPoints}; " +
        $"extMut={ExternalMutationPoints}; consumed={ConsumedResults}; total={Total}";

    private static void Add(List<string> parts, int value, string singular, string plural)
    {
        if (value <= 0)
            return;

        parts.Add(value == 1 ? $"1 {singular}" : $"{value} {plural}");
    }
}
