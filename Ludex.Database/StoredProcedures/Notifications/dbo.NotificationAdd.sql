/*
    NotificationAdd
    Adds a notification for an account.
*/
CREATE PROCEDURE [dbo].[NotificationAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Kind         TINYINT,
    @Text         NVARCHAR (300),
    @Url          NVARCHAR (200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    INSERT INTO [dbo].[Notification] ([UserID], [GameID], [Kind], [Text], [Url]) VALUES (@UserID, @GameID, @Kind, @Text, @Url);
END
