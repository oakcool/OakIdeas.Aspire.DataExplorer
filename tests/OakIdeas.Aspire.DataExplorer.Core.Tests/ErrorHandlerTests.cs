using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class ErrorHandlerTests
{
    [Fact]
    public void MapException_WhenTimeoutException_ReturnsQueryTimeoutError()
    {
        var sut = CreateHandler();

        var error = sut.MapException(
            new TimeoutException("The connection timed out."),
            new ErrorContext("load-metadata", "applicationdb", DatabaseProviderType.SqlServer));

        error.Category.Should().Be(ErrorCategory.QueryTimeout);
        error.Operation.Should().Be("load-metadata");
        error.Target.Should().Be("applicationdb");
        error.RecoverySuggestion.Should().Contain("Retry");
    }

    [Fact]
    public void CreateError_WhenCalled_PopulatesTimestampAndDiagnosticCode()
    {
        var sut = CreateHandler();

        var before = DateTimeOffset.UtcNow;
        var error = sut.CreateError(
            ErrorCategory.ResourceNotFound,
            "Missing resource.",
            "Refresh and try again.",
            new ErrorContext("discover-resources", "sql-main"),
            diagnosticCode: "resource-not-found");
        var after = DateTimeOffset.UtcNow;

        error.Category.Should().Be(ErrorCategory.ResourceNotFound);
        error.DiagnosticCode.Should().Be("resource-not-found");
        error.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    private static IErrorHandler CreateHandler()
        => new ErrorHandler(NullLogger<ErrorHandler>.Instance, []);
}
