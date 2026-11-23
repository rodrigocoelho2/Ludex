/*
    GameSleepers
    Sleeper hits: games rated well on this Ludex (average @MinRating or more, from at least @MinRatings accounts) that
    few accounts have (@MaxOwners at most), best rated first.
*/
CREATE PROCEDURE [dbo].[GameSleepers]
    @MinRating  DECIMAL (4, 2),
    @MinRatings INT,
    @MaxOwners  INT,
    @Take       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    WITH [Rated] AS
    (
        SELECT e.[GameID],
               COUNT(DISTINCT e.[UserID]) AS [Owners],
               COUNT(DISTINCT CASE WHEN e.[Rating] IS NOT NULL THEN e.[UserID] END) AS [Ratings],
               AVG(CAST(e.[Rating] AS DECIMAL (5, 2))) AS [AverageRating]
        FROM [dbo].[LibraryEntry] e
        GROUP BY e.[GameID]
    )
    SELECT TOP (@Take)
           g.[GameID], g.[Name], g.[Slug], g.[CoverImageID], g.[HeroImageID], g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt], g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion],
           r.[Owners], r.[Ratings], r.[AverageRating]
    FROM [Rated] r
    INNER JOIN [dbo].[Game] g ON g.[GameID] = r.[GameID]
    WHERE r.[Ratings] >= @MinRatings AND r.[AverageRating] >= @MinRating AND r.[Owners] <= @MaxOwners
    ORDER BY r.[AverageRating] DESC, r.[Owners] ASC, g.[Name];
END
