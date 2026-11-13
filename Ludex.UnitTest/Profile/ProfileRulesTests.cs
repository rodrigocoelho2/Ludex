using Ludex.Library.Enums;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Profile.Services;

namespace Ludex.UnitTest.Profile;

/// <summary>
/// XP and levels, badges, the favourite game and what a profile photo may be.
/// </summary>
public sealed class ProfileRulesTests
{
    [Fact]
    public void XpCountsFinishedGamesRatingsGamesAndHours()
    {
        LibraryRow[] rows =
        [
            Row(1, GameStatus.Finished, rating: 9, hours: 40.6m),   // 100 + 20 + 5 + 40
            Row(2, GameStatus.Playing, hours: 10m),                 // 5 + 10
            Row(3, GameStatus.Wishlist),                            // nothing
        ];

        Assert.Equal(180, ProfileRules.Xp(rows));
    }

    [Theory]
    [InlineData(0, 1, 0, 100)]
    [InlineData(99, 1, 0, 100)]
    [InlineData(100, 2, 100, 400)]
    [InlineData(399, 2, 100, 400)]
    [InlineData(400, 3, 400, 900)]
    [InlineData(8100, 10, 8100, 10000)]
    public void LevelLStartsAtAHundredTimesLMinusOneSquared(int xp, int level, int start, int next)
    {
        var result = ProfileRules.Level(xp);

        Assert.Equal(level, result.Level);
        Assert.Equal(start, result.LevelXp);
        Assert.Equal(next, result.NextLevelXp);
        Assert.Equal(xp, result.Xp);
    }

    [Fact]
    public void AnEmptyLibraryHasEveryBadgeButNoneEarned()
    {
        var badges = ProfileRules.Badges([], 0);

        Assert.Equal(22, badges.Count);
        Assert.DoesNotContain(badges, static b => b.Earned);
        Assert.Equal(badges.Count, badges.Select(static b => b.Id).Distinct().Count());
    }

    [Fact]
    public void BadgesAreEarnedFromTheLibrary()
    {
        LibraryRow[] rows =
        [
            Row(1, GameStatus.Finished, rating: 10, hours: 120m, released: new DateTime(1998, 11, 21)),
            Row(2, GameStatus.Dropped, rating: 2, platform: "Switch"),
            Row(3, GameStatus.Playing, platform: "PS5", released: new DateTime(2026, 10, 1), added: new DateTime(2026, 10, 10)),
        ];

        var earned = ProfileRules.Badges(rows, 5).Where(static b => b.Earned).Select(static b => b.Id).ToHashSet();

        Assert.Contains("first-finish", earned);
        Assert.Contains("hundred-hours", earned);
        Assert.Contains("perfect-ten", earned);
        Assert.Contains("tough-crowd", earned);
        Assert.Contains("retro-soul", earned);
        Assert.Contains("day-one", earned);
        Assert.Contains("good-company", earned);
        Assert.Contains("not-for-me", earned);
        Assert.DoesNotContain("finisher", earned);
        Assert.DoesNotContain("platform-hopper", earned);
    }

    [Fact]
    public void BadgeProgressStopsAtTheTargetAndEarnedComeFirst()
    {
        LibraryRow[] rows = [.. Enumerable.Range(1, 12).Select(static i => Row(i, GameStatus.Finished))];

        var badges = ProfileRules.Badges(rows, 0);
        var finisher = badges.Single(static b => b.Id == "finisher");

        Assert.True(finisher.Earned);
        Assert.Equal(10, finisher.Progress);
        Assert.True(badges.TakeWhile(static b => b.Earned).Count() == badges.Count(static b => b.Earned));
    }

    [Fact]
    public void TheChosenFavouriteWinsWhileItIsInTheLibrary()
    {
        LibraryRow[] rows = [Row(1, GameStatus.Finished, rating: 10), Row(2, GameStatus.Playing, rating: 6)];

        Assert.Equal((2, true), Pick(ProfileService.Favorite(rows, 2)));
        Assert.Equal((1, false), Pick(ProfileService.Favorite(rows, 99)));
        Assert.Equal((1, false), Pick(ProfileService.Favorite(rows, null)));
        Assert.Null(ProfileService.Favorite([Row(3, GameStatus.Wishlist, rating: 9)], null).Row);
    }

