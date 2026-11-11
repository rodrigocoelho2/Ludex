/*
    LibraryDelete
    Takes a game out of an account's library, with its activity lines (the game stays in dbo.Game).
*/
CREATE PROCEDURE [dbo].[LibraryDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    BEGIN TRANSACTION;

    DELETE FROM [dbo].[Activity]
    WHERE [UserID] = @UserID AND [GameID] = @GameID;

    DELETE FROM [dbo].[LibraryEntry]
    WHERE [UserID] = @UserID AND [GameID] = @GameID;

    COMMIT TRANSACTION;
END
