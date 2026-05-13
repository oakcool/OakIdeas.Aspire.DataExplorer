using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Guards;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class DevelopmentEnvironmentGuardTests
{
    [Fact]
    public void EnsureDevelopment_WhenNotDevelopment_ThrowsInvalidOperationException()
    {
        Action act = () => DevelopmentEnvironmentGuard.EnsureDevelopment(false, "not allowed");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("not allowed");
    }

    [Fact]
    public void EnsureDevelopment_WhenDevelopment_DoesNotThrow()
    {
        Action act = () => DevelopmentEnvironmentGuard.EnsureDevelopment(true, "unused");

        act.Should().NotThrow();
    }

    [Fact]
    public void DataExplorerOptions_ShouldUseSafeDefaults()
    {
        var options = new DataExplorerOptions();

        options.EnableWriteOperations.Should().BeTrue();
        options.EnableAdHocQueries.Should().BeTrue();
        options.RequireLocalConnections.Should().BeTrue();
        options.DefaultPageSize.Should().Be(100);
        options.MaxPageSize.Should().Be(1000);
        options.QueryTimeoutSeconds.Should().Be(30);
        options.MaxQueryRows.Should().Be(1000);
    }
}
