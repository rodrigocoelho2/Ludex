/*
    UserDelete
    Deletes an account. Its friendships are removed first (each row points at two accounts, so they can't cascade);
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

    DELETE FROM [dbo].[User]
    WHERE [UserID] = @UserID;

    COMMIT TRANSACTION;
END
