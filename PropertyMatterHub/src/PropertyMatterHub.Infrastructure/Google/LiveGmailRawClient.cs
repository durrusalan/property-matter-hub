using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Production IGmailRawClient backed by the real Google.Apis.Gmail.v1.GmailService.
/// Credentials are obtained lazily via GoogleAuthService so the first call
/// triggers OAuth if a token is not yet cached on the Z: drive.
/// </summary>
public sealed class LiveGmailRawClient : IGmailRawClient
{
    private const string UserId = "me";

    private readonly GoogleAuthService _auth;
    private GmailService? _service;

    public LiveGmailRawClient(GoogleAuthService auth) => _auth = auth;

    public async Task<ListMessagesResponse> ListAsync(
        string query, int maxResults, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        var req  = svc.Users.Messages.List(UserId);
        req.Q           = query;
        req.MaxResults  = maxResults;
        return await req.ExecuteAsync(ct);
    }

    public async Task<Message> GetAsync(string messageId, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        var req  = svc.Users.Messages.Get(UserId, messageId);
        req.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        return await req.ExecuteAsync(ct);
    }

    public async Task SendAsync(string base64UrlRaw, string? threadId, CancellationToken ct)
    {
        var svc     = await GetServiceAsync(ct);
        var message = new Message { Raw = base64UrlRaw };
        if (threadId is not null)
            message.ThreadId = threadId;
        await svc.Users.Messages.Send(message, UserId).ExecuteAsync(ct);
    }

    public async Task ModifyLabelsAsync(
        string messageId, IEnumerable<string> removeLabels, CancellationToken ct)
    {
        var svc  = await GetServiceAsync(ct);
        var body = new ModifyMessageRequest { RemoveLabelIds = [.. removeLabels] };
        await svc.Users.Messages.Modify(body, UserId, messageId).ExecuteAsync(ct);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task<GmailService> GetServiceAsync(CancellationToken ct)
    {
        if (_service is not null)
            return _service;

        var credential = await _auth.GetCredentialAsync(ct);
        _service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "PropertyMatterHub"
        });
        return _service;
    }
}
