namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

public sealed class TodoList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TodoItem> TodoItems { get; set; } = [];
}

public sealed class TodoCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#64748b";
    public bool IsSystem { get; set; }

    public ICollection<TodoItem> TodoItems { get; set; } = [];
}

public sealed class TodoTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TodoItemTag> TodoItemTags { get; set; } = [];
}

public sealed class TodoPriority
{
    public byte Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte SortOrder { get; set; }
    public bool IsDefault { get; set; }

    public ICollection<TodoItem> TodoItems { get; set; } = [];
}

public sealed class TodoStatus
{
    public byte Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte SortOrder { get; set; }
    public bool IsClosed { get; set; }

    public ICollection<TodoItem> TodoItems { get; set; } = [];
}

public sealed class TodoItem
{
    public int Id { get; set; }
    public int TodoListId { get; set; }
    public int? TodoCategoryId { get; set; }
    public byte TodoPriorityId { get; set; }
    public byte TodoStatusId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public TodoList TodoList { get; set; } = null!;
    public TodoCategory? TodoCategory { get; set; }
    public TodoPriority TodoPriority { get; set; } = null!;
    public TodoStatus TodoStatus { get; set; } = null!;
    public ICollection<TodoItemTag> TodoItemTags { get; set; } = [];
    public ICollection<TodoComment> TodoComments { get; set; } = [];
}

public sealed class TodoItemTag
{
    public int TodoItemId { get; set; }
    public int TodoTagId { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    public TodoItem TodoItem { get; set; } = null!;
    public TodoTag TodoTag { get; set; } = null!;
}

public sealed class TodoComment
{
    public int Id { get; set; }
    public int TodoItemId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public TodoItem TodoItem { get; set; } = null!;
}
