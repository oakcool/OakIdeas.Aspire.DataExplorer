using OakIdeas.Aspire.DataExplorer.Sample.Web.Models;

namespace OakIdeas.Aspire.DataExplorer.Sample.Web.Services;

public sealed class TodoApiClient(HttpClient httpClient)
{
    public async Task<List<TodoItem>> GetTodosAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<TodoItem>>("/todoitems", cancellationToken) ?? [];

    public async Task CreateTodoAsync(string title, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/todoitems", new { Title = title, IsCompleted = false }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleTodoAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/todoitems/{item.Id}",
            new { item.Title, IsCompleted = !item.IsCompleted }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTodoAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/todoitems/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
