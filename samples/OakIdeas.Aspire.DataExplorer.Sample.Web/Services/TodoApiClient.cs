using System.Net.Http.Json;
using OakIdeas.Aspire.DataExplorer.Sample.Web.Models;

namespace OakIdeas.Aspire.DataExplorer.Sample.Web.Services;

public sealed class TodoApiClient(HttpClient httpClient)
{
    public async Task<TodoLookupData> GetLookupsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<TodoLookupData>("/todoitems/lookups", cancellationToken)
            ?? new TodoLookupData([], [], [], [], []);

    public async Task<List<TodoSummary>> GetTodosAsync(TodoFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();

        if (filter.ListId.HasValue)
        {
            query.Add($"listId={filter.ListId.Value}");
        }

        if (filter.CategoryId.HasValue)
        {
            query.Add($"categoryId={filter.CategoryId.Value}");
        }

        if (filter.StatusId.HasValue)
        {
            query.Add($"statusId={filter.StatusId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query.Add($"search={Uri.EscapeDataString(filter.Search.Trim())}");
        }

        var path = query.Count > 0 ? $"/todoitems?{string.Join("&", query)}" : "/todoitems";
        return await httpClient.GetFromJsonAsync<List<TodoSummary>>(path, cancellationToken) ?? [];
    }

    public async Task<TodoDetail?> GetTodoAsync(int id, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<TodoDetail>($"/todoitems/{id}", cancellationToken);

    public async Task CreateTodoAsync(UpsertTodoRequest request, CancellationToken cancellationToken = default)
        => await SendAndEnsureSuccessAsync(() => httpClient.PostAsJsonAsync("/todoitems", request, cancellationToken));

    public async Task UpdateTodoAsync(int id, UpsertTodoRequest request, CancellationToken cancellationToken = default)
        => await SendAndEnsureSuccessAsync(() => httpClient.PutAsJsonAsync($"/todoitems/{id}", request, cancellationToken));

    public async Task DeleteTodoAsync(int id, CancellationToken cancellationToken = default)
        => await SendAndEnsureSuccessAsync(() => httpClient.DeleteAsync($"/todoitems/{id}", cancellationToken));

    public async Task CompleteTodoAsync(int id, CancellationToken cancellationToken = default)
        => await SendAndEnsureSuccessAsync(() => httpClient.PostAsync($"/todoitems/{id}/complete", null, cancellationToken));

    public async Task ReopenTodoAsync(int id, CancellationToken cancellationToken = default)
        => await SendAndEnsureSuccessAsync(() => httpClient.PostAsync($"/todoitems/{id}/reopen", null, cancellationToken));

    public async Task AddCommentAsync(int id, CreateTodoCommentRequest request, CancellationToken cancellationToken = default)
        => await SendAndEnsureSuccessAsync(() => httpClient.PostAsJsonAsync($"/todoitems/{id}/comments", request, cancellationToken));

    private static async Task SendAndEnsureSuccessAsync(Func<Task<HttpResponseMessage>> send)
    {
        var response = await send();
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var message = string.IsNullOrWhiteSpace(content)
            ? "Request failed. Please try again."
            : $"Request failed: {content}";

        throw new InvalidOperationException(message);
    }
}
