using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Connection;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerConnectionProviderTests
{
    [Fact]
    public void Constructor_WhenNotDevelopment_Throws()
    {
        var hostEnvironment = new StubHostEnvironment(isDevelopment: false);
        var options = Options.Create(new SqlServerConnectionOptions());

        Action act = () => _ = new SqlServerConnectionProvider(hostEnvironment, options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*development-time-only*");
    }

    [Fact]
    public void Constructor_WhenDevelopment_DoesNotThrow()
    {
        var hostEnvironment = new StubHostEnvironment(isDevelopment: true);
        var options = Options.Create(new SqlServerConnectionOptions());

        Action act = () => _ = new SqlServerConnectionProvider(hostEnvironment, options);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenConnectionStringNullOrWhitespace_ReturnsFalse()
    {
        var provider = CreateProvider();

        var result = await provider.ValidateConnectionAsync("   ", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("null or empty");
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenConnectionStringEmpty_ReturnsFalse()
    {
        var provider = CreateProvider();

        var result = await provider.ValidateConnectionAsync(string.Empty, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenConnectionUnreachable_ReturnsFalseWithMessage()
    {
        var provider = CreateProvider(validationTimeoutSeconds: 2);

        const string unreachableConnectionString =
            "Server=localhost,59999;Database=NoDb;User Id=sa;Password=BadPwd;Connect Timeout=1;TrustServerCertificate=True;";

        var result = await provider.ValidateConnectionAsync(unreachableConnectionString, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateConnectionAsync_WhenConnectionStringEmpty_Throws()
    {
        var provider = CreateProvider();

        Func<Task> act = () => provider.CreateConnectionAsync(string.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Connection string must not be null or empty*");
    }

    [Fact]
    public async Task CreateConnectionAsync_WhenConnectionStringWhitespace_Throws()
    {
        var provider = CreateProvider();

        Func<Task> act = () => provider.CreateConnectionAsync("   ", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Connection string must not be null or empty*");
    }

    [Fact]
    public async Task GetConnectionAsync_WhenContextIsNull_Throws()
    {
        var provider = CreateProvider();

        Func<Task> act = () => provider.GetConnectionAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetConnectionAsync_WhenContextIsInvalid_Throws()
    {
        var provider = CreateProvider();
        var context = CreateInvalidContext("sql-db", "Context is not valid.");

        Func<Task> act = () => provider.GetConnectionAsync(context, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid*");
    }

    [Fact]
    public async Task GetConnectionAsync_WhenNoConnectionStringInMetadata_Throws()
    {
        var provider = CreateProvider();
        var context = CreateValidContext("sql-db", new Dictionary<string, string?>());

        Func<Task> act = () => provider.GetConnectionAsync(context, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No connection string found*");
    }

    [Fact]
    public async Task GetConnectionAsync_WhenDirectConnectionStringInMetadata_AttemptsConnection()
    {
        var provider = CreateProvider(connectionTimeoutSeconds: 1);
        var metadata = new Dictionary<string, string?>
        {
            ["connectionString"] = "Server=localhost,59999;Database=NoDb;User Id=sa;Password=BadPwd;Connect Timeout=1;TrustServerCertificate=True;",
        };
        var context = CreateValidContext("sql-db", metadata);

        Func<Task> act = () => provider.GetConnectionAsync(context, CancellationToken.None);

        // Connection attempt should throw since the server is unreachable, not an InvalidOperationException
        await act.Should().ThrowAsync<Exception>()
            .Where(ex => ex.GetType() != typeof(InvalidOperationException));
    }

    [Fact]
    public async Task GetConnectionAsync_WhenEnvVarConnectionStringInMetadata_AttemptsConnection()
    {
        const string envVarName = "TEST_SQL_CONNECTION_STRING_NONEXISTENT";
        var provider = CreateProvider(connectionTimeoutSeconds: 1);
        var metadata = new Dictionary<string, string?>
        {
            ["connectionStringEnvironmentVariable"] = envVarName,
        };
        var context = CreateValidContext("sql-db", metadata);

        // Environment variable not set → no connection string
        Func<Task> act = () => provider.GetConnectionAsync(context, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No connection string found*");
    }

    [Fact]
    public void SqlServerConnectionOptions_HasSafeDefaults()
    {
        var options = new SqlServerConnectionOptions();

        options.ConnectionTimeoutSeconds.Should().Be(30);
        options.ValidationTimeoutSeconds.Should().Be(10);
    }

    [Fact]
    public void ConnectionValidationResult_ReflectsValues()
    {
        var success = new ConnectionValidationResult(true, null);
        var failure = new ConnectionValidationResult(false, "Some error");

        success.IsValid.Should().BeTrue();
        success.ErrorMessage.Should().BeNull();
        failure.IsValid.Should().BeFalse();
        failure.ErrorMessage.Should().Be("Some error");
    }

    private static SqlServerConnectionProvider CreateProvider(
        int connectionTimeoutSeconds = 30,
        int validationTimeoutSeconds = 10)
    {
        var hostEnvironment = new StubHostEnvironment(isDevelopment: true);
        var options = Options.Create(new SqlServerConnectionOptions
        {
            ConnectionTimeoutSeconds = connectionTimeoutSeconds,
            ValidationTimeoutSeconds = validationTimeoutSeconds,
        });

        return new SqlServerConnectionProvider(hostEnvironment, options);
    }

    private static SelectedDatabaseContext CreateValidContext(
        string resourceId,
        IReadOnlyDictionary<string, string?> metadata)
    {
        var resource = new DiscoveredDatabaseResource(
            resourceId,
            resourceId,
            $"{resourceId}-db",
            DatabaseProviderType.SqlServer,
            new ConnectionMetadata(metadata),
            IsAvailable: true,
            DateTimeOffset.UtcNow);

        return new SelectedDatabaseContext(resource, IsValid: true, ValidationMessage: null);
    }

    private static SelectedDatabaseContext CreateInvalidContext(string resourceId, string validationMessage)
    {
        var resource = new DiscoveredDatabaseResource(
            resourceId,
            resourceId,
            $"{resourceId}-db",
            DatabaseProviderType.SqlServer,
            new ConnectionMetadata(new Dictionary<string, string?>()),
            IsAvailable: false,
            DateTimeOffset.UtcNow);

        return new SelectedDatabaseContext(resource, IsValid: false, ValidationMessage: validationMessage);
    }

    private sealed class StubHostEnvironment(bool isDevelopment) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = isDevelopment ? "Development" : "Production";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
