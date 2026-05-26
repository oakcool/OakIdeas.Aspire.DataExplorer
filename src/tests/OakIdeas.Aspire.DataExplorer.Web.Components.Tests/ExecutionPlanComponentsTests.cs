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
    public void MermaidDiagram_ValidateMermaidDiagram_WhenNonEmpty_ReturnsNull()
    {
        var validation = MermaidDiagram.ValidateMermaidDiagram("not a mermaid diagram");

        validation.Should().BeNull();
    }

    [Fact]
    public void MermaidDiagram_NormalizeMermaidDiagram_WhenFenced_RemovesMarkdownFence()
    {
        var normalized = MermaidDiagram.NormalizeMermaidDiagram(
            """
            ```mermaid
            flowchart TD
                A[Query Start] --> B[Index Seek]
            ```
            """);

        normalized.Should().StartWith("flowchart TD");
        normalized.Should().NotContain("```");
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

    [Fact]
    public void ExecutionPlanViewer_WhenPlanAvailable_PassesDiagramValueToRenderer()
    {
        var expectedDiagram = "flowchart TD\nA-->B";
        var mermaidModule = JSInterop.SetupModule("./_content/OakIdeas.Aspire.DataExplorer.Web.Components/Components/Atoms/MermaidDiagram.razor.js");
        mermaidModule.Setup<string?>("renderMermaid", invocation =>
        {
            if (invocation.Arguments.Count < 2)
            {
                return false;
            }

            return invocation.Arguments[1] is string source
                && string.Equals(source, expectedDiagram, StringComparison.Ordinal);
        }).SetResult(null);

        RenderComponent<ExecutionPlanViewer>(parameters => parameters
            .Add(p => p.ExecutionPlan, new ExecutionPlanResponse(
                IsAvailable: true,
                Provider: "SqlServer",
                MermaidDiagram: expectedDiagram,
                RawPlan: "<ShowPlanXML />",
                Message: null)));

        var matchingInvocationCount = mermaidModule.Invocations.Count(invocation =>
            invocation.Identifier == "renderMermaid"
            && invocation.Arguments.Count >= 2
            && string.Equals(invocation.Arguments[1]?.ToString(), expectedDiagram, StringComparison.Ordinal));

        matchingInvocationCount.Should().Be(1);
    }
}
