using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class InMemoryQueryPerformanceServiceTests
{
    [Fact]
    public async Task GetTopQueriesAsync_ReturnsUnsupportedResponse()
    {
        var service = new InMemoryQueryPerformanceService();
        var request = new GetQueryPerformanceRequest();

        var response = await service.GetTopQueriesAsync(request);

        response.IsSupported.Should().BeFalse();
        response.Entries.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.UnsupportedReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetTopQueriesAsync_WithAnyRequest_ReturnsNoDataSource()
    {
        var service = new InMemoryQueryPerformanceService();
        var request = new GetQueryPerformanceRequest
        {
            DatabaseName = "mydb",
            SortBy = QueryPerformanceSortField.TotalDuration,
            Limit = 10,
        };

        var response = await service.GetTopQueriesAsync(request);

        response.DataSource.Should().BeNull();
    }

    [Fact]
    public async Task GetTopQueriesAsync_IsCancellable()
    {
        var service = new InMemoryQueryPerformanceService();
        var request = new GetQueryPerformanceRequest();

        using var cts = new CancellationTokenSource();
        var response = await service.GetTopQueriesAsync(request, cts.Token);

        response.Should().NotBeNull();
    }
}
