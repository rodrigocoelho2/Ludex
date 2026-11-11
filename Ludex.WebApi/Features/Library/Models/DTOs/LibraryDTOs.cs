using Ludex.Library.Enums;
using Ludex.WebApi.Features.Friends.Models.DTOs;
using Ludex.WebApi.Features.Games.Models.DTOs;

namespace Ludex.WebApi.Features.Library.Models.DTOs;

/// <summary>One game of the library.</summary>
public sealed class LibraryEntryDTO
{
    /// <summary>Gets or sets the game.</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the rating (1–10).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the hours played.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets where it was played.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the family of that platform (PC, PlayStation, Nintendo, Xbox, Mobile, Other), for the filter.</summary>
    public string PlatformFamily { get; set; } = "Other";

    /// <summary>Gets or sets when it was added.</summary>
    public DateTimeOffset AddedAt { get; set; }

    /// <summary>Gets or sets when it was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets when it was first finished.</summary>
    public DateTimeOffset? FinishedAt { get; set; }
}

/// <summary>How many games there are with each status.</summary>
public sealed class LibraryCountsDTO
{
    /// <summary>Gets or sets every game.</summary>
    public int All { get; set; }

    /// <summary>Gets or sets the games being played.</summary>
    public int Playing { get; set; }

    /// <summary>Gets or sets the games finished.</summary>
    public int Finished { get; set; }

    /// <summary>Gets or sets the games on the wishlist.</summary>
    public int Wishlist { get; set; }

    /// <summary>Gets or sets the games dropped.</summary>
    public int Dropped { get; set; }
}

/// <summary>The library.</summary>
public sealed class LibraryDTO
{
    /// <summary>Gets or sets every game, most recently changed first.</summary>
    public List<LibraryEntryDTO> Entries { get; set; } = [];

    /// <summary>Gets or sets the counts per status.</summary>
    public LibraryCountsDTO Counts { get; set; } = new();
}

/// <summary>The numbers on the home tiles.</summary>
public sealed class LibraryStatsDTO
{
    /// <summary>Gets or sets the games in the library (the wishlist included).</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets the games finished this year.</summary>
    public int FinishedThisYear { get; set; }

    /// <summary>Gets or sets the year <see cref="FinishedThisYear"/> counts.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the average rating (null: nothing rated).</summary>
    public decimal? AverageRating { get; set; }

    /// <summary>Gets or sets the games on the wishlist.</summary>
    public int WishlistCount { get; set; }

    /// <summary>Gets or sets the hours played in all.</summary>
    public decimal TotalHours { get; set; }
}

/// <summary>The home page.</summary>
public sealed class HomeDTO
{
    /// <summary>Gets or sets the game in the hero: the one played most recently (null: empty library).</summary>
    public LibraryEntryDTO? Hero { get; set; }

    /// <summary>Gets or sets the IGDB image id of the hero's wide art.</summary>
    public string? HeroImageId { get; set; }

    /// <summary>Gets or sets the other games being played.</summary>
    public List<LibraryEntryDTO> AlsoPlaying { get; set; } = [];

    /// <summary>Gets or sets the numbers on the tiles.</summary>
    public LibraryStatsDTO Stats { get; set; } = new();

    /// <summary>Gets or sets the games added last.</summary>
    public List<LibraryEntryDTO> Recent { get; set; } = [];

    /// <summary>Gets or sets what the friends did lately.</summary>
    public List<ActivityDTO> Feed { get; set; } = [];

    /// <summary>Gets or sets the games the friends have and the account doesn't.</summary>
    public List<PopularGameDTO> Popular { get; set; } = [];

    /// <summary>Gets or sets how many friends the account has.</summary>
    public int FriendCount { get; set; }

    /// <summary>Gets or sets the catalog in use.</summary>
    public CatalogSource Source { get; set; }
}

/// <summary>Adds a game to the library or changes it (the game page's form).</summary>
public sealed class LibrarySaveRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the rating (1–10; null: not rated).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the hours played.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets where it was played.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>Sets only the status of a game, adding it when it isn't in the library (the search page's buttons).</summary>
public sealed class LibraryStatusRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; }
}
