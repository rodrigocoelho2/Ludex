using Ludex.WebApi.Features.Catalog;

namespace Ludex.UnitTest.Imports;

/// <summary>
/// Names from other libraries matched to the catalog: folded the same way, editions put aside, and nothing taken
/// that is only close (a sequel isn't its first game).
/// </summary>
public sealed class NameMatchTests
{
    [Theory]
    [InlineData("Marvel’s Spider-Man™ 2", "marvels spider man 2")]
    [InlineData("Ratchet & Clank", "ratchet and clank")]
    [InlineData("God of War Ragnarök", "god of war ragnarok")]
    [InlineData("  DOOM: Eternal ", "doom eternal")]
    [InlineData("Tom Clancy's Rainbow Six® Siege", "tom clancys rainbow six siege")]
    public void NamesAreFoldedTheSameWay(string name, string key) => Assert.Equal(key, NameMatch.Key(name));

    [Theory]
    [InlineData("the witcher 3 wild hunt game of the year edition", "the witcher 3 wild hunt")]
    [InlineData("control ultimate edition", "control")]
    [InlineData("death stranding director s cut", "death stranding")]
    [InlineData("hades", "hades")]
    public void EditionsArePutAside(string key, string stem) => Assert.Equal(stem, NameMatch.Base(key));

    [Fact]
    public void TheSameNameWins()
    {
        var hades = Game(1, "Hades");
        var two = Game(2, "Hades II");
        Assert.Same(two, NameMatch.Best("Hades II", [hades, two]));
    }

    [Fact]
    public void ASequelIsNotItsFirstGame() => Assert.Null(NameMatch.Best("Hades II", [Game(1, "Hades")]));

    [Fact]
    public void AnEditionIsTheGame() => Assert.Equal(7, NameMatch.Best("The Witcher 3: Wild Hunt - Game of the Year Edition", [Game(9, "The Witcher 2"), Game(7, "The Witcher 3: Wild Hunt")])?.Id);

    [Fact]
    public void ASubtitleIsTheGameWhenOnlyOneFits() =>
        Assert.Equal(3, NameMatch.Best("Final Fantasy VII Remake Intergrade", [Game(4, "Final Fantasy VII"), Game(3, "Final Fantasy VII Remake")])?.Id);

    [Fact]
    public void TiesGoToThePlatform()
    {
        var pc = Game(5, "Tetris");
        pc.Platforms = ["PC"];
        var switchGame = Game(6, "Tetris");
        switchGame.Platforms = ["Switch"];
        Assert.Same(switchGame, NameMatch.Best("Tetris", [pc, switchGame], "Switch"));
    }

    private static CatalogGame Game(int id, string name) => new() { Id = id, Name = name };
}
