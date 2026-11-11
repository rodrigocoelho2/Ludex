using Ludex.Library.Enums;
using Ludex.Library.Platforms;

namespace Ludex.UnitTest.Platforms;

/// <summary>
/// Which copies a platform can have: stores on a PC, physical or digital on a console, only what the platform had.
/// </summary>
public sealed class GameCopiesTests
{
    [Theory]
    [InlineData("PC", true)]
    [InlineData("Steam Deck", true)]
    [InlineData("PS5", false)]
    [InlineData(null, false)]
    public void OnlyPcsHaveStores(string? platform, bool stores) => Assert.Equal(stores, GameCopies.UsesStores(platform));

    [Theory]
    [InlineData("PS5", "Physical,Digital")]
    [InlineData("Switch", "Physical,Digital")]
    [InlineData("Xbox 360", "Physical,Digital")]
    [InlineData("SNES", "Physical")]
    [InlineData("PS2", "Physical")]
    [InlineData("Xbox", "Physical")]
    [InlineData("iOS", "Digital")]
    [InlineData("PC", "Digital")]
    [InlineData(null, "Physical,Digital")]
    public void EachPlatformHasTheFormatsItHad(string? platform, string formats) =>
        Assert.Equal(formats, string.Join(",", GameCopies.Formats(platform)));

    [Fact]
    public void APcStoreMakesTheCopyDigitalAndIsSpelledLikeLudex()
    {
        Assert.Equal(("Steam", (GameFormat?)GameFormat.Digital, (string?)null), GameCopies.Normalize("PC", "steam", null));
        Assert.Equal(("Epic Games", (GameFormat?)GameFormat.Digital, (string?)null), GameCopies.Normalize("PC", "Epic Games", GameFormat.Digital));
    }

    [Fact]
    public void AStoreOnAConsoleOrAnUnknownStoreIsAProblem()
    {
        Assert.Equal("store", GameCopies.Normalize("PS5", "Steam", null).Problem);
        Assert.Equal("store", GameCopies.Normalize("PC", "My Shop", null).Problem);
    }

    [Fact]
    public void AFormatThePlatformNeverHadIsAProblem()
    {
        Assert.Equal("format", GameCopies.Normalize("NES", null, GameFormat.Digital).Problem);
        Assert.Equal("format", GameCopies.Normalize("iOS", null, GameFormat.Physical).Problem);
        Assert.Equal("format", GameCopies.Normalize("PC", null, GameFormat.Physical).Problem);
    }

    [Fact]
    public void APlatformWithOneFormatGetsItWithoutAsking()
    {
        Assert.Equal(GameFormat.Physical, GameCopies.Normalize("GameCube", null, null).Format);
        Assert.Equal(GameFormat.Digital, GameCopies.Normalize("Android", null, null).Format);
        Assert.Null(GameCopies.Normalize("PS5", null, null).Format);
        Assert.Null(GameCopies.Normalize("PC", null, null).Format);
    }

    [Fact]
    public void ThePageOffersStoresOnAPcAndFormatsElsewhere()
    {
        Assert.Contains("GOG", GameCopies.Choices("PC"));
        Assert.Equal(new[] { "Physical", "Digital" }, GameCopies.Choices("PS4"));
        Assert.Equal(new[] { "Physical" }, GameCopies.Choices("N64"));
    }
}
