namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

public sealed record TodoSummaryResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    string ListName,
    string? CategoryName,
    string PriorityName,
    string StatusName,
    IReadOnlyList<string> Tags);

public sealed record TodoDetailResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    LookupValue<int> List,
    LookupValue<int>? Category,
    LookupValue<byte> Priority,
    LookupValue<byte> Status,
    IReadOnlyList<LookupValue<int>> Tags,
    IReadOnlyList<TodoCommentResponse> Comments);

public sealed record TodoCommentResponse(
    int Id,
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record TodoLookupResponse(
    IReadOnlyList<LookupValue<int>> Lists,
    IReadOnlyList<CategoryLookupValue> Categories,
    IReadOnlyList<LookupValue<int>> Tags,
    IReadOnlyList<StatusLookupValue> Statuses,
    IReadOnlyList<PriorityLookupValue> Priorities);

public sealed record TodoShowcaseResponse(
    int MirroredListCount,
    int MirroredItemCount,
    int OpenMirroredItemCount,
    IReadOnlyList<TodoShowcaseRow> ProcedureRows,
    IReadOnlyList<TodoShowcaseRow> ViewRows);

public sealed record TodoShowcaseRow(
    int TodoItemId,
    string Title,
    string ListName,
    string StatusName,
    string PriorityName);

public sealed record LookupValue<T>(T Id, string Name) where T : struct;

public sealed record CategoryLookupValue(int Id, string Name, string ColorHex, bool IsSystem);

public sealed record StatusLookupValue(byte Id, string Name, bool IsClosed, byte SortOrder);

public sealed record PriorityLookupValue(byte Id, string Name, byte SortOrder, bool IsDefault);

public sealed record UpsertTodoItemRequest(
    string Title,
    int TodoListId,
    int? TodoCategoryId,
    byte TodoPriorityId,
    byte TodoStatusId,
    string? Description,
    DateTimeOffset? DueAt,
    IReadOnlyList<int>? TagIds);

public sealed record CreateTodoCommentRequest(string AuthorName, string Body);
