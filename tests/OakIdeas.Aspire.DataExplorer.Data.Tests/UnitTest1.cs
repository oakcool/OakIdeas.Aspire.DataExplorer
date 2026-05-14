using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Data.Infrastructure;

namespace OakIdeas.Aspire.DataExplorer.Data.Tests;

public sealed class DbConnectionFactoryTests
{
    [Fact]
    public void CreateSqlConnection_UsesProvidedConnectionString()
    {
        const string connectionString = "Server=localhost;Database=DataExplorer;Integrated Security=True;";
        var factory = new DbConnectionFactory();

        using var connection = factory.CreateSqlConnection(connectionString);

        connection.ConnectionString.Should().Be(connectionString);
    }
}
