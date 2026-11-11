namespace Ludex.WebApi.Features.Games.Services;

/// <summary>
/// A dark colour per game, always the same for the same game: the card behind the cover while it loads, and the whole
/// card when there is no cover. All pass 4.5:1 against the white titles drawn on them.
/// </summary>
internal static class GameTint
{
    private static readonly string[] Palette =
    [
        "#4A3A1E", "#5A1E2E", "#2E2445", "#1F3B3A", "#24405C", "#5C4A10", "#5A2A18", "#22332A",
        "#3D2357", "#2F4A22", "#3A1F4A", "#2B3550", "#5A3A14", "#1E2B3E", "#5C1A1A", "#1F3550",
        "#3B2E28", "#2A3540", "#4A2020", "#3A2A55",
    ];

    /// <summary>
    /// The colour of a game.
    /// </summary>
    /// <param name="gameId">The game id.</param>
    /// <returns>A hex colour.</returns>
    public static string For(int gameId)
    {
        // A cheap integer hash, so neighbouring ids (the built-in list) don't step through the palette in order.
        var hash = unchecked((uint)gameId * 2654435761u);
        return Palette[(int)(hash % (uint)Palette.Length)];
    }
}
