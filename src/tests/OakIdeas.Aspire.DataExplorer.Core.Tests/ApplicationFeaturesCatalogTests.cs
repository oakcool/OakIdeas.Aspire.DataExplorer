using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class ApplicationFeaturesCatalogTests
{
    [Fact]
    public void All_ContainsAtLeastOneFeature()
    {
        ApplicationFeatures.All.Should().NotBeEmpty();
    }

    [Fact]
    public void All_FeatureKeysAreUnique()
    {
        var keys = ApplicationFeatures.All.Select(f => f.Key).ToArray();
        keys.Should().OnlyHaveUniqueItems("feature keys must be unique across the catalog");
    }

    [Fact]
    public void All_EveryFeatureHasNonEmptyKey()
    {
        foreach (var feature in ApplicationFeatures.All)
        {
            feature.Key.Should().NotBeNullOrWhiteSpace($"feature '{feature.DisplayName}' must have a non-empty key");
        }
    }

    [Fact]
    public void All_EveryFeatureHasNonEmptyDisplayName()
    {
        foreach (var feature in ApplicationFeatures.All)
        {
            feature.DisplayName.Should().NotBeNullOrWhiteSpace($"feature '{feature.Key}' must have a display name");
        }
    }

    [Fact]
    public void All_EveryGenerallyAvailableFeatureDefaultsToEnabled()
    {
        foreach (var feature in ApplicationFeatures.All)
        {
            if (string.Equals(feature.Key, FeatureKeys.ExplorerSchemaMigrations, StringComparison.Ordinal))
            {
                continue;
            }

            feature.DefaultEnabled.Should().BeTrue(
                $"existing feature '{feature.Key}' must default to enabled to preserve current behavior");
        }
    }

    [Fact]
    public void SchemaMigrations_DefaultsToDisabledForSafeRollout()
    {
        ApplicationFeatures.SchemaMigrations.DefaultEnabled.Should().BeFalse();
    }

    [Fact]
    public void FeatureKeys_AllConstantsAreNonEmpty()
    {
        var keyType = typeof(FeatureKeys);
        var constants = keyType
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string?)f.GetValue(null))
            .ToArray();

        constants.Should().NotBeEmpty();
        foreach (var constant in constants)
        {
            constant.Should().NotBeNullOrWhiteSpace("every FeatureKeys constant must be non-empty");
        }
    }

    [Fact]
    public void FeatureKeys_AllConstantsFollowDotNotation()
    {
        var keyType = typeof(FeatureKeys);
        var constants = keyType
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string?)f.GetValue(null))
            .ToArray();

        foreach (var key in constants)
        {
            key.Should().Contain(".", $"feature key '{key}' must follow the 'Area.Capability' naming convention");
        }
    }

    [Fact]
    public void All_DependencyKeysMustReferToRegisteredFeatures()
    {
        var registeredKeys = new HashSet<string>(
            ApplicationFeatures.All.Select(f => f.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var feature in ApplicationFeatures.All)
        {
            foreach (var dep in feature.DependsOn)
            {
                registeredKeys.Should().Contain(dep,
                    $"feature '{feature.Key}' declares dependency '{dep}' which must be registered");
            }
        }
    }

    [Fact]
    public void All_ContainsExpectedFeatures()
    {
        var keys = ApplicationFeatures.All.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        keys.Should().Contain(FeatureKeys.ExplorerObjectExplorer);
        keys.Should().Contain(FeatureKeys.ExplorerObjectDetails);
        keys.Should().Contain(FeatureKeys.ExplorerViews);
        keys.Should().Contain(FeatureKeys.ExplorerStoredProcedures);
        keys.Should().Contain(FeatureKeys.ExplorerFunctions);
        keys.Should().Contain(FeatureKeys.ExplorerTriggers);
        keys.Should().Contain(FeatureKeys.ExplorerIndexes);
        keys.Should().Contain(FeatureKeys.ExplorerConstraints);
        keys.Should().Contain(FeatureKeys.ExplorerForeignKeys);
        keys.Should().Contain(FeatureKeys.ExplorerPrimaryKeys);
        keys.Should().Contain(FeatureKeys.ExplorerObjectDefinition);
        keys.Should().Contain(FeatureKeys.ExplorerSchemaMigrations);
        keys.Should().Contain(FeatureKeys.QueryEditor);
        keys.Should().Contain(FeatureKeys.QueryAutoExecute);
        keys.Should().Contain(FeatureKeys.QueryExecutionPlan);
        keys.Should().Contain(FeatureKeys.DiagramDatabaseDiagram);
        keys.Should().Contain(FeatureKeys.DataEditingInsert);
        keys.Should().Contain(FeatureKeys.DataEditingUpdate);
        keys.Should().Contain(FeatureKeys.DataEditingDelete);
        keys.Should().Contain(FeatureKeys.ProvidersMultipleDatabases);
    }
}
