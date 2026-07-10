using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class ExplorerPageTests : BunitContext
{
    public ExplorerPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void TableSelection_LoadsKeysAndTriggers()
    {
        Services.AddSingleton<IExplorerService>(new FakeExplorerService());
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/explorer?objectId=dbo.Users&objectType=table&objectName=Users&schemaName=dbo&connectionName=sql-main&databaseName=applicationdb");

        var component = Render<ExplorerPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Keys");
            component.Markup.Should().Contain("PK_Users");
            component.Markup.Should().Contain("Triggers");
            component.Markup.Should().Contain("trg_Users_Audit");
        });
    }

    [Fact]
    public void ProcedureSelection_LoadsParameterDirectionAndDefault()
    {
        Services.AddSingleton<IExplorerService>(new FakeExplorerService());
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/explorer?objectId=dbo.SearchUsers&objectType=procedure&objectName=SearchUsers&schemaName=dbo&connectionName=sql-main&databaseName=applicationdb");

        var component = Render<ExplorerPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("@SearchText");
            component.Markup.Should().Contain("Input");
            component.Markup.Should().Contain("Has default");
        });
    }

    [Fact]
    public void FunctionSelection_LoadsParametersAndReturnType()
    {
        Services.AddSingleton<IExplorerService>(new FakeExplorerService());
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/explorer?objectId=dbo.GetUserDisplayName&objectType=function&objectName=GetUserDisplayName&schemaName=dbo&connectionName=sql-main&databaseName=applicationdb");

        var component = Render<ExplorerPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Parameters");
            component.Markup.Should().Contain("@UserId");
            component.Markup.Should().Contain("Return Type");
            component.Markup.Should().Contain("nvarchar(200)");
        });
    }

    [Fact]
    public void TableSelection_LoadsMetadata_WhenAggregatedTableKeysUseQuotedNames()
    {
        Services.AddSingleton<IExplorerService>(new FakeExplorerService(useQuotedObjectKeys: true));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/explorer?objectId=dbo.Users&objectType=table&objectName=Users&schemaName=dbo&connectionName=sql-main&databaseName=applicationdb");

        var component = Render<ExplorerPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Columns");
            component.Markup.Should().Contain("Id");
            component.Markup.Should().Contain("Keys");
            component.Markup.Should().Contain("PK_Users");
        });
    }

    [Fact]
    public void PartialMetadataError_DoesNotRenderMetadataErrorAlert()
    {
        Services.AddSingleton<IExplorerService>(new FakeExplorerService(
            collectionStatus: MetadataCollectionStatus.PartialSuccess,
            metadataError: new DataExplorerError(
                ErrorCategory.ProviderError,
                "SQL Server reported an error while completing this operation.",
                "Retry metadata refresh.",
                "functions",
                "applicationdb",
                DateTimeOffset.UtcNow,
                "metadata-partial-failure")));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/explorer?objectId=dbo.Users&objectType=table&objectName=Users&schemaName=dbo&connectionName=sql-main&databaseName=applicationdb");

        var component = Render<ExplorerPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Columns");
            component.Markup.Should().NotContain("metadata-partial-failure");
            component.Markup.Should().NotContain("SQL Server reported an error while completing this operation.");
        });
    }

    private sealed class FakeExplorerService : IExplorerService
    {
        private readonly bool _useQuotedObjectKeys;
        private readonly MetadataCollectionStatus _collectionStatus;
        private readonly DataExplorerError? _metadataError;

        public FakeExplorerService(
            bool useQuotedObjectKeys = false,
            MetadataCollectionStatus collectionStatus = MetadataCollectionStatus.Success,
            DataExplorerError? metadataError = null)
        {
            _useQuotedObjectKeys = useQuotedObjectKeys;
            _collectionStatus = collectionStatus;
            _metadataError = metadataError;
        }

        public Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetAvailableDatabasesResponse([]));

        public Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
            => Task.FromResult(new SelectDatabaseResponse(false, null, []));

        public Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetSelectedDatabaseResponse(null));

        public Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetDatabaseMetadataResponse(
                Metadata: null,
                AggregatedMetadata: CreateMetadata(_useQuotedObjectKeys),
                CollectionStatus: _collectionStatus,
                FailureDetails: [],
                Errors: [],
                Error: _metadataError));

        public Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(string objectId, DatabaseObjectType objectType, CancellationToken cancellationToken)
            => Task.FromResult(new GetObjectDefinitionResponse(objectId, objectType, $"CREATE {objectType} {objectId}", true, null, []));

        public Task<ExecuteDatabaseQueryResponse> ExecuteQueryAsync(string sql, bool includeExecutionPlan, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        private static DatabaseMetadata CreateMetadata(bool useQuotedObjectKeys)
        {
            var table = new TableObject("dbo.Users", "dbo", "Users");
            var tableKey = useQuotedObjectKeys ? "[dbo].[Users]" : "dbo.Users";
            var procedure = new StoredProcedureMetadata(
                "dbo",
                "SearchUsers",
                "dbo.SearchUsers",
                true,
                [
                    new StoredProcedureParameterMetadata("@SearchText", "nvarchar(100)", RoutineParameterDirection.Input, true),
                ],
                DateTimeOffset.UtcNow);
            var function = new FunctionMetadata(
                "dbo",
                "GetUserDisplayName",
                FunctionType.Scalar,
                "dbo.GetUserDisplayName",
                "nvarchar(200)",
                true,
                DateTimeOffset.UtcNow,
                [
                    new FunctionParameterMetadata("@UserId", "int"),
                ]);

            return new DatabaseMetadata(
                DatabaseName: "applicationdb",
                ProviderType: DatabaseProviderType.SqlServer,
                ResourceId: "sql-main",
                Schemas: [new SchemaObject("dbo", "dbo")],
                Tables: [table],
                Views: [],
                ProceduresBySchema: new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbo"] = [procedure],
                },
                FunctionsBySchema: new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbo"] = new Dictionary<FunctionType, IReadOnlyList<FunctionMetadata>>
                    {
                        [FunctionType.Scalar] = [function],
                    },
                },
                Triggers:
                [
                    new TriggerMetadata("trg_Users_Audit", "dbo", "Users", TriggerParentObjectType.Table, TriggerType.After | TriggerType.Insert, true, true, "dbo.trg_Users_Audit", DateTimeOffset.UtcNow, "dbo"),
                ],
                Constraints: [],
                ColumnsByObject: new Dictionary<string, IReadOnlyList<ColumnMetadata>>(StringComparer.OrdinalIgnoreCase)
                {
                    [tableKey] =
                    [
                        new ColumnMetadata("Id", 1, "int", null, null, null, false, true, false, null, null, new Dictionary<string, object?>()),
                    ],
                },
                PrimaryKeysByTable: new Dictionary<string, IReadOnlyList<PrimaryKeyConstraint>>(StringComparer.OrdinalIgnoreCase)
                {
                    [tableKey] =
                    [
                        new PrimaryKeyConstraint("PK_Users", "Users", "dbo", ["Id"], true, "dbo.PK_Users"),
                    ],
                },
                ForeignKeysByTable: new Dictionary<string, IReadOnlyList<ForeignKeyConstraint>>(StringComparer.OrdinalIgnoreCase),
                IndexesByTable: new Dictionary<string, IReadOnlyList<IndexMetadata>>(StringComparer.OrdinalIgnoreCase),
                MetadataCollectionTime: DateTimeOffset.UtcNow,
                CollectionStatus: MetadataCollectionStatus.Success,
                FailureDetails: []);
        }
    }
}
