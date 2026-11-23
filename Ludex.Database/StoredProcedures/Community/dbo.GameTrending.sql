/*
    GameTrending
    The games most accounts did something with in the last @Days days (added, started, finished, rated, reviewed, wrote
    down a session), with their average rating on this Ludex and how many accounts have them.
*/
CREATE PROCEDURE [dbo].[GameTrending]
    @Days INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @Since DATETIME2 (3) = DATEADD(DAY, -@Days, SYSUTCDATETIME());

    WITH [Touched] AS
    (
        SELECT a.[GameID], a.[UserID] FROM [dbo].[Activity] a WHERE a.[At] >= @Since
        UNION
        SELECT s.[GameID], s.[UserID] FROM [dbo].[PlaySession] s WHERE s.[Day] >= CAST(@Since AS DATE)
    ),
    [Counted] AS
    (
        SELECT t.[GameID], COUNT(DISTINCT t.[UserID]) AS [Active]
        FROM [Touched] t
        GROUP BY t.[GameID]
    )
    SELECT TOP (@Take)
           g.[GameID], g.[Name], g.[Slug], g.[CoverImageID], g.[HeroImageID], g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt], g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion],
           c.[Active],
           o.[Owners], o.[AverageRating]
    FROM [Counted] c
    INNER JOIN [dbo].[Game] g ON g.[GameID] = c.[GameID]
    OUTER APPLY (SELECT COUNT(DISTINCT e.[UserID]) AS [Owners], AVG(CAST(e.[Rating] AS DECIMAL (5, 2))) AS [AverageRating]
                 FROM [dbo].[LibraryEntry] e WHERE e.[GameID] = c.[GameID]) o
    ORDER BY c.[Active] DESC, o.[Owners] DESC, g.[Name];
END
