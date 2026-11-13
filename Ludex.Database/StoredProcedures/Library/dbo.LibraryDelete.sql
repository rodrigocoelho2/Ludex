/*
    LibraryDelete
    Takes a game out of an account's library: one entry (@EntryID), or every entry of the game (NULL). The activity
    lines go when no entry of the game is left (the game stays in dbo.Game).
*/
CREATE PROCEDURE [dbo].[LibraryDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @EntryID      BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    BEGIN TRANSACTION;

    DELETE FROM [dbo].[LibraryEntry]
    WHERE [UserID] = @UserID AND [GameID] = @GameID AND (@EntryID IS NULL OR [EntryID] = @EntryID);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[LibraryEntry] WHERE [UserID] = @UserID AND [GameID] = @GameID)
        DELETE FROM [dbo].[Activity]
        WHERE [UserID] = @UserID AND [GameID] = @GameID;

    COMMIT TRANSACTION;
END
