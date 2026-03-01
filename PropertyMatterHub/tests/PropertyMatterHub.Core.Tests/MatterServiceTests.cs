using FluentAssertions;
using NSubstitute;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.Core.Tests;

public class MatterServiceTests
{
    private readonly IMatterRepository _matterRepo = Substitute.For<IMatterRepository>();
    private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
    private readonly IExcelSyncService _excelSync = Substitute.For<IExcelSyncService>();
    private readonly MatterService _sut;

    public MatterServiceTests()
    {
        _sut = new MatterService(_matterRepo, _clientRepo, _excelSync);
    }

    [Fact]
    public async Task CreateMatterAsync_ValidMatter_SavesAndWritesToExcel()
    {
        var matter = new Matter { MatterRef = "PROP-2026-0099", Title = "Purchase – Test St", ClientId = 1 };
        var saved = new Matter { Id = 42, MatterRef = matter.MatterRef, Title = matter.Title, ClientId = 1 };

        _matterRepo.GetByRefAsync("PROP-2026-0099").Returns((Matter?)null);
        _matterRepo.AddAsync(Arg.Any<Matter>()).Returns(saved);

        var result = await _sut.CreateMatterAsync(matter);

        result.Id.Should().Be(42);
        await _matterRepo.Received(1).AddAsync(Arg.Any<Matter>());
        await _excelSync.Received(1).WriteMatterAsync(saved);
    }

    [Fact]
    public async Task CreateMatterAsync_EmptyRef_ThrowsArgumentException()
    {
        var matter = new Matter { MatterRef = "", Title = "Some Matter", ClientId = 1 };

        var act = () => _sut.CreateMatterAsync(matter);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*reference*");
    }

    [Fact]
    public async Task CreateMatterAsync_EmptyTitle_ThrowsArgumentException()
    {
        var matter = new Matter { MatterRef = "PROP-2026-0099", Title = "", ClientId = 1 };

        var act = () => _sut.CreateMatterAsync(matter);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*title*");
    }

    [Fact]
    public async Task CreateMatterAsync_DuplicateRef_ThrowsInvalidOperationException()
    {
        var existing = new Matter { Id = 1, MatterRef = "PROP-2026-0042", Title = "Existing" };
        _matterRepo.GetByRefAsync("PROP-2026-0042").Returns(existing);

        var matter = new Matter { MatterRef = "PROP-2026-0042", Title = "Duplicate" };

        var act = () => _sut.CreateMatterAsync(matter);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PROP-2026-0042*");
    }

    [Fact]
    public async Task CreateMatterAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var matter = new Matter { MatterRef = "PROP-2026-0100", Title = "Test", ClientId = 1 };
        var saved = new Matter { Id = 1, MatterRef = matter.MatterRef, Title = matter.Title };

        _matterRepo.GetByRefAsync(Arg.Any<string>()).Returns((Matter?)null);
        _matterRepo.AddAsync(Arg.Any<Matter>()).Returns(saved);

        await _sut.CreateMatterAsync(matter);

        matter.CreatedAt.Should().BeAfter(before);
        matter.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateMatterAsync_SetsUpdatedAtAndSyncsExcel()
    {
        var matter = new Matter { Id = 5, MatterRef = "PROP-2026-0005", Title = "Updated", ClientId = 1 };

        await _sut.UpdateMatterAsync(matter);

        await _matterRepo.Received(1).UpdateAsync(matter);
        await _excelSync.Received(1).WriteMatterAsync(matter);
        matter.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
