using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>Internal wrapper associating a source provider with its priority.</summary>
public sealed record OrderedSourceProvider(int Priority, IFeatureFlagSourceProvider Provider);
