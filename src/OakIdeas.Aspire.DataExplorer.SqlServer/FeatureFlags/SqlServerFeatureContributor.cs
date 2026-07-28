using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.FeatureFlags;

/// <summary>
/// Contributes SQL Server-specific feature flags to the application feature catalog.
/// Register this contributor by calling
/// <c>builder.Services.AddFeatureFlags().AddFeatureContributor&lt;SqlServerFeatureContributor&gt;()</c>
/// alongside the SQL Server provider registration.
/// Each contributed flag declares a <see cref="FeatureFlag.DependsOn"/> link to the
/// corresponding application-level flag, so disabling the broader Explorer or Query feature
/// automatically cascades to the SQL Server-specific sub-feature.
/// </summary>
public sealed class SqlServerFeatureContributor : IFeatureFlagContributor
{
    /// <inheritdoc />
    public IReadOnlyList<FeatureFlag> GetFeatures() => SqlServerFeatures.All;
}
