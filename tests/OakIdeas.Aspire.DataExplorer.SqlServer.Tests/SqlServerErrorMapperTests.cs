using System.Reflection;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Diagnostics;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerErrorMapperTests
{
    [Theory]
    [InlineData(-2, ErrorCategory.QueryTimeout, "timed out")]
    [InlineData(18456, ErrorCategory.ConnectionFailed, "rejected")]
    [InlineData(229, ErrorCategory.PermissionDenied, "permission")]
    [InlineData(4060, ErrorCategory.ConnectionFailed, "unavailable")]
    public void TryMap_WhenKnownSqlErrorNumber_ReturnsHelpfulMappedError(
        int sqlErrorNumber,
        ErrorCategory expectedCategory,
        string expectedMessageFragment)
    {
        var sut = new SqlServerErrorMapper();
        var exception = CreateSqlException(sqlErrorNumber, "Original provider error");

        var mapped = sut.TryMap(
            exception,
            new ErrorContext("load-metadata", "applicationdb", DatabaseProviderType.SqlServer),
            out var error);

        mapped.Should().BeTrue();
        error.Category.Should().Be(expectedCategory);
        error.Message.Should().Contain(expectedMessageFragment);
        error.Message.Should().NotContain("Original provider error");
    }

    private static SqlException CreateSqlException(int number, string message)
    {
        var collection = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection),
            nonPublic: true)!;

        var error = (SqlError)Activator.CreateInstance(
            typeof(SqlError),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [number, (byte)0, (byte)0, "server", message, "procedure", 1, null!],
            culture: null)!;

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(collection, [error]);

        return (SqlException)Activator.CreateInstance(
            typeof(SqlException),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [message, collection, null!, Guid.NewGuid()],
            culture: null)!;
    }
}
