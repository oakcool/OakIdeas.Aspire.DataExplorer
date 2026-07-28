using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using ContractColumnMetadata = OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class MetadataAggregationService(
    IProviderFactory providerFactory,
    IMetadataCache metadataCache,
    IOptions<MetadataAggregationOptions> options,
    IErrorHandler errorHandler,
    IFeatureFlagService featureFlagService) : IMetadataAggregationService
{
    private readonly IProviderFactory _providerFactory = providerFactory;
    private readonly IMetadataCache _metadataCache = metadataCache;
    private readonly MetadataAggregationOptions _options = options.Value;
    private readonly IErrorHandler _errorHandler = errorHandler;
    private readonly IFeatureFlagService _featureFlagService = featureFlagService;

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
            return cached;
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.AggregationTimeoutSeconds)));
        var operationToken = timeoutSource.Token;

        var failures = new List<MetadataCollectionFailure>();
        var context = new ErrorContext("load-metadata", databaseName, providerType);

        try
        {
            var provider = _providerFactory.Create(providerType);
            var resource = CreateResource(selectedDbContext);

            var schemaProvider = provider as ISchemaDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support schema discovery.");
            var tableProvider = provider as ITableDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support table discovery.");
            var columnProvider = provider as IColumnDiscoveryProvider
                ?? throw new InvalidOperationException($"Provider '{provider.GetType().Name}' does not support column discovery.");

            // Evaluate feature flags once before starting optional discovery tasks.
            // Disabled flags suppress the corresponding discoverer even when the provider supports it.
            var viewsEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.Views, null, operationToken).ConfigureAwait(false);
            var storedProceduresEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.StoredProcedures, null, operationToken).ConfigureAwait(false);
            var functionsEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.Functions, null, operationToken).ConfigureAwait(false);
            var triggersEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.Triggers, null, operationToken).ConfigureAwait(false);
            var indexesEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.Indexes, null, operationToken).ConfigureAwait(false);
            var constraintsEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.Constraints, null, operationToken).ConfigureAwait(false);
            var foreignKeysEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.ForeignKeys, null, operationToken).ConfigureAwait(false);
            var primaryKeysEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.PrimaryKeys, null, operationToken).ConfigureAwait(false);
            var objectDefinitionEnabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.ObjectDefinition, null, operationToken).ConfigureAwait(false);

            var viewProvider = viewsEnabled ? provider as IViewDiscoveryProvider : null;
            var primaryKeyProvider = primaryKeysEnabled ? provider as IPrimaryKeyDiscoveryProvider : null;
            var foreignKeyProvider = foreignKeysEnabled ? provider as IForeignKeyDiscoveryProvider : null;
            var indexProvider = indexesEnabled ? provider as IIndexDiscoveryProvider : null;
            var constraintProvider = constraintsEnabled ? provider as IConstraintDiscoveryProvider : null;
            var procedureProvider = storedProceduresEnabled ? provider as IStoredProcedureDiscoveryProvider : null;
            var functionProvider = functionsEnabled ? provider as IFunctionDiscoveryProvider : null;
            var triggerProvider = triggersEnabled ? provider as ITriggerDiscoveryProvider : null;
            var definitionProvider = objectDefinitionEnabled ? provider as IObjectDefinitionProvider : null;

            var schemasResponse = await DiscoverRequiredAsync(
                "schemas",
                null,
                token => schemaProvider.DiscoverSchemasAsync(resource, new DiscoverSchemasRequest(), token),
                failures,
                operationToken,
                context);

            var tablesTask = DiscoverOptionalAsync(
                "tables",
                null,
                token => tableProvider.DiscoverTablesAsync(resource, new DiscoverTablesRequest(), token),
                () => new DiscoverTablesResponse([]),
                failures,
                operationToken,
                context);
            var viewsTask = DiscoverOptionalAsync(
                "views",
                null,
                token => viewProvider is null
                    ? Task.FromResult(new DiscoverViewsResponse([]))
                    : viewProvider.DiscoverViewsAsync(resource, new DiscoverViewsRequest(), token),
                () => new DiscoverViewsResponse([]),
                failures,
                operationToken,
                context);
            var proceduresTask = DiscoverOptionalAsync(
                "procedures",
                null,
                token => procedureProvider is null
                    ? Task.FromResult(new DiscoverStoredProceduresResponse(new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)))
                    : procedureProvider.DiscoverStoredProceduresAsync(resource, new DiscoverStoredProceduresRequest(), token),
                () => new DiscoverStoredProceduresResponse(new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)),
                failures,
                operationToken,
                context);
            var functionsTask = DiscoverOptionalAsync(
                "functions",
                null,
                token => functionProvider is null
                    ? Task.FromResult(new DiscoverFunctionsResponse(new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)))
                    : functionProvider.DiscoverFunctionsAsync(resource, new DiscoverFunctionsRequest(), token),
                () => new DiscoverFunctionsResponse(new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)),
                failures,
                operationToken,
                context);
            var triggersTask = DiscoverOptionalAsync(
                "triggers",
                null,
                token => triggerProvider is null
                    ? Task.FromResult(new DiscoverTriggersResponse([]))
                    : triggerProvider.DiscoverTriggersAsync(resource, new DiscoverTriggersRequest(), token),
                () => new DiscoverTriggersResponse([]),
                failures,
                operationToken,
                context);

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
                operationToken,
                context)).ToArray();
            var viewDiscoveryTasks = views.Select(view => DiscoverViewDetailsAsync(
                resource,
                view,
                columnProvider,
                failures,
                operationToken,
                context)).ToArray();

            await Task.WhenAll(tableDiscoveryTasks);
            await Task.WhenAll(viewDiscoveryTasks);

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
                .ToDictionary(entry => entry.Key, entry => entry.Columns, StringComparer.OrdinalIgnoreCase);
            foreach (var viewEntry in viewDetails)
            {
                columnsByObject[viewEntry.Key] = viewEntry.Columns;
            }

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

            var discoverResponse = new DiscoverDatabaseMetadataResponse(
                Metadata: root,
                AggregatedMetadata: aggregateMetadata,
                CollectionStatus: status,
                FailureDetails: failures,
                Error: failures.Count == 0 ? null : CreateAggregateError(failures, context));

            await _metadataCache.SetAsync(resourceId, databaseName, discoverResponse, operationToken);

            return discoverResponse;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.QueryTimeout,
                "Metadata aggregation timed out before discovery completed.",
                "Retry the operation after the database workload settles.",
                context,
                diagnosticCode: "aggregation-timeout");
            failures.Add(new MetadataCollectionFailure("aggregation", null, error.Message));

            return new DiscoverDatabaseMetadataResponse(
                Metadata: new DatabaseMetadataRoot(
                    databaseName: databaseName,
                    providerType: providerType,
                    resourceId: resourceId,
                    metadataCollectionTime: DateTimeOffset.UtcNow),
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Failed,
                FailureDetails: failures,
                Error: error);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = _errorHandler.MapException(ex, context);
            failures.Add(new MetadataCollectionFailure("aggregation", databaseName, error.Message));

            return new DiscoverDatabaseMetadataResponse(
                Metadata: new DatabaseMetadataRoot(
                    databaseName: databaseName,
                    providerType: providerType,
                    resourceId: resourceId,
                    metadataCollectionTime: DateTimeOffset.UtcNow),
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Failed,
                FailureDetails: failures,
                Error: error);
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
        CancellationToken cancellationToken,
        ErrorContext context)
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
            cancellationToken,
            context);

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
            cancellationToken,
            context);

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
            cancellationToken,
            context);

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
            cancellationToken,
            context);

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
            cancellationToken,
            context);

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
        CancellationToken cancellationToken,
        ErrorContext context)
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
            cancellationToken,
            context);

        return new ViewDiscoveryDetails(
            Key: key,
            Columns: columns.Columns);
    }

    private async Task<T> DiscoverRequiredAsync<T>(
        string operation,
        string? target,
        Func<CancellationToken, Task<T>> discover,
        List<MetadataCollectionFailure> failures,
        CancellationToken cancellationToken,
        ErrorContext context)
    {
        try
        {
            return await ExecuteWithRetryAsync(discover, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = _errorHandler.MapException(ex, context with { Operation = operation, Target = target ?? context.Target });
            failures.Add(new MetadataCollectionFailure(operation, target, error.Message));
            throw;
        }
    }

    private async Task<T> DiscoverOptionalAsync<T>(
        string operation,
        string? target,
        Func<CancellationToken, Task<T>> discover,
        Func<T> onFailure,
        List<MetadataCollectionFailure> failures,
        CancellationToken cancellationToken,
        ErrorContext context)
    {
        try
        {
            return await ExecuteWithRetryAsync(discover, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = _errorHandler.MapException(ex, context with { Operation = operation, Target = target ?? context.Target });
            failures.Add(new MetadataCollectionFailure(operation, target, error.Message));
            return onFailure();
        }
    }

    private DataExplorerError CreateAggregateError(
        IReadOnlyList<MetadataCollectionFailure> failures,
        ErrorContext context)
    {
        var firstFailure = failures[0];
        return _errorHandler.CreateError(
            ErrorCategory.ProviderError,
            firstFailure.Message,
            "Review the diagnostic details and retry the operation if needed.",
            context with { Operation = firstFailure.Operation, Target = firstFailure.Target ?? context.Target },
            diagnosticCode: "metadata-partial-failure");
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
        IReadOnlyList<ContractColumnMetadata> Columns,
        IReadOnlyList<PrimaryKeyConstraint> PrimaryKeys,
        IReadOnlyList<ForeignKeyConstraint> ForeignKeys,
        IReadOnlyList<IndexMetadata> Indexes,
        IReadOnlyList<ConstraintMetadata> Constraints);

    private sealed record ViewDiscoveryDetails(
        string Key,
        IReadOnlyList<ContractColumnMetadata> Columns);
}
