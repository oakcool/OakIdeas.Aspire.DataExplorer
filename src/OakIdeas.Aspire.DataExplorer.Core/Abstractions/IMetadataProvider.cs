using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IMetadataProvider
{
    DatabaseProviderType ProviderType { get; }

    ProviderCapabilities Capabilities { get; }

    Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(
        DatabaseResource resource,
        CancellationToken cancellationToken);

    Task<QueryResult> ExecuteQueryAsync(
        DatabaseResource resource,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken);
}

