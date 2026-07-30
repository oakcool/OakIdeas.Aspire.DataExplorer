using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Allows a database provider to enrich a <see cref="CorrelatedSpan"/> before it is stored
/// in the trace correlation service. Implementations live in provider projects and follow
/// the same isolation rules as other provider-specific discovery interfaces.
/// </summary>
public interface ITraceEnrichmentProvider
{
    /// <summary>
    /// The provider type this enrichment provider applies to.
    /// Used to route spans to the correct enrichment implementation.
    /// </summary>
    DatabaseProviderType ProviderType { get; }

    /// <summary>
    /// Enriches a span with provider-specific information.
    /// Must return a non-null span; return the original span unchanged if no enrichment applies.
    /// </summary>
    /// <param name="span">The span to enrich. Must not be <see langword="null"/>.</param>
    /// <returns>The enriched span (may be the same instance).</returns>
    CorrelatedSpan Enrich(CorrelatedSpan span);
}
