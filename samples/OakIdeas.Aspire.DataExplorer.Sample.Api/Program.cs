using Microsoft.EntityFrameworkCore;
using OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddSqlServerDbContext<SampleDbContext>("sampledb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup.DatabaseMigrations");

    var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
    var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToArray();

    logger.LogInformation(
        "EF migration diagnostics. Applied={AppliedCount} Pending={PendingCount} AppliedList={AppliedList} PendingList={PendingList}",
        appliedMigrations.Count(),
        pendingMigrations.Length,
        string.Join(", ", appliedMigrations),
        pendingMigrations.Length == 0 ? "(none)" : string.Join(", ", pendingMigrations));

    await db.Database.MigrateAsync();

    logger.LogInformation("EF MigrateAsync completed successfully.");
}

app.MapGet("/todoitems/lookups", async (SampleDbContext db, CancellationToken cancellationToken) =>
{
    var response = new TodoLookupResponse(
        Lists: await db.TodoLists.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupValue<int>(x.Id, x.Name)).ToListAsync(cancellationToken),
        Categories: await db.TodoCategories.AsNoTracking().OrderBy(x => x.Name).Select(x => new CategoryLookupValue(x.Id, x.Name, x.ColorHex, x.IsSystem)).ToListAsync(cancellationToken),
        Tags: await db.TodoTags.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupValue<int>(x.Id, x.Name)).ToListAsync(cancellationToken),
        Statuses: await db.TodoStatuses.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new StatusLookupValue(x.Id, x.Name, x.IsClosed, x.SortOrder)).ToListAsync(cancellationToken),
        Priorities: await db.TodoPriorities.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new PriorityLookupValue(x.Id, x.Name, x.SortOrder, x.IsDefault)).ToListAsync(cancellationToken));

    return Results.Ok(response);
});

app.MapGet("/todoitems", async (
    int? listId,
    int? categoryId,
    byte? statusId,
    string? search,
    SampleDbContext db,
    CancellationToken cancellationToken) =>
{
    var query = db.TodoItems
        .AsNoTracking()
        .Include(x => x.TodoList)
        .Include(x => x.TodoCategory)
        .Include(x => x.TodoPriority)
        .Include(x => x.TodoStatus)
        .Include(x => x.TodoItemTags)
        .ThenInclude(x => x.TodoTag)
        .AsQueryable();

    if (listId.HasValue)
    {
        query = query.Where(x => x.TodoListId == listId.Value);
    }

    if (categoryId.HasValue)
    {
        query = query.Where(x => x.TodoCategoryId == categoryId.Value);
    }

    if (statusId.HasValue)
    {
        query = query.Where(x => x.TodoStatusId == statusId.Value);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var trimmed = search.Trim();
        query = query.Where(x => x.Title.Contains(trimmed) || (x.Description != null && x.Description.Contains(trimmed)));
    }

    var items = await query
        .OrderBy(x => x.TodoStatus.SortOrder)
        .ThenByDescending(x => x.TodoPriority.SortOrder)
        .ThenBy(x => x.DueAt ?? DateTimeOffset.MaxValue)
        .Select(x => new TodoSummaryResponse(
            x.Id,
            x.Title,
            x.Description,
            x.TodoStatus.IsClosed,
            x.CreatedAt,
            x.DueAt,
            x.CompletedAt,
            x.TodoList.Name,
            x.TodoCategory != null ? x.TodoCategory.Name : null,
            x.TodoPriority.Name,
            x.TodoStatus.Name,
            x.TodoItemTags.Select(t => t.TodoTag.Name).OrderBy(x => x).ToList()))
        .ToListAsync(cancellationToken);

    return Results.Ok(items);
});

