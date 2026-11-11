using Ludex.Library.Enums;
using Ludex.WebApi.Features.Collections.Repositories;
using Ludex.WebApi.Features.Collections.Services;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Prices.Services;
using Ludex.WebApi.Features.Steam.Services;

namespace Ludex.UnitTest.Steam;

/// <summary>
/// Achievements as the game page shows them, the best price, collections, and the Backlog status.
/// </summary>
public sealed class SteamSyncTests
{
    [Fact]
    public void AchievementsJoinTheGameTheAccountAndEveryone()
    {
        SteamSchemaAchievement[] schema =
        [
            new() { Name = "A", DisplayName = "First steps", Description = "Start", Icon = "a.jpg", IconGray = "a-grey.jpg" },
            new() { Name = "B", DisplayName = "Secret", Description = "Spoiler", Hidden = 1, Icon = "b.jpg", IconGray = "b-grey.jpg" },
            new() { Name = "C", DisplayName = "Common", Description = "Easy", Icon = "c.jpg", IconGray = "c-grey.jpg" },
        ];
        SteamPlayerAchievement[] mine = [new() { ApiName = "A", Achieved = 1, UnlockTime = 1_700_000_000 }, new() { ApiName = "B", Achieved = 0 }];
        var global = new Dictionary<string, double> { ["A"] = 80.04, ["B"] = 2.5, ["C"] = 50 };

        var page = SteamRules.Achievements(620, schema, mine, global);

        Assert.Equal((1, 3, false), (page.Unlocked, page.Total, page.Private));
        Assert.Equal("First steps", page.Items[0].Name);
        Assert.Equal("a.jpg", page.Items[0].IconUrl);
        Assert.Equal(80.0, page.Items[0].GlobalPercent);
        Assert.Equal(new[] { "Common", "Secret" }, page.Items.Skip(1).Select(static i => i.Name));
        Assert.Null(page.Items[2].Description);   // hidden and locked: no spoiler
        Assert.Equal("b-grey.jpg", page.Items[2].IconUrl);
    }

    [Fact]
    public void WithoutTheAccountsAchievementsTheyAreAllLockedAndPrivate()
    {
        var page = SteamRules.Achievements(620, [new() { Name = "A", DisplayName = "One" }], null, new Dictionary<string, double>());

        Assert.True(page.Private);
        Assert.Equal(0, page.Unlocked);
    }

    [Fact]
    public void TheBestPriceIsTheCheapestStore()
    {
        var prices = new ItadGamePrices
        {
            Id = "x",
            HistoryLow = new ItadHistoryLow { All = new ItadPrice { Amount = 4.99m, Currency = "EUR" } },
            Deals =
            [
                new ItadDeal { Shop = new ItadShop { Name = "Steam" }, Price = new ItadPrice { Amount = 19.99m, Currency = "EUR" }, Regular = new ItadPrice { Amount = 39.99m }, Cut = 50 },
                new ItadDeal { Shop = new ItadShop { Name = "GOG" }, Price = new ItadPrice { Amount = 17.49m, Currency = "EUR" }, Regular = new ItadPrice { Amount = 34.99m }, Cut = 50, Url = "https://gog" },
            ],
        };

        var best = PriceRules.Best(7, "x", prices);

        Assert.Equal((17.49m, "GOG", (byte?)50, 4.99m, "https://gog"), (best.Amount!.Value, best.Shop!, best.Cut, best.Low!.Value, best.Url!));
        Assert.Null(PriceRules.Best(7, "x", null).Amount);
    }

    [Fact]
    public void CollectionsGroupTheirGames()
    {
        CollectionRow[] rows =
        [
            new() { CollectionID = 2, Name = "Souls-like", GameID = 10 },
            new() { CollectionID = 2, Name = "Souls-like", GameID = 11 },
            new() { CollectionID = 1, Name = "Favourites", GameID = 10 },
            new() { CollectionID = 3, Name = "Empty", GameID = null },
        ];

        var collections = CollectionService.ToDtos(rows);

        Assert.Equal(new[] { "Empty", "Favourites", "Souls-like" }, collections.Select(static c => c.Name));
        Assert.Empty(collections[0].GameIds);
        Assert.Equal(new[] { 1, 2 }, CollectionService.ByGame(collections)[10].Order());
    }

    [Fact]
    public void MovingAGameToTheBacklogTellsFriendsNothing() =>
        Assert.Empty(LibraryRules.Activities(GameStatus.Playing, null, GameStatus.Backlog, null));

    [Fact]
    public void ANewBacklogGameIsAnAddedLine() =>
        Assert.Equal(new (ActivityKind, int?)[] { (ActivityKind.Added, null) }, LibraryRules.Activities(null, null, GameStatus.Backlog, null));
}
