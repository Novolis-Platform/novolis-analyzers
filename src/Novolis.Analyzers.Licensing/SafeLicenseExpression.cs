using System.Text.RegularExpressions;

namespace Novolis.Analyzers.Licensing;

/// <summary>
/// SPDX allowlist for Novolis safe licenses: MIT and Apache-2.0 only (including OR combinations of those).
/// </summary>
public static class SafeLicenseExpression
{
    private static readonly HashSet<string> AllowedLicenses = new(StringComparer.OrdinalIgnoreCase)
    {
        "MIT",
        "Apache-2.0",
    };

    private static readonly Regex OrSplit = new(
        @"\s+OR\s+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Returns true when <paramref name="expression"/> is empty or whitespace.
    /// </summary>
    public static bool IsMissing(string? expression) =>
        string.IsNullOrWhiteSpace(expression);

    /// <summary>
    /// Returns true when the SPDX expression is composed only of MIT and/or Apache-2.0 joined by OR.
    /// </summary>
    public static bool IsAllowed(string? expression)
    {
        if (IsMissing(expression))
            return false;

        var normalized = expression!.Trim();

        if (normalized.IndexOf("WITH", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        if (OrSplit.IsMatch(normalized) == false
            && normalized.IndexOf("AND", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (normalized.IndexOf("AND", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        while (normalized.Length >= 2 && normalized[0] == '(' && normalized[normalized.Length - 1] == ')')
            normalized = normalized.Substring(1, normalized.Length - 2).Trim();

        var parts = OrSplit.Split(normalized);
        if (parts.Length == 0)
            return false;

        foreach (var part in parts)
        {
            var token = part.Trim().Trim('(', ')').Trim();
            if (token.Length == 0 || !AllowedLicenses.Contains(token))
                return false;
        }

        return true;
    }
}
