/*
    SteamOwnedRemember
    Remembers Steam apps as already seen (@AppIDs: a JSON array of numbers), and when the account was synced.
*/
CREATE PROCEDURE [dbo].[SteamOwnedRemember]
    @UserUniqueID UNIQUEIDENTIFIER,
    @AppIDs       NVARCHAR (MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    INSERT INTO [dbo].[SteamOwned] ([UserID], [AppID])
    SELECT DISTINCT @UserID, ids.[AppID]
    FROM OPENJSON(@AppIDs) WITH ([AppID] INT '$') ids
    WHERE ids.[AppID] IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [dbo].[SteamOwned] o WHERE o.[UserID] = @UserID AND o.[AppID] = ids.[AppID]);

    UPDATE [dbo].[Profile] SET [SteamSyncedAt] = SYSUTCDATETIME() WHERE [UserID] = @UserID;
END
