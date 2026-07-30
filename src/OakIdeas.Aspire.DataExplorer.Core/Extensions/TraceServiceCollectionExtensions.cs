using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

/// <summary>
/// Extension methods for registering Request-to-Database Trace services.
/// </summary>
public static class TraceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory trace correlation service and the default trace insights analyser
    /// as singletons. Call this method from the application composition root when the
    /// <c>Telemetry.RequestTrace</c> feature flag is enabled.
    /// </summary>
    public static IServiceCollection AddTraceCorrelationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITraceCorrelationService>(sp =>
        {
            var enrichmentProviders = sp.GetServices<ITraceEnrichmentProvider>();
            return new InMemoryTraceCorrelationService(enrichmentProviders);
        });

        services.AddSingleton<ITraceInsightsAnalyzer, TraceInsightsAnalyzer>();

        return services;
    }
}
