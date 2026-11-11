/*
    FriendRemove
    Ends a friendship, or cancels / declines a request, in both directions.
*/
CREATE PROCEDURE [dbo].[FriendRemove]
    @UserUniqueID   UNIQUEIDENTIFIER,
    @FriendUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    DECLARE @FriendID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @FriendUniqueID);

    DELETE FROM [dbo].[Friendship]
    WHERE ([UserID] = @UserID AND [FriendUserID] = @FriendID)
       OR ([UserID] = @FriendID AND [FriendUserID] = @UserID);
END
