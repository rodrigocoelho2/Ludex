/*
    ListItems
    The games of a list the reader may see, in order, with where the reader is with each (status and rating of their
    first copy).
*/
CREATE PROCEDURE [dbo].[ListItems]
    @ViewerUniqueID UNIQUEIDENTIFIER,
    @ListID         INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @ViewerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @ViewerUniqueID);

    SELECT i.[Position], i.[Note], i.[AddedAt], mine.[Status] AS [MyStatus], mine.[Rating] AS [MyRating],
           g.[GameID], g.[Name], g.[Slug], g.[CoverImageID], g.[HeroImageID], g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt], g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion]
    FROM [dbo].[GameListItem] i
    INNER JOIN [dbo].[Game] g ON g.[GameID] = i.[GameID]
    OUTER APPLY (SELECT TOP (1) me.[Status], me.[Rating]
                 FROM [dbo].[LibraryEntry] me
                 WHERE me.[UserID] = @ViewerID AND me.[GameID] = i.[GameID]
                 ORDER BY me.[EntryID]) mine
    WHERE i.[ListID] = @ListID
      AND EXISTS (SELECT 1 FROM [dbo].[GameList] l WHERE l.[ListID] = @ListID AND (l.[UserID] = @ViewerID OR l.[Visibility] = 1 OR (l.[Visibility] = 2 AND EXISTS (SELECT 1 FROM [dbo].[Friendship] f WHERE f.[UserID] = @ViewerID AND f.[FriendUserID] = l.[UserID] AND f.[AcceptedAt] IS NOT NULL))))
    ORDER BY i.[Position];
END
