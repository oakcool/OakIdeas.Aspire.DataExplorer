using Bunit;
using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ExecutionPlanComponentsTests : BunitContext
{
    public ExecutionPlanComponentsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ExecutionPlanViewer_WhenPlanUnavailable_ShowsEmptyState()
    {
        var component = Render<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: false,
                Provider: "SqlServer",
                Nodes: null,
                Edges: null,
                RawPlan: null,
                Message: "Execution plan is not available for this query or provider.")));

        component.Markup.Should().Contain("Execution plan is not available for this query or provider.");
    }

    [Fact]
    public void ExecutionPlanViewer_WhenPlanAvailable_RendersExecutionPlanDiagram()
    {
        var nodes = new List<ExecutionPlanNode>
        {
            new("N1", "Index Seek", null, "dbo.Users", "access",
                [new ExecutionPlanMetric("Est. Rows", "10")],
                []),
        };

        var component = Render<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: true,
                Provider: "SqlServer",
                Nodes: nodes,
                Edges: [],
                RawPlan: "<ShowPlanXML />",
                Message: null)));

        component.Markup.Should().Contain("de-ep-diagram");
    }

    [Fact]
    public void ExecutionPlanViewer_WhenPlanAvailable_ShowsProviderBadge()
    {
        var nodes = new List<ExecutionPlanNode>
        {
            new("N1", "Clustered Index Scan", null, null, "access", [], []),
        };

        var component = Render<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: true,
                Provider: "SqlServer",
                Nodes: nodes,
                Edges: [],
                RawPlan: null,
                Message: null)));

        component.Markup.Should().Contain("SqlServer");
    }

    [Fact]
    public void ExecutionPlanViewer_WhenPlanAvailable_PassesPlanDataToRenderer()
    {
        var expectedNodes = new List<ExecutionPlanNode>
        {
            new("N1", "Index Seek", null, "dbo.Users", "access",
                [new ExecutionPlanMetric("Est. Rows", "10")],
                []),
        };
        var expectedEdges = new List<ExecutionPlanEdge>
        {
            new("N0", "N1"),
        };

        var jsModule = JSInterop.SetupModule("./_content/OakIdeas.Aspire.DataExplorer.Web.Components/Components/Molecules/ExecutionPlanDiagram.razor.js");
        jsModule.SetupVoid("initPlan", _ => true);

        Render<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: true,
                Provider: "SqlServer",
                Nodes: expectedNodes,
                Edges: expectedEdges,
                RawPlan: null,
                Message: null)));

        var initPlanInvocations = jsModule.Invocations
            .Where(i => i.Identifier == "initPlan")
            .ToList();

        initPlanInvocations.Should().HaveCount(1);
    }
}

