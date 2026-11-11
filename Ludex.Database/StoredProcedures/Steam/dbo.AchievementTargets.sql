/*
    AchievementTargets
    The Steam games of an account whose achievement counts need reading: never read, played in the last two weeks,
    or read more than a week ago.
*/
CREATE PROCEDURE [dbo].[AchievementTargets]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT e.[GameID], e.[SteamAppID]
    FROM [dbo].[LibraryEntry] e
    WHERE e.[UserID] = @UserID
      AND e.[SteamAppID] IS NOT NULL
      AND (e.[AchievementsAt] IS NULL OR ISNULL(e.[RecentMinutes], 0) > 0 OR e.[AchievementsAt] < DATEADD(DAY, -7, SYSUTCDATETIME()))
    ORDER BY CASE WHEN e.[AchievementsAt] IS NULL THEN 0 ELSE 1 END, e.[UpdatedAt] DESC;
END