app.MapGet("/todoitems/{id:int}", async (int id, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var item = await db.TodoItems
        .AsNoTracking()
        .Include(x => x.TodoList)
        .Include(x => x.TodoCategory)
        .Include(x => x.TodoPriority)
        .Include(x => x.TodoStatus)
        .Include(x => x.TodoItemTags)
        .ThenInclude(x => x.TodoTag)
        .Include(x => x.TodoComments)
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (item is null)
    {
        return Results.NotFound();
    }

    var response = new TodoDetailResponse(
        item.Id,
        item.Title,
        item.Description,
        item.TodoStatus.IsClosed,
        item.CreatedAt,
        item.UpdatedAt,
        item.DueAt,
        item.CompletedAt,
        new LookupValue<int>(item.TodoList.Id, item.TodoList.Name),
        item.TodoCategory is null ? null : new LookupValue<int>(item.TodoCategory.Id, item.TodoCategory.Name),
        new LookupValue<byte>(item.TodoPriority.Id, item.TodoPriority.Name),
        new LookupValue<byte>(item.TodoStatus.Id, item.TodoStatus.Name),
        item.TodoItemTags.Select(x => new LookupValue<int>(x.TodoTagId, x.TodoTag.Name)).OrderBy(x => x.Name).ToList(),
        item.TodoComments
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TodoCommentResponse(x.Id, x.AuthorName, x.Body, x.CreatedAt))
            .ToList());

    return Results.Ok(response);
});

app.MapPost("/todoitems", async (UpsertTodoItemRequest request, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var validationErrors = await ValidateTodoRequestAsync(request, db, cancellationToken);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var now = DateTimeOffset.UtcNow;
    var todo = new TodoItem
    {
        Title = request.Title.Trim(),
        Description = NormalizeOptionalText(request.Description),
        TodoListId = request.TodoListId,
        TodoCategoryId = request.TodoCategoryId,
        TodoPriorityId = request.TodoPriorityId,
        TodoStatusId = request.TodoStatusId,
        DueAt = request.DueAt,
        CreatedAt = now,
        UpdatedAt = now,
        CompletedAt = await IsClosedStatusAsync(request.TodoStatusId, db, cancellationToken) ? now : null
    };

    var distinctTagIds = request.TagIds?.Distinct().ToArray() ?? [];
    foreach (var tagId in distinctTagIds)
    {
        todo.TodoItemTags.Add(new TodoItemTag { TodoTagId = tagId, AddedAt = now });
    }

    db.TodoItems.Add(todo);

    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
        return Results.Problem("Unable to save the todo item. Please review your input and try again.", statusCode: StatusCodes.Status400BadRequest);
    }

    return Results.Created($"/todoitems/{todo.Id}", new { todo.Id });
});

