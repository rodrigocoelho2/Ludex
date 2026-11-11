/*
    FriendRequest
    Asks another account to be friends. When that account had already asked, both become friends at once.
    Returns the state afterwards: 1 friends, 2 request sent (0 when either account doesn't exist).
*/
CREATE PROCEDURE [dbo].[FriendRequest]
    @UserUniqueID   UNIQUEIDENTIFIER,
    @FriendUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    DECLARE @FriendID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @FriendUniqueID);
    DECLARE @State TINYINT = 0;

    IF @UserID IS NULL OR @FriendID IS NULL OR @UserID = @FriendID
    BEGIN
        SELECT @State AS [State];
        RETURN;
    END

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [dbo].[Friendship] WITH (UPDLOCK, HOLDLOCK) WHERE [UserID] = @UserID AND [FriendUserID] = @FriendID)
    BEGIN
        SELECT @State = CASE WHEN [AcceptedAt] IS NULL THEN 2 ELSE 1 END
        FROM [dbo].[Friendship]
        WHERE [UserID] = @UserID AND [FriendUserID] = @FriendID;
    END
    ELSE IF EXISTS (SELECT 1 FROM [dbo].[Friendship] WITH (UPDLOCK, HOLDLOCK) WHERE [UserID] = @FriendID AND [FriendUserID] = @UserID)
    BEGIN
        UPDATE [dbo].[Friendship]
        SET [AcceptedAt] = COALESCE([AcceptedAt], SYSUTCDATETIME())
        WHERE [UserID] = @FriendID AND [FriendUserID] = @UserID;

        INSERT INTO [dbo].[Friendship] ([UserID], [FriendUserID], [AcceptedAt])
        VALUES (@UserID, @FriendID, SYSUTCDATETIME());

        SET @State = 1;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[Friendship] ([UserID], [FriendUserID])
        VALUES (@UserID, @FriendID);

        SET @State = 2;
    END

    COMMIT TRANSACTION;

    SELECT @State AS [State];
END
