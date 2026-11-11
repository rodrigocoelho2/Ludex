using Ludex.Library.Enums;
using Ludex.WebApi.Features.Steam.Services;

namespace Ludex.UnitTest.Steam;

/// <summary>
/// What a typed Steam profile is, hours from minutes, and the status an imported game is suggested with.
/// </summary>
public sealed class SteamRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("76561198000000001", "76561198000000001", null)]
    [InlineData("https://steamcommunity.com/profiles/76561198000000001/", "76561198000000001", null)]
    [InlineData("steamcommunity.com/profiles/76561198000000001", "76561198000000001", null)]
    [InlineData("https://steamcommunity.com/id/rodas_pt/", null, "rodas_pt")]
    [InlineData("  rodas-pt ", null, "rodas-pt")]
    [InlineData("https://steamcommunity.com/profiles/123/", null, null)]
    [InlineData("not a profile!", null, null)]
    [InlineData("", null, null)]
    public void AProfileIsAnIdAnAddressOrAName(string typed, string? steamId, string? vanity) =>
        Assert.Equal((steamId, vanity), SteamRules.ParseProfile(typed));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, 0.5)]
    [InlineData(95, 1.6)]
    [InlineData(8520, 142)]
    public void HoursComeFromMinutesToOneDecimal(int minutes, double hours) => Assert.Equal((decimal)hours, SteamRules.Hours(minutes));

    [Fact]
    public void NeverPlayedIsBacklogAndLeftOut() => Assert.Equal((GameStatus.Backlog, false), SteamRules.Suggest(0, null, Now));

    [Fact]
    public void PlayedInTheLast30DaysIsPlaying() => Assert.Equal((GameStatus.Playing, true), SteamRules.Suggest(45, Now.AddDays(-12), Now));

    [Fact]
    public void TwoHoursOrMoreNotLatelyIsFinished() => Assert.Equal((GameStatus.Finished, true), SteamRules.Suggest(120, Now.AddDays(-200), Now));

    [Fact]
    public void LessThanTwoHoursNotLatelyIsDropped() => Assert.Equal((GameStatus.Dropped, true), SteamRules.Suggest(119, null, Now));

    [Fact]
    public void NeverPlayedHasNoLastPlayed()
    {
        Assert.Null(SteamRules.LastPlayed(0));
        Assert.Null(SteamRules.LastPlayed(null));
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000), SteamRules.LastPlayed(1_700_000_000));
    }
}
