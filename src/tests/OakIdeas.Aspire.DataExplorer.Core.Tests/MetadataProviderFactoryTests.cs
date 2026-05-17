using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class MetadataProviderFactoryTests
{
    [Fact]
    public void Create_WhenProviderRegistered_ReturnsProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<StubSqlServerMetadataProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var options = CreateOptions(configure => configure.Register(DatabaseProviderType.SqlServer, typeof(StubSqlServerMetadataProvider)));
        var factory = new MetadataProviderFactory(serviceProvider, options);

        IMetadataProvider provider = factory.Create(DatabaseProviderType.SqlServer);

        provider.Should().BeOfType<StubSqlServerMetadataProvider>();
        provider.ProviderType.Should().Be(DatabaseProviderType.SqlServer);
    }

    [Fact]
    public void TryCreate_WhenProviderNotRegistered_ReturnsFalse()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var factory = new MetadataProviderFactory(serviceProvider, CreateOptions(_ => { }));

        var result = factory.TryCreate(DatabaseProviderType.PostgreSql, out var provider);

        result.Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void Create_WhenProviderTypeNotRegistered_Throws()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var factory = new MetadataProviderFactory(serviceProvider, CreateOptions(_ => { }));

        var act = () => factory.Create(DatabaseProviderType.SqlServer);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No metadata provider is registered*");
    }

    [Fact]
    public void Register_WhenTypeDoesNotImplementMetadataProvider_Throws()
    {
        var options = new MetadataProviderFactoryOptions();

        var act = () => options.Register(DatabaseProviderType.SqlServer, typeof(object));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must implement IMetadataProvider*");
    }

    [Fact]
    public void Create_WhenProviderNotRegisteredInServiceProvider_Throws()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var options = CreateOptions(configure => configure.Register(DatabaseProviderType.SqlServer, typeof(StubSqlServerMetadataProvider)));
        var factory = new MetadataProviderFactory(serviceProvider, options);

        var act = () => factory.Create(DatabaseProviderType.SqlServer);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*is not registered in the service provider*");
    }

    private static IOptions<MetadataProviderFactoryOptions> CreateOptions(Action<MetadataProviderFactoryOptions> configure)
    {
        var options = new MetadataProviderFactoryOptions();
        configure(options);
        return Options.Create(options);
    }

    private sealed class StubSqlServerMetadataProvider : IMetadataProvider
    {
        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

        public ProviderCapabilities Capabilities => new();

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(
            DatabaseResource resource,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>(Array.Empty<SchemaMetadata>());

        public Task<QueryResult> ExecuteQueryAsync(
            DatabaseResource resource,
            ExecuteQueryRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(
                new QueryResult(
                    Columns: Array.Empty<string>(),
                    Rows: Array.Empty<IReadOnlyDictionary<string, object?>>(),
                    RowCount: 0,
                    Duration: TimeSpan.Zero));
    }
}
