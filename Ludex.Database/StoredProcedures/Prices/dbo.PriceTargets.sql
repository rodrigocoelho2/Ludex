/*
    PriceTargets
    The games on anyone's wishlist whose price wasn't checked in the last @Hours hours, with what is known to look them
    up (IsThereAnyDeal's id, a Steam app).
*/
CREATE PROCEDURE [dbo].[PriceTargets]
    @Hours INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT g.[GameID], g.[Name], gp.[ItadID], MAX(e.[SteamAppID]) AS [SteamAppID]
    FROM [dbo].[LibraryEntry] e
    INNER JOIN [dbo].[Game] g ON g.[GameID] = e.[GameID]
    LEFT JOIN [dbo].[GamePrice] gp ON gp.[GameID] = g.[GameID]
    WHERE e.[Status] = 3
      AND (gp.[GameID] IS NULL OR gp.[CheckedAt] < DATEADD(HOUR, -@Hours, SYSUTCDATETIME()))
    GROUP BY g.[GameID], g.[Name], gp.[ItadID];
END