    [Fact]
    public void APhotoIsAJpegPngOrWebPByItsBytes()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        byte[] webp = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'];
        byte[] gif = [(byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 0, 0, 0, 0, 0, 0];

        Assert.Equal("image/jpeg", ProfileRules.AvatarType(jpeg));
        Assert.Equal("image/png", ProfileRules.AvatarType(png));
        Assert.Equal("image/webp", ProfileRules.AvatarType(webp));
        Assert.Null(ProfileRules.AvatarType(gif));
        Assert.Null(ProfileRules.AvatarType(null));
        Assert.Null(ProfileRules.AvatarType(new byte[ProfileRules.MaxAvatarBytes + 1]));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  Souls-likes \r\n and Balatro  ", "Souls-likes \n and Balatro")]
    public void TextIsTrimmedAndEmptyBecomesNull(string? typed, string? saved) => Assert.Equal(saved, ProfileRules.Clean(typed));

    [Fact]
    public void NoShowcasesSavedShowsTheDefaults() =>
        Assert.Equal(ProfileRules.DefaultShowcases.ToArray(), ProfileRules.Showcases(null));

    [Fact]
    public void ShowcasesKeepTheirOrderDropUnknownAndRepeatsAndStopAtSix()
    {
        var result = ProfileRules.Showcases(["year", " TOP ", "nonsense", "year", "rare", "badges", "recent", "playing", "finished"]);

        Assert.Equal(new[] { "year", "top", "rare", "badges", "recent", "playing" }, result);
    }

    [Fact]
    public void AnEmptyListOfShowcasesStaysEmpty() => Assert.Empty(ProfileRules.Showcases([]));

    [Fact]
    public void TagsAreSplitTrimmedCutShortAndKeptOnceUpToFive()
    {
        var result = ProfileRules.Tags(["Soulslike, trophy hunter", "SOULSLIKE|JRPG", "  ", "A tag that is far longer than twenty-four", "Indie", "Retro", "One too many"]);

        Assert.Equal(new[] { "Soulslike", "trophy hunter", "JRPG", "A tag that is far longer", "Indie" }, result);
    }

    [Fact]
    public void HoursAreSplitByPlatformFamilyMostFirst()
    {
        LibraryRow[] rows =
        [
            Row(1, GameStatus.Playing, hours: 10m, platform: "PS5"),
            Row(2, GameStatus.Playing, hours: 5.3m, platform: "PS4"),
            Row(3, GameStatus.Finished, hours: 40m),
            Row(4, GameStatus.Finished, hours: 2m, platform: "Switch"),
            Row(5, GameStatus.Backlog, platform: "Xbox Series X|S"),
        ];

        var result = ProfileRules.PlatformHours(rows);

        Assert.Equal(new[] { "PC", "PlayStation", "Nintendo" }, result.Select(static f => f.Family));
        Assert.Equal(new[] { 40m, 15.3m, 2m }, result.Select(static f => f.Hours));
    }

    private static (int, bool) Pick((LibraryRow? Row, bool Chosen) favorite) => (favorite.Row?.GameID ?? 0, favorite.Chosen);

    private static LibraryRow Row(
        int id,
        GameStatus status,
        int? rating = null,
        decimal? hours = null,
        string? platform = null,
        DateTime? released = null,
        DateTime? added = null) => new()
        {
            GameID = id,
            Name = $"Game {id}",
            Status = (byte)status,
            Rating = rating is { } r ? (byte)r : null,
            Hours = hours,
            Platform = platform,
            FirstReleaseDate = released,
            AddedAt = added ?? new DateTime(2026, 1, 1),
            UpdatedAt = added ?? new DateTime(2026, 1, 1),
            FinishedAt = status == GameStatus.Finished ? new DateTime(2026, 3, 1) : null,
        };
}
