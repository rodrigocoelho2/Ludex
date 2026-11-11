/*
    GameGet
    One game by its id (no row when nobody has added or opened it yet).
*/
CREATE PROCEDURE [dbo].[GameGet]
    @GameID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT g.[GameID], g.[Name], g.[Slug], g.[Summary], g.[CoverImageID], g.[HeroImageID], g.[Screenshots],
           g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt]
    FROM [dbo].[Game] g
    WHERE g.[GameID] = @GameID;
END
