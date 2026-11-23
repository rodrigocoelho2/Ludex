namespace Ludex.Library.Enums;

/// <summary>
/// The role of an account.
/// </summary>
public enum UserRole
{
    /// <summary>A regular account: its own library and friends.</summary>
    User,

    /// <summary>The first account: can also delete other accounts.</summary>
    Admin,
}

/// <summary>
/// Where a game stands in someone's library. The numbers are stored in the database: never renumber them.
/// </summary>
public enum GameStatus
{
    /// <summary>Being played now.</summary>
    Playing = 1,

    /// <summary>Played to the end (the credits rolled).</summary>
    Finished = 2,

    /// <summary>Not played yet, but wanted.</summary>
    Wishlist = 3,

    /// <summary>Started and left.</summary>
    Dropped = 4,

    /// <summary>Owned, not started yet (games bought on Steam come in like this).</summary>
    Backlog = 5,

    /// <summary>Played, but there's no end to reach (CS2, Apex, Minecraft) or it isn't one you finish: not on it now.</summary>
    Played = 6,
}

/// <summary>
/// What happened to a game, for the friends' activity feed. Stored in the database: never renumber them.
/// </summary>
public enum ActivityKind
{
    /// <summary>Added to the library.</summary>
    Added = 1,

    /// <summary>Started playing (status changed to Playing).</summary>
    Started = 2,

    /// <summary>Finished.</summary>
    Finished = 3,

    /// <summary>Rated (or rated again).</summary>
    Rated = 4,

    /// <summary>Put on the wishlist.</summary>
    Wishlisted = 5,

    /// <summary>Dropped.</summary>
    Dropped = 6,

    /// <summary>Marked as played (a game without an end).</summary>
    Played = 7,

    /// <summary>Wrote a review.</summary>
    Reviewed = 8,
}

/// <summary>
/// How a game ended for someone (Log a game). Stored in the database: never renumber them.
/// </summary>
public enum GameEnding
{
    /// <summary>The main story, to the credits.</summary>
    Completed = 1,

    /// <summary>Everything: 100%, every achievement or trophy.</summary>
    Mastered = 2,

    /// <summary>Put aside for now, to come back to.</summary>
    Shelved = 3,

    /// <summary>Left for good.</summary>
    Abandoned = 4,
}

/// <summary>
/// Who can see a list. Stored in the database: never renumber them.
/// </summary>
public enum ListVisibility
{
    /// <summary>Everyone on this Ludex.</summary>
    Public = 1,

    /// <summary>Friends only.</summary>
    Friends = 2,

    /// <summary>Only the owner.</summary>
    Private = 3,
}

/// <summary>
/// How a list of reviews is ordered. Sent to dbo.ReviewList: never renumber them.
/// </summary>
public enum ReviewSort
{
    /// <summary>Most liked lately (this week), then of all time.</summary>
    Popular = 1,

    /// <summary>Newest first.</summary>
    Recent = 2,

    /// <summary>The reader's friends only, newest first.</summary>
    Friends = 3,

    /// <summary>Highest rated first.</summary>
    Highest = 4,

    /// <summary>Most liked of all time.</summary>
    AllTime = 5,
}

/// <summary>
/// Where a line of the journal comes from.
/// </summary>
public enum SessionSource
{
    /// <summary>Written down by the person.</summary>
    Manual = 1,

    /// <summary>Seen by the Steam or PlayStation sync (the hours went up that day).</summary>
    Sync = 2,
}

/// <summary>
/// How another account relates to the signed-in one.
/// </summary>
public enum FriendState
{
    /// <summary>No link.</summary>
    None = 0,

    /// <summary>Friends: each sees the other's library.</summary>
    Friends = 1,

    /// <summary>A request was sent and waits for the other account.</summary>
    RequestSent = 2,

    /// <summary>The other account sent a request that waits for an answer.</summary>
    RequestReceived = 3,
}

/// <summary>
/// Where the game catalog comes from.
/// </summary>
public enum CatalogSource
{
    /// <summary>IGDB (Twitch credentials set): every game, with covers, artwork and screenshots.</summary>
    Igdb,

    /// <summary>The small list built into Ludex, without images (no IGDB credentials yet).</summary>
    Offline,
}

/// <summary>
/// The kind of copy owned. Stored in the database: never renumber them.
/// </summary>
public enum GameFormat
{
    /// <summary>A disc or a cartridge.</summary>
    Physical = 1,

    /// <summary>A download (a console store, or a PC store such as Steam).</summary>
    Digital = 2,
}

/// <summary>
/// What a notification is about. Stored in the database: never renumber them.
/// </summary>
public enum NotificationKind
{
    /// <summary>A wishlisted game's price reached its target.</summary>
    PriceAlert = 1,

    /// <summary>The daily Steam sync added games bought since.</summary>
    SteamNewGames = 2,

    /// <summary>The daily PlayStation sync added games played since.</summary>
    NewGames = 3,

    /// <summary>A connected account (PlayStation) needs to be connected again.</summary>
    ConnectionProblem = 4,
}
