using FluentAssertions;
using Google.Apis.Gmail.v1.Data;
using NSubstitute;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for LiveGmailApiAdapter.
/// The adapter is unit-tested via the IGmailRawClient abstraction.
/// </summary>
public class LiveGmailAdapterTests
{
    private readonly IGmailRawClient _rawClient = Substitute.For<IGmailRawClient>();
    private LiveGmailApiAdapter BuildSut() => new(_rawClient);

    // ── FetchUnreadAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task FetchUnreadAsync_QueriesOnlyUnreadInbox()
    {
        _rawClient.ListAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                  .Returns(new ListMessagesResponse { Messages = [] });

        await BuildSut().FetchUnreadAsync(maxResults: 25, CancellationToken.None);

        await _rawClient.Received(1).ListAsync(
            Arg.Is<string>(q => q.Contains("is:unread")),
            25,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchUnreadAsync_FetchesFullMessageForEachId()
    {
        var listResponse = new ListMessagesResponse
        {
            Messages = [new Message { Id = "msg-1" }, new Message { Id = "msg-2" }]
        };
        _rawClient.ListAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                  .Returns(listResponse);
        _rawClient.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new Message { Id = "msg-1" }, new Message { Id = "msg-2" });

        var result = await BuildSut().FetchUnreadAsync(maxResults: 50, CancellationToken.None);

        result.Should().HaveCount(2);
        await _rawClient.Received(1).GetAsync("msg-1", Arg.Any<CancellationToken>());
        await _rawClient.Received(1).GetAsync("msg-2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchUnreadAsync_ReturnsEmptyList_WhenNoMessages()
    {
        _rawClient.ListAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                  .Returns(new ListMessagesResponse { Messages = null });

        var result = await BuildSut().FetchUnreadAsync(maxResults: 50, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── SendRawAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SendRawAsync_DelegatesToRawClient_WithCorrectPayload()
    {
        const string raw = "base64encodedraw==";

        await BuildSut().SendRawAsync(raw, threadId: null, CancellationToken.None);

        await _rawClient.Received(1).SendAsync(raw, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRawAsync_PassesThreadId_WhenProvided()
    {
        await BuildSut().SendRawAsync("raw==", threadId: "thread-99", CancellationToken.None);

        await _rawClient.Received(1).SendAsync(
            Arg.Any<string>(),
            "thread-99",
            Arg.Any<CancellationToken>());
    }

    // ── MarkReadAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_RemovesUnreadLabel()
    {
        const string msgId = "msg-abc";

        await BuildSut().MarkReadAsync(msgId, CancellationToken.None);

        await _rawClient.Received(1).ModifyLabelsAsync(
            msgId,
            Arg.Is<IEnumerable<string>>(l => l.Contains("UNREAD")),
            Arg.Any<CancellationToken>());
    }
}
