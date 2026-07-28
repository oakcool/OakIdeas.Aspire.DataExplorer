using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Registration entry for a source provider, including its priority.
/// </summary>
public sealed record SourceProviderRegistration(
    int Priority,
    Type ProviderImplementationType);

