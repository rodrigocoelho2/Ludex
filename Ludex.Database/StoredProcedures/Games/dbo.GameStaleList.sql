/*
    GameStaleList
    Games in someone's library whose data is older than the WebApi's current version (new fields to fetch), IGDB
    games only, at most @Take.
*/
CREATE PROCEDURE [dbo].[GameStaleList]
    @DataVersion TINYINT,
    @Take        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT TOP (@Take) g.[GameID]
    FROM [dbo].[Game] g
    WHERE g.[DataVersion] < @DataVersion
      AND g.[GameID] < 1000000000
      AND EXISTS (SELECT 1 FROM [dbo].[LibraryEntry] e WHERE e.[GameID] = g.[GameID])
    ORDER BY g.[FetchedAt];
END
