using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerQueryStoreTargetResolverTests
{
    [Fact]
    public void GetTargets_IncludesAllChildDatabasesWhenConfiguredOnServer()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("sql-password", secret: true);
        var sqlServer = builder.AddSqlServer("sql", password).WithQueryStore();
        var firstDatabase = sqlServer.AddDatabase("appdb");
        var secondDatabase = sqlServer.AddDatabase("reportingdb");

        var targets = SqlServerQueryStoreTargetResolver.GetTargets(new DistributedApplicationModel(builder.Resources));

        targets.Select(target => target.Database).Should().Equal(firstDatabase.Resource, secondDatabase.Resource);
    }

    [Fact]
    public void GetTargets_PrefersDatabaseSpecificConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("sql-password", secret: true);
        var sqlServer = builder.AddSqlServer("sql", password).WithQueryStore();
        var explicitDatabase = sqlServer.AddDatabase("appdb");
        var inheritedDatabase = sqlServer.AddDatabase("reportingdb");

        explicitDatabase.WithQueryStore();

        var targets = SqlServerQueryStoreTargetResolver.GetTargets(new DistributedApplicationModel(builder.Resources));

        targets.Should().HaveCount(2);
        targets.Single(target => target.Database == explicitDatabase.Resource).Options
            .Should().NotBeSameAs(targets.Single(target => target.Database == inheritedDatabase.Resource).Options);
    }
}
