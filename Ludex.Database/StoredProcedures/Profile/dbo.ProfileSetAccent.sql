/*
    ProfileSetAccent
    Sets (or clears, with NULL) only the colour theme.
*/
CREATE PROCEDURE [dbo].[ProfileSetAccent]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Accent       VARCHAR (20)
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
        UPDATE SET [Accent] = @Accent, [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [Accent]) VALUES (@UserID, @Accent);
END
