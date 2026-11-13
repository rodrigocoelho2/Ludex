/*
    LibraryClear
    Empties an account's library, to import everything again: every entry, the activity lines, the hours history,
    what's in its collections (the collections themselves stay), the favourite game, and what the Steam and PlayStation
    imports remembered (so their next import offers every game again, and their syncs add nothing until then). The
    account, its friends, its profile and its Steam and PlayStation connections stay. Returns how many entries went.
*/
CREATE PROCEDURE [dbo].[LibraryClear]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
    BEGIN
        SELECT 0 AS [Removed];
        RETURN;
    END

    DECLARE @Removed INT;

    BEGIN TRANSACTION;

    DELETE cg
    FROM [dbo].[CollectionGame] cg
    INNER JOIN [dbo].[Collection] c ON c.[CollectionID] = cg.[CollectionID]
    WHERE c.[UserID] = @UserID;

    DELETE FROM [dbo].[Activity] WHERE [UserID] = @UserID;
    DELETE FROM [dbo].[PlaytimeDay] WHERE [UserID] = @UserID;
    DELETE FROM [dbo].[SteamOwned] WHERE [UserID] = @UserID;
    DELETE FROM [dbo].[PsnTitle] WHERE [UserID] = @UserID;
    UPDATE [dbo].[PsnAccount] SET [SyncedAt] = NULL WHERE [UserID] = @UserID;
    UPDATE [dbo].[Profile] SET [FavoriteGameID] = NULL WHERE [UserID] = @UserID;

    DELETE FROM [dbo].[LibraryEntry] WHERE [UserID] = @UserID;
    SET @Removed = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT @Removed AS [Removed];
END
