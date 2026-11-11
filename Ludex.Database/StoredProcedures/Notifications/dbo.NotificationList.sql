/*
    NotificationList
    An account's latest notifications, newest first, and how many are unread (second result).
*/
CREATE PROCEDURE [dbo].[NotificationList]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Take         INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT TOP (@Take) n.[NotificationID], n.[GameID], n.[Kind], n.[Text], n.[Url], n.[CreatedAt], n.[ReadAt]
    FROM [dbo].[Notification] n
    WHERE n.[UserID] = @UserID
    ORDER BY n.[CreatedAt] DESC, n.[NotificationID] DESC;
END
