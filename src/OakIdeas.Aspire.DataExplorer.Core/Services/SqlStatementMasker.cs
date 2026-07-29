using System.Text.RegularExpressions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// Masks potentially sensitive values in SQL statement text before display.
/// Replaces string literals and inline numeric values in common SQL patterns.
/// Named parameters (e.g. <c>@param</c>) are left as-is because they carry no data values.
/// </summary>
public static partial class SqlStatementMasker
{
    private const string MaskedValue = "?";

    // Matches single-quoted string literals, including escaped single quotes ('').
    [GeneratedRegex(@"'(?:[^']|'')*'", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex StringLiteralPattern();

    // Matches bare numeric literals that follow = < > ( , or whitespace.
    // (?!\w) prevents matching the start of an identifier like a column alias.
    // Only matches values in a value context (after an operator or delimiter),
    // not schema names, table names, or function parameters by position.
    [GeneratedRegex(
        @"(?<=[=<>(,\s])-?\d+(?:\.\d+)?(?!\w)",
        RegexOptions.None,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex NumericLiteralPattern();

    /// <summary>
    /// Returns a copy of <paramref name="sql"/> with string and numeric literals replaced by <c>?</c>.
    /// Returns an empty string when <paramref name="sql"/> is <see langword="null"/> or whitespace.
    /// </summary>
    public static string Mask(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var masked = StringLiteralPattern().Replace(sql, MaskedValue);
        masked = NumericLiteralPattern().Replace(masked, MaskedValue);
        return masked;
    }
}
