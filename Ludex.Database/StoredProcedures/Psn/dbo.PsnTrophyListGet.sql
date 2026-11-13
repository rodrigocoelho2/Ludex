/*
    PsnTrophyListGet
    The trophy list of a game an account played on PlayStation ("NPWR12345_00:trophy2"), for the game page; no row
    when none is known (not a PlayStation game, or not synced since trophy lists were kept).
*/
CREATE PROCEDURE [dbo].[PsnTrophyListGet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT TOP (1) t.[TrophyList]
    FROM [dbo].[PsnTitle] t
    INNER JOIN [dbo].[User] u ON u.[UserID] = t.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID AND t.[GameID] = @GameID AND t.[TrophyList] IS NOT NULL
    ORDER BY t.[FirstSeenAt];
END
