/*
    LibraryGet
    One entry of a game in an account's library (no row when the game isn't there): @EntryID when given and still
    there, otherwise the game's first entry.
*/
CREATE PROCEDURE [dbo].[LibraryGet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @EntryID      BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT TOP (1) e.[EntryID], e.[Status], e.[Rating], e.[Hours], e.[Platform], e.[Store], e.[Format], e.[Notes], e.[AddedAt], e.[UpdatedAt], e.[FinishedAt],
           e.[SteamAppID], e.[RecentMinutes], e.[AchievementsUnlocked], e.[AchievementsTotal], e.[PriceTarget],
           gp.[Amount] AS [PriceAmount], gp.[Regular] AS [PriceRegular], gp.[Cut] AS [PriceCut], gp.[Currency] AS [PriceCurrency],
           gp.[Shop] AS [PriceShop], gp.[Url] AS [PriceUrl], gp.[Low] AS [PriceLow], gp.[CheckedAt] AS [PriceCheckedAt],
           g.[GameID], g.[Name], g.[Slug], g.[Summary], g.[CoverImageID], g.[HeroImageID], g.[Screenshots],
           g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt],
           g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion],
           (SELECT COUNT(DISTINCT fe.[UserID])
            FROM [dbo].[Friendship] f
            INNER JOIN [dbo].[LibraryEntry] fe ON fe.[UserID] = f.[FriendUserID] AND fe.[GameID] = e.[GameID]
            WHERE f.[UserID] = @UserID AND f.[AcceptedAt] IS NOT NULL) AS [FriendCount]
    FROM [dbo].[LibraryEntry] e
    INNER JOIN [dbo].[Game] g ON g.[GameID] = e.[GameID]
    LEFT JOIN [dbo].[GamePrice] gp ON gp.[GameID] = e.[GameID]
    WHERE e.[UserID] = @UserID AND e.[GameID] = @GameID
    ORDER BY CASE WHEN e.[EntryID] = @EntryID THEN 0 ELSE 1 END, e.[EntryID];
END
