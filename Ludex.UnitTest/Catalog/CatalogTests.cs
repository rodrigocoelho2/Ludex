using System.Text.Json;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Services;

namespace Ludex.UnitTest.Catalog;

/// <summary>
/// The built-in list, the IGDB mapping, platform names and the game colours.
/// </summary>
public sealed class CatalogTests
{
    [Fact]
    public void TheBuiltInListHasUniqueIdsAboveIgdbs()
    {
        var ids = OfflineCatalog.All.Select(static g => g.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, static id => Assert.True(id > OfflineCatalog.FirstId));
        Assert.All(OfflineCatalog.All, static g => Assert.False(string.IsNullOrWhiteSpace(g.Summary)));
    }

    [Fact]
    public async Task SearchingTheBuiltInListMatchesEveryWordIgnoringCaseAndAccents()
    {
        var catalog = new OfflineCatalog();

        var zelda = await catalog.SearchAsync("zelda", 10);
        var pokemon = await catalog.SearchAsync("POKEMON z-a", 10);
        var none = await catalog.SearchAsync("zzzz", 10);

        Assert.Equal(4, zelda.Count);
        Assert.All(zelda, static g => Assert.Contains("Zelda", g.Name, StringComparison.Ordinal));
        Assert.Equal("Pokémon Legends: Z-A", Assert.Single(pokemon).Name);
        Assert.Empty(none);
    }

    [Fact]
    public async Task ANameThatStartsWithTheTextComesFirst()
    {
        var results = await new OfflineCatalog().SearchAsync("hades", 10);

        Assert.Equal(new[] { "Hades", "Hades II" }, results.Select(static g => g.Name));
    }

    [Fact]
    public void FoldDropsAccentsAndPunctuation()
    {
        Assert.Equal("pokemon legends z a", OfflineCatalog.Fold("Pokémon Legends: Z-A"));
        Assert.Equal("baldur s gate 3", OfflineCatalog.Fold("Baldur's Gate 3"));
    }

    [Fact]
    public void AnIgdbGameMapsToTheCatalogs()
    {
        const string json = """
            [{
              "id": 119133,
              "name": "Elden Ring",
              "slug": "elden-ring",
              "summary": "Rise, Tarnished.",
              "first_release_date": 1645747200,
              "cover": { "id": 1, "image_id": "co4jni" },
              "artworks": [ { "id": 2, "image_id": "ar1abc" } ],
              "screenshots": [ { "id": 3, "image_id": "sc1" }, { "id": 4, "image_id": "sc2" } ],
              "platforms": [ { "id": 6, "name": "PC (Microsoft Windows)", "abbreviation": "PC" }, { "id": 167, "name": "PlayStation 5", "abbreviation": "PS5" }, { "id": 169, "name": "Xbox Series X|S", "abbreviation": "Series X|S" } ],
              "genres": [ { "id": 12, "name": "Role-playing (RPG)" } ],
              "game_modes": [ { "id": 1, "name": "Single player" } ],
              "involved_companies": [
                { "id": 9, "company": { "id": 1, "name": "Bandai Namco" }, "developer": false, "publisher": true },
                { "id": 8, "company": { "id": 2, "name": "FromSoftware" }, "developer": true, "publisher": false }
              ]
            }]
            """;
        var igdb = JsonSerializer.Deserialize<List<IgdbGame>>(json)!;

        var game = IgdbCatalog.Map(igdb[0]);

        Assert.Equal(119133, game.Id);
        Assert.Equal("Elden Ring", game.Name);
        Assert.Equal("co4jni", game.CoverImageId);
        Assert.Equal("ar1abc", game.HeroImageId);
        Assert.Equal(new[] { "sc1", "sc2" }, game.Screenshots);
        Assert.Equal(new DateOnly(2022, 2, 25), game.ReleaseDate);
        Assert.Equal("FromSoftware", game.Developer);
        Assert.Equal("Bandai Namco", game.Publisher);
        Assert.Equal(new[] { "PC", "PS5", "Xbox Series" }, game.Platforms);
        Assert.Equal(new[] { "Role-playing (RPG)" }, game.Genres);
        Assert.Equal(new[] { "Single player" }, game.Modes);
    }

    [Fact]
    public void WithoutArtworkTheHeroIsTheFirstScreenshot()
    {
        var game = IgdbCatalog.Map(new IgdbGame { Id = 1, Name = "X", Screenshots = [new IgdbImage { ImageId = "s1" }] });

        Assert.Equal("s1", game.HeroImageId);
        Assert.Null(game.CoverImageId);
        Assert.Null(game.ReleaseDate);
    }

    [Fact]
    public void QueriesAreEscaped()
    {
        Assert.Equal("say \\\"hi\\\" \\\\o/", IgdbCatalog.Escape("  say \"hi\" \\o/ "));
        Assert.Equal(100, IgdbCatalog.Escape(new string('a', 300)).Length);
    }

    [Theory]
    [InlineData("PC (Microsoft Windows)", "PC", "PC")]
    [InlineData("PlayStation 5", "PS5", "PS5")]
    [InlineData("Nintendo Switch", "Switch", "Switch")]
    [InlineData("Xbox Series X|S", "Series X|S", "Xbox Series")]
    [InlineData("Some Console; Deluxe", null, "Some Console, Deluxe")]
    [InlineData(null, "ABC", "ABC")]
    public void PlatformsGetShortNames(string? name, string? abbreviation, string expected)
    {
        Assert.Equal(expected, PlatformNames.Friendly(name, abbreviation));
    }

    [Theory]
    [InlineData("PS5", "PlayStation")]
    [InlineData("PS Vita", "PlayStation")]
    [InlineData("Switch 2", "Nintendo")]
    [InlineData("3DS", "Nintendo")]
    [InlineData("Xbox Series", "Xbox")]
    [InlineData("PC", "PC")]
    [InlineData("Steam Deck", "PC")]
    [InlineData("iOS", "Mobile")]
    [InlineData("Dreamcast", "Other")]
    [InlineData("", "Other")]
    [InlineData(null, "Other")]
    public void PlatformsFallIntoFamilies(string? platform, string expected)
    {
        Assert.Equal(expected, PlatformNames.Family(platform));
    }

    [Fact]
    public void ListsThatDontFitTheirColumnLeaveOutTheLastItems()
    {
        Assert.Equal("ab;cd", GameRow.Join(new[] { "ab", " cd ", "", "ef" }, ';', 6));
        Assert.Null(GameRow.Join(Array.Empty<string>(), ';', 10));
        Assert.Equal(new[] { "ab", "cd" }, GameRow.Split("ab; cd ;"));
    }

    [Fact]
    public void EveryGameAlwaysGetsTheSameColour()
    {
        Assert.Equal(GameTint.For(119133), GameTint.For(119133));
        Assert.Matches("^#[0-9A-F]{6}$", GameTint.For(OfflineCatalog.FirstId + 1));
        Assert.True(Enumerable.Range(OfflineCatalog.FirstId + 1, 12).Select(GameTint.For).Distinct().Count() > 4);
    }
}
