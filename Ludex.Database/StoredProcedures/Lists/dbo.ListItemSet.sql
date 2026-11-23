/*
    ListItemSet
    Puts a game at the end of one of the account's lists (@In 1; a game already there keeps its place and gets the
    note), or takes it out (@In 0) and closes the gap. The game must already be in dbo.Game. Returns 1 when the list is
    the account's.
*/
CREATE PROCEDURE [dbo].[ListItemSet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ListID       INT,
    @GameID       INT,
    @In           BIT,
    @Note         NVARCHAR (300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[GameList] WHERE [ListID] = @ListID AND [UserID] = @UserID)
    BEGIN
        SELECT CAST(0 AS BIT) AS [Done];
        RETURN;
    END

    BEGIN TRANSACTION;

    DECLARE @Position INT = (SELECT [Position] FROM [dbo].[GameListItem] WITH (UPDLOCK, HOLDLOCK) WHERE [ListID] = @ListID AND [GameID] = @GameID);

    IF @In = 1
    BEGIN
        IF @Position IS NULL
            INSERT INTO [dbo].[GameListItem] ([ListID], [GameID], [Position], [Note])
            SELECT @ListID, @GameID, COALESCE(MAX(i.[Position]), 0) + 1, @Note
            FROM [dbo].[GameListItem] i WITH (UPDLOCK, HOLDLOCK)
            WHERE i.[ListID] = @ListID;
        ELSE IF @Note IS NOT NULL
            UPDATE [dbo].[GameListItem] SET [Note] = @Note WHERE [ListID] = @ListID AND [GameID] = @GameID;
    END
    ELSE IF @Position IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[GameListItem] WHERE [ListID] = @ListID AND [GameID] = @GameID;
        UPDATE [dbo].[GameListItem] SET [Position] = [Position] - 1 WHERE [ListID] = @ListID AND [Position] > @Position;
    END

    UPDATE [dbo].[GameList] SET [UpdatedAt] = SYSUTCDATETIME() WHERE [ListID] = @ListID;

    COMMIT TRANSACTION;

    SELECT CAST(1 AS BIT) AS [Done];
END
