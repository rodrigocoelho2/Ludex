/*
    JournalList
    An account's journal from @From to @To (inclusive), newest day first: the sessions it wrote down (Source 1), and the
    time the Steam and PlayStation syncs saw (Source 2: how much a game's hours in dbo.PlaytimeDay went up since its
    snapshot before; a game's first snapshot says nothing about that day, so it isn't one). @GameID: one game only.
*/
CREATE PROCEDURE [dbo].[JournalList]
    @UserUniqueID UNIQUEIDENTIFIER,
    @From         DATE,
    @To           DATE,
    @GameID       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    WITH [Snapshots] AS
    (
        SELECT d.[GameID], d.[Day], d.[Hours], LAG(d.[Hours]) OVER (PARTITION BY d.[GameID] ORDER BY d.[Day]) AS [Before]
        FROM [dbo].[PlaytimeDay] d
        WHERE d.[UserID] = @UserID AND (@GameID IS NULL OR d.[GameID] = @GameID) AND d.[Day] <= @To
    ),
    [Lines] AS
    (
        SELECT s.[SessionID], CAST(1 AS TINYINT) AS [Source], s.[GameID], s.[Day], s.[Minutes], s.[Platform], s.[Note], s.[CreatedAt]
        FROM [dbo].[PlaySession] s
        WHERE s.[UserID] = @UserID AND s.[Day] BETWEEN @From AND @To AND (@GameID IS NULL OR s.[GameID] = @GameID)
        UNION ALL
        SELECT CAST(NULL AS BIGINT), CAST(2 AS TINYINT), n.[GameID], n.[Day], CAST(ROUND((n.[Hours] - n.[Before]) * 60, 0) AS INT),
               CAST(NULL AS NVARCHAR (60)), CAST(NULL AS NVARCHAR (500)), CAST(n.[Day] AS DATETIME2 (3))
        FROM [Snapshots] n
        WHERE n.[Day] >= @From AND n.[Before] IS NOT NULL AND n.[Hours] > n.[Before]
    )
    SELECT l.[SessionID], l.[Source], l.[Day], l.[Minutes], l.[Platform], l.[Note], l.[CreatedAt],
           g.[GameID], g.[Name], g.[Slug], g.[CoverImageID], g.[HeroImageID], g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt], g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion]
    FROM [Lines] l
    INNER JOIN [dbo].[Game] g ON g.[GameID] = l.[GameID]
    ORDER BY l.[Day] DESC, l.[CreatedAt] DESC;
END
