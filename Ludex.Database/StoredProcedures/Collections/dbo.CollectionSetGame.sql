/*
    CollectionSetGame
    Puts a game of the library in one of the account's collections (@In = 1) or takes it out (@In = 0).
*/
CREATE PROCEDURE [dbo].[CollectionSetGame]
    @UserUniqueID UNIQUEIDENTIFIER,
    @CollectionID INT,
    @GameID       INT,
    @In           BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Collection] WHERE [CollectionID] = @CollectionID AND [UserID] = @UserID)
        RETURN;

    IF @In = 1
    BEGIN
        IF EXISTS (SELECT 1 FROM [dbo].[LibraryEntry] WHERE [UserID] = @UserID AND [GameID] = @GameID)
           AND NOT EXISTS (SELECT 1 FROM [dbo].[CollectionGame] WHERE [CollectionID] = @CollectionID AND [GameID] = @GameID)
            INSERT INTO [dbo].[CollectionGame] ([CollectionID], [GameID]) VALUES (@CollectionID, @GameID);
    END
    ELSE
        DELETE FROM [dbo].[CollectionGame] WHERE [CollectionID] = @CollectionID AND [GameID] = @GameID;
END
