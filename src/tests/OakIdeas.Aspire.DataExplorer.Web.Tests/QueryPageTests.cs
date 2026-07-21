using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class QueryPageTests : BunitContext
{
    public QueryPageTests()
    {
        // QueryPanel uses IJSRuntime for the editor JS module; allow all calls to succeed silently
        JSInterop.Mode = JSRuntimeMode.Loose;
        // Circuit-scoped services required by QueryPage
        Services.AddScoped<QueryNavigationState>();
        Services.AddScoped<QuerySessionState>();
    }

    [Fact]
    public void ExecuteQuery_PreservesSqlAfterExecution()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            // The editor must retain the SQL after execution — it must never be cleared
            component.Find("textarea").GetAttribute("value").Should().Be("SELECT 1");
            service.ExecuteCalls.Should().Be(1);
        });
    }

    [Fact]
    public void ExecuteQuery_ShowsSuccessStatusWithoutLastRunBar()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Query executed successfully");
            component.Markup.Should().NotContain("de-query-lastrun");
            service.ExecuteCalls.Should().Be(1);
        });
    }

    [Fact]
    public void DestructiveQuery_RequiresSecondConfirmationClick()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions
        {
            EnableWriteOperations = true,
        }));

        var component = Render<QueryPage>();
        component.Find("textarea").Input("DELETE FROM dbo.Users");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.Markup.Should().Contain("Run Execute again to confirm");
        service.ExecuteCalls.Should().Be(0);

        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.WaitForAssertion(() => service.ExecuteCalls.Should().Be(1));
    }

    [Fact]
    public void ExecuteQuery_RepeatedRuns_DoNotRenderLastRunBar()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        var editor = component.Find("textarea");
        var executeButtonSelector = "button[title='Execute (Ctrl+Enter)']";

        for (var index = 1; index <= 25; index++)
        {
            editor.Input($"SELECT {index}");
            component.Find(executeButtonSelector).Click();
        }

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().NotContain("de-query-lastrun");
            service.ExecuteCalls.Should().Be(25);
        });
    }

    [Fact]
    public void ExecuteQuery_WhenProviderReturnsError_SwitchesToErrorsTab()
    {
        var service = new FakeExplorerService(returnError: true);
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
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

        var component = Render<QueryPage>();
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

        var component = Render<QueryPage>();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("de-query-errors__panel"));

        service.ReturnError = false;
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Query executed successfully");
            component.Markup.Should().NotContain("de-query-errors__panel");
        });
    }

    [Fact]
    public void ErrorsTab_WhenNoError_ShowsNoErrorsMessage()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("button.de-query-tab--errors").Click();

        component.Markup.Should().Contain("No errors");
    }

    [Fact]
    public void AutoExecute_ViaNavigationState_ExecutesSql()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var navState = Services.GetRequiredService<QueryNavigationState>();

        var component = Render<QueryPage>();
        service.ExecuteCalls.Should().Be(0);

        // Simulate Object Explorer context menu: set the SQL + auto-execute flag then navigate
        navState.SetPendingSql("SELECT COUNT(*) FROM dbo.Users");
        navState.RequestAutoExecute();
        navigationManager.NavigateTo("/query");
        component.Render();

        component.WaitForAssertion(() => service.ExecuteCalls.Should().Be(1));
        component.Find("textarea").GetAttribute("value").Should().Be("SELECT COUNT(*) FROM dbo.Users");
    }

    [Fact]
    public void PendingSql_ViaNavigationState_PopulatesEditor()
    {
        // State-service SQL (from context-menu navigation) must populate the editor
        // without requiring a ?sql= URL parameter.
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var navState = Services.GetRequiredService<QueryNavigationState>();

        navState.SetPendingSql("SELECT TOP 100 * FROM dbo.Orders");
        navigationManager.NavigateTo("/query");

        var component = Render<QueryPage>();
        component.Render();

        component.Find("textarea").GetAttribute("value").Should().Be("SELECT TOP 100 * FROM dbo.Orders");
        service.ExecuteCalls.Should().Be(0);
    }

    [Fact]
    public void PendingSql_WithAutoExecute_ViaNavigationState_ExecutesSql()
    {
        // State-service SQL + auto-execute flag (context-menu "Select Top 1000") must
        // populate the editor AND trigger execution — without any URL parameters.
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var navState = Services.GetRequiredService<QueryNavigationState>();

        navState.SetPendingSql("SELECT TOP 1000 * FROM dbo.Users");
        navState.RequestAutoExecute();
        navigationManager.NavigateTo("/query");

        var component = Render<QueryPage>();
        component.Render();

        component.WaitForAssertion(() => service.ExecuteCalls.Should().Be(1));
        component.Find("textarea").GetAttribute("value").Should().Be("SELECT TOP 1000 * FROM dbo.Users");
    }

    [Fact]
    public void IncludeExecutionPlanToggle_PassesFlagToExecuteRequest()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("button[title*='Include execution plan']").Click();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();

        component.WaitForAssertion(() =>
        {
            service.ExecuteCalls.Should().Be(1);
            service.LastIncludeExecutionPlan.Should().BeTrue();
        });
    }

    [Fact]
    public void ExecutionPlanTab_WhenToggleEnabled_ShowsMermaidViewer()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("button[title*='Include execution plan']").Click();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.WaitForAssertion(() => component.Markup.Should().Contain("Execution Plan"));

        component.FindAll("button")
            .Single(button => button.TextContent.Contains("Execution Plan", StringComparison.Ordinal))
            .Click();

        component.Markup.Should().Contain("mermaid-diagram");
    }

    [Fact]
    public void ExecutionPlanTab_WhenUnavailable_ShowsEmptyState()
    {
        var service = new FakeExplorerService
        {
            IncludeExecutionPlanResponse = new ExecutionPlanResponse(
                IsAvailable: false,
                Provider: "SqlServer",
                MermaidDiagram: null,
                RawPlan: null,
                Message: "Execution plan is not available for this query or provider."),
        };
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("button[title*='Include execution plan']").Click();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.WaitForAssertion(() => component.Markup.Should().Contain("Execution Plan"));

        component.FindAll("button")
            .Single(button => button.TextContent.Contains("Execution Plan", StringComparison.Ordinal))
            .Click();

        component.Markup.Should().Contain("Execution plan is not available for this query or provider.");
    }

    [Fact]
    public void ResultTabs_WhenExecutionPlanEnabled_KeepErrorsTabAsLastTab()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("button[title*='Include execution plan']").Click();

        var tabButtons = component.FindAll(".de-query-pane__tabs button");
        tabButtons.Should().HaveCount(3);
        tabButtons[0].TextContent.Should().Contain("Results");
        tabButtons[1].TextContent.Should().Contain("Execution Plan");
        tabButtons[2].TextContent.Should().Contain("Errors");
    }

    [Fact]
    public void ExecutionPlanRenderFailure_SwitchesToErrorsTab_WithDetailedDiagnostics()
    {
        var mermaidModule = JSInterop.SetupModule("./_content/OakIdeas.Aspire.DataExplorer.Web.Components/Components/Atoms/MermaidDiagram.razor.js");
        mermaidModule.Setup<string?>("renderMermaid", _ => true).SetResult("invalid: simulated parse failure");

        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions()));

        var component = Render<QueryPage>();
        component.Find("button[title*='Include execution plan']").Click();
        component.Find("textarea").Input("SELECT 1");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.WaitForAssertion(() => component.Markup.Should().Contain("Execution Plan"));

        component.FindAll("button")
            .Single(button => button.TextContent.Contains("Execution Plan", StringComparison.Ordinal))
            .Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("de-query-errors__panel");
            component.Markup.Should().Contain("Execution plan rendering failed.");
            component.Markup.Should().Contain("Unable to render execution plan diagram.");
            component.Markup.Should().Contain("simulated parse failure");
            component.Markup.Should().Contain("render-execution-plan");
            component.Markup.Should().Contain("execution-plan-render-error");
        });
    }


    [Fact]
    public void WriteModeToggle_WhenReadOnlyMode_ShowsEnableWritesButton()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions { EnableWriteOperations = false }));

        var component = Render<QueryPage>();

        component.Markup.Should().Contain("Read-only mode");
        component.Markup.Should().Contain("Enable writes");
        component.Markup.Should().NotContain("Write mode");
    }

    [Fact]
    public void WriteModeToggle_WhenClickedInReadOnlyMode_SwitchesToWriteMode()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions { EnableWriteOperations = false }));

        var component = Render<QueryPage>();
        component.Find("button[title='Enable write operations for this session']").Click();

        component.Markup.Should().Contain("Write mode");
        component.Markup.Should().Contain("Disable writes");
        component.Markup.Should().NotContain("Read-only mode");
    }

    [Fact]
    public void WriteModeToggle_WhenWriteEnabled_ShowsDisableWritesButton()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions { EnableWriteOperations = true }));

        var component = Render<QueryPage>();

        component.Markup.Should().Contain("Write mode");
        component.Markup.Should().Contain("Disable writes");
        component.Markup.Should().NotContain("Read-only mode");
    }

    [Fact]
    public void WriteModeToggle_WhenClickedInWriteMode_ReturnsToReadOnlyMode()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions { EnableWriteOperations = true }));

        var component = Render<QueryPage>();
        component.Find("button[title='Return to read-only mode for this session']").Click();

        component.Markup.Should().Contain("Read-only mode");
        component.Markup.Should().Contain("Enable writes");
        component.Markup.Should().NotContain("Write mode");
    }

    [Fact]
    public void DestructiveQuery_WhenWriteEnabledViaToggle_ExecutesAfterConfirmation()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);
        Services.AddSingleton<IOptions<DataExplorerOptions>>(Options.Create(new DataExplorerOptions { EnableWriteOperations = false }));

        var component = Render<QueryPage>();
        // Enable writes via toggle
        component.Find("button[title='Enable write operations for this session']").Click();

        component.Find("textarea").Input("DELETE FROM dbo.Users");
        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.Markup.Should().Contain("Run Execute again to confirm");
        service.ExecuteCalls.Should().Be(0);

        component.Find("button[title='Execute (Ctrl+Enter)']").Click();
        component.WaitForAssertion(() => service.ExecuteCalls.Should().Be(1));
    }


    private sealed class FakeExplorerService(bool returnError = false) : IExplorerService
    {
        public int ExecuteCalls { get; private set; }
        public bool ReturnError { get; set; } = returnError;
        public bool LastIncludeExecutionPlan { get; private set; }
        public ExecutionPlanResponse? IncludeExecutionPlanResponse { get; set; } = new(
            IsAvailable: true,
            Provider: "SqlServer",
            MermaidDiagram: "flowchart TD\nA[Query Start]-->B[Index Seek]",
            RawPlan: "<ShowPlanXML />",
            Message: null);

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

        public Task<ExecuteDatabaseQueryResponse> ExecuteQueryAsync(string sql, bool includeExecutionPlan, bool readOnly, CancellationToken cancellationToken)
        {
            ExecuteCalls++;
            LastIncludeExecutionPlan = includeExecutionPlan;
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
                    ExecutionPlan: includeExecutionPlan ? IncludeExecutionPlanResponse : null,
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
                IsTruncated: false,
                ExecutionPlan: includeExecutionPlan ? IncludeExecutionPlanResponse : null));
        }
    }
}
