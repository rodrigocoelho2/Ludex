/*
    LibraryList
    Every entry in an account's library (a game owned on two platforms is two entries) with its game data, and how
    many of the account's friends have the game too. Most recently changed first.
*/
CREATE PROCEDURE [dbo].[LibraryList]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT e.[EntryID], e.[Status], e.[Rating], e.[Hours], e.[Platform], e.[Store], e.[Format], e.[Notes], e.[AddedAt], e.[UpdatedAt], e.[FinishedAt],
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
    WHERE e.[UserID] = @UserID
    ORDER BY e.[UpdatedAt] DESC, g.[Name], e.[EntryID];
END
