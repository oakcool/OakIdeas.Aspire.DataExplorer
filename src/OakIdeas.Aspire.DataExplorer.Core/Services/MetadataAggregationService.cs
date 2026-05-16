using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class MetadataAggregationService(
    IProviderFactory providerFactory,
    IMetadataCache metadataCache,
    IOptions<MetadataAggregationOptions> options) : IMetadataAggregationService
{
    private readonly IProviderFactory _providerFactory = providerFactory;
    private readonly IMetadataCache _metadataCache = metadataCache;
    private readonly MetadataAggregationOptions _options = options.Value;

    public async Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
        SelectedDatabaseContext selectedDbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedDbContext);
        cancellationToken.ThrowIfCancellationRequested();

        var resourceId = selectedDbContext.Resource.ResourceId;
        var databaseName = selectedDbContext.Resource.DatabaseName;
        var providerType = selectedDbContext.Resource.ProviderType;

        var cached = await _metadataCache.GetAsync(resourceId, databaseName, cancellationToken);
        if (cached is not null)
        {
            return new DiscoverDatabaseMetadataResponse(cached);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.AggregationTimeoutSeconds)));
        var operationToken = timeoutSource.Token;

        var failures = new List<MetadataCollectionFailure>();

        try
        {
            var provider = _providerFactory.Create(providerType);
            var resource = CreateResource(selectedDbContext);

            var schemaProvider = provider as ISchemaDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support schema discovery.");
            var tableProvider = provider as ITableDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support table discovery.");
            var viewProvider = provider as IViewDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support view discovery.");
            var columnProvider = provider as IColumnDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support column discovery.");
            var primaryKeyProvider = provider as IPrimaryKeyDiscoveryProvider;
            var foreignKeyProvider = provider as IForeignKeyDiscoveryProvider;
            var indexProvider = provider as IIndexDiscoveryProvider;
            var constraintProvider = provider as IConstraintDiscoveryProvider;
            var procedureProvider = provider as IStoredProcedureDiscoveryProvider;
            var functionProvider = provider as IFunctionDiscoveryProvider;
            var triggerProvider = provider as ITriggerDiscoveryProvider;
            var definitionProvider = provider as IObjectDefinitionProvider;

            var schemasResponse = await DiscoverRequiredAsync(
                "schemas",
                null,
                token => schemaProvider.DiscoverSchemasAsync(resource, new DiscoverSchemasRequest(), token),
                failures,
                operationToken);

            var tablesTask = DiscoverOptionalAsync(
                "tables",
                null,
                token => tableProvider.DiscoverTablesAsync(resource, new DiscoverTablesRequest(), token),
                () => new DiscoverTablesResponse([]),
                failures,
                operationToken);
            var viewsTask = DiscoverOptionalAsync(
                "views",
                null,
                token => viewProvider.DiscoverViewsAsync(resource, new DiscoverViewsRequest(), token),
                () => new DiscoverViewsResponse([]),
                failures,
                operationToken);
            var proceduresTask = DiscoverOptionalAsync(
                "procedures",
                null,
                token => procedureProvider is null
                    ? Task.FromResult(new DiscoverStoredProceduresResponse(new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)))
                    : procedureProvider.DiscoverStoredProceduresAsync(resource, new DiscoverStoredProceduresRequest(), token),
                () => new DiscoverStoredProceduresResponse(new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)),
                failures,
                operationToken);
            var functionsTask = DiscoverOptionalAsync(
                "functions",
                null,
                token => functionProvider is null
                    ? Task.FromResult(new DiscoverFunctionsResponse(new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)))
                    : functionProvider.DiscoverFunctionsAsync(resource, new DiscoverFunctionsRequest(), token),
                () => new DiscoverFunctionsResponse(new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)),
                failures,
                operationToken);
            var triggersTask = DiscoverOptionalAsync(
                "triggers",
                null,
                token => triggerProvider is null
                    ? Task.FromResult(new DiscoverTriggersResponse([]))
                    : triggerProvider.DiscoverTriggersAsync(resource, new DiscoverTriggersRequest(), token),
                () => new DiscoverTriggersResponse([]),
                failures,
                operationToken);

            await Task.WhenAll(tablesTask, viewsTask, proceduresTask, functionsTask, triggersTask);

            var tables = tablesTask.Result.Tables;
            var views = viewsTask.Result.Views;

            var tableDiscoveryTasks = tables.Select(table => DiscoverTableDetailsAsync(
                resource,
                table,
                columnProvider,
                primaryKeyProvider,
                foreignKeyProvider,
                indexProvider,
                constraintProvider,
                failures,
                operationToken)).ToArray();
            var viewDiscoveryTasks = views.Select(view => DiscoverViewDetailsAsync(
                resource,
                view,
                columnProvider,
                failures,
                operationToken)).ToArray();

            await Task.WhenAll(tableDiscoveryTasks.Concat(viewDiscoveryTasks));

            var tableDetails = tableDiscoveryTasks.Select(task => task.Result).ToArray();
            var viewDetails = viewDiscoveryTasks.Select(task => task.Result).ToArray();

            var procedures = proceduresTask.Result.ProceduresBySchema;
            var functions = functionsTask.Result.FunctionsBySchema;
            var triggers = triggersTask.Result.Triggers;

            if (_options.EnableBackgroundDefinitionLoading && definitionProvider is not null)
            {
                _ = LoadDefinitionsInBackgroundAsync(
                    definitionProvider,
                    resource,
                    views,
                    procedures,
                    functions,
                    triggers);
            }

            var collectedAt = DateTimeOffset.UtcNow;
            var constraints = tableDetails.SelectMany(result => result.Constraints).ToArray();

            var objects = BuildObjects(
                schemasResponse.Schemas,
                tables,
                views,
                procedures,
                functions,
                triggers);

            var root = new DatabaseMetadataRoot(
                databaseName: databaseName,
                providerType: providerType,
                resourceId: resourceId,
                metadataCollectionTime: collectedAt,
                objects: objects);

            var status = DetermineStatus(
                failures,
                schemasResponse.Schemas.Count,
                tables.Count,
                views.Count,
                procedures.Count,
                functions.Count,
                triggers.Count,
                constraints.Length);

            var columnsByObject = tableDetails
                .Concat(viewDetails)
                .ToDictionary(entry => entry.Key, entry => entry.Columns, StringComparer.OrdinalIgnoreCase);

            var primaryKeysByTable = tableDetails
                .ToDictionary(entry => entry.Key, entry => entry.PrimaryKeys, StringComparer.OrdinalIgnoreCase);
            var foreignKeysByTable = tableDetails
                .ToDictionary(entry => entry.Key, entry => entry.ForeignKeys, StringComparer.OrdinalIgnoreCase);
            var indexesByTable = tableDetails
                .ToDictionary(entry => entry.Key, entry => entry.Indexes, StringComparer.OrdinalIgnoreCase);

            var aggregateMetadata = new DatabaseMetadata(
                DatabaseName: databaseName,
                ProviderType: providerType,
                ResourceId: resourceId,
                Schemas: schemasResponse.Schemas,
                Tables: tables,
                Views: views,
                ProceduresBySchema: procedures,
                FunctionsBySchema: functions,
                Triggers: triggers,
                Constraints: constraints,
                ColumnsByObject: columnsByObject,
                PrimaryKeysByTable: primaryKeysByTable,
                ForeignKeysByTable: foreignKeysByTable,
                IndexesByTable: indexesByTable,
                MetadataCollectionTime: collectedAt,
                CollectionStatus: status,
                FailureDetails: failures);

            await _metadataCache.SetAsync(resourceId, databaseName, root, operationToken);

            return new DiscoverDatabaseMetadataResponse(
                Metadata: root,
                AggregatedMetadata: aggregateMetadata,
                CollectionStatus: status,
                FailureDetails: failures);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            failures.Add(new MetadataCollectionFailure("aggregation", null, "Metadata aggregation timed out."));

            return new DiscoverDatabaseMetadataResponse(
                Metadata: new DatabaseMetadataRoot(
                    databaseName: databaseName,
                    providerType: providerType,
                    resourceId: resourceId,
                    metadataCollectionTime: DateTimeOffset.UtcNow),
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Failed,
                FailureDetails: failures);
        }
    }

    private async Task<TableDiscoveryDetails> DiscoverTableDetailsAsync(
        DatabaseResource resource,
        TableObject table,
        IColumnDiscoveryProvider columnProvider,
        IPrimaryKeyDiscoveryProvider? primaryKeyProvider,
        IForeignKeyDiscoveryProvider? foreignKeyProvider,
        IIndexDiscoveryProvider? indexProvider,
        IConstraintDiscoveryProvider? constraintProvider,
        List<MetadataCollectionFailure> failures,
        CancellationToken cancellationToken)
    {
        var key = table.FullyQualifiedName;
        var columnsTask = DiscoverOptionalAsync(
            "columns",
            key,
            token => columnProvider.DiscoverColumnsAsync(
                resource,
                new DiscoverColumnsRequest(ObjectId: table.ObjectId, ObjectType: DatabaseObjectType.Table),
                token),
            () => new DiscoverColumnsResponse([]),
            failures,
            cancellationToken);

        var primaryKeysTask = DiscoverOptionalAsync(
            "primary-keys",
            key,
            token => primaryKeyProvider is null
                ? Task.FromResult(new DiscoverPrimaryKeysResponse([]))
                : primaryKeyProvider.DiscoverPrimaryKeysAsync(
                    resource,
                    new DiscoverPrimaryKeysRequest(SchemaName: table.SchemaName, TableName: table.ObjectName),
                    token),
            () => new DiscoverPrimaryKeysResponse([]),
            failures,
            cancellationToken);

        var foreignKeysTask = DiscoverOptionalAsync(
            "foreign-keys",
            key,
            token => foreignKeyProvider is null
                ? Task.FromResult(new DiscoverForeignKeysResponse([]))
                : foreignKeyProvider.DiscoverForeignKeysAsync(
                    resource,
                    new DiscoverForeignKeysRequest(ParentSchemaName: table.SchemaName, ParentTableName: table.ObjectName),
                    token),
            () => new DiscoverForeignKeysResponse([]),
            failures,
            cancellationToken);

        var indexesTask = DiscoverOptionalAsync(
            "indexes",
            key,
            token => indexProvider is null
                ? Task.FromResult(new DiscoverIndexesResponse([]))
                : indexProvider.DiscoverIndexesAsync(
                    resource,
                    new DiscoverIndexesRequest(SchemaName: table.SchemaName, TableName: table.ObjectName),
                    token),
            () => new DiscoverIndexesResponse([]),
            failures,
            cancellationToken);

        var constraintsTask = DiscoverOptionalAsync(
            "constraints",
            key,
            token => constraintProvider is null
                ? Task.FromResult(new DiscoverConstraintsResponse([]))
                : constraintProvider.DiscoverConstraintsAsync(
                    resource,
                    new DiscoverConstraintsRequest(SchemaName: table.SchemaName, TableName: table.ObjectName),
                    token),
            () => new DiscoverConstraintsResponse([]),
            failures,
            cancellationToken);

        await Task.WhenAll(columnsTask, primaryKeysTask, foreignKeysTask, indexesTask, constraintsTask);

        return new TableDiscoveryDetails(
            Key: key,
            Columns: columnsTask.Result.Columns,
            PrimaryKeys: primaryKeysTask.Result.PrimaryKeys,
            ForeignKeys: foreignKeysTask.Result.ForeignKeys,
            Indexes: indexesTask.Result.Indexes,
            Constraints: constraintsTask.Result.Constraints);
    }

    private async Task<ViewDiscoveryDetails> DiscoverViewDetailsAsync(
        DatabaseResource resource,
        ViewObject view,
        IColumnDiscoveryProvider columnProvider,
        List<MetadataCollectionFailure> failures,
        CancellationToken cancellationToken)
    {
        var key = view.FullyQualifiedName;
        var columns = await DiscoverOptionalAsync(
            "columns",
            key,
            token => columnProvider.DiscoverColumnsAsync(
                resource,
                new DiscoverColumnsRequest(ObjectId: view.ObjectId, ObjectType: DatabaseObjectType.View),
                token),
            () => new DiscoverColumnsResponse([]),
            failures,
            cancellationToken);

        return new ViewDiscoveryDetails(
            Key: key,
            Columns: columns.Columns);
    }

    private async Task<T> DiscoverRequiredAsync<T>(
        string operation,
        string? target,
        Func<CancellationToken, Task<T>> discover,
        List<MetadataCollectionFailure> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteWithRetryAsync(discover, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failures.Add(new MetadataCollectionFailure(operation, target, ex.Message));
            throw;
        }
    }

    private async Task<T> DiscoverOptionalAsync<T>(
        string operation,
        string? target,
        Func<CancellationToken, Task<T>> discover,
        Func<T> onFailure,
        List<MetadataCollectionFailure> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteWithRetryAsync(discover, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failures.Add(new MetadataCollectionFailure(operation, target, ex.Message));
            return onFailure();
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var retries = Math.Max(0, _options.TransientRetryCount);
        var delay = Math.Max(1, _options.RetryDelayMilliseconds);
        Exception? lastException = null;

        for (var attempt = 0; attempt <= retries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                if (!IsTransient(ex) || attempt == retries)
                {
                    break;
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Metadata discovery failed.");
    }

    private static bool IsTransient(Exception exception)
        => exception is TimeoutException or IOException;

    private static Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>> BuildObjects(
        IReadOnlyList<SchemaObject> schemas,
        IReadOnlyList<TableObject> tables,
        IReadOnlyList<ViewObject> views,
        IReadOnlyDictionary<string, IReadOnlyList<StoredProcedureMetadata>> proceduresBySchema,
        IReadOnlyDictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>> functionsBySchema,
        IReadOnlyList<TriggerMetadata> triggers)
    {
        var objects = new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>();

        objects[DatabaseObjectType.Schema] = schemas.ToDictionary(
            schema => schema.ObjectName,
            schema => (DatabaseObject)schema,
            StringComparer.OrdinalIgnoreCase);

        objects[DatabaseObjectType.Table] = tables.ToDictionary(
            table => table.FullyQualifiedName,
            table => (DatabaseObject)table,
            StringComparer.OrdinalIgnoreCase);

        objects[DatabaseObjectType.View] = views.ToDictionary(
            view => view.FullyQualifiedName,
            view => (DatabaseObject)view,
            StringComparer.OrdinalIgnoreCase);

        var procedures = proceduresBySchema
            .SelectMany(group => group.Value.Select(procedure => new ProcedureObject(
                objectId: procedure.ObjectId,
                schemaName: procedure.SchemaName,
                objectName: procedure.ProcedureName)))
            .ToArray();

        objects[DatabaseObjectType.Procedure] = procedures.ToDictionary(
            procedure => procedure.FullyQualifiedName,
            procedure => (DatabaseObject)procedure,
            StringComparer.OrdinalIgnoreCase);

        var functions = functionsBySchema
            .SelectMany(group => group.Value.SelectMany(typeGroup => typeGroup.Value.Select(function => new FunctionObject(
                objectId: function.ObjectId,
                schemaName: function.SchemaName,
                objectName: function.FunctionName))))
            .ToArray();

        objects[DatabaseObjectType.Function] = functions.ToDictionary(
            function => function.FullyQualifiedName,
            function => (DatabaseObject)function,
            StringComparer.OrdinalIgnoreCase);

        var triggerObjects = triggers
            .Select(trigger => new TriggerObject(
                objectId: trigger.ObjectId,
                schemaName: trigger.SchemaName,
                objectName: trigger.TriggerName))
            .ToArray();

        objects[DatabaseObjectType.Trigger] = triggerObjects.ToDictionary(
            trigger => trigger.FullyQualifiedName,
            trigger => (DatabaseObject)trigger,
            StringComparer.OrdinalIgnoreCase);

        return objects;
    }

    private static MetadataCollectionStatus DetermineStatus(
        IReadOnlyList<MetadataCollectionFailure> failures,
        params int[] discoveredCounts)
    {
        if (failures.Count == 0)
        {
            return MetadataCollectionStatus.Success;
        }

        return discoveredCounts.Any(count => count > 0)
            ? MetadataCollectionStatus.PartialSuccess
            : MetadataCollectionStatus.Failed;
    }

    private static DatabaseResource CreateResource(SelectedDatabaseContext context)
    {
        var connectionString = ResolveConnectionString(context.Resource.ConnectionMetadata.Properties);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string could be resolved for resource '{context.Resource.ResourceId}'.");
        }

        return new DatabaseResource(
            Name: context.Resource.ResourceName,
            Provider: context.Resource.ProviderType.ToString(),
            ConnectionString: connectionString,
            IsLocal: true,
            IsWritable: true);
    }

    private static string? ResolveConnectionString(IReadOnlyDictionary<string, string?> metadata)
    {
        if (metadata.TryGetValue("connectionString", out var directConnectionString)
            && !string.IsNullOrWhiteSpace(directConnectionString))
        {
            return directConnectionString;
        }

        if (metadata.TryGetValue("connectionStringEnvironmentVariable", out var environmentVariableName)
            && !string.IsNullOrWhiteSpace(environmentVariableName))
        {
            return Environment.GetEnvironmentVariable(environmentVariableName);
        }

        return null;
    }

    private static async Task LoadDefinitionsInBackgroundAsync(
        IObjectDefinitionProvider definitionProvider,
        DatabaseResource resource,
        IReadOnlyList<ViewObject> views,
        IReadOnlyDictionary<string, IReadOnlyList<StoredProcedureMetadata>> proceduresBySchema,
        IReadOnlyDictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>> functionsBySchema,
        IReadOnlyList<TriggerMetadata> triggers)
    {
        try
        {
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var token = timeoutSource.Token;

            var definitionTasks = new List<Task>();

            definitionTasks.AddRange(views.Where(view => view.HasDefinitionAvailable).Select(view =>
                definitionProvider.GetDefinitionAsync(resource, new ObjectDefinitionRequest(view.ObjectId, DatabaseObjectType.View), token)));

            definitionTasks.AddRange(proceduresBySchema.SelectMany(schema => schema.Value)
                .Where(procedure => procedure.HasDefinitionAvailable)
                .Select(procedure => definitionProvider.GetDefinitionAsync(
                    resource,
                    new ObjectDefinitionRequest(procedure.ObjectId, DatabaseObjectType.Procedure),
                    token)));

            definitionTasks.AddRange(functionsBySchema.SelectMany(schema => schema.Value.SelectMany(functionGroup => functionGroup.Value))
                .Where(function => function.HasDefinitionAvailable)
                .Select(function => definitionProvider.GetDefinitionAsync(
                    resource,
                    new ObjectDefinitionRequest(function.ObjectId, DatabaseObjectType.Function),
                    token)));

            definitionTasks.AddRange(triggers.Where(trigger => trigger.HasDefinitionAvailable).Select(trigger =>
                definitionProvider.GetDefinitionAsync(
                    resource,
                    new ObjectDefinitionRequest(trigger.ObjectId, DatabaseObjectType.Trigger),
                    token)));

            await Task.WhenAll(definitionTasks);
        }
        catch
        {
            // Background definition loading is best-effort and intentionally non-fatal.
        }
    }

    private sealed record TableDiscoveryDetails(
        string Key,
        IReadOnlyList<ColumnMetadata> Columns,
        IReadOnlyList<PrimaryKeyConstraint> PrimaryKeys,
        IReadOnlyList<ForeignKeyConstraint> ForeignKeys,
        IReadOnlyList<IndexMetadata> Indexes,
        IReadOnlyList<ConstraintMetadata> Constraints);

    private sealed record ViewDiscoveryDetails(
        string Key,
        IReadOnlyList<ColumnMetadata> Columns);
}
