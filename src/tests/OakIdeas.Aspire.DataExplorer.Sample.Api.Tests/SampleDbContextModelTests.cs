using Microsoft.EntityFrameworkCore;
using OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Tests;

public sealed class SampleDbContextModelTests
{
    private static SampleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SampleDbContext(options);
    }

    [Fact]
    public void TodoItem_HasExpectedRelationshipsAndNullableCategory()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(TodoItem));

        entity.Should().NotBeNull();
        entity!.FindProperty(nameof(TodoItem.TodoCategoryId))!.IsNullable.Should().BeTrue();

        entity.FindForeignKeys(entity.FindProperty(nameof(TodoItem.TodoListId))!)
            .Should().ContainSingle();
        entity.FindForeignKeys(entity.FindProperty(nameof(TodoItem.TodoPriorityId))!)
            .Should().ContainSingle();
        entity.FindForeignKeys(entity.FindProperty(nameof(TodoItem.TodoStatusId))!)
            .Should().ContainSingle();
    }

    [Fact]
    public void Model_SeedsLookupAndRelationalData()
    {
        using var context = CreateContext();
        context.Database.EnsureCreated();

        context.TodoStatuses.Should().HaveCount(4);
        context.TodoPriorities.Should().HaveCount(4);
        context.TodoLists.Should().HaveCount(2);
        context.TodoTags.Should().HaveCount(5);
        context.TodoItems.Should().HaveCount(4);
        context.TodoItemTags.Should().HaveCount(6);
        context.TodoComments.Should().HaveCount(4);
    }
}
