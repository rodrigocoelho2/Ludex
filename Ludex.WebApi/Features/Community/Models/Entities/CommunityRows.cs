using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Community.Models.Entities;

/// <summary>A game with its numbers on this Ludex (dbo.GameTrending, dbo.GameSleepers).</summary>
public sealed class CommunityGameRow : GameRow
{
    /// <summary>Gets or sets how many accounts did something with it lately.</summary>
    public int Active { get; set; }

    /// <summary>Gets or sets how many accounts have it.</summary>
    public int Owners { get; set; }

    /// <summary>Gets or sets how many accounts rated it.</summary>
    public int Ratings { get; set; }

    /// <summary>Gets or sets its average rating.</summary>
    public decimal? AverageRating { get; set; }
}

/// <summary>A count of a game's ratings or statuses (dbo.GameRatings: Kind 1 rating, 2 status).</summary>
public sealed class GameRatingRow
{
    /// <summary>Gets or sets what is counted (1 a rating, 2 a status).</summary>
    public byte Kind { get; set; }

    /// <summary>Gets or sets the rating or the status.</summary>
    public byte Value { get; set; }

    /// <summary>Gets or sets how many accounts.</summary>
    public int Count { get; set; }
}

/// <summary>A game and a number (dbo.GameWishlistCounts).</summary>
public sealed class GameCountRow
{
    /// <summary>Gets or sets the game.</summary>
    public int GameID { get; set; }

    /// <summary>Gets or sets the number.</summary>
    public int Count { get; set; }
}

/// <summary>How many accounts there are (dbo.CommunityCounts).</summary>
public sealed class CommunityCountsRow
{
    /// <summary>Gets or sets the real accounts.</summary>
    public int? Real { get; set; }

    /// <summary>Gets or sets every account, demo ones too.</summary>
    public int? All { get; set; }
}
