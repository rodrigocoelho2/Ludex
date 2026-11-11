/*
    GameCoverWall
    Covers for the wall behind the sign-in form: the games that are in most libraries. Only names and covers, never who
    has them (the sign-in page is public).
*/
CREATE PROCEDURE [dbo].[GameCoverWall]
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT TOP (@Take)
           g.[GameID], g.[Name], g.[Slug], g.[Summary], g.[CoverImageID], g.[HeroImageID], g.[Screenshots],
           g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt]
    FROM [dbo].[Game] g
    OUTER APPLY (SELECT COUNT(*) AS [Owners] FROM [dbo].[LibraryEntry] e WHERE e.[GameID] = g.[GameID]) o
    WHERE g.[CoverImageID] IS NOT NULL
    ORDER BY o.[Owners] DESC, g.[FetchedAt] DESC;
END
