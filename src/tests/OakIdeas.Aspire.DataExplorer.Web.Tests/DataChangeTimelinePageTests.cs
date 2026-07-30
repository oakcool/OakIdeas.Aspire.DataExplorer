using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class DataChangeTimelinePageTests : BunitContext
{
    [Fact]
    public void ExportButton_DownloadsJsonForSelectedSession()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var downloadCall = JSInterop.SetupVoid("downloadDataChangeTimelineExport", _ => true);

        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(true));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<IChangeTimelineService>(new FakeChangeTimelineService());
        Services.AddSingleton<IExplorerService>(new FakeExplorerService());

        var component = Render<DataChangeTimelinePage>();

        component.FindAll("button")
            .Single(button => button.TextContent.Contains("Export", StringComparison.Ordinal))
            .Click();

        downloadCall.Invocations.Should().ContainSingle();
        downloadCall.Invocations[0].Arguments[0].Should().BeOfType<string>()
            .Which.Should().StartWith("seed-session-");
        downloadCall.Invocations[0].Arguments[1].Should().BeOfType<string>()
            .Which.Should().Contain("\"events\"");
    }

    private sealed class FixedFeatureFlagService(bool enabled) : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagResult
            {
                Key = feature.Key,
                IsEnabled = string.Equals(feature.Key, FeatureKeys.TimelineDataChangeTimeline, StringComparison.Ordinal)
                    ? enabled
                    : true,
                WinningSource = "Test",
                UsedCatalogDefault = false,
                EvaluationTrace = [],
            });

        public ValueTask<bool> IsEnabledAsync(
            FeatureFlag feature,
            FeatureEvaluationContext? context = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                string.Equals(feature.Key, FeatureKeys.TimelineDataChangeTimeline, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }

    private sealed class FakeExplorerService : IExplorerService
    {
        public Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetAvailableDatabasesResponse([]));

        public Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
            => Task.FromResult(new SelectDatabaseResponse(false, null, []));

        public Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetSelectedDatabaseResponse(new ExplorerDatabaseSelection(
                "sql-main",
                "sql-main",
                "applicationdb",
                DatabaseProviderType.SqlServer,
                true,
                true,
                null)));

        public Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetDatabaseMetadataResponse(null, null, MetadataCollectionStatus.Success, [], []));

        public Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new RefreshMetadataResponse(RefreshStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false, null));

        public Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(string objectId, DatabaseObjectType objectType, CancellationToken cancellationToken)
            => Task.FromResult(new GetObjectDefinitionResponse(objectId, objectType, null, false, null, []));

        public Task<GetDatabaseMetadataResponse> GetDiagramDataAsync(CancellationToken cancellationToken)
            => GetDatabaseMetadataAsync(cancellationToken);

        public Task<ExecuteDatabaseQueryResponse> ExecuteQueryAsync(string sql, bool includeExecutionPlan, bool readOnly, CancellationToken cancellationToken)
            => Task.FromResult(new ExecuteDatabaseQueryResponse("applicationdb", [], [], 0, null, TimeSpan.Zero, false));
    }

    private sealed class FakeChangeTimelineService : IChangeTimelineService
    {
        private readonly CaptureSession _session = new(
            "session-1",
            "seed session",
            "applicationdb",
            new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero),
            null,
            null,
            CaptureSessionState.Active,
            1);

        private readonly DataChangeEvent _event = new(
            "event-1",
            "session-1",
            new DateTimeOffset(2026, 7, 30, 0, 1, 0, TimeSpan.Zero),
            DataChangeOperation.Insert,
            "applicationdb",
            "dbo",
            "Users",
            ["Id"],
            new Dictionary<string, string?> { ["Id"] = "1" },
            new Dictionary<string, ColumnChange> { ["Name"] = new(null, "Alice") },
            "trace-1",
            "tx-1");

        public CaptureSession? ActiveSession => _session;

        public IReadOnlyList<CaptureSession> Sessions => [_session];

        public int TotalEventCount => 1;

        public void ClearEvents(string sessionId)
        {
        }

        public void DeleteSession(string sessionId)
        {
        }

        public IReadOnlyList<string> GetTableNames(string sessionId)
            => ["dbo.Users"];

        public void PauseSession(string sessionId)
        {
        }

        public DataChangeQueryResponse Query(string sessionId, DataChangeQueryRequest request)
            => new([_event], 1, false);

        public void RecordEvent(DataChangeEvent evt)
        {
        }

        public void ResumeSession(string sessionId)
        {
        }

        public CaptureSession StartSession(string databaseName, string? label = null)
            => _session;

        public void StopSession(string sessionId)
        {
        }
    }
}
