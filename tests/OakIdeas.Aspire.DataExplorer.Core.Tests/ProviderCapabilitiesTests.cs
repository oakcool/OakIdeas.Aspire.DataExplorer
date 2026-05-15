using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class ProviderCapabilitiesTests
{
    [Fact]
    public void DefaultConstructor_InitializesAllFlagsToFalse()
    {
        var capabilities = new ProviderCapabilities();

        capabilities.Should().BeEquivalentTo(new ProviderCapabilities
        {
            SupportsSchemas = false,
            SupportsTables = false,
            SupportsViews = false,
            SupportsStoredProcedures = false,
            SupportsFunctions = false,
            SupportsTriggers = false,
            SupportsIndexes = false,
            SupportsConstraints = false,
            SupportsKeys = false,
            SupportsDefinitionRetrieval = false,
            SupportsLiveStats = false,
        });
    }
}
