namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Represents the before-and-after value for a single column in a data change event.
/// Values are stored as safe string representations with sensitive values masked by the provider.
/// </summary>
/// <param name="Before">The column value before the change, or <see langword="null"/> for inserts or when unavailable.</param>
/// <param name="After">The column value after the change, or <see langword="null"/> for deletes or when unavailable.</param>
public sealed record ColumnChange(string? Before, string? After);
