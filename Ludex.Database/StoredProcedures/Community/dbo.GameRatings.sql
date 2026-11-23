/*
    GameRatings
    How everyone on this Ludex rated a game: how many accounts gave each rating (1–10, an account's first copy), and
    how many accounts have it in each status. One row per (Kind, Value): Kind 1 = rating, Kind 2 = status.
*/
CREATE PROCEDURE [dbo].[GameRatings]
    @GameID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    WITH [Main] AS
    (
        SELECT e.[UserID], e.[Status], e.[Rating],
               ROW_NUMBER() OVER (PARTITION BY e.[UserID] ORDER BY CASE WHEN e.[Rating] IS NULL THEN 1 ELSE 0 END, e.[EntryID]) AS [N]
        FROM [dbo].[LibraryEntry] e
        WHERE e.[GameID] = @GameID
    )
    SELECT CAST(1 AS TINYINT) AS [Kind], m.[Rating] AS [Value], COUNT(*) AS [Count]
    FROM [Main] m
    WHERE m.[N] = 1 AND m.[Rating] IS NOT NULL
    GROUP BY m.[Rating]
    UNION ALL
    SELECT CAST(2 AS TINYINT), m.[Status], COUNT(*)
    FROM [Main] m
    WHERE m.[N] = 1
    GROUP BY m.[Status];
END