app.MapPut("/todoitems/{id:int}", async (int id, UpsertTodoItemRequest request, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var todo = await db.TodoItems
        .Include(x => x.TodoItemTags)
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (todo is null)
    {
        return Results.NotFound();
    }

    var validationErrors = await ValidateTodoRequestAsync(request, db, cancellationToken);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var now = DateTimeOffset.UtcNow;

    todo.Title = request.Title.Trim();
    todo.Description = NormalizeOptionalText(request.Description);
    todo.TodoListId = request.TodoListId;
    todo.TodoCategoryId = request.TodoCategoryId;
    todo.TodoPriorityId = request.TodoPriorityId;
    todo.TodoStatusId = request.TodoStatusId;
    todo.DueAt = request.DueAt;
    todo.UpdatedAt = now;
    todo.CompletedAt = await IsClosedStatusAsync(request.TodoStatusId, db, cancellationToken)
        ? todo.CompletedAt ?? now
        : null;

    var requestedTags = (request.TagIds ?? []).Distinct().ToHashSet();
    var existingTags = todo.TodoItemTags.Select(x => x.TodoTagId).ToHashSet();

    var removedTags = todo.TodoItemTags.Where(x => !requestedTags.Contains(x.TodoTagId)).ToList();
    if (removedTags.Count > 0)
    {
        db.TodoItemTags.RemoveRange(removedTags);
    }

    var addedTags = requestedTags.Where(x => !existingTags.Contains(x));
    foreach (var tagId in addedTags)
    {
        todo.TodoItemTags.Add(new TodoItemTag { TodoItemId = todo.Id, TodoTagId = tagId, AddedAt = now });
    }

    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
        return Results.Problem("Unable to update the todo item. Please refresh and try again.", statusCode: StatusCodes.Status400BadRequest);
    }

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id:int}", async (int id, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var item = await db.TodoItems.FindAsync([id], cancellationToken);
    if (item is null)
    {
        return Results.NotFound();
    }

    db.TodoItems.Remove(item);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.MapPost("/todoitems/{id:int}/complete", async (int id, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var item = await db.TodoItems.FindAsync([id], cancellationToken);
    if (item is null)
    {
        return Results.NotFound();
    }

    var closedStatusId = await db.TodoStatuses
        .Where(x => x.IsClosed)
        .OrderBy(x => x.SortOrder)
        .Select(x => x.Id)
        .FirstOrDefaultAsync(cancellationToken);

    if (closedStatusId == 0)
    {
        return Results.Problem("No closed status is configured for this sample database.", statusCode: StatusCodes.Status500InternalServerError);
    }

    item.TodoStatusId = closedStatusId;
    item.CompletedAt ??= DateTimeOffset.UtcNow;
    item.UpdatedAt = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.MapPost("/todoitems/{id:int}/reopen", async (int id, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var item = await db.TodoItems.FindAsync([id], cancellationToken);
    if (item is null)
    {
        return Results.NotFound();
    }

    var openStatusId = await db.TodoStatuses
        .Where(x => !x.IsClosed)
        .OrderBy(x => x.SortOrder)
        .Select(x => x.Id)
        .FirstOrDefaultAsync(cancellationToken);

    if (openStatusId == 0)
    {
        return Results.Problem("No open status is configured for this sample database.", statusCode: StatusCodes.Status500InternalServerError);
    }

    item.TodoStatusId = openStatusId;
    item.CompletedAt = null;
    item.UpdatedAt = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.MapPost("/todoitems/{id:int}/comments", async (int id, CreateTodoCommentRequest request, SampleDbContext db, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.AuthorName))
    {
        errors["authorName"] = ["Author name is required."];
    }
    else if (request.AuthorName.Trim().Length > 80)
    {
        errors["authorName"] = ["Author name must be 80 characters or fewer."];
    }

    if (string.IsNullOrWhiteSpace(request.Body))
    {
        errors["body"] = ["Comment text is required."];
    }
    else if (request.Body.Trim().Length > 1000)
    {
        errors["body"] = ["Comment text must be 1000 characters or fewer."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var exists = await db.TodoItems.AnyAsync(x => x.Id == id, cancellationToken);
    if (!exists)
    {
        return Results.NotFound();
    }

    db.TodoComments.Add(new TodoComment
    {
        TodoItemId = id,
        AuthorName = request.AuthorName.Trim(),
        Body = request.Body.Trim(),
        CreatedAt = DateTimeOffset.UtcNow
    });

    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.Run();

static async Task<bool> IsClosedStatusAsync(byte statusId, SampleDbContext db, CancellationToken cancellationToken)
    => await db.TodoStatuses.AsNoTracking().Where(x => x.Id == statusId).Select(x => x.IsClosed).SingleAsync(cancellationToken);

static string? NormalizeOptionalText(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static async Task<Dictionary<string, string[]>> ValidateTodoRequestAsync(
    UpsertTodoItemRequest request,
    SampleDbContext db,
    CancellationToken cancellationToken)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors["title"] = ["Title is required."];
    }
    else if (request.Title.Trim().Length > 200)
    {
        errors["title"] = ["Title must be 200 characters or fewer."];
    }

    if (!await db.TodoLists.AsNoTracking().AnyAsync(x => x.Id == request.TodoListId, cancellationToken))
    {
        errors["todoListId"] = ["A valid list is required."];
    }

    if (request.TodoCategoryId.HasValue && !await db.TodoCategories.AsNoTracking().AnyAsync(x => x.Id == request.TodoCategoryId.Value, cancellationToken))
    {
        errors["todoCategoryId"] = ["Selected category does not exist."];
    }

    if (!await db.TodoPriorities.AsNoTracking().AnyAsync(x => x.Id == request.TodoPriorityId, cancellationToken))
    {
        errors["todoPriorityId"] = ["A valid priority is required."];
    }

    if (!await db.TodoStatuses.AsNoTracking().AnyAsync(x => x.Id == request.TodoStatusId, cancellationToken))
    {
        errors["todoStatusId"] = ["A valid status is required."];
    }

    if (request.Description?.Length > 2000)
    {
        errors["description"] = ["Description must be 2000 characters or fewer."];
    }

    var tagIds = request.TagIds?.Distinct().ToArray() ?? [];
    if (tagIds.Length > 0)
    {
        var validTagCount = await db.TodoTags.AsNoTracking().CountAsync(x => tagIds.Contains(x.Id), cancellationToken);
        if (validTagCount != tagIds.Length)
        {
            errors["tagIds"] = ["One or more selected tags do not exist."];
        }
    }

    return errors;
}
