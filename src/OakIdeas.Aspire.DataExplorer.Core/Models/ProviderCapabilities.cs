namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record ProviderCapabilities
{
    public bool SupportsSchemas { get; init; }

    public bool SupportsTables { get; init; }

    public bool SupportsViews { get; init; }

    public bool SupportsStoredProcedures { get; init; }

    public bool SupportsFunctions { get; init; }

    public bool SupportsTriggers { get; init; }

    public bool SupportsIndexes { get; init; }

    public bool SupportsConstraints { get; init; }

    public bool SupportsKeys { get; init; }

    public bool SupportsDefinitionRetrieval { get; init; }

    public bool SupportsLiveStats { get; init; }

    public bool SupportsRelationshipNavigation { get; init; }
}
