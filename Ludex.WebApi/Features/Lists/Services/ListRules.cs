using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Lists.Models.DTOs;
using Ludex.WebApi.Features.Lists.Models.Entities;

namespace Ludex.WebApi.Features.Lists.Services;

/// <summary>
/// Lists: limits, and how rows become what the Website shows.
/// </summary>
internal static class ListRules
{
    /// <summary>Longest name.</summary>
    public const int MaxNameLength = 80;

    /// <summary>Longest description.</summary>
    public const int MaxDescriptionLength = 1000;

    /// <summary>Longest note about a game.</summary>
    public const int MaxNoteLength = 300;

    /// <summary>Most games in a list.</summary>
    public const int MaxGames = 500;

    /// <summary>How many covers a list card stacks.</summary>
    public const int CoverCount = 5;

    /// <summary>
    /// Lists as the Website shows them, each with its first covers.
    /// </summary>
    /// <param name="rows">The lists.</param>
    /// <param name="covers">Their first games.</param>
    /// <returns>The lists, in the same order.</returns>
    public static List<ListSummaryDTO> Summaries(IEnumerable<ListRow> rows, IEnumerable<ListCoverRow> covers)
    {
        var byList = covers.GroupBy(static c => c.ListID).ToDictionary(static g => g.Key, static g => g.OrderBy(static c => c.Position).ToList());
        return [.. rows.Select(row => Summary(row, byList.GetValueOrDefault(row.ListID) ?? []))];
    }

    /// <summary>
    /// One list as the Website shows it.
    /// </summary>
    /// <param name="row">The list.</param>
    /// <param name="covers">Its first games.</param>
    /// <returns>The list.</returns>
    public static ListSummaryDTO Summary(ListRow row, IEnumerable<ListCoverRow> covers) => new()
    {
        Id = row.ListID,
        Name = row.Name,
        Description = row.Description,
        Ranked = row.Ranked,
        Visibility = Visibility(row.Visibility),
        OwnerId = row.OwnerUniqueID.ToString("N"),
        OwnerName = row.OwnerName,
        OwnerAvatarVersion = row.OwnerAvatarVersion,
        OwnerIsDemo = row.OwnerIsDemo,
        GameCount = row.GameCount,
        Likes = row.Likes,
        LikedByMe = row.LikedByMe,
        IsMine = row.IsMine,
        PlayedByMe = row.PlayedByMe,
        UpdatedAt = GameMapper.Utc(row.UpdatedAt),
        Covers = [.. covers.Select(static c => GameMapper.Summary(c))],
    };

    /// <summary>
    /// A game of a list as the Website shows it.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The game.</returns>
    public static ListItemDTO Item(ListItemRow row) => new()
    {
        Position = row.Position,
        Note = row.Note,
        Game = GameMapper.Summary(row, GameMapper.Status(row.MyStatus)),
        MyRating = row.MyRating,
    };

    /// <summary>
    /// A stored visibility, or "everyone" for anything unknown.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <returns>The visibility.</returns>
    public static ListVisibility Visibility(byte value) => Enum.IsDefined((ListVisibility)value) ? (ListVisibility)value : ListVisibility.Public;

    /// <summary>
    /// Whether a status counts as played for a list's progress (finished, played or dropped).
    /// </summary>
    /// <param name="status">The status.</param>
    /// <returns>True when played.</returns>
    public static bool IsPlayed(GameStatus? status) => status is GameStatus.Finished or GameStatus.Played or GameStatus.Dropped;
}
