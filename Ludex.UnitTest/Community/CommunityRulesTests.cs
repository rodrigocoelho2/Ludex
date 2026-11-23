using Ludex.Library.Enums;
using Ludex.WebApi.Features.Community.Models.DTOs;
using Ludex.WebApi.Features.Community.Models.Entities;
using Ludex.WebApi.Features.Community.Services;
using Ludex.WebApi.Features.Demo.Services;
using Ludex.WebApi.Features.Journal.Models.DTOs;
using Ludex.WebApi.Features.Journal.Services;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Lists.Services;
using Ludex.WebApi.Features.Profile.Services;
using Ludex.WebApi.Features.Reviews.Services;

namespace Ludex.UnitTest.Community;

/// <summary>
/// The Backloggd-style parts: a game's spread of ratings, sleeper hits, review text, how it ended, the journal's
/// totals and streak, a profile's four favourites and numbers, and the demo data hanging together.
/// </summary>
public sealed class CommunityRulesTests
{
    [Fact]
    public void FillCountsRatingsStatusesAndTheAverage()
    {
        var block = new GameCommunityDTO();
        CommunityRules.Fill(
        [
            new GameRatingRow { Kind = 1, Value = 10, Count = 2 },
            new GameRatingRow { Kind = 1, Value = 7, Count = 1 },
            new GameRatingRow { Kind = 2, Value = (byte)GameStatus.Finished, Count = 2 },
            new GameRatingRow { Kind = 2, Value = (byte)GameStatus.Played, Count = 1 },
            new GameRatingRow { Kind = 2, Value = (byte)GameStatus.Wishlist, Count = 4 },
        ], block);

        Assert.Equal(10, block.Histogram.Count);
        Assert.Equal(2, block.Histogram[9]);
        Assert.Equal(1, block.Histogram[6]);
        Assert.Equal(3, block.RatingCount);
        Assert.Equal(9.0m, block.AverageRating);
        Assert.Equal(3, block.Played);
        Assert.Equal(4, block.Wishlist);
    }

    [Fact]
    public void NoRatingsMeansNoAverage()
    {
        var block = new GameCommunityDTO();
        CommunityRules.Fill([], block);
        Assert.Null(block.AverageRating);
        Assert.Equal(0, block.RatingCount);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(8, 2)]
    [InlineData(9, 3)]
    [InlineData(40, 10)]
    public void SleeperHitsAreOwnedByAQuarterAtMost(int accounts, int maxOwners) => Assert.Equal(maxOwners, CommunityRules.SleeperMaxOwners(accounts));

    [Fact]
    public void ReviewTextIsTidied()
    {
        Assert.Null(ReviewRules.Clean("   \r\n  "));
        Assert.Equal("One.\n\nTwo.", ReviewRules.Clean("  One.  \r\n\r\n\r\n\r\nTwo.\n\n"));
    }

    [Theory]
    [InlineData(GameStatus.Finished, GameEnding.Mastered, GameEnding.Mastered)]
    [InlineData(GameStatus.Dropped, GameEnding.Abandoned, GameEnding.Abandoned)]
    [InlineData(GameStatus.Playing, GameEnding.Completed, null)]
    [InlineData(GameStatus.Wishlist, GameEnding.Shelved, null)]
    public void OnlyGamesThatEndedHaveAnEnding(GameStatus status, GameEnding chosen, GameEnding? kept) => Assert.Equal(kept, ReviewRules.EndingFor(status, chosen));

    [Fact]
    public void TheStreakCountsDaysInARowUpToToday()
    {
        var today = new DateOnly(2026, 11, 23);
        Assert.Equal(3, JournalRules.Streak([today, today.AddDays(-1), today.AddDays(-2), today.AddDays(-4)], today));
        Assert.Equal(2, JournalRules.Streak([today.AddDays(-1), today.AddDays(-2)], today));   // today not played yet
        Assert.Equal(0, JournalRules.Streak([today.AddDays(-3)], today));
    }

    [Fact]
    public void TheJournalAddsUpEachGame()
    {
        var a = new Ludex.WebApi.Features.Games.Models.DTOs.GameSummaryDTO { Id = 1, Name = "A" };
        var b = new Ludex.WebApi.Features.Games.Models.DTOs.GameSummaryDTO { Id = 2, Name = "B" };
        var day = new DateOnly(2026, 11, 1);
        var games = JournalRules.Games(
        [
            new JournalEntryDTO { Game = a, Day = day, Minutes = 30 },
            new JournalEntryDTO { Game = b, Day = day, Minutes = 90 },
            new JournalEntryDTO { Game = a, Day = day.AddDays(1), Minutes = 45 },
        ]);

        Assert.Equal([2, 1], games.Select(static g => g.Game.Id));
        Assert.Equal(75, games[1].Minutes);
        Assert.Equal(2, games[1].Days);
    }

