using Microsoft.EntityFrameworkCore;
using OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddSqlServerDbContext<SampleDbContext>("sampledb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/todoitems", async (SampleDbContext db) =>
    await db.TodoItems.OrderBy(t => t.CreatedAt).ToListAsync());

app.MapGet("/todoitems/{id:int}", async (int id, SampleDbContext db) =>
    await db.TodoItems.FindAsync(id) is TodoItem item
        ? Results.Ok(item)
        : Results.NotFound());

app.MapPost("/todoitems", async (TodoItem item, SampleDbContext db) =>
{
    item.CreatedAt = DateTimeOffset.UtcNow;
    db.TodoItems.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/todoitems/{item.Id}", item);
});

app.MapPut("/todoitems/{id:int}", async (int id, TodoItem input, SampleDbContext db) =>
{
    var item = await db.TodoItems.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Title = input.Title;
    item.IsCompleted = input.IsCompleted;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todoitems/{id:int}", async (int id, SampleDbContext db) =>
{
    var item = await db.TodoItems.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.TodoItems.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
