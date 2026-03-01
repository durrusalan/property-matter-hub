using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Logging;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Live implementation of IGmailApiAdapter.
/// All raw API calls are delegated to IGmailRawClient for testability.
/// </summary>
public sealed class LiveGmailApiAdapter : IGmailApiAdapter
{
    private readonly IGmailRawClient _client;

    public LiveGmailApiAdapter(IGmailRawClient client) => _client = client;

    public async Task<IReadOnlyList<Message>> FetchUnreadAsync(int maxResults, CancellationToken ct)
    {
        var list = await _client.ListAsync("is:unread in:inbox", maxResults, ct);
        if (list.Messages is null or { Count: 0 })
            return [];

        var messages = new List<Message>(list.Messages.Count);
        foreach (var stub in list.Messages)
            messages.Add(await _client.GetAsync(stub.Id, ct));

        return messages;
    }

    public Task<Message> GetMessageAsync(string messageId, CancellationToken ct) =>
        _client.GetAsync(messageId, ct);

    public Task SendRawAsync(string base64UrlRaw, string? threadId, CancellationToken ct) =>
        _client.SendAsync(base64UrlRaw, threadId, ct);

    public Task MarkReadAsync(string messageId, CancellationToken ct) =>
        _client.ModifyLabelsAsync(messageId, ["UNREAD"], ct);
}
