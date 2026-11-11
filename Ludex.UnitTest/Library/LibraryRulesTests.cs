using Ludex.Library.Enums;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Services;

namespace Ludex.UnitTest.Library;

/// <summary>
/// What goes into the friends' feed when a library changes, and the numbers on the home tiles.
/// </summary>
public sealed class LibraryRulesTests
{
    [Theory]
    [InlineData(GameStatus.Wishlist, ActivityKind.Wishlisted)]
    [InlineData(GameStatus.Playing, ActivityKind.Started)]
    [InlineData(GameStatus.Finished, ActivityKind.Added)]
    [InlineData(GameStatus.Dropped, ActivityKind.Added)]
    public void ANewGameAddsOneLineByItsStatus(GameStatus status, ActivityKind expected)
    {
        var lines = LibraryRules.Activities(null, null, status, null);

        Assert.Equal(new (ActivityKind, int?)[] { (expected, null) }, lines);
    }

    [Fact]
    public void ANewGameWithARatingAlsoAddsTheRating()
    {
        var lines = LibraryRules.Activities(null, null, GameStatus.Finished, 9);

        Assert.Equal(new (ActivityKind, int?)[] { (ActivityKind.Added, null), (ActivityKind.Rated, 9) }, lines);
    }

    [Theory]
    [InlineData(GameStatus.Wishlist, GameStatus.Playing, ActivityKind.Started)]
    [InlineData(GameStatus.Playing, GameStatus.Finished, ActivityKind.Finished)]
    [InlineData(GameStatus.Playing, GameStatus.Dropped, ActivityKind.Dropped)]
    [InlineData(GameStatus.Dropped, GameStatus.Wishlist, ActivityKind.Wishlisted)]
    public void ANewStatusAddsItsLine(GameStatus before, GameStatus after, ActivityKind expected)
    {
        var lines = LibraryRules.Activities(before, 7, after, 7);

        Assert.Equal(new (ActivityKind, int?)[] { (expected, null) }, lines);
    }

    [Fact]
    public void SavingTheSameStatusAndRatingAddsNothing()
    {
        Assert.Empty(LibraryRules.Activities(GameStatus.Playing, 8, GameStatus.Playing, 8));
    }

    [Fact]
    public void TakingTheRatingAwayAddsNothing()
    {
        Assert.Empty(LibraryRules.Activities(GameStatus.Finished, 8, GameStatus.Finished, null));
    }

    [Fact]
    public void ANewRatingAddsARatedLine()
    {
        Assert.Equal(new (ActivityKind, int?)[] { (ActivityKind.Rated, 10) }, LibraryRules.Activities(GameStatus.Finished, 8, GameStatus.Finished, 10));
    }

    [Fact]
    public void TheHomeNumbersCountThisYearsFinishesAndAverageTheRatings()
    {
        var rows = new List<LibraryRow>
        {
            Row(GameStatus.Finished, 10, 120.5m, new DateTime(2026, 3, 1)),
            Row(GameStatus.Finished, 8, 40m, new DateTime(2025, 12, 31)),
            Row(GameStatus.Playing, 9, 12m, null),
            Row(GameStatus.Wishlist, null, null, null),
            Row(GameStatus.Dropped, 4, 3.5m, null),
        };

        var stats = LibraryRules.Stats(rows, 2026);

        Assert.Equal(5, stats.GameCount);
        Assert.Equal(1, stats.FinishedThisYear);
        Assert.Equal(2026, stats.Year);
        Assert.Equal(7.8m, stats.AverageRating);
        Assert.Equal(1, stats.WishlistCount);
        Assert.Equal(176m, stats.TotalHours);
    }

    [Fact]
    public void AnEmptyLibraryHasNoAverage()
    {
        var stats = LibraryRules.Stats(new List<LibraryRow>(), 2026);

        Assert.Equal(0, stats.GameCount);
        Assert.Null(stats.AverageRating);
        Assert.Equal(0m, stats.TotalHours);
    }

    [Fact]
    public void TheCountsSplitByStatus()
    {
        var counts = LibraryRules.Counts(new List<LibraryRow> { Row(GameStatus.Playing), Row(GameStatus.Playing), Row(GameStatus.Finished), Row(GameStatus.Wishlist) });

        Assert.Equal(4, counts.All);
        Assert.Equal(2, counts.Playing);
        Assert.Equal(1, counts.Finished);
        Assert.Equal(1, counts.Wishlist);
        Assert.Equal(0, counts.Dropped);
    }

    [Fact]
    public void AnEntryFallsBackToTheGamesFirstPlatformForTheFilter()
    {
        var row = Row(GameStatus.Playing);
        row.Platforms = "Switch;PC";

        Assert.Equal("Nintendo", LibraryRules.Entry(row).PlatformFamily);

        row.Platform = "PS5";
        Assert.Equal("PlayStation", LibraryRules.Entry(row).PlatformFamily);
    }

    private static LibraryRow Row(GameStatus status, int? rating = null, decimal? hours = null, DateTime? finishedAt = null) => new()
    {
        GameID = 1,
        Name = "A game",
        Status = (byte)status,
        Rating = (byte?)rating,
        Hours = hours,
        FinishedAt = finishedAt,
        AddedAt = new DateTime(2026, 1, 1),
        UpdatedAt = new DateTime(2026, 1, 2),
        FetchedAt = new DateTime(2026, 1, 1),
    };
}
