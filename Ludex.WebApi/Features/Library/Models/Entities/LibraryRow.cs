using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Library.Models.Entities;

/// <summary>
/// One game in an account's library (dbo.LibraryEntry joined with dbo.Game).
/// </summary>
public sealed class LibraryRow : GameRow
{
    /// <summary>Gets or sets the status (<see cref="Ludex.Library.Enums.GameStatus"/>).</summary>
    public byte Status { get; set; }

    /// <summary>Gets or sets the rating (1–10).</summary>
    public byte? Rating { get; set; }

    /// <summary>Gets or sets the hours played.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets where it was played.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store the copy came from.</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets the kind of copy (<see cref="Ludex.Library.Enums.GameFormat"/>).</summary>
    public byte? Format { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets when it was added (UTC).</summary>
    public DateTime AddedAt { get; set; }

    /// <summary>Gets or sets when it was last changed (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets when it was first finished (UTC).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Gets or sets how many friends have it too.</summary>
    public int FriendCount { get; set; }

    /// <summary>Gets or sets its Steam app (Steam import and sync).</summary>
    public int? SteamAppID { get; set; }

    /// <summary>Gets or sets the minutes played on Steam in the last two weeks.</summary>
    public int? RecentMinutes { get; set; }

    /// <summary>Gets or sets the Steam achievements unlocked.</summary>
    public short? AchievementsUnlocked { get; set; }

    /// <summary>Gets or sets the game's Steam achievements (0: it has none).</summary>
    public short? AchievementsTotal { get; set; }

    /// <summary>Gets or sets the price under which to alert (wishlist).</summary>
    public decimal? PriceTarget { get; set; }

    /// <summary>Gets or sets the best current price (dbo.GamePrice).</summary>
    public decimal? PriceAmount { get; set; }

    /// <summary>Gets or sets the regular price.</summary>
    public decimal? PriceRegular { get; set; }

    /// <summary>Gets or sets the % off.</summary>
    public byte? PriceCut { get; set; }

    /// <summary>Gets or sets the currency.</summary>
    public string? PriceCurrency { get; set; }

    /// <summary>Gets or sets the shop.</summary>
    public string? PriceShop { get; set; }

    /// <summary>Gets or sets the shop's page.</summary>
    public string? PriceUrl { get; set; }

    /// <summary>Gets or sets the lowest price ever.</summary>
    public decimal? PriceLow { get; set; }

    /// <summary>Gets or sets when the price was checked (UTC; null: never).</summary>
    public DateTime? PriceCheckedAt { get; set; }
}
