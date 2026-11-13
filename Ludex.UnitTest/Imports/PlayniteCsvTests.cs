using Ludex.Library.Enums;
using Ludex.WebApi.Features.Playnite.Services;

namespace Ludex.UnitTest.Imports;

/// <summary>
/// Reading the CSV of Playnite's Library Exporter Advanced: columns named in Playnite's language, time in seconds,
/// dates written the computer's way, sources and platforms turned into Ludex's copies.
/// </summary>
public sealed class PlayniteCsvTests
{
    private const string English =
        "Name,Sources,Platforms,Time Played,Completion Status,Last Played\r\n"
        + "Hades,Epic,PC (Windows),90000,Beaten,13/11/2026 21:04:33\r\n"
        + "God of War,PlayStation,\"Sony PlayStation 4, Sony PlayStation 5\",3600,Not Played,\r\n"
        + "Celeste,GOG,,0,Plan to Play,\r\n"
        + "\"Ratchet & Clank, Rift Apart\",Steam,PC (Windows),7200,Playing,01/11/2026 10:00:00\r\n";

    [Fact]
    public void TheEnglishExportIsRead()
    {
        var file = PlayniteCsv.Read(English);

        Assert.Null(file.Problem);
        Assert.Empty(file.Missing);
        Assert.Equal(4, file.Games.Count);

        var hades = file.Games[0];
        Assert.Equal(("Hades", "PC", (string?)"Epic Games", (GameFormat?)GameFormat.Digital, 1500, (GameStatus?)GameStatus.Finished), (hades.Name, hades.Platform, hades.Store, hades.Format, hades.Minutes, hades.Status));
        Assert.Equal(new DateTimeOffset(2026, 11, 13, 21, 4, 33, TimeSpan.Zero), hades.LastPlayed);

        var gow = file.Games[1];
        Assert.Equal(("PS4", (string?)null, (GameFormat?)GameFormat.Digital, 60, (GameStatus?)null), (gow.Platform, gow.Store, gow.Format, gow.Minutes, gow.Status));

        var celeste = file.Games[2];
        Assert.Equal(("PC", (string?)"GOG", (GameStatus?)GameStatus.Backlog), (celeste.Platform, celeste.Store, celeste.Status));

        Assert.Equal(("Ratchet & Clank, Rift Apart", (string?)"Steam"), (file.Games[3].Name, file.Games[3].Store));
    }

    [Fact]
    public void ThePortugueseExportIsRead()
    {
        var file = PlayniteCsv.Read("﻿Nome,Fontes,Plataformas,Tempo de jogo,Estado de conclusão,Última sessão\nAlan Wake 2,Epic,PC (Windows),36000,Completed,05/11/2026 22:10:00\n");

        Assert.Null(file.Problem);
        var game = Assert.Single(file.Games);
        Assert.Equal(("Alan Wake 2", 600, (GameStatus?)GameStatus.Finished), (game.Name, game.Minutes, game.Status));
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 22, 10, 0, TimeSpan.Zero), game.LastPlayed);
    }

    [Fact]
    public void MissingColumnsAreNamed()
    {
        var file = PlayniteCsv.Read("Name,Developers,Publishers,Installed,Platforms,Sources\nHades,Supergiant,Supergiant,True,PC (Windows),Epic\n");

        Assert.Equal(new[] { "Time Played", "Completion Status", "Last Played" }, file.Missing);
        Assert.Equal(0, Assert.Single(file.Games).Minutes);
    }

    [Fact]
    public void SomethingElseIsRefused() => Assert.NotNull(PlayniteCsv.Read("{\"games\":[]}").Problem);

    [Theory]
    [InlineData(new[] { "13/11/2026 21:04:33" }, true)]
    [InlineData(new[] { "11/13/2026 9:04:33 PM" }, false)]
    [InlineData(new[] { "01/02/2026 10:00:00", "02/25/2026 10:00:00" }, false)]
    [InlineData(new[] { "01/02/2026 10:00:00" }, true)]
    public void DaysOrMonthsFirstAreReadFromTheRows(string[] dates, bool dayFirst) => Assert.Equal(dayFirst, PlayniteCsv.DayFirst(dates));

    [Theory]
    [InlineData("11/13/2026 9:04:33 PM", false, 21)]
    [InlineData("11/13/2026 12:30:00 AM", false, 0)]
    [InlineData("2026-11-13 21:04:33", true, 21)]
    [InlineData("13.11.2026 21:04:33", true, 21)]
    public void DatesAreRead(string text, bool dayFirst, int hour)
    {
        var date = PlayniteCsv.Date(text, dayFirst);
        Assert.Equal((2026, 11, 13, hour), (date!.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour));
    }

    [Theory]
    [InlineData("Steam", "Steam")]
    [InlineData("Epic", "Epic Games")]
    [InlineData("EA app", "EA app")]
    [InlineData("Origin", "EA app")]
    [InlineData("Ubisoft Connect", "Ubisoft Connect")]
    [InlineData("Battle.net", "Battle.net")]
    [InlineData("Xbox", "Microsoft Store")]
    [InlineData("itch.io", "itch.io")]
    [InlineData("Amazon Games", "Other store")]
    [InlineData("PlayStation", null)]
    [InlineData("Playnite", null)]
    [InlineData(null, null)]
    public void SourcesBecomeStores(string? source, string? store) => Assert.Equal(store, PlayniteCsv.Store(source));

    [Theory]
    [InlineData("PC (Windows)", null, "PC")]
    [InlineData("Sony PlayStation 5", null, "PS5")]
    [InlineData("Nintendo Switch", null, "Switch")]
    [InlineData("Microsoft Xbox Series", null, "Xbox Series")]
    [InlineData("Sony PlayStation 4, PC (Windows)", "Steam", "PC")]
    [InlineData("", "GOG", "PC")]
    public void PlatformsGetLudexNames(string platforms, string? source, string platform) => Assert.Equal(platform, PlayniteCsv.Platform(platforms, source));
}
