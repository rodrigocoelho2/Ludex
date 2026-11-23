/*
    ReviewDelete
    Deletes an account's own review of a game (its likes and comments go with it).
*/
CREATE PROCEDURE [dbo].[ReviewDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    DELETE FROM [dbo].[Review]
    WHERE [UserID] = @UserID AND [GameID] = @GameID;
END
