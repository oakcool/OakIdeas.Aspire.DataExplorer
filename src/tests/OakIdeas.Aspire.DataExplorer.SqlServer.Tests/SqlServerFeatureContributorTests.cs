using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.SqlServer.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerFeatureContributorTests
{
    private readonly SqlServerFeatureContributor _sut = new();

    [Fact]
    public void GetFeatures_ReturnsAllSqlServerFeatures()
    {
        var features = _sut.GetFeatures();

        features.Should().HaveCount(SqlServerFeatures.All.Count);
    }

    [Fact]
    public void GetFeatures_AllFeaturesHaveNonEmptyKeys()
    {
        foreach (var feature in _sut.GetFeatures())
        {
            feature.Key.Should().NotBeNullOrWhiteSpace(
                $"SQL Server feature '{feature.DisplayName}' must have a non-empty key");
        }
    }

    [Fact]
    public void GetFeatures_AllFeaturesUseProviderCategory()
    {
        foreach (var feature in _sut.GetFeatures())
        {
            feature.Category.Should().Be(FeatureCategory.Provider,
                $"SQL Server feature '{feature.Key}' must use the Provider category");
        }
    }

    [Fact]
    public void GetFeatures_AllFeaturesDefaultToEnabled()
    {
        foreach (var feature in _sut.GetFeatures())
        {
            feature.DefaultEnabled.Should().BeTrue(
                $"SQL Server feature '{feature.Key}' must default to enabled to preserve current behavior");
        }
    }

    [Fact]
    public void GetFeatures_AllFeatureKeysFollowSqlServerDotNotation()
    {
        foreach (var feature in _sut.GetFeatures())
        {
            feature.Key.Should().StartWith("SqlServer.",
                $"SQL Server feature key '{feature.Key}' must follow the 'SqlServer.Capability' naming convention");
        }
    }

    [Fact]
    public void GetFeatures_AllFeaturesHaveOwnerSetToSqlServer()
    {
        foreach (var feature in _sut.GetFeatures())
        {
            feature.Owner.Should().Be("SqlServer",
                $"SQL Server feature '{feature.Key}' must have Owner set to 'SqlServer'");
        }
    }

    [Fact]
    public void GetFeatures_AllFeaturesHaveAtLeastOneApplicationLevelDependency()
    {
        var applicationFeatureKeys = ApplicationFeatures.All
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var feature in _sut.GetFeatures())
        {
            feature.DependsOn.Should().NotBeEmpty(
                $"SQL Server feature '{feature.Key}' must declare a DependsOn link to its application-level counterpart");

            feature.DependsOn.Should().ContainSingle(dep =>
                applicationFeatureKeys.Contains(dep),
                $"SQL Server feature '{feature.Key}' must depend on a registered application-level feature");
        }
    }

    [Fact]
    public void GetFeatures_KeysAreUnique()
    {
        var keys = _sut.GetFeatures().Select(f => f.Key).ToArray();
        keys.Should().OnlyHaveUniqueItems("SQL Server feature keys must be unique");
    }

    [Fact]
    public void GetFeatures_ContainsExpectedCapabilities()
    {
        var keys = _sut.GetFeatures()
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        keys.Should().Contain(SqlServerFeatureKeys.StoredProcedures);
        keys.Should().Contain(SqlServerFeatureKeys.Functions);
        keys.Should().Contain(SqlServerFeatureKeys.Triggers);
        keys.Should().Contain(SqlServerFeatureKeys.Indexes);
        keys.Should().Contain(SqlServerFeatureKeys.Constraints);
        keys.Should().Contain(SqlServerFeatureKeys.ForeignKeys);
        keys.Should().Contain(SqlServerFeatureKeys.PrimaryKeys);
        keys.Should().Contain(SqlServerFeatureKeys.ObjectDefinition);
        keys.Should().Contain(SqlServerFeatureKeys.ExecutionPlan);
    }

    [Fact]
    public void GetFeatures_StoredProceduresDependsOnExplorerStoredProcedures()
    {
        var feature = _sut.GetFeatures().Single(f => f.Key == SqlServerFeatureKeys.StoredProcedures);
        feature.DependsOn.Should().Contain(FeatureKeys.ExplorerStoredProcedures);
    }

    [Fact]
    public void GetFeatures_FunctionsDependsOnExplorerFunctions()
    {
        var feature = _sut.GetFeatures().Single(f => f.Key == SqlServerFeatureKeys.Functions);
        feature.DependsOn.Should().Contain(FeatureKeys.ExplorerFunctions);
    }

    [Fact]
    public void GetFeatures_TriggersDependsOnExplorerTriggers()
    {
        var feature = _sut.GetFeatures().Single(f => f.Key == SqlServerFeatureKeys.Triggers);
        feature.DependsOn.Should().Contain(FeatureKeys.ExplorerTriggers);
    }

    [Fact]
    public void GetFeatures_ExecutionPlanDependsOnQueryExecutionPlan()
    {
        var feature = _sut.GetFeatures().Single(f => f.Key == SqlServerFeatureKeys.ExecutionPlan);
        feature.DependsOn.Should().Contain(FeatureKeys.QueryExecutionPlan);
    }
}
