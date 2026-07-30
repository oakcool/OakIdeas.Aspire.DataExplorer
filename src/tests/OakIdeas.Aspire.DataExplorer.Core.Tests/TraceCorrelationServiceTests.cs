using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class TraceCorrelationServiceTests
{
    private static CorrelatedSpan MakeSpan(
        string? traceId = null,
        string? spanId = null,
        string? serviceName = null,
        string? dbName = null,
        string? dbStatement = null,
        SpanStatusCode status = SpanStatusCode.Ok,
        double durationMs = 50.0,
        DateTimeOffset? startTime = null) => new(
        SpanId: spanId ?? Guid.NewGuid().ToString("N")[..16],
        TraceId: traceId ?? Guid.NewGuid().ToString("N"),
        ServiceName: serviceName ?? "test-service",
        DbSystem: "mssql",
        DbName: dbName ?? "TestDb",
        DbStatement: dbStatement ?? "SELECT 1",
        PeerAddress: "localhost:1433",
        StartTime: startTime ?? DateTimeOffset.UtcNow,
        Duration: TimeSpan.FromMilliseconds(durationMs),
        StatusCode: status,
        ErrorMessage: null);

    [Fact]
    public void IngestSpan_StoresSingleSpan()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        var span = MakeSpan();

        sut.IngestSpan(span);

        sut.SpanCount.Should().Be(1);
    }

    [Fact]
    public void IngestSpan_ThrowsOnNull()
    {
        var sut = new InMemoryTraceCorrelationService([]);

        var act = () => sut.IngestSpan(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Query_ReturnsAllSpansWhenNoFilters()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "aaa", serviceName: "svc-a"));
        sut.IngestSpan(MakeSpan(spanId: "bbb", serviceName: "svc-b"));

        var result = sut.Query(new TraceQueryRequest());

        result.Spans.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.IsTruncated.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Query_FiltersByServiceName()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "aaa", serviceName: "api"));
        sut.IngestSpan(MakeSpan(spanId: "bbb", serviceName: "worker"));

        var result = sut.Query(new TraceQueryRequest(ServiceName: "api"));

        result.Spans.Should().HaveCount(1);
        result.Spans[0].SpanId.Should().Be("aaa");
    }

    [Fact]
    public void Query_FiltersByDbName()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "aaa", dbName: "MainDb"));
        sut.IngestSpan(MakeSpan(spanId: "bbb", dbName: "ReportsDb"));

        var result = sut.Query(new TraceQueryRequest(DbName: "MainDb"));

        result.Spans.Should().HaveCount(1);
        result.Spans[0].SpanId.Should().Be("aaa");
    }

    [Fact]
    public void Query_FiltersByStatusCode()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "ok1", status: SpanStatusCode.Ok));
        sut.IngestSpan(MakeSpan(spanId: "err1", status: SpanStatusCode.Error));

        var result = sut.Query(new TraceQueryRequest(StatusCode: SpanStatusCode.Error));

        result.Spans.Should().HaveCount(1);
        result.Spans[0].SpanId.Should().Be("err1");
    }

    [Fact]
    public void Query_FiltersByMinDuration()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "fast", durationMs: 10));
        sut.IngestSpan(MakeSpan(spanId: "slow", durationMs: 600));

        var result = sut.Query(new TraceQueryRequest(MinDurationMs: 500));

        result.Spans.Should().HaveCount(1);
        result.Spans[0].SpanId.Should().Be("slow");
    }

    [Fact]
    public void Query_FiltersBySince()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "old", startTime: now.AddMinutes(-10)));
        sut.IngestSpan(MakeSpan(spanId: "new", startTime: now.AddMinutes(-1)));

        var result = sut.Query(new TraceQueryRequest(Since: now.AddMinutes(-5)));

        result.Spans.Should().HaveCount(1);
        result.Spans[0].SpanId.Should().Be("new");
    }

    [Fact]
    public void Query_ReturnsSpansInDescendingStartTimeOrder()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(spanId: "first", startTime: now.AddSeconds(-2)));
        sut.IngestSpan(MakeSpan(spanId: "second", startTime: now.AddSeconds(-1)));
        sut.IngestSpan(MakeSpan(spanId: "third", startTime: now));

        var result = sut.Query(new TraceQueryRequest());

        result.Spans.Select(s => s.SpanId)
            .Should().ContainInOrder("third", "second", "first");
    }

    [Fact]
    public void Query_TruncatesWhenOverMaxSpans()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        for (var i = 0; i < 10; i++)
        {
            sut.IngestSpan(MakeSpan());
        }

        var result = sut.Query(new TraceQueryRequest(MaxSpans: 3));

        result.Spans.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.IsTruncated.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllSpans()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan());
        sut.IngestSpan(MakeSpan());

        sut.Clear();

        sut.SpanCount.Should().Be(0);
        sut.Query(new TraceQueryRequest()).Spans.Should().BeEmpty();
    }

    [Fact]
    public void MaxSpans_EvictsOldestWhenFull()
    {
        var sut = new InMemoryTraceCorrelationService([], maxSpans: 3);
        sut.IngestSpan(MakeSpan(spanId: "first"));
        sut.IngestSpan(MakeSpan(spanId: "second"));
        sut.IngestSpan(MakeSpan(spanId: "third"));
        sut.IngestSpan(MakeSpan(spanId: "fourth"));

        sut.SpanCount.Should().Be(3);
        var ids = sut.Query(new TraceQueryRequest()).Spans.Select(s => s.SpanId).ToHashSet();
        ids.Should().Contain("fourth");
        ids.Should().NotContain("first");
    }

    [Fact]
    public void ServiceNames_ReturnsDistinctSortedNames()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(serviceName: "worker"));
        sut.IngestSpan(MakeSpan(serviceName: "api"));
        sut.IngestSpan(MakeSpan(serviceName: "api"));

        sut.ServiceNames.Should().BeEquivalentTo(["api", "worker"],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void DatabaseNames_ReturnsDistinctSortedNames()
    {
        var sut = new InMemoryTraceCorrelationService([]);
        sut.IngestSpan(MakeSpan(dbName: "Reports"));
        sut.IngestSpan(MakeSpan(dbName: "Main"));
        sut.IngestSpan(MakeSpan(dbName: "Main"));

        sut.DatabaseNames.Should().BeEquivalentTo(["Main", "Reports"],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void Constructor_ThrowsWhenMaxSpansIsZero()
    {
        var act = () => new InMemoryTraceCorrelationService([], maxSpans: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EnrichmentProvider_IsAppliedOnIngest()
    {
        var provider = new AppendingEnrichmentProvider();
        var sut = new InMemoryTraceCorrelationService([provider]);
        var span = MakeSpan(dbName: "Original");

        sut.IngestSpan(span);

        var stored = sut.Query(new TraceQueryRequest()).Spans.Single();
        stored.DbName.Should().Be("Original-enriched");
    }

    private sealed class AppendingEnrichmentProvider : Abstractions.ITraceEnrichmentProvider
    {
        public Contracts.Models.DatabaseProviderType ProviderType
            => Contracts.Models.DatabaseProviderType.SqlServer;

        public CorrelatedSpan Enrich(CorrelatedSpan span)
            => span with { DbName = span.DbName + "-enriched" };
    }
}
