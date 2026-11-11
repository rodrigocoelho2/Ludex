/*
    AchievementSave
    Keeps how many of a game's achievements an account has (0 of 0: the game has none).
*/
CREATE PROCEDURE [dbo].[AchievementSave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Unlocked     SMALLINT,
    @Total        SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    UPDATE [dbo].[LibraryEntry]
    SET [AchievementsUnlocked] = @Unlocked, [AchievementsTotal] = @Total, [AchievementsAt] = SYSUTCDATETIME()
    WHERE [UserID] = @UserID AND [GameID] = @GameID;
END
