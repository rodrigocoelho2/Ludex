using Ludex.Library.Enums;
using Ludex.WebApi.Features;
using Ludex.WebApi.Features.Psn.Services;
using Microsoft.Extensions.Options;

namespace Ludex.UnitTest.Imports;

/// <summary>
/// PlayStation's answers as Ludex reads them: the NPSSO pasted, time played, one game for its PS4 and PS5 versions,
/// trophies matched by name, and the refresh token kept encrypted.
/// </summary>
public sealed class PsnRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly string Npsso = new string('a', 32) + new string('7', 32);

    [Fact]
    public void TheNpssoIsFoundInWhatWasPasted()
    {
        Assert.Equal(Npsso, PsnRules.Npsso(Npsso));
        Assert.Equal(Npsso, PsnRules.Npsso($"{{\"npsso\":\"{Npsso}\"}}"));
        Assert.Null(PsnRules.Npsso("not it"));
        Assert.Null(PsnRules.Npsso(Npsso + "x"));
    }

    [Theory]
    [InlineData("PT228H56M33S", (228 * 60) + 56)]
    [InlineData("PT45M", 45)]
    [InlineData("P1DT2H", 26 * 60)]
    [InlineData("PT0S", 0)]
    [InlineData("nonsense", 0)]
    [InlineData(null, 0)]
    public void TimePlayedIsRead(string? duration, int minutes) => Assert.Equal(minutes, PsnRules.Minutes(duration));

    [Fact]
    public void VersionsAreOneGameWithTheirTrophies()
    {
        var played = new List<PsnPlayedTitle>
        {
            new() { TitleId = "PPSA01", Name = "God of War Ragnarök", Category = "ps5_native_game", Concept = new PsnConcept { Id = 1, Name = "God of War Ragnarök" }, PlayDuration = "PT10H", LastPlayedDateTime = Now.AddDays(-3) },
            new() { TitleId = "CUSA01", Name = "God of War Ragnarök", Category = "ps4_game", Concept = new PsnConcept { Id = 1 }, PlayDuration = "PT2H30M", LastPlayedDateTime = Now.AddDays(-90) },
        };
        var trophies = new List<PsnTrophyTitle>
        {
            new() { NpCommunicationId = "NPWR1", TrophyTitleName = "God of War Ragnarök", TrophyTitlePlatform = "PS5", DefinedTrophies = new PsnTrophyCounts { Bronze = 30, Silver = 2, Gold = 3, Platinum = 1 }, EarnedTrophies = new PsnTrophyCounts { Bronze = 30, Silver = 2, Gold = 3, Platinum = 1 } },
            new() { NpCommunicationId = "NPWR2", TrophyTitleName = "Uncharted 2: Among Thieves™", TrophyTitlePlatform = "PS3", DefinedTrophies = new PsnTrophyCounts { Bronze = 40 }, EarnedTrophies = new PsnTrophyCounts { Bronze = 12 }, LastUpdatedDateTime = Now.AddYears(-10) },
        };

        var games = PsnRules.Games(played, trophies);

        Assert.Equal(2, games.Count);
        var gow = games[0];
        Assert.Equal(("c:1", "PS5", 750, (int?)36, (int?)36, true), (gow.Key, gow.Platform, gow.Minutes, gow.TrophiesEarned, gow.TrophiesTotal, gow.Platinum));
        Assert.Equal(GameStatus.Finished, PsnRules.Status(gow, Now));

        var uncharted = games[1];
        Assert.Equal(("np:NPWR2", "Uncharted 2: Among Thieves", "PS3", 0, (int?)12), (uncharted.Key, uncharted.Name, uncharted.Platform, uncharted.Minutes, uncharted.TrophiesEarned));
        Assert.Equal(GameStatus.Played, PsnRules.Status(uncharted, Now));
    }

    [Fact]
    public void TheTokenIsKeptEncrypted()
    {
        var protector = new PsnTokenProtector(Options.Create(new LudexOptions { ApiKey = "one key" }));
        var saved = protector.Protect("refresh-token");

        Assert.StartsWith("v1:", saved, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-token", saved, StringComparison.Ordinal);
        Assert.Equal("refresh-token", protector.Unprotect(saved));
        Assert.Null(new PsnTokenProtector(Options.Create(new LudexOptions { ApiKey = "another key" })).Unprotect(saved));
        Assert.Null(protector.Unprotect("v1:not base64!"));
    }
}
