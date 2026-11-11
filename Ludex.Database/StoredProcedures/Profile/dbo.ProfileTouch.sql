/*
    ProfileTouch
    Marks the account as seen now (the Website calls it while someone is using Ludex).
*/
CREATE PROCEDURE [dbo].[ProfileTouch]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    MERGE [dbo].[Profile] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [LastSeenAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [LastSeenAt]) VALUES (@UserID, SYSUTCDATETIME());
END
