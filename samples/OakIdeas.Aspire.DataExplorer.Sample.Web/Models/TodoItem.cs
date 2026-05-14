namespace OakIdeas.Aspire.DataExplorer.Sample.Web.Models;

public sealed record TodoItem(int Id, string Title, bool IsCompleted, DateTimeOffset CreatedAt);
