using Aspire.Hosting;
using Aspire.Hosting.Lifecycle;
using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;
using OakIdeas.Aspire.DataExplorer.SqlServer.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class DataExplorerSqlServerHostingExtensionsTests
{
    [Fact]
    public void WithQueryStore_OnSqlServer_AddsConfigurationAnnotationAndLifecycleHook()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("sql-password", secret: true);
        var sqlServer = builder.AddSqlServer("sql", password);

        var returnedBuilder = sqlServer.WithQueryStore();

        returnedBuilder.Should().BeSameAs(sqlServer);
        sqlServer.Resource.Annotations.OfType<QueryStoreConfigurationAnnotation>()
            .Should().ContainSingle();
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IDistributedApplicationEventingSubscriber)
            && descriptor.ImplementationType == typeof(SqlServerQueryStoreEventingSubscriber));
    }

    [Fact]
    public void WithQueryStore_OnDatabase_ReplacesExistingConfigurationAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("sql-password", secret: true);
        var database = builder.AddSqlServer("sql", password).AddDatabase("appdb");
        var firstOptions = new QueryStoreOptions();
        var secondOptions = new QueryStoreOptions();

        database.WithQueryStore(firstOptions);
        database.WithQueryStore(secondOptions);

        database.Resource.Annotations.OfType<QueryStoreConfigurationAnnotation>()
            .Should().ContainSingle(annotation => ReferenceEquals(annotation.Options, secondOptions));
    }
}
