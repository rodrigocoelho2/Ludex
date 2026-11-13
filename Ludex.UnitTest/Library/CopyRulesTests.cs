using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Services;

namespace Ludex.UnitTest.Library;

/// <summary>
/// A game owned on two platforms is two entries: an import goes into the copy of the same platform family, or the
/// one whose platform was never said, or a new one.
/// </summary>
public sealed class CopyRulesTests
{
    private static readonly LibraryCopyRow Steam = new() { EntryID = 1, Platform = "PC", SteamAppID = 1245620 };
    private static readonly LibraryCopyRow Ps5 = new() { EntryID = 2, Platform = "PS5" };
    private static readonly LibraryCopyRow Blank = new() { EntryID = 3 };

    [Theory]
    [InlineData("PC", "PC")]
    [InlineData("Steam Deck", "PC")]
    [InlineData("Mac", "PC")]
    [InlineData("PS4", "PlayStation")]
    [InlineData("PS5", "PlayStation")]
    [InlineData("Switch", "Nintendo")]
    [InlineData("Xbox Series", "Xbox")]
    [InlineData(null, "")]
    public void PlatformsFallIntoFamilies(string? platform, string family) => Assert.Equal(family, CopyRules.Family(platform));

    [Fact]
    public void PlayStationGoesIntoThePlayStationCopy() => Assert.Same(Ps5, CopyRules.Pick(new[] { Steam, Ps5 }, "PS4"));

    [Fact]
    public void SteamGoesIntoTheSteamCopy() => Assert.Same(Steam, CopyRules.Pick(new[] { Ps5, Steam }, null, 1245620));

    [Fact]
    public void ACopyOnAnotherFamilyIsANewEntry() => Assert.Null(CopyRules.Pick(new[] { Steam }, "PS5"));

    [Fact]
    public void ACopyWithNoPlatformIsTaken() => Assert.Same(Blank, CopyRules.Pick(new[] { Steam, Blank }, "PS5"));

    [Fact]
    public void NoCopiesIsANewEntry() => Assert.Null(CopyRules.Pick(Array.Empty<LibraryCopyRow>(), "PC"));
}
