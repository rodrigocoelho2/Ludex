/*
    FriendAccept
    Accepts the request another account sent: both become friends. Nothing happens when there is no such request.
*/
CREATE PROCEDURE [dbo].[FriendAccept]
    @UserUniqueID   UNIQUEIDENTIFIER,
    @FriendUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    DECLARE @FriendID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @FriendUniqueID);

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [dbo].[Friendship] WITH (UPDLOCK, HOLDLOCK) WHERE [UserID] = @FriendID AND [FriendUserID] = @UserID)
    BEGIN
        UPDATE [dbo].[Friendship]
        SET [AcceptedAt] = COALESCE([AcceptedAt], SYSUTCDATETIME())
        WHERE [UserID] = @FriendID AND [FriendUserID] = @UserID;

        IF EXISTS (SELECT 1 FROM [dbo].[Friendship] WITH (UPDLOCK, HOLDLOCK) WHERE [UserID] = @UserID AND [FriendUserID] = @FriendID)
            UPDATE [dbo].[Friendship]
            SET [AcceptedAt] = COALESCE([AcceptedAt], SYSUTCDATETIME())
            WHERE [UserID] = @UserID AND [FriendUserID] = @FriendID;
        ELSE
            INSERT INTO [dbo].[Friendship] ([UserID], [FriendUserID], [AcceptedAt])
            VALUES (@UserID, @FriendID, SYSUTCDATETIME());
    END

    COMMIT TRANSACTION;
END
