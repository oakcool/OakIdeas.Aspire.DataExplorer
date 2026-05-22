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

    [Fact]
    public void ExecuteQuery_HistoryIsLimitedToTwentyEntries()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        var editor = component.Find("textarea");
        var executeButtonSelector = "button[title='Execute (Ctrl+Enter)']";

        for (var index = 1; index <= 25; index++)
        {
            editor.Input($"SELECT {index}");
            component.Find(executeButtonSelector).Click();
        }

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Recent queries");
            component.FindAll(".de-query-history li").Count.Should().Be(20);
            service.ExecuteCalls.Should().Be(25);
        });
    }

    [Fact]
    public void ExecuteQuery_WhenProviderReturnsError_SwitchesToErrorsTab()
    {
        var service = new FakeExplorerService(returnError: true);
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("de-query-errors__panel");
            component.Markup.Should().Contain("Synthetic provider error");
        });
    }

    [Fact]
    public void ExecuteQuery_WhenProviderReturnsError_ShowsFullErrorDetails()
    {
        var service = new FakeExplorerService(returnError: true);
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Synthetic provider error");
            component.Markup.Should().Contain("Retry the operation");
            component.Markup.Should().Contain("ProviderError");
            component.Markup.Should().Contain("execute-query");
            component.Markup.Should().Contain("test-error");
        });
    }

    [Fact]
    public void ExecuteQuery_WhenSuccessAfterError_SwitchesBackToResultsTab()
    {
        var service = new FakeExplorerService(returnError: true);
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("de-query-errors__panel"));

        service.ReturnError = false;
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Completed in");
            component.Markup.Should().NotContain("de-query-errors__panel");
        });
    }

    [Fact]
    public void ErrorsTab_WhenNoError_ShowsNoErrorsMessage()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = RenderComponent<QueryPage>();
        component.Find("button.de-query-tab--errors").Click();

        component.Markup.Should().Contain("No errors");
    }


    private sealed class FakeExplorerService(bool returnError = false) : IExplorerService
    {
        public int ExecuteCalls { get; private set; }
        public bool ReturnError { get; set; } = returnError;

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
            if (ReturnError)
            {
                return Task.FromResult(new ExecuteDatabaseQueryResponse(
                    DatabaseName: "applicationdb",
                    Columns: [],
                    Rows: [],
                    RowCount: 0,
                    AffectedRowCount: null,
                    Duration: TimeSpan.Zero,
                    IsTruncated: false,
                    Error: new DataExplorerError(
                        Category: ErrorCategory.ProviderError,
                        Message: "Synthetic provider error",
                        RecoverySuggestion: "Retry the operation",
                        Operation: "execute-query",
                        Target: "applicationdb",
                        Timestamp: DateTimeOffset.UtcNow,
                        DiagnosticCode: "test-error")));
            }

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
