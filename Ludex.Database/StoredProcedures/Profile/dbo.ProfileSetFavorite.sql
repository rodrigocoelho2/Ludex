/*
    ProfileSetFavorite
    Sets (or clears, with NULL) only the favourite game.
*/
CREATE PROCEDURE [dbo].[ProfileSetFavorite]
    @UserUniqueID   UNIQUEIDENTIFIER,
    @FavoriteGameID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    IF @FavoriteGameID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Game] WHERE [GameID] = @FavoriteGameID)
        RETURN;

    MERGE [dbo].[Profile] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [FavoriteGameID] = @FavoriteGameID, [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [FavoriteGameID]) VALUES (@UserID, @FavoriteGameID);
END
