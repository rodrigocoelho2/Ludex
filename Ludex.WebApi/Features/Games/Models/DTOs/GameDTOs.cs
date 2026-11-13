using Ludex.Library.Enums;
using Ludex.WebApi.Features.Library.Models.DTOs;

namespace Ludex.WebApi.Features.Games.Models.DTOs;

/// <summary>A game on a card: cover, name, year, and where the signed-in account and its friends are with it.</summary>
public sealed class GameSummaryDTO
{
    /// <summary>Gets or sets the game id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the year of the first release.</summary>
    public int? Year { get; set; }

    /// <summary>Gets or sets the IGDB image id of the cover (null: draw the card in <see cref="Tint"/> with the name).</summary>
    public string? CoverImageId { get; set; }

    /// <summary>Gets or sets the developer.</summary>
    public string? Developer { get; set; }

    /// <summary>Gets or sets the platforms (short names).</summary>
    public List<string> Platforms { get; set; } = [];

    /// <summary>Gets or sets the game's colour (behind the cover while it loads).</summary>
    public string Tint { get; set; } = "#2B3550";

    /// <summary>Gets or sets the status in the signed-in account's library (null: not there).</summary>
    public GameStatus? MyStatus { get; set; }

    /// <summary>Gets or sets the friends who have it (user names).</summary>
    public List<string> FriendNames { get; set; } = [];

    /// <summary>Gets or sets the critics' rating (0–100).</summary>
    public int? CriticScore { get; set; }

    /// <summary>Gets or sets the hours to finish the main story.</summary>
    public decimal? TimeToBeatMain { get; set; }

    /// <summary>Gets or sets the hours to finish everything.</summary>
    public decimal? TimeToBeatFull { get; set; }
}

/// <summary>A friend's place in a game.</summary>
public sealed class FriendGameDTO
{
    /// <summary>Gets or sets the friend's account id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the friend's user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the friend's status.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the friend's rating (1–10).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the friend's hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets where the friend played it.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets when the friend last changed it.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the version of the account's photo (null: none).</summary>
    public int? AvatarVersion { get; set; }
}

/// <summary>Everything the game page shows.</summary>
public sealed class GameDetailDTO
{
    /// <summary>Gets or sets the game on a card.</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the summary.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets the IGDB image id of the wide art behind the title.</summary>
    public string? HeroImageId { get; set; }

    /// <summary>Gets or sets the IGDB image ids of the screenshots.</summary>
    public List<string> Screenshots { get; set; } = [];

    /// <summary>Gets or sets the first release.</summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>Gets or sets the publisher.</summary>
    public string? Publisher { get; set; }

    /// <summary>Gets or sets the genres.</summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>Gets or sets the modes.</summary>
    public List<string> Modes { get; set; } = [];

    /// <summary>Gets or sets the signed-in account's entry (null: not in the library).</summary>
    public MyGameDTO? Mine { get; set; }

    /// <summary>Gets or sets the friends who have it.</summary>
    public List<FriendGameDTO> Friends { get; set; } = [];

    /// <summary>Gets or sets the friends' average rating.</summary>
    public decimal? FriendsAverageRating { get; set; }

    /// <summary>Gets or sets the catalog the data came from.</summary>
    public CatalogSource Source { get; set; }

    /// <summary>Gets or sets the account's collections (to put the game in).</summary>
    public List<CollectionDTO> Collections { get; set; } = [];

    /// <summary>Gets or sets the best PC price (wishlist; null: not checked or not tracked).</summary>
    public PriceDTO? Price { get; set; }
}

/// <summary>The signed-in account's own entry for a game (one copy, when the game is owned on several platforms).</summary>
public sealed class MyGameDTO
{
    /// <summary>Gets or sets the entry.</summary>
    public long EntryId { get; set; }

    /// <summary>Gets or sets the account's entries of this game (every copy, this one included), oldest first.</summary>
    public List<MyCopyDTO> Copies { get; set; } = [];

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the rating (1–10).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the hours played.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets where it was played.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store the copy came from (Steam, Epic Games…).</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets the kind of copy: physical or digital.</summary>
    public GameFormat? Format { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets when it was added.</summary>
    public DateTimeOffset AddedAt { get; set; }

    /// <summary>Gets or sets when it was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets when it was first finished.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Gets or sets its Steam app (null: not from Steam).</summary>
    public int? SteamAppId { get; set; }

    /// <summary>Gets or sets the hours played on Steam in the last two weeks.</summary>
    public decimal? RecentHours { get; set; }

    /// <summary>Gets or sets the Steam achievements unlocked.</summary>
    public int? AchievementsUnlocked { get; set; }

    /// <summary>Gets or sets the game's Steam achievements.</summary>
    public int? AchievementsTotal { get; set; }

    /// <summary>Gets or sets the collections it is in.</summary>
    public List<int> CollectionIds { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether it is the favourite game on the account's profile.</summary>
    public bool IsFavorite { get; set; }
}

/// <summary>One of the account's copies of a game.</summary>
public sealed class MyCopyDTO
{
    /// <summary>Gets or sets the entry.</summary>
    public long EntryId { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store.</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets the achievements or trophies earned.</summary>
    public int? AchievementsUnlocked { get; set; }

    /// <summary>Gets or sets the achievements or trophies the game has.</summary>
    public int? AchievementsTotal { get; set; }
}

/// <summary>The answer to a search.</summary>
public sealed class SearchResultDTO
{
    /// <summary>Gets or sets the text searched for.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Gets or sets the games found, best match first.</summary>
    public List<GameSummaryDTO> Results { get; set; } = [];

    /// <summary>Gets or sets the catalog searched.</summary>
    public CatalogSource Source { get; set; }
}

/// <summary>The covers behind the sign-in form (no account data: the page is public).</summary>
public sealed class CoverWallDTO
{
    /// <summary>Gets or sets the games.</summary>
    public List<GameSummaryDTO> Games { get; set; } = [];

    /// <summary>Gets or sets the catalog in use.</summary>
    public CatalogSource Source { get; set; }
}
