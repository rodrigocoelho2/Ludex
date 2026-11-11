/*
    ProfileSave
    Saves the bio, location and favourite game (the favourite must be a game in dbo.Game, or NULL).
*/
CREATE PROCEDURE [dbo].[ProfileSave]
    @UserUniqueID   UNIQUEIDENTIFIER,
    @Bio            NVARCHAR (300),
    @Location       NVARCHAR (80),
    @FavoriteGameID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    IF @FavoriteGameID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Game] WHERE [GameID] = @FavoriteGameID)
        SET @FavoriteGameID = NULL;

    MERGE [dbo].[Profile] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [Bio] = @Bio, [Location] = @Location, [FavoriteGameID] = @FavoriteGameID, [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [Bio], [Location], [FavoriteGameID]) VALUES (@UserID, @Bio, @Location, @FavoriteGameID);
END
