/*
    GameWishlistCounts
    How many accounts have each game on their wishlist (the "waiting" of Coming soon). @GameIDs is a JSON array of ids.
*/
CREATE PROCEDURE [dbo].[GameWishlistCounts]
    @GameIDs NVARCHAR (MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT e.[GameID], COUNT(DISTINCT e.[UserID]) AS [Count]
    FROM OPENJSON(@GameIDs) j
    INNER JOIN [dbo].[LibraryEntry] e ON e.[GameID] = TRY_CAST(j.[value] AS INT) AND e.[Status] = 3
    GROUP BY e.[GameID];
END
