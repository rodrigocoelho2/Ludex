
namespace Ludex.Sdk.Models;

/// <summary>A game on a community shelf (trending, coming soon, sleeper hits), with its numbers on this Ludex.</summary>
public sealed class CommunityGameDTO
{
    /// <summary>Gets or sets the game.</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets how many accounts did something with it lately (trending).</summary>
    public int Active { get; set; }

    /// <summary>Gets or sets how many accounts have it.</summary>
    public int Owners { get; set; }

    /// <summary>Gets or sets how many accounts rated it.</summary>
    public int Ratings { get; set; }

    /// <summary>Gets or sets its average rating here (1–10).</summary>
    public decimal? AverageRating { get; set; }

    /// <summary>Gets or sets how many accounts have it on their wishlist (coming soon).</summary>
    public int Waiting { get; set; }

    /// <summary>Gets or sets when it comes out (coming soon).</summary>
    public DateOnly? ReleaseDate { get; set; }
}

/// <summary>The community part of home: trending, popular reviews, lists, coming soon and sleeper hits.</summary>
public sealed class CommunityHomeDTO
{
    /// <summary>Gets or sets the games most played lately.</summary>
    public List<CommunityGameDTO> Trending { get; set; } = [];

    /// <summary>Gets or sets how many days "trending" looked back (7, or 30 when the week was quiet).</summary>
    public int TrendingDays { get; set; } = 7;

    /// <summary>Gets or sets the most liked reviews this week.</summary>
    public List<ReviewDTO> PopularReviews { get; set; } = [];

    /// <summary>Gets or sets the friends' latest reviews.</summary>
    public List<ReviewDTO> FriendReviews { get; set; } = [];

    /// <summary>Gets or sets the most liked reviews of all time.</summary>
    public List<ReviewDTO> AllTimeReviews { get; set; } = [];

    /// <summary>Gets or sets the most liked lists.</summary>
    public List<ListSummaryDTO> PopularLists { get; set; } = [];

    /// <summary>Gets or sets the games coming out that people wait for.</summary>
    public List<CommunityGameDTO> ComingSoon { get; set; } = [];

    /// <summary>Gets or sets the well rated games few people have.</summary>
    public List<CommunityGameDTO> Sleepers { get; set; } = [];
}

/// <summary>What everyone on this Ludex thinks of one game (the game page).</summary>
public sealed class GameCommunityDTO
{
    /// <summary>Gets or sets how many accounts gave each rating, from 1 (half a star) to 10 (five stars).</summary>
    public List<int> Histogram { get; set; } = [];

    /// <summary>Gets or sets how many accounts rated it.</summary>
    public int RatingCount { get; set; }

    /// <summary>Gets or sets its average rating (1–10).</summary>
    public decimal? AverageRating { get; set; }

    /// <summary>Gets or sets how many accounts finished it or played it.</summary>
    public int Played { get; set; }

    /// <summary>Gets or sets how many are playing it.</summary>
    public int Playing { get; set; }

    /// <summary>Gets or sets how many have it in their backlog.</summary>
    public int Backlog { get; set; }

    /// <summary>Gets or sets how many want it.</summary>
    public int Wishlist { get; set; }

    /// <summary>Gets or sets how many dropped it.</summary>
    public int Dropped { get; set; }

    /// <summary>Gets or sets the reader's own review.</summary>
    public ReviewDTO? MyReview { get; set; }

    /// <summary>Gets or sets the reviews (not the reader's), in the order asked.</summary>
    public ReviewListDTO Reviews { get; set; } = new();

    /// <summary>Gets or sets lists the game is in.</summary>
    public List<ListSummaryDTO> Lists { get; set; } = [];

    /// <summary>Gets or sets the reader's lists, and whether the game is in each.</summary>
    public List<MyListDTO> MyLists { get; set; } = [];

    /// <summary>Gets or sets the reader's last sessions of the game.</summary>
    public List<JournalEntryDTO> MySessions { get; set; } = [];

    /// <summary>Gets or sets the minutes in the reader's journal for the game.</summary>
    public int MyMinutes { get; set; }
}
