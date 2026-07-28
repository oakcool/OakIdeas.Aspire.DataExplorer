using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Builder returned by <see cref="FeatureFlagServiceCollectionExtensions.AddFeatureFlags"/> to enable fluent source registration.
/// </summary>
public sealed class FeatureFlagBuilder(IServiceCollection services)
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; } = services;
}

