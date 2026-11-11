/*
    LibraryGet
    One game of an account's library (no row when it isn't there).
*/
CREATE PROCEDURE [dbo].[LibraryGet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT e.[Status], e.[Rating], e.[Hours], e.[Platform], e.[Notes], e.[AddedAt], e.[UpdatedAt], e.[FinishedAt],
           g.[GameID], g.[Name], g.[Slug], g.[Summary], g.[CoverImageID], g.[HeroImageID], g.[Screenshots],
           g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt],
           (SELECT COUNT(*)
            FROM [dbo].[Friendship] f
            INNER JOIN [dbo].[LibraryEntry] fe ON fe.[UserID] = f.[FriendUserID] AND fe.[GameID] = e.[GameID]
            WHERE f.[UserID] = @UserID AND f.[AcceptedAt] IS NOT NULL) AS [FriendCount]
    FROM [dbo].[LibraryEntry] e
    INNER JOIN [dbo].[Game] g ON g.[GameID] = e.[GameID]
    WHERE e.[UserID] = @UserID AND e.[GameID] = @GameID;
END
