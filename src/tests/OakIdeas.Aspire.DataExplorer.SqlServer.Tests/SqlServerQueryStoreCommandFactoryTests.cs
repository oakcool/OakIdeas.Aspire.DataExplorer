using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;
using OakIdeas.Aspire.DataExplorer.SqlServer.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerQueryStoreCommandFactoryTests
{
    [Fact]
    public void CreateEnableCommand_ReturnsQueryStoreEnableStatement()
    {
        var command = SqlServerQueryStoreCommandFactory.CreateEnableCommand(new QueryStoreOptions());

        command.Should().Be("ALTER DATABASE CURRENT SET QUERY_STORE = ON;");
    }
}
