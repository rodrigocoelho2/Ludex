/*
    ListsForGame
    The account's own lists, and whether a game is in each (the game page's "Add to list").
*/
CREATE PROCEDURE [dbo].[ListsForGame]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT l.[ListID], l.[Name], l.[Ranked], l.[Visibility],
           CAST(CASE WHEN EXISTS (SELECT 1 FROM [dbo].[GameListItem] i WHERE i.[ListID] = l.[ListID] AND i.[GameID] = @GameID) THEN 1 ELSE 0 END AS BIT) AS [HasGame]
    FROM [dbo].[GameList] l
    WHERE l.[UserID] = @UserID
    ORDER BY l.[UpdatedAt] DESC;
END
