/*
    UserDelete
    Deletes an account. Its friendships, and the likes and comments it left on others' reviews and lists, are removed first (they can't cascade);
    its library and activity go with it through the cascading foreign keys.
*/
CREATE PROCEDURE [dbo].[UserDelete]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    BEGIN TRANSACTION;

    DELETE FROM [dbo].[Friendship]
    WHERE [UserID] = @UserID OR [FriendUserID] = @UserID;

    -- Likes and comments the account left on other accounts' reviews and lists (its own reviews and lists cascade).
    DELETE FROM [dbo].[ReviewLike] WHERE [UserID] = @UserID;
    DELETE FROM [dbo].[ReviewComment] WHERE [UserID] = @UserID;
    DELETE FROM [dbo].[GameListLike] WHERE [UserID] = @UserID;

    DELETE FROM [dbo].[User]
    WHERE [UserID] = @UserID;

    COMMIT TRANSACTION;
END
