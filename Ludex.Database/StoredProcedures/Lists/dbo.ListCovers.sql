/*
    ListCovers
    The first @Take games of each list (@ListIDs: a JSON array of ids), for the stacked covers on list cards.
*/
CREATE PROCEDURE [dbo].[ListCovers]
    @ListIDs NVARCHAR (MAX),
    @Take    INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    WITH [Ranked] AS
    (
        SELECT i.[ListID], i.[GameID], i.[Position],
               ROW_NUMBER() OVER (PARTITION BY i.[ListID] ORDER BY i.[Position]) AS [N]
        FROM OPENJSON(@ListIDs) j
        INNER JOIN [dbo].[GameListItem] i ON i.[ListID] = TRY_CAST(j.[value] AS INT)
    )
    SELECT r.[ListID], r.[Position],
           g.[GameID], g.[Name], g.[CoverImageID], g.[FirstReleaseDate], g.[Developer], g.[Platforms], g.[FetchedAt]
    FROM [Ranked] r
    INNER JOIN [dbo].[Game] g ON g.[GameID] = r.[GameID]
    WHERE r.[N] <= @Take
    ORDER BY r.[ListID], r.[Position];
END
