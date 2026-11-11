/*
    FriendPopular
    The games most of an account's friends have that the account doesn't, with their average rating among those friends.
*/
CREATE PROCEDURE [dbo].[FriendPopular]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Take         INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    WITH [Owned] AS
    (
        SELECT e.[GameID],
               COUNT(*) AS [FriendCount],
               AVG(CAST(e.[Rating] AS DECIMAL (5, 2))) AS [AverageRating],
               MAX(e.[UpdatedAt]) AS [LastAt]
        FROM [dbo].[Friendship] f
        INNER JOIN [dbo].[LibraryEntry] e ON e.[UserID] = f.[FriendUserID]
        WHERE f.[UserID] = @UserID AND f.[AcceptedAt] IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [dbo].[LibraryEntry] mine WHERE mine.[UserID] = @UserID AND mine.[GameID] = e.[GameID])
        GROUP BY e.[GameID]
    )
    SELECT TOP (@Take)
           g.[GameID], g.[Name], g.[Slug], g.[Summary], g.[CoverImageID], g.[HeroImageID], g.[Screenshots],
           g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt],
           o.[FriendCount], o.[AverageRating]
    FROM [Owned] o
    INNER JOIN [dbo].[Game] g ON g.[GameID] = o.[GameID]
    ORDER BY o.[FriendCount] DESC, o.[AverageRating] DESC, o.[LastAt] DESC;
END
