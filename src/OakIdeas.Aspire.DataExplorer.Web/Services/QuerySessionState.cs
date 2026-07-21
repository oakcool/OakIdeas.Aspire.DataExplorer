using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Circuit-scoped state that carries per-session query settings, including whether write
/// operations are currently enabled for this session.
/// </summary>
/// <remarks>
/// Defaults to the value of <see cref="DataExplorerOptions.EnableWriteOperations"/> so that
/// the global configuration is respected at session start, while still allowing the user to
/// toggle write mode without restarting the application.
/// </remarks>
public sealed class QuerySessionState(IOptions<DataExplorerOptions> options)
{
    /// <summary>
    /// Whether write operations are enabled for the current session.
    /// Initialized from <see cref="DataExplorerOptions.EnableWriteOperations"/>.
    /// </summary>
    public bool WriteEnabled { get; set; } = options.Value.EnableWriteOperations;
}
