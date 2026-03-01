using FluentAssertions;
using PropertyMatterHub.Infrastructure.FileSystem;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// Tests ZDriveScanner folder-pattern matching using in-memory fake directory trees.
/// No real disk I/O — we inject a fake file-system abstraction.
/// </summary>
public class ZDriveScannerTests
{
    // Default pattern: "ClientName - CaseNumber"
    private static FolderStructureConfig DefaultConfig(string root) => new()
    {
        RootPath          = root,
        CaseFolderPattern = @"^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$",
        CaseFolderDepth   = 1
    };

    // ── ParseFolderName ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("Murphy Siobhan - PROP-2026-0042", "Murphy Siobhan",  "PROP-2026-0042")]
    [InlineData("Patrick Doyle - PROP-2026-0018",  "Patrick Doyle",   "PROP-2026-0018")]
    [InlineData("Smith John-  PROP-2025-0001",      "Smith John",      "PROP-2025-0001")]
    public void ParseFolderName_MatchingPattern_ExtractsClientAndCaseNumber(
        string folderName, string expectedClient, string expectedCase)
    {
        var scanner = new ZDriveScanner(DefaultConfig("Z:\\"));

        var result = scanner.ParseFolderName(folderName);

        result.Should().NotBeNull();
        result!.ClientName.Should().Be(expectedClient);
        result.CaseNumber.Should().Be(expectedCase);
    }

    [Theory]
    [InlineData("Random Folder")]
    [InlineData("")]
    [InlineData("NoSeparatorHere")]
    public void ParseFolderName_NonMatchingPattern_ReturnsNull(string folderName)
    {
        var scanner = new ZDriveScanner(DefaultConfig("Z:\\"));

        var result = scanner.ParseFolderName(folderName);

        result.Should().BeNull();
    }

    // ── ScanFolders ───────────────────────────────────────────────────────────

    [Fact]
    public void ScanFolders_WithMatchingSubfolders_ReturnsParsedEntries()
    {
        var fakeFs = new FakeFileSystem(new[]
        {
            @"Z:\Murphy Siobhan - PROP-2026-0042",
            @"Z:\Patrick Doyle - PROP-2026-0018",
            @"Z:\Emma Collins - PROP-2026-0009"
        });

        var scanner = new ZDriveScanner(DefaultConfig("Z:\\"), fakeFs);
        var results = scanner.ScanFolders();

        results.Should().HaveCount(3);
        results.Should().ContainSingle(r => r.CaseNumber == "PROP-2026-0042" && r.ClientName == "Murphy Siobhan");
        results.Should().ContainSingle(r => r.CaseNumber == "PROP-2026-0018" && r.ClientName == "Patrick Doyle");
    }

    [Fact]
    public void ScanFolders_WithMixedFolders_SkipsNonMatchingAsUncategorized()
    {
        var fakeFs = new FakeFileSystem(new[]
        {
            @"Z:\Murphy Siobhan - PROP-2026-0042",
            @"Z:\__Archive",
            @"Z:\Templates"
        });

        var scanner = new ZDriveScanner(DefaultConfig("Z:\\"), fakeFs);
        var results = scanner.ScanFolders();

        results.Should().HaveCount(3);

        var matched     = results.Where(r => r.IsMatched).ToList();
        var unmatched   = results.Where(r => !r.IsMatched).ToList();

        matched.Should().HaveCount(1);
        unmatched.Should().HaveCount(2, "non-matching folders are indexed as uncategorized, not lost");
    }

    [Fact]
    public void ScanFolders_EmptyRoot_ReturnsEmpty()
    {
        var fakeFs  = new FakeFileSystem(Array.Empty<string>());
        var scanner = new ZDriveScanner(DefaultConfig("Z:\\"), fakeFs);

        scanner.ScanFolders().Should().BeEmpty();
    }

    // ── Depth=2 (year-based grouping) ─────────────────────────────────────────

    [Fact]
    public void ScanFolders_Depth2_ScansOneExtraLevelDown()
    {
        var fakeFs = new FakeFileSystem(new[]
        {
            @"Z:\2026\Murphy Siobhan - PROP-2026-0042",
            @"Z:\2025\Patrick Doyle - PROP-2025-0018"
        }, depth: 2);

        var config  = DefaultConfig("Z:\\") with { CaseFolderDepth = 2 };
        var scanner = new ZDriveScanner(config, fakeFs);
        var results = scanner.ScanFolders();

        results.Should().HaveCount(2);
        results.All(r => r.IsMatched).Should().BeTrue();
    }

    // ── Custom regex ──────────────────────────────────────────────────────────

    [Fact]
    public void ScanFolders_CustomPattern_ParsesAlternateFormat()
    {
        // Format: "CASE-0042 - Client Name"  (case number first)
        var config = new FolderStructureConfig
        {
            RootPath          = "Z:\\",
            CaseFolderPattern = @"^(?<CaseNumber>CASE-\d+)\s*-\s*(?<ClientName>.+)$",
            CaseFolderDepth   = 1
        };

        var fakeFs  = new FakeFileSystem(new[] { @"Z:\CASE-0042 - Murphy Siobhan" });
        var scanner = new ZDriveScanner(config, fakeFs);
        var results = scanner.ScanFolders();

        results.Should().ContainSingle();
        results[0].CaseNumber.Should().Be("CASE-0042");
        results[0].ClientName.Should().Be("Murphy Siobhan");
    }
}
