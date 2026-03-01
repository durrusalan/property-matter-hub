using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;
using PropertyMatterHub.Infrastructure.Data.Repositories;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// Repository tests use SQLite in-memory via EF Core so they exercise real SQL queries.
/// </summary>
public class MatterRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private MatterRepository _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db  = new AppDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();
        _sut = new MatterRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.CloseConnectionAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsAndReturnsWithId()
    {
        var client = new Client { Name = "Test Client" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var matter = new Matter
        {
            MatterRef    = "PROP-2026-0001",
            Title        = "Purchase – Test Road",
            PracticeArea = "Residential Purchase",
            ClientId     = client.Id
        };

        var saved = await _sut.AddAsync(matter);

        saved.Id.Should().BeGreaterThan(0);
        saved.MatterRef.Should().Be("PROP-2026-0001");
    }

    [Fact]
    public async Task GetByRefAsync_ExistingRef_ReturnsMatter()
    {
        var client = new Client { Name = "Test Client" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        _db.Matters.Add(new Matter { MatterRef = "PROP-2026-0042", Title = "Oak Lane", ClientId = client.Id });
        await _db.SaveChangesAsync();

        var result = await _sut.GetByRefAsync("PROP-2026-0042");

        result.Should().NotBeNull();
        result!.MatterRef.Should().Be("PROP-2026-0042");
    }

    [Fact]
    public async Task GetByRefAsync_NonExistentRef_ReturnsNull()
    {
        var result = await _sut.GetByRefAsync("PROP-9999-9999");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveMatters()
    {
        var client = new Client { Name = "Client" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        _db.Matters.AddRange(
            new Matter { MatterRef = "PROP-2026-0001", Title = "Active 1",  Status = MatterStatus.Active, ClientId = client.Id },
            new Matter { MatterRef = "PROP-2026-0002", Title = "Active 2",  Status = MatterStatus.Active, ClientId = client.Id },
            new Matter { MatterRef = "PROP-2025-0001", Title = "Closed",    Status = MatterStatus.Closed, ClientId = client.Id }
        );
        await _db.SaveChangesAsync();

        var active = await _sut.GetActiveAsync();

        active.Should().HaveCount(2);
        active.Should().AllSatisfy(m => m.Status.Should().Be(MatterStatus.Active));
    }

    [Fact]
    public async Task SearchAsync_MatchesRefAndTitle()
    {
        var client = new Client { Name = "Murphy Siobhan" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        _db.Matters.AddRange(
            new Matter { MatterRef = "PROP-2026-0042", Title = "Purchase – Oak Lane",   ClientId = client.Id },
            new Matter { MatterRef = "PROP-2026-0018", Title = "Sale – Harbour View",   ClientId = client.Id },
            new Matter { MatterRef = "PROP-2026-0009", Title = "Purchase – Sandymount", ClientId = client.Id }
        );
        await _db.SaveChangesAsync();

        var results = await _sut.SearchAsync("Oak");

        results.Should().ContainSingle();
        results[0].Title.Should().Contain("Oak");
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var client = new Client { Name = "Client" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var matter = new Matter { MatterRef = "PROP-2026-0001", Title = "Original", ClientId = client.Id };
        _db.Matters.Add(matter);
        await _db.SaveChangesAsync();

        matter.Title = "Updated Title";
        await _sut.UpdateAsync(matter);

        var fetched = await _sut.GetByRefAsync("PROP-2026-0001");
        fetched!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_RemovesMatter()
    {
        var client = new Client { Name = "Client" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var matter = new Matter { MatterRef = "PROP-2026-0001", Title = "To Delete", ClientId = client.Id };
        _db.Matters.Add(matter);
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(matter.Id);

        var fetched = await _sut.GetByRefAsync("PROP-2026-0001");
        fetched.Should().BeNull();
    }
}

public class ClientRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private ClientRepository _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db  = new AppDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();
        _sut = new ClientRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.CloseConnectionAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsClient()
    {
        var client = new Client { Name = "Siobhán Murphy", Email = "siobhan@email.ie" };

        var saved = await _sut.AddAsync(client);

        saved.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FindByEmailAsync_ExistingEmail_ReturnsClient()
    {
        _db.Clients.Add(new Client { Name = "Patrick Doyle", Email = "patrick@email.ie" });
        await _db.SaveChangesAsync();

        var result = await _sut.FindByEmailAsync("patrick@email.ie");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patrick Doyle");
    }

    [Fact]
    public async Task FindByEmailAsync_CaseInsensitive()
    {
        _db.Clients.Add(new Client { Name = "Emma Collins", Email = "emma@email.ie" });
        await _db.SaveChangesAsync();

        var result = await _sut.FindByEmailAsync("EMMA@EMAIL.IE");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_MatchesNameAndAddress()
    {
        _db.Clients.AddRange(
            new Client { Name = "Siobhán Murphy", Address = "12 Oak Lane, Dublin 6" },
            new Client { Name = "Patrick Doyle",  Address = "4 Harbour View, Bray"  }
        );
        await _db.SaveChangesAsync();

        var results = await _sut.SearchAsync("Murphy");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Siobhán Murphy");
    }
}
