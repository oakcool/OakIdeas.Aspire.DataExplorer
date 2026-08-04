using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

/// <summary>
/// Extension methods for registering Test Data Scenario Builder services.
/// </summary>
public static class ScenarioBuilderServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory test data scenario builder service as a singleton.
    /// Call this method from the application composition root when the
    /// <c>ScenarioBuilder.TestDataScenarioBuilder</c> feature flag is enabled.
    /// </summary>
    public static IServiceCollection AddScenarioBuilderServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IScenarioBuilderService, InMemoryScenarioBuilderService>();

        return services;
    }
}
