using Bunit;
using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ExecutionPlanComponentsTests : TestContext
{
    public ExecutionPlanComponentsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void MermaidDiagram_ValidateMermaidDiagram_WhenFlowchart_ReturnsNull()
    {
        var validation = MermaidDiagram.ValidateMermaidDiagram("flowchart TD\nA-->B");

        validation.Should().BeNull();
    }

    [Fact]
    public void MermaidDiagram_ValidateMermaidDiagram_WhenInvalid_ReturnsError()
    {
        var validation = MermaidDiagram.ValidateMermaidDiagram("not a mermaid diagram");

        validation.Should().Be("Execution plan diagram format is invalid.");
    }

    [Fact]
    public void ExecutionPlanViewer_WhenPlanUnavailable_ShowsEmptyState()
    {
        var component = RenderComponent<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: false,
                Provider: "SqlServer",
                MermaidDiagram: null,
                RawPlan: null,
                Message: "Execution plan is not available for this query or provider.")));

        component.Markup.Should().Contain("Execution plan is not available for this query or provider.");
    }

    [Fact]
    public void ExecutionPlanViewer_WhenPlanAvailable_RendersMermaidDiagram()
    {
        var component = RenderComponent<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: true,
                Provider: "SqlServer",
                MermaidDiagram: "flowchart TD\nA-->B",
                RawPlan: "<ShowPlanXML />",
                Message: null)));

        component.Markup.Should().Contain("mermaid-diagram");
    }
}