    [Fact]
    public void FavouritesAreGamesOfTheLibraryEachOnceAtMostFour()
    {
        Assert.Equal("3,1,4,5", ProfileStats.FavoritesValue([3, 1, 3, 9, 4, 5, 6], new HashSet<int> { 1, 3, 4, 5, 6 }));
        Assert.Null(ProfileStats.FavoritesValue([9], new HashSet<int> { 1 }));
        Assert.Equal([3, 1], ProfileStats.FavoriteIds("3, 1,x,,1"));
    }

    [Fact]
    public void RatingCountsUseEachGameOnce()
    {
        LibraryRow[] rows =
        [
            new() { GameID = 1, EntryID = 1, Rating = 9, Status = (byte)GameStatus.Finished },
            new() { GameID = 1, EntryID = 2, Rating = 4, Status = (byte)GameStatus.Finished },
            new() { GameID = 2, EntryID = 3, Rating = 9, Status = (byte)GameStatus.Played },
            new() { GameID = 3, EntryID = 4, Status = (byte)GameStatus.Backlog },
        ];

        var counts = ProfileStats.RatingCounts(rows);
        Assert.Equal(2, counts[8]);
        Assert.Equal(0, counts[3]);
    }

    [Fact]
    public void FinishedPerYearUsesTheDayGivenFirst()
    {
        LibraryRow[] rows =
        [
            new() { GameID = 1, Status = (byte)GameStatus.Finished, FinishedAt = new DateTime(2026, 1, 2), FinishedOn = new DateTime(2024, 5, 1) },
            new() { GameID = 2, Status = (byte)GameStatus.Finished, FinishedAt = new DateTime(2026, 3, 2) },
            new() { GameID = 3, Status = (byte)GameStatus.Playing, FinishedAt = new DateTime(2026, 3, 2) },
        ];

        var years = ProfileStats.FinishedByYear(rows);
        Assert.Equal([2026, 2024], years.Select(static y => y.Year));
        Assert.All(years, static y => Assert.Equal(1, y.Count));
    }

    [Fact]
    public void ListVisibilityFallsBackToPublic()
    {
        Assert.Equal(ListVisibility.Private, ListRules.Visibility(3));
        Assert.Equal(ListVisibility.Public, ListRules.Visibility(9));
    }

    [Fact]
    public void TheDemoDataHangsTogether()
    {
        var users = DemoContent.Users.Select(static u => u.Name).ToHashSet();
        var entries = DemoContent.Entries.Select(static e => (e.User, e.Game)).ToHashSet();

        Assert.Equal(6, users.Count);
        Assert.All(DemoContent.Entries, e => Assert.True(users.Contains(e.User)));
        Assert.Equal(DemoContent.Entries.Count, entries.Count);   // a game once per account
        Assert.All(DemoContent.Entries, static e => Assert.True(e.Rating is null or (>= 1 and <= 10)));

        // Every review is of a game its author logged, with a rating; nobody likes their own.
        Assert.All(DemoContent.Reviews, r => Assert.True(entries.Contains((r.User, r.Game))));
        Assert.All(DemoContent.Reviews, r => Assert.DoesNotContain(r.User, r.Likes));
        Assert.All(DemoContent.Reviews, r => Assert.All(r.Likes, l => Assert.True(users.Contains(l))));
        Assert.Equal(DemoContent.Reviews.Count, DemoContent.Reviews.Select(static r => (r.User, r.Game)).Distinct().Count());

        // Every comment is under a review that exists; lists and sessions belong to the accounts.
        var reviews = DemoContent.Reviews.Select(static r => (r.User, r.Game)).ToHashSet();
        Assert.All(DemoContent.Comments, c => Assert.True(reviews.Contains((c.Author, c.Game))));
        Assert.All(DemoContent.Lists, l => Assert.DoesNotContain(l.User, l.Likes));
        Assert.All(DemoContent.Lists, static l => Assert.Equal(l.Games.Length, l.Games.Distinct().Count()));
        Assert.All(DemoContent.Sessions, s => Assert.True(entries.Contains((s.User, s.Game))));

        // Some of it happened this week, so Trending and Popular reviews have something to show.
        Assert.True(DemoContent.Reviews.Count(static r => r.DaysAgo <= 7) >= 4);
        Assert.Equal(16, DemoContent.Lists.Single(static l => l.Name == "Every Final Fantasy, ranked").Games.Length);
    }
}
