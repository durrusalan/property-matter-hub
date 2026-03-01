using FluentAssertions;
using NSubstitute;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.Core.Tests;

public class ClientServiceTests
{
    private readonly IClientRepository _repo = Substitute.For<IClientRepository>();
    private readonly IExcelSyncService _excelSync = Substitute.For<IExcelSyncService>();
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        _sut = new ClientService(_repo, _excelSync);
    }

    [Fact]
    public async Task CreateClientAsync_ValidClient_SavesAndWritesToExcel()
    {
        var client = new Client { Name = "Siobhán Murphy", Email = "siobhan@email.ie" };
        var saved  = new Client { Id = 7, Name = client.Name, Email = client.Email };

        _repo.FindByEmailAsync("siobhan@email.ie").Returns((Client?)null);
        _repo.AddAsync(Arg.Any<Client>()).Returns(saved);

        var result = await _sut.CreateClientAsync(client);

        result.Id.Should().Be(7);
        await _repo.Received(1).AddAsync(Arg.Any<Client>());
        await _excelSync.Received(1).WriteClientAsync(saved);
    }

    [Fact]
    public async Task CreateClientAsync_EmptyName_ThrowsArgumentException()
    {
        var client = new Client { Name = "  " };

        var act = () => _sut.CreateClientAsync(client);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public async Task CreateClientAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _repo.FindByEmailAsync("dupe@email.ie").Returns(new Client { Id = 1, Name = "Existing" });

        var client = new Client { Name = "New Person", Email = "dupe@email.ie" };

        var act = () => _sut.CreateClientAsync(client);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*dupe@email.ie*");
    }

    [Fact]
    public async Task CreateClientAsync_NoEmail_SkipsDuplicateCheck()
    {
        var client = new Client { Name = "Walk-in Client" };
        var saved  = new Client { Id = 3, Name = client.Name };

        _repo.AddAsync(Arg.Any<Client>()).Returns(saved);

        var result = await _sut.CreateClientAsync(client);

        result.Id.Should().Be(3);
        await _repo.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task CreateClientAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var client = new Client { Name = "Test Client" };
        _repo.AddAsync(Arg.Any<Client>()).Returns(new Client { Id = 1, Name = "Test Client" });

        await _sut.CreateClientAsync(client);

        client.CreatedAt.Should().BeAfter(before);
        client.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateClientAsync_SetsUpdatedAtAndSyncsExcel()
    {
        var client = new Client { Id = 2, Name = "Updated Name", Email = "x@y.com" };

        await _sut.UpdateClientAsync(client);

        await _repo.Received(1).UpdateAsync(client);
        await _excelSync.Received(1).WriteClientAsync(client);
        client.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
