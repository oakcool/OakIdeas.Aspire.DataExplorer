using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerQueryPerformanceServiceTests
{
    // ── DetectRegression ─────────────────────────────────────────────────────

    [Fact]
    public void DetectRegression_SinglePlan_ReturnsFalse()
    {
        var result = SqlServerQueryPerformanceService.DetectRegression(
            planCount: 1,
            avgDurationMs: 100,
            maxDurationMs: 800);

        result.Should().BeFalse();
    }

    [Fact]
    public void DetectRegression_MultiplePlans_MaxAboveThreshold_ReturnsTrue()
    {
        // max > avg * 3 → regression
        var result = SqlServerQueryPerformanceService.DetectRegression(
            planCount: 2,
            avgDurationMs: 100,
            maxDurationMs: 310);

        result.Should().BeTrue();
    }

    [Fact]
    public void DetectRegression_MultiplePlans_MaxBelowThreshold_ReturnsFalse()
    {
        // max = avg * 2 → not enough to flag
        var result = SqlServerQueryPerformanceService.DetectRegression(
            planCount: 2,
            avgDurationMs: 100,
            maxDurationMs: 200);

        result.Should().BeFalse();
    }

    [Fact]
    public void DetectRegression_MultiplePlans_ZeroAvgDuration_ReturnsFalse()
    {
        var result = SqlServerQueryPerformanceService.DetectRegression(
            planCount: 2,
            avgDurationMs: 0,
            maxDurationMs: 500);

        result.Should().BeFalse();
    }

    // ── SortAndLimit ─────────────────────────────────────────────────────────

    [Fact]
    public void SortAndLimit_SortByAvgDuration_ReturnsSlowestFirst()
    {
        var entries = BuildEntries(
            avgDuration: [10, 50, 30]);

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.AvgDuration, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Select(e => e.AvgDurationMs).Should().ContainInOrder(50, 30, 10);
    }

    [Fact]
    public void SortAndLimit_SortByTotalDuration_ReturnsMostExpensiveFirst()
    {
        var entries = BuildEntries(totalDuration: [100, 500, 200]);

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.TotalDuration, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Select(e => e.TotalDurationMs).Should().ContainInOrder(500, 200, 100);
    }

    [Fact]
    public void SortAndLimit_SortByExecutionCount_ReturnsMostFrequentFirst()
    {
        var entries = BuildEntries(executionCount: [5, 20, 10]);

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.ExecutionCount, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Select(e => e.ExecutionCount).Should().ContainInOrder(20, 10, 5);
    }

    [Fact]
    public void SortAndLimit_SortByTotalLogicalReads_ReturnsHighestIoFirst()
    {
        var entries = BuildEntries(totalLogicalReads: [1000, 5000, 3000]);

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.TotalLogicalReads, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Select(e => e.TotalLogicalReads).Should().ContainInOrder(5000, 3000, 1000);
    }

    [Fact]
    public void SortAndLimit_SortByFailureCount_ReturnsMostFailuresFirst()
    {
        var entries = BuildEntries(failureCount: [2, 10, 0]);

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.FailureCount, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Select(e => e.FailureCount).Should().ContainInOrder(10, 2, 0);
    }

    [Fact]
    public void SortAndLimit_SortByLastSeenAt_ReturnsLatestFirst()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new List<QueryPerformanceEntry>
        {
            MakeEntry("1", lastSeenAt: now.AddMinutes(-30)),
            MakeEntry("2", lastSeenAt: now),
            MakeEntry("3", lastSeenAt: now.AddMinutes(-10)),
        };

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.LastSeenAt, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Select(e => e.QueryId).Should().ContainInOrder("2", "3", "1");
    }

    [Fact]
    public void SortAndLimit_LimitApplied_ReturnsOnlyTopN()
    {
        var entries = BuildEntries(avgDuration: [10, 50, 30, 20, 40]);

        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.AvgDuration, Limit = 3 };
        var result = SqlServerQueryPerformanceService.SortAndLimit(entries, request);

        result.Should().HaveCount(3);
        result[0].AvgDurationMs.Should().Be(50);
    }

    [Fact]
    public void SortAndLimit_EmptyEntries_ReturnsEmpty()
    {
        var request = new GetQueryPerformanceRequest { SortBy = QueryPerformanceSortField.AvgDuration, Limit = 10 };
        var result = SqlServerQueryPerformanceService.SortAndLimit([], request);

        result.Should().BeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IReadOnlyList<QueryPerformanceEntry> BuildEntries(
        double[]? avgDuration = null,
        double[]? totalDuration = null,
        long[]? executionCount = null,
        double[]? totalLogicalReads = null,
        long[]? failureCount = null)
    {
        var source = avgDuration ?? totalDuration?.Select(_ => 0.0).ToArray() ?? executionCount?.Select(_ => 0.0).ToArray()
            ?? totalLogicalReads?.Select(_ => 0.0).ToArray() ?? failureCount?.Select(_ => 0.0).ToArray() ?? [];

        return source.Select((_, i) => MakeEntry(
            id: (i + 1).ToString(),
            avgDuration: avgDuration?[i] ?? 0,
            totalDuration: totalDuration?[i] ?? 0,
            executionCount: executionCount?[i] ?? 0,
            totalLogicalReads: totalLogicalReads?[i] ?? 0,
            failureCount: failureCount?[i] ?? 0)).ToList();
    }

    private static QueryPerformanceEntry MakeEntry(
        string id = "1",
        double avgDuration = 0,
        double maxDuration = 0,
        double totalDuration = 0,
        long executionCount = 0,
        double totalLogicalReads = 0,
        long failureCount = 0,
        DateTimeOffset? lastSeenAt = null)
        => new()
        {
            QueryId = id,
            QueryText = $"SELECT {id}",
            DatabaseName = "testdb",
            ExecutionCount = executionCount,
            AvgDurationMs = avgDuration,
            MaxDurationMs = maxDuration == 0 ? avgDuration : maxDuration,
            TotalDurationMs = totalDuration,
            AvgLogicalReads = 0,
            TotalLogicalReads = totalLogicalReads,
            AvgLogicalWrites = 0,
            TotalLogicalWrites = 0,
            FailureCount = failureCount,
            AvgRowCount = 0,
            FirstSeenAt = null,
            LastSeenAt = lastSeenAt,
            PlanCount = 1,
            HasRegression = false,
        };
}
