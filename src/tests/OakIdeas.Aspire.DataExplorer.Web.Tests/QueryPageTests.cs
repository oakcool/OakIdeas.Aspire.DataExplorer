using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class QueryPageTests : TestContext
{
    [Fact]
    public void ExecuteQuery_AddsHistoryAndStatus()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Completed in");
            component.Markup.Should().Contain("Recent queries");
            service.ExecuteCalls.Should().Be(1);
        });
    }

    [Fact]
    public void DestructiveQuery_RequiresSecondConfirmationClick()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        component.Find("textarea").Input("DELETE FROM dbo.Users");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.Markup.Should().Contain("Run Execute again to confirm");
        service.ExecuteCalls.Should().Be(0);

        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.WaitForAssertion(() => service.ExecuteCalls.Should().Be(1));
    }

    private sealed class FakeExplorerService : IExplorerService
    {
        public int ExecuteCalls { get; private set; }

        public Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetAvailableDatabasesResponse([]));

        public Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
            => Task.FromResult(new SelectDatabaseResponse(true, null, []));

        public Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetSelectedDatabaseResponse(null));

        public Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetDatabaseMetadataResponse(
                Metadata: null,
                AggregatedMetadata: new DatabaseMetadata(
                    DatabaseName: "applicationdb",
                    ProviderType: DatabaseProviderType.SqlServer,
                    ResourceId: "sql-main",
                    Schemas: [new SchemaObject("dbo", "dbo")],
                    Tables: [new TableObject("dbo.Users", "dbo", "Users")],
                    Views: [],
                    ProceduresBySchema: new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase),
                    FunctionsBySchema: new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase),
                    Triggers: [],
                    Constraints: [],
                    ColumnsByObject: new Dictionary<string, IReadOnlyList<ColumnMetadata>>
                    {
                        ["dbo.Users"] =
                        [
                            new ColumnMetadata(
                                Name: "Id",
                                Ordinal: 1,
                                DataType: "int",
                                MaxLength: null,
                                Precision: null,
                                Scale: null,
                                IsNullable: false,
                                IsIdentity: false,
                                IsComputed: false,
                                DefaultValue: null,
                                Description: null,
                                ProviderMetadata: new Dictionary<string, object?>())
                        ],
                    },
                    PrimaryKeysByTable: new Dictionary<string, IReadOnlyList<PrimaryKeyConstraint>>(StringComparer.OrdinalIgnoreCase),
                    ForeignKeysByTable: new Dictionary<string, IReadOnlyList<ForeignKeyConstraint>>(StringComparer.OrdinalIgnoreCase),
                    IndexesByTable: new Dictionary<string, IReadOnlyList<IndexMetadata>>(StringComparer.OrdinalIgnoreCase),
                    MetadataCollectionTime: DateTimeOffset.UtcNow,
                    CollectionStatus: MetadataCollectionStatus.Success,
                    FailureDetails: []),
                CollectionStatus: MetadataCollectionStatus.Success,
                FailureDetails: [],
                Errors: []));

        public Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new RefreshMetadataResponse(
                Status: RefreshStatus.Completed,
                StartedAt: DateTimeOffset.UtcNow,
                CompletedAt: DateTimeOffset.UtcNow,
                Errors: [],
                IsPartialSuccess: false,
                Metadata: null));

        public Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(string objectId, DatabaseObjectType objectType, CancellationToken cancellationToken)
            => Task.FromResult(new GetObjectDefinitionResponse(objectId, objectType, null, false, null, []));

        public Task<ExecuteDatabaseQueryResponse> ExecuteQueryAsync(string sql, CancellationToken cancellationToken)
        {
            ExecuteCalls++;
            return Task.FromResult(new ExecuteDatabaseQueryResponse(
                DatabaseName: "applicationdb",
                Columns: ["Id"],
                Rows: [new Dictionary<string, object?> { ["Id"] = 1 }],
                RowCount: 1,
                AffectedRowCount: null,
                Duration: TimeSpan.FromMilliseconds(4),
                IsTruncated: false));
        }
    }
}
