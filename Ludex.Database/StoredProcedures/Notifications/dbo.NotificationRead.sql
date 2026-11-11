/*
    NotificationRead
    Marks every notification of an account as seen, and forgets the ones older than 90 days.
*/
CREATE PROCEDURE [dbo].[NotificationRead]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    UPDATE [dbo].[Notification] SET [ReadAt] = SYSUTCDATETIME() WHERE [UserID] = @UserID AND [ReadAt] IS NULL;
    DELETE FROM [dbo].[Notification] WHERE [UserID] = @UserID AND [CreatedAt] < DATEADD(DAY, -90, SYSUTCDATETIME());
END
