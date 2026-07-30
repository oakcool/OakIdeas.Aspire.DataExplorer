using System.Reflection;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerSchemaMigrationsProvider : ISchemaMigrationsProvider
{
    private const string AppliedMigrationsSql = """
        SELECT [MigrationId], [ProductVersion]
        FROM [__EFMigrationsHistory]
        ORDER BY [MigrationId];
        """;

    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    public async Task<SchemaMigrationsOverviewResponse> GetOverviewAsync(
        DatabaseResource resource,
        ConnectionMetadata connectionMetadata,
        DatabaseMetadata liveMetadata,
        DatabaseMetadata? comparisonMetadata,
        string? comparisonDatabaseName,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var migrations = new List<SchemaMigrationEntry>();
        var driftItems = new List<SchemaDriftItem>();

        var appliedMigrations = await GetAppliedMigrationsAsync(resource.ConnectionString, cancellationToken);

        if (TryGetDbContextHints(connectionMetadata, out var projectPath, out var dbContextTypeName))
        {
            try
            {
                using var bundle = LoadDbContextBundle(resource.ConnectionString, projectPath!, dbContextTypeName!);
                migrations.AddRange(BuildMigrationEntries(bundle.MigrationsAssembly.Migrations.Keys, appliedMigrations));
                driftItems.AddRange(CompareDescriptors(
                    BuildDescriptor(liveMetadata),
                    BuildDescriptor(bundle.DbContext.Model.GetRelationalModel()),
                    SchemaDriftSource.LiveVsModel));

                if (bundle.MigrationsAssembly.ModelSnapshot is { } snapshot)
                {
                    driftItems.AddRange(CompareDescriptors(
                        BuildDescriptor(liveMetadata),
                        BuildDescriptor(snapshot.Model.GetRelationalModel()),
                        SchemaDriftSource.LiveVsSnapshot));
                }
                else
                {
                    warnings.Add("No EF Core model snapshot was found for the configured DbContext.");
                }

                return new SchemaMigrationsOverviewResponse(
                    DatabaseName: liveMetadata.DatabaseName,
                    DbContextTypeName: bundle.DbContext.GetType().FullName,
                    Migrations: migrations,
                    DriftItems: AddComparisonDrift(driftItems, liveMetadata, comparisonMetadata),
                    Warnings: AddComparisonWarning(warnings, comparisonMetadata, comparisonDatabaseName),
                    ComparisonDatabaseName: comparisonDatabaseName,
                    ProjectMetadataAvailable: true,
                    CanGenerateScripts: true,
                    CanExecuteScripts: true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                warnings.Add($"EF Core project metadata could not be loaded: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("Schema migrations project metadata is not configured for this database resource.");
        }

        if (migrations.Count == 0)
        {
            migrations.AddRange(appliedMigrations.Select(entry => new SchemaMigrationEntry(
                entry.MigrationId,
                entry.ProductVersion,
                SchemaMigrationState.MissingFromProject,
                KnownToProject: false,
                AppliedToDatabase: true,
                Notes: "Applied in the database but project metadata is unavailable.")));
        }

        return new SchemaMigrationsOverviewResponse(
            DatabaseName: liveMetadata.DatabaseName,
            DbContextTypeName: dbContextTypeName,
            Migrations: migrations,
            DriftItems: AddComparisonDrift(driftItems, liveMetadata, comparisonMetadata),
            Warnings: AddComparisonWarning(warnings, comparisonMetadata, comparisonDatabaseName),
            ComparisonDatabaseName: comparisonDatabaseName,
            ProjectMetadataAvailable: false,
            CanGenerateScripts: false,
            CanExecuteScripts: false);
    }

    public Task<GenerateSchemaMigrationsScriptResponse> GenerateScriptAsync(
        DatabaseResource resource,
        ConnectionMetadata connectionMetadata,
        GenerateSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetDbContextHints(connectionMetadata, out var projectPath, out var dbContextTypeName))
        {
            return Task.FromResult(new GenerateSchemaMigrationsScriptResponse(
                resource.Name,
                string.Empty,
                request.Kind,
                false,
                ["Schema migrations project metadata is not configured for this database resource."]));
        }

        using var bundle = LoadDbContextBundle(resource.ConnectionString, projectPath!, dbContextTypeName!);
        var applied = GetAppliedMigrationsAsync(resource.ConnectionString, cancellationToken).GetAwaiter().GetResult();
        var lastApplied = applied.LastOrDefault().MigrationId;

        var script = request.Kind switch
        {
            SchemaScriptKind.Pending => bundle.Migrator.GenerateScript(lastApplied, null),
            SchemaScriptKind.Idempotent => bundle.Migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent),
            _ => bundle.Migrator.GenerateScript("0", null),
        };

        return Task.FromResult(new GenerateSchemaMigrationsScriptResponse(
            resource.Name,
            script,
            request.Kind,
            request.Kind == SchemaScriptKind.Idempotent,
            []));
    }

    public async Task<ExecuteSchemaMigrationsScriptResponse> ExecuteScriptAsync(
        DatabaseResource resource,
        ExecuteSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ConfirmationText, resource.Name, StringComparison.Ordinal))
        {
            return new ExecuteSchemaMigrationsScriptResponse(
                resource.Name,
                false,
                0,
                ["Type the exact selected database name before executing schema changes."],
                DateTimeOffset.UtcNow);
        }

        var batches = SplitBatches(request.Script).Where(static batch => !string.IsNullOrWhiteSpace(batch)).ToArray();
        await using var connection = new SqlConnection(resource.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var batch in batches)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandTimeout = 60;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new ExecuteSchemaMigrationsScriptResponse(
            resource.Name,
            true,
            batches.Length,
            [$"Executed {batches.Length} schema batch(es)."],
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<string> AddComparisonWarning(
        List<string> warnings,
        DatabaseMetadata? comparisonMetadata,
        string? comparisonDatabaseName)
    {
        if (comparisonMetadata is null && !string.IsNullOrWhiteSpace(comparisonDatabaseName))
        {
            warnings.Add("Comparison database metadata could not be loaded.");
        }

        return warnings;
    }

    private static IReadOnlyList<SchemaDriftItem> AddComparisonDrift(
        List<SchemaDriftItem> driftItems,
        DatabaseMetadata liveMetadata,
        DatabaseMetadata? comparisonMetadata)
    {
        if (comparisonMetadata is not null)
        {
            driftItems.AddRange(CompareDescriptors(
                BuildDescriptor(liveMetadata),
                BuildDescriptor(comparisonMetadata),
                SchemaDriftSource.LiveVsComparisonDatabase));
        }

        return driftItems
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.ObjectType, StringComparer.Ordinal)
            .ThenBy(item => item.ObjectName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryGetDbContextHints(
        ConnectionMetadata connectionMetadata,
        out string? projectPath,
        out string? dbContextTypeName)
    {
        connectionMetadata.Properties.TryGetValue("schemaMigrationsProjectPath", out projectPath);
        connectionMetadata.Properties.TryGetValue("schemaMigrationsDbContextType", out dbContextTypeName);
        return !string.IsNullOrWhiteSpace(projectPath) && !string.IsNullOrWhiteSpace(dbContextTypeName);
    }

    private static SchemaMigrationsDbContextBundle LoadDbContextBundle(
        string connectionString,
        string projectPath,
        string dbContextTypeName)
    {
        var assemblyPath = ResolveAssemblyPath(projectPath);
        var assembly = Assembly.LoadFrom(assemblyPath);
        var dbContextType = assembly.GetType(dbContextTypeName, throwOnError: true)!;
        var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
        var optionsBuilder = Activator.CreateInstance(optionsBuilderType)!;
        var useSqlServer = typeof(Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
            {
                if (!string.Equals(method.Name, "UseSqlServer", StringComparison.Ordinal))
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length >= 2
                    && parameters[0].ParameterType == typeof(DbContextOptionsBuilder)
                    && parameters[1].ParameterType == typeof(string);
            });

        _ = useSqlServer.Invoke(null, [optionsBuilder, connectionString, null]);

        var options = typeof(DbContextOptionsBuilder).GetProperty("Options")!.GetValue(optionsBuilder)!;
        var dbContext = (DbContext)Activator.CreateInstance(dbContextType, options)!;
        return new SchemaMigrationsDbContextBundle(
            dbContext,
            dbContext.GetService<IMigrationsAssembly>(),
            dbContext.GetService<IMigrator>());
    }

    private static string ResolveAssemblyPath(string projectPath)
    {
        var projectFile = new FileInfo(projectPath);
        if (!projectFile.Exists)
        {
            throw new FileNotFoundException("The configured project file could not be found.", projectPath);
        }

        var assemblyName = ResolveAssemblyName(projectFile);
        var matches = Directory.GetFiles(projectFile.DirectoryName!, $"{assemblyName}.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (matches.Length == 0)
        {
            throw new FileNotFoundException($"No compiled assembly for '{assemblyName}' could be found under the project output.");
        }

        return matches[0];
    }

    private static string ResolveAssemblyName(FileInfo projectFile)
    {
        var document = XDocument.Load(projectFile.FullName);
        var assemblyName = document.Descendants("AssemblyName").Select(element => element.Value).FirstOrDefault();
        return string.IsNullOrWhiteSpace(assemblyName)
            ? Path.GetFileNameWithoutExtension(projectFile.Name)
            : assemblyName.Trim();
    }

    private static async Task<IReadOnlyList<AppliedMigrationRow>> GetAppliedMigrationsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var rows = new List<AppliedMigrationRow>();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = AppliedMigrationsSql;
            command.CommandType = System.Data.CommandType.Text;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new AppliedMigrationRow(
                    reader.GetString(0),
                    reader.GetString(1)));
            }
        }
        catch (SqlException ex) when (ex.Number is 208)
        {
            return [];
        }

        return rows;
    }

    private static IReadOnlyList<SchemaMigrationEntry> BuildMigrationEntries(
        IEnumerable<string> knownMigrationIds,
        IReadOnlyList<AppliedMigrationRow> appliedMigrations)
    {
        var known = knownMigrationIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var appliedSet = appliedMigrations.Select(row => row.MigrationId).ToHashSet(StringComparer.Ordinal);
        var lastAppliedIndex = known.Length == 0
            ? -1
            : Array.FindLastIndex(known, migrationId => appliedSet.Contains(migrationId));

        var entries = new List<SchemaMigrationEntry>(known.Length + appliedMigrations.Count);

        for (var index = 0; index < known.Length; index++)
        {
            var migrationId = known[index];
            var isApplied = appliedSet.Contains(migrationId);
            var state = isApplied
                ? SchemaMigrationState.Applied
                : index < lastAppliedIndex
                    ? SchemaMigrationState.OutOfOrder
                    : SchemaMigrationState.Pending;

            entries.Add(new SchemaMigrationEntry(
                migrationId,
                appliedMigrations.FirstOrDefault(row => string.Equals(row.MigrationId, migrationId, StringComparison.Ordinal)).ProductVersion,
                state,
                KnownToProject: true,
                AppliedToDatabase: isApplied,
                Notes: state == SchemaMigrationState.OutOfOrder ? "A later migration is already applied." : null));
        }

        entries.AddRange(appliedMigrations
            .Where(row => !known.Contains(row.MigrationId, StringComparer.Ordinal))
            .Select(row => new SchemaMigrationEntry(
                row.MigrationId,
                row.ProductVersion,
                SchemaMigrationState.MissingFromProject,
                KnownToProject: false,
                AppliedToDatabase: true,
                Notes: "Applied in the database but missing from the configured project.")));

        return entries;
    }

    private static IReadOnlyList<SchemaDriftItem> CompareDescriptors(
        SchemaDescriptor live,
        SchemaDescriptor expected,
        SchemaDriftSource source)
    {
        var drift = new List<SchemaDriftItem>();

        foreach (var table in expected.Tables.Keys.Except(live.Tables.Keys, StringComparer.OrdinalIgnoreCase))
        {
            drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Additive, "Table", table, "Expected table is missing from the live database."));
        }

        foreach (var table in live.Tables.Keys.Except(expected.Tables.Keys, StringComparer.OrdinalIgnoreCase))
        {
            drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Informational, "Table", table, "Live database contains a table not present in the comparison source."));
        }

        foreach (var table in live.Tables.Keys.Intersect(expected.Tables.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var liveTable = live.Tables[table];
            var expectedTable = expected.Tables[table];

            foreach (var column in expectedTable.Columns.Keys.Except(liveTable.Columns.Keys, StringComparer.OrdinalIgnoreCase))
            {
                drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Additive, "Column", $"{table}.{column}", "Expected column is missing from the live database."));
            }

            foreach (var column in liveTable.Columns.Keys.Except(expectedTable.Columns.Keys, StringComparer.OrdinalIgnoreCase))
            {
                drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Informational, "Column", $"{table}.{column}", "Live database contains an extra column."));
            }

            foreach (var column in liveTable.Columns.Keys.Intersect(expectedTable.Columns.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var liveColumn = liveTable.Columns[column];
                var expectedColumn = expectedTable.Columns[column];
                if (!string.Equals(liveColumn.StoreType, expectedColumn.StoreType, StringComparison.OrdinalIgnoreCase)
                    || liveColumn.IsNullable != expectedColumn.IsNullable)
                {
                    drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Breaking, "Column", $"{table}.{column}", "Column shape does not match the comparison source."));
                }
            }

            foreach (var index in expectedTable.Indexes.Except(liveTable.Indexes, StringComparer.OrdinalIgnoreCase))
            {
                drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Additive, "Index", $"{table}.{index}", "Expected index is missing from the live database."));
            }

            foreach (var foreignKey in expectedTable.ForeignKeys.Except(liveTable.ForeignKeys, StringComparer.OrdinalIgnoreCase))
            {
                drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Breaking, "Foreign Key", $"{table}.{foreignKey}", "Expected relationship is missing from the live database."));
            }

            if (!string.Equals(liveTable.PrimaryKey, expectedTable.PrimaryKey, StringComparison.OrdinalIgnoreCase))
            {
                drift.Add(new SchemaDriftItem(source, SchemaDriftSeverity.Breaking, "Primary Key", table, "Primary key definition does not match the comparison source."));
            }
        }

        return drift;
    }

    private static SchemaDescriptor BuildDescriptor(DatabaseMetadata metadata)
    {
        var tables = new Dictionary<string, TableDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in metadata.Tables)
        {
            var key = $"{table.SchemaName}.{table.ObjectName}";
            var columns = metadata.ColumnsByObject.TryGetValue(key, out var tableColumns)
                ? tableColumns.ToDictionary(
                    column => column.Name,
                    column => new ColumnDescriptor(column.DataType, column.IsNullable),
                    StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, ColumnDescriptor>(StringComparer.OrdinalIgnoreCase);

            var indexes = metadata.IndexesByTable.TryGetValue(key, out var tableIndexes)
                ? tableIndexes.Select(index => index.IndexName).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : [];

            var foreignKeys = metadata.ForeignKeysByTable.TryGetValue(key, out var tableForeignKeys)
                ? tableForeignKeys.Select(foreignKey => foreignKey.ConstraintName).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : [];

            var primaryKey = metadata.PrimaryKeysByTable.TryGetValue(key, out var primaryKeys)
                ? primaryKeys.FirstOrDefault()?.ConstraintName
                : null;

            tables[key] = new TableDescriptor(columns, indexes, foreignKeys, primaryKey);
        }

        return new SchemaDescriptor(tables);
    }

    private static SchemaDescriptor BuildDescriptor(IRelationalModel relationalModel)
    {
        var tables = relationalModel.Tables
            .ToDictionary(
                table => $"{table.Schema ?? "dbo"}.{table.Name}",
                table => new TableDescriptor(
                    table.Columns.ToDictionary(
                        column => column.Name,
                        column => new ColumnDescriptor(column.StoreType, column.IsNullable),
                        StringComparer.OrdinalIgnoreCase),
                    table.Indexes.Select(index => index.Name ?? BuildSyntheticName(index.Columns.Select(column => column.Name))).ToHashSet(StringComparer.OrdinalIgnoreCase),
                    table.ForeignKeyConstraints.Select(foreignKey => foreignKey.Name ?? BuildSyntheticName(foreignKey.Columns.Select(column => column.Name))).ToHashSet(StringComparer.OrdinalIgnoreCase),
                    table.PrimaryKey?.Name),
                StringComparer.OrdinalIgnoreCase);

        return new SchemaDescriptor(tables);
    }

    private static string BuildSyntheticName(IEnumerable<string> values)
        => string.Join("_", values);

    private static IReadOnlyList<string> SplitBatches(string script)
    {
        var batches = new List<string>();
        using var reader = new StringReader(script);
        var currentBatch = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBatch.Count > 0)
                {
                    batches.Add(string.Join(Environment.NewLine, currentBatch));
                    currentBatch.Clear();
                }

                continue;
            }

            currentBatch.Add(line);
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(string.Join(Environment.NewLine, currentBatch));
        }

        return batches;
    }

    private readonly record struct AppliedMigrationRow(
        string MigrationId,
        string ProductVersion);

    private sealed record SchemaDescriptor(
        IReadOnlyDictionary<string, TableDescriptor> Tables);

    private sealed record TableDescriptor(
        IReadOnlyDictionary<string, ColumnDescriptor> Columns,
        IReadOnlySet<string> Indexes,
        IReadOnlySet<string> ForeignKeys,
        string? PrimaryKey);

    private sealed record ColumnDescriptor(
        string StoreType,
        bool IsNullable);

    private sealed class SchemaMigrationsDbContextBundle(
        DbContext dbContext,
        IMigrationsAssembly migrationsAssembly,
        IMigrator migrator) : IDisposable
    {
        public DbContext DbContext { get; } = dbContext;

        public IMigrationsAssembly MigrationsAssembly { get; } = migrationsAssembly;

        public IMigrator Migrator { get; } = migrator;

        public void Dispose()
        {
            DbContext.Dispose();
        }
    }
}
