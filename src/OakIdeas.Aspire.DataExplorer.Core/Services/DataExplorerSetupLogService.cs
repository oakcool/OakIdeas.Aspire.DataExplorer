using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

internal sealed class DataExplorerSetupLogService : IHostedService
{
    private readonly ILogger<DataExplorerSetupLogService> _logger;

    public DataExplorerSetupLogService(ILogger<DataExplorerSetupLogService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Setting up Data Explorer Aspire resource discovery.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data Explorer Aspire resource discovery started successfully.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
