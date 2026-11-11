/*
    NotificationUnread
    How many notifications an account hasn't seen.
*/
CREATE PROCEDURE [dbo].[NotificationUnread]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT COUNT(*)
    FROM [dbo].[Notification] n
    INNER JOIN [dbo].[User] u ON u.[UserID] = n.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID AND n.[ReadAt] IS NULL;
END
