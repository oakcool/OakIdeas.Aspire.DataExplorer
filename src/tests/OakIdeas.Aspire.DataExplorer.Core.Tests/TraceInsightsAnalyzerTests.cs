using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class TraceInsightsAnalyzerTests
{
    private readonly TraceInsightsAnalyzer _sut = new();

    private static CorrelatedSpan MakeSpan(
        string? traceId = null,
        string? spanId = null,
        string? statement = null,
        double durationMs = 50.0,
        SpanStatusCode status = SpanStatusCode.Ok) => new(
        SpanId: spanId ?? Guid.NewGuid().ToString("N")[..16],
        TraceId: traceId ?? "trace001",
        ServiceName: "test-svc",
        DbSystem: "mssql",
        DbName: "TestDb",
        DbStatement: statement ?? "SELECT * FROM Orders",
        PeerAddress: null,
        StartTime: DateTimeOffset.UtcNow,
        Duration: TimeSpan.FromMilliseconds(durationMs),
        StatusCode: status,
        ErrorMessage: null);

    [Fact]
    public void EmptyList_ReturnsNoInsights()
    {
        var result = _sut.Analyze([]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void NullList_ThrowsArgumentNullException()
    {
        var act = () => _sut.Analyze(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RepeatedQuery_DetectedWhenThresholdExceeded()
    {
        var spans = Enumerable.Range(0, TraceInsightsAnalyzer.RepeatedQueryThreshold)
            .Select(i => MakeSpan(spanId: $"s{i}", statement: "SELECT * FROM Products"))
            .ToArray();

        var insights = _sut.Analyze(spans);

        insights.Should().ContainSingle(i => i.Kind == TraceInsightKind.RepeatedQuery);
        insights.Single(i => i.Kind == TraceInsightKind.RepeatedQuery)
            .AffectedSpanIds.Should().HaveCount(TraceInsightsAnalyzer.RepeatedQueryThreshold);
    }

    [Fact]
    public void RepeatedQuery_NotDetectedWhenBelowThreshold()
    {
        var spans = Enumerable.Range(0, TraceInsightsAnalyzer.RepeatedQueryThreshold - 1)
            .Select(i => MakeSpan(spanId: $"s{i}", statement: "SELECT * FROM Products"))
            .ToArray();

        var insights = _sut.Analyze(spans);

        insights.Should().NotContain(i => i.Kind == TraceInsightKind.RepeatedQuery);
    }

    [Fact]
    public void RepeatedQuery_MatchesAcrossLiteralVariation()
    {
        // Same structural query, different literal values - should match after normalisation.
        var spans = new[]
        {
            MakeSpan(spanId: "a1", statement: "SELECT * FROM Orders WHERE Id = 1"),
            MakeSpan(spanId: "a2", statement: "SELECT * FROM Orders WHERE Id = 2"),
            MakeSpan(spanId: "a3", statement: "SELECT * FROM Orders WHERE Id = 3"),
        };

        var insights = _sut.Analyze(spans);

        insights.Should().ContainSingle(i => i.Kind == TraceInsightKind.RepeatedQuery);
    }

    [Fact]
    public void SlowCall_DetectedWhenThresholdExceeded()
    {
        var spans = new[]
        {
            MakeSpan(spanId: "fast", durationMs: 10),
            MakeSpan(spanId: "slow", durationMs: TraceInsightsAnalyzer.SlowCallThresholdMs + 1),
        };

        var insights = _sut.Analyze(spans);

        insights.Should().ContainSingle(i => i.Kind == TraceInsightKind.SlowCall);
        insights.Single(i => i.Kind == TraceInsightKind.SlowCall)
            .AffectedSpanIds.Should().Contain("slow");
    }

    [Fact]
    public void SlowCall_NotDetectedWhenAllSpansFast()
    {
        var spans = new[]
        {
            MakeSpan(spanId: "fast1", durationMs: 10),
            MakeSpan(spanId: "fast2", durationMs: 200),
        };

        var insights = _sut.Analyze(spans);

        insights.Should().NotContain(i => i.Kind == TraceInsightKind.SlowCall);
    }

    [Fact]
    public void NPlusOne_DetectedWhenManyShortQueriesInTrace()
    {
        var traceId = "trace-n1";
        var spans = Enumerable.Range(0, TraceInsightsAnalyzer.NPlusOneThreshold)
            .Select(i => MakeSpan(
                spanId: $"n{i}",
                traceId: traceId,
                statement: "SELECT * FROM Products WHERE ProductId = @p0",
                durationMs: TraceInsightsAnalyzer.NPlusOneShortQueryMs - 1))
            .ToArray();

        var insights = _sut.Analyze(spans);

        insights.Should().ContainSingle(i => i.Kind == TraceInsightKind.LikelyNPlusOne);
    }

    [Fact]
    public void NPlusOne_NotDetectedWhenQueriesAreSlow()
    {
        var traceId = "trace-slow";
        // Same query repeated many times but each is slow — not N+1 heuristic.
        var spans = Enumerable.Range(0, TraceInsightsAnalyzer.NPlusOneThreshold + 1)
            .Select(i => MakeSpan(
                spanId: $"s{i}",
                traceId: traceId,
                statement: "SELECT * FROM Orders WHERE OrderId = @p0",
                durationMs: TraceInsightsAnalyzer.NPlusOneShortQueryMs + 50))
            .ToArray();

        var insights = _sut.Analyze(spans);

        insights.Should().NotContain(i => i.Kind == TraceInsightKind.LikelyNPlusOne);
    }

    [Fact]
    public void NormalizeStatement_CollapsesWhitespaceAndUppercases()
    {
        var sql = "  select  *\n  from  Orders  ";
        var normalised = TraceInsightsAnalyzer.NormalizeStatement(sql);

        normalised.Should().Be("SELECT * FROM ORDERS");
    }

    [Fact]
    public void NormalizeStatement_MasksStringLiterals()
    {
        var sql = "SELECT * FROM Products WHERE Name = 'Widget'";
        var normalised = TraceInsightsAnalyzer.NormalizeStatement(sql);

        normalised.Should().NotContain("WIDGET");
        normalised.Should().Contain("?");
    }
}
