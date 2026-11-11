using Ludex.Library.Themes;

namespace Ludex.UnitTest.Themes;

/// <summary>
/// The colour themes: only known keys are saved, anything else falls back to the default.
/// </summary>
public sealed class AccentsTests
{
    [Theory]
    [InlineData("cyan", true)]
    [InlineData("violet", true)]
    [InlineData("Violet", false)]
    [InlineData("pink-ish", false)]
    [InlineData(null, false)]
    public void IsKnown_OnlyTheListedKeys(string? key, bool expected) => Assert.Equal(expected, Accents.IsKnown(key));

    [Theory]
    [InlineData("emerald", "emerald")]
    [InlineData(null, "cyan")]
    [InlineData("", "cyan")]
    [InlineData("<script>", "cyan")]
    public void Normalize_FallsBackToTheDefault(string? key, string expected) => Assert.Equal(expected, Accents.Normalize(key));

    [Fact]
    public void All_HasTheDefaultFirst_AndUniqueKeys()
    {
        Assert.Equal(Accents.Default, Accents.All[0].Key);
        Assert.Equal(Accents.All.Count, Accents.All.Select(static a => a.Key).Distinct().Count());
        Assert.All(Accents.All, static a => Assert.Matches("^#[0-9A-F]{6}$", a.Color));
    }
}
